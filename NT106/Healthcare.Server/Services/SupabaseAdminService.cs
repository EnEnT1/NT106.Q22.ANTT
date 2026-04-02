using Healthcare.Server.SupabaseIntegration;
using Microsoft.Extensions.Configuration;
using Postgrest.Attributes;
using Postgrest.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Healthcare.Server.Services
{
    public class SupabaseAdminService
    {
        private readonly SupabaseAdminHelper _adminHelper;
        private readonly string _url;
        private readonly string _key;

        // Tiêm cả Helper và Configuration vào
        public SupabaseAdminService(SupabaseAdminHelper adminHelper, IConfiguration config)
        {
            _adminHelper = adminHelper;
            _url = config["SUPABASE_URL"];
            _key = config["SUPABASE_SERVICE_ROLE_KEY"];
        }

        public async Task<bool> DeleteUserCompletelyAsync(string id)
        {
            try
            {
                // BƯỚC 1: XÓA AUTH BẰNG HTTP (Vì SDK C# của bạn thiếu hàm Admin)
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _key);
                http.DefaultRequestHeaders.Add("apikey", _key);

                var res = await http.DeleteAsync($"{_url}/auth/v1/admin/users/{id}");

                if (res.IsSuccessStatusCode)
                {
                    Console.WriteLine("[SUCCESS] Da xoa user thanh cong!");
                    return true;
                }

                Console.WriteLine($"[FAILED] Supabase tu choi xoa: {await res.Content.ReadAsStringAsync()}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateUserCompletelyAsync(string email, string pass, string name, string role)
        {
            try
            {
                Console.WriteLine($"\n[CREATE] Đang tạo tài khoản: {email}");

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _key);
                http.DefaultRequestHeaders.Add("apikey", _key);
                http.DefaultRequestHeaders.Add("Prefer", "return=representation");

                // 1. TẠO AUTH USER (Bước này của bạn đã chạy thành công)
                var authPayload = new { email = email, password = pass, email_confirm = true };
                var authRes = await http.PostAsync($"{_url}/auth/v1/admin/users",
                    new StringContent(JsonSerializer.Serialize(authPayload), Encoding.UTF8, "application/json"));

                if (!authRes.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[FAILED] Loi tao Auth: {await authRes.Content.ReadAsStringAsync()}");
                    return false;
                }

                string uid = JsonDocument.Parse(await authRes.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString();
                Console.WriteLine($"[SUCCESS] Đa tao Auth UID: {uid}");

                await Task.Delay(1000); // Chờ Trigger Database đồng bộ 1 giây

                // 2. THÊM MỚI VÀO BẢNG USERS (Vì Supabase chưa có Trigger tự động)
                var userPayload = new Dictionary<string, string>
                {
                    {    "id", uid }, 
                    { "full_name", name },
                    { "role", role },
                    { "email", email }
                }  ;

                
                var userRes = await http.PostAsync($"{_url}/rest/v1/users",
                    new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json"));

                if (!userRes.IsSuccessStatusCode)
                {
                    // Nếu lỗi trùng lặp (23505) tức là bạn đã có Trigger ngầm, bỏ qua lỗi này
                    var err = await userRes.Content.ReadAsStringAsync();
                    if (!err.Contains("23505"))
                    {
                        Console.WriteLine($"[FAILED] Loi bang users: {err}");
                        return false;
                    }
                }
                // 3. TẠO DÒNG PROFILE TƯƠNG ỨNG
                string table = role == "Doctor" ? "doctor_profiles" : "patient_profiles";
                string col = role == "Doctor" ? "doctor_id" : "patient_id";

                var profilePayload = new Dictionary<string, string>
        {
            { col, uid }
        };


                // Nếu là Bác sĩ, chèn thêm cột specialty để Database không báo lỗi NOT NULL
                if (role == "Doctor")
                {
                    profilePayload.Add("specialty", "Đa khoa");
                }
                var profileRes = await http.PostAsync($"{_url}/rest/v1/{table}",
                    new StringContent(JsonSerializer.Serialize(profilePayload), Encoding.UTF8, "application/json"));

                if (profileRes.IsSuccessStatusCode)
                {
                    Console.WriteLine("[SUCCESS] Tao tai khoan thanh cong");
                    return true;
                }

                Console.WriteLine($"[FAILED] Loi tao profile: {await profileRes.Content.ReadAsStringAsync()}");
                return false;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Code Exception: {ex.Message}");
                return false;
            }
        }
    }

    
}