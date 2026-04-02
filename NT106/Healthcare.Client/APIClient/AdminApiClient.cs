using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Healthcare.Client.APIClient
{
    public static class AdminApiClient
    {
        private static readonly string ServerBaseUrl = "https://localhost:5000/api/admin";

        public static async Task<bool> DeleteUserViaServerAsync(string userId)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.DeleteAsync($"{ServerBaseUrl}/users/{userId}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gọi Server: {ex.Message}");
                return false;
            }
        }
    }
}