using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Healthcare.Server.Services
{
    public class SupabaseAdminService
    {
        private readonly string _supabaseUrl;
        private readonly string _serviceRoleKey;

        public SupabaseAdminService(IConfiguration configuration)
        {
            // Đọc cấu hình từ file .env
            _supabaseUrl = configuration["SUPABASE_URL"];
            _serviceRoleKey = configuration["SUPABASE_SERVICE_ROLE_KEY"];

            if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_serviceRoleKey))
            {
                throw new Exception("Thiếu cấu hình Supabase trong file .env!");
            }
        }

        public async Task<bool> DeleteUserCompletelyAsync(string userId)
        {
            try
            {
                // Dùng HttpClient gọi trực tiếp REST API của Supabase
                using var httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
                httpClient.DefaultRequestHeaders.Add("apikey", _serviceRoleKey);

                // Gửi lệnh DELETE thẳng lên Server của Supabase
                var response = await httpClient.DeleteAsync($"{_supabaseUrl}/auth/v1/admin/users/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Supabase từ chối xóa: {errorDetails}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi mạng hoặc hệ thống khi xóa user {userId}: {ex.Message}");
                return false;
            }
        }
    }
}