using System;
using System.Threading.Tasks;

namespace Healthcare.Client.APIClient
{
    // Kế thừa BaseHttpClient 
    public class AdminApiClient : BaseHttpClient
    {
        public AdminApiClient() : base("http://localhost:5246/api/")
        {
        }

        public async Task<bool> DeleteUserViaServerAsync(string userId)
        {
            try
            {
                return await DeleteAsync($"admin/users/{userId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KHÔNG GỌI ĐƯỢC SERVER: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateUserViaServerAsync(string email, string password, string fullName, string role)
        {
            try
            {
                var payload = new { Email = email, Password = password, FullName = fullName, Role = role };
                return await PostAsync("admin/users", payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gọi Server (Thêm): {ex.Message}");
                return false;
            }
        }
    }
}