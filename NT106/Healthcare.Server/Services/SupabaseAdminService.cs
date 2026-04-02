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
                    Console.WriteLine("[SUCCESS] Đã xóa User thành công!");
                    return true;
                }

                Console.WriteLine($"[FAILED] Supabase từ chối xóa: {await res.Content.ReadAsStringAsync()}");
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
                // BƯỚC 1: TẠO AUTH BẰNG HTTP
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _key);
                http.DefaultRequestHeaders.Add("apikey", _key);

                var authPayload = new { email = email, password = pass, email_confirm = true };
                var authRes = await http.PostAsync($"{_url}/auth/v1/admin/users",
                    new StringContent(JsonSerializer.Serialize(authPayload), Encoding.UTF8, "application/json"));

                if (!authRes.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[FAILED] Lỗi tạo Auth: {await authRes.Content.ReadAsStringAsync()}");
                    return false;
                }

                string uid = JsonDocument.Parse(await authRes.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString();
                await Task.Delay(1000); // Chờ Trigger Database đồng bộ

                // BƯỚC 2: DÙNG SUPABASE_ADMIN_HELPER (SDK) ĐỂ CẬP NHẬT DATABASE

                // Update bảng users
                var userUpdate = new AppUser { Id = uid, FullName = name, Role = role };
                await _adminHelper.AdminClient.From<AppUser>().Update(userUpdate);

                // Insert Profile
                if (role == "Doctor")
                {
                    var docProfile = new DoctorProfileModel { DoctorId = uid };
                    await _adminHelper.AdminClient.From<DoctorProfileModel>().Insert(docProfile);
                }
                else
                {
                    var patProfile = new PatientProfileModel { PatientId = uid };
                    await _adminHelper.AdminClient.From<PatientProfileModel>().Insert(patProfile);
                }

                Console.WriteLine("[SUCCESS] Tạo tài khoản và Profile qua SDK thành công!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Code Exception: {ex.Message}");
                return false;
            }
        }
    }

    // =====================================================================
    // KHAI BÁO CÁC MODEL CHO SDK (NẰM NGAY TRONG FILE NÀY CHO TIỆN)
    // =====================================================================

    [Table("users")]
    public class AppUser : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("role")]
        public string Role { get; set; }
    }

    [Table("doctor_profiles")]
    public class DoctorProfileModel : BaseModel
    {
        [PrimaryKey("doctor_id", false)]
        public string DoctorId { get; set; }
    }

    [Table("patient_profiles")]
    public class PatientProfileModel : BaseModel
    {
        [PrimaryKey("patient_id", false)]
        public string PatientId { get; set; }
    }
}