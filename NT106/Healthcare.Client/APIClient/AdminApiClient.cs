using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Healthcare.Client.APIClient
{
    public static class AdminApiClient
    {
        // Lưu ý: Đảm bảo URL này khớp với URL mà Server đang lắng nghe
        private static readonly string ServerBaseUrl = "http://localhost:5246/api/admin";

        public static async Task<bool> DeleteUserViaServerAsync(string userId)
        {
            try
            {
                using var client = new HttpClient();
                var res = await client.DeleteAsync($"{ServerBaseUrl}/users/{userId}");

                if (!res.IsSuccessStatusCode)
                {
                    var error = await res.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Server báo lỗi: {error}");
                }
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KHÔNG GỌI ĐƯỢC SERVER: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> CreateUserViaServerAsync(string email, string password, string fullName, string role)
        {
            try
            {
                var payload = new { Email = email, Password = password, FullName = fullName, Role = role };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                var response = await client.PostAsync($"{ServerBaseUrl}/users", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gọi Server (Thêm): {ex.Message}");
                return false;
            }
        }
    }
}