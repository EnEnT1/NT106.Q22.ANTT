using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Healthcare.Client.APIClient
{
    public class PaymentClient
    {
        private readonly HttpClient _httpClient;

        public PaymentClient()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:5246/api/");
        }
        public async Task<string> GetVNPayUrlAsync(string appointmentId, double amount)
        {
            try
            {
                var requestData = new { AppointmentId = appointmentId, Amount = amount };
                var response = await _httpClient.PostAsJsonAsync("payment/create-url", requestData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PaymentUrlResponse>();
                    return result?.PaymentUrl;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gọi API Thanh toán: {ex.Message}");
                return null;
            }
        }

        private class PaymentUrlResponse
        {
            public string PaymentUrl { get; set; }
        }
    }
}