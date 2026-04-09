using System.Threading.Tasks;

namespace Healthcare.Client.APIClient
{
    // Kế thừa BaseHttpClient
    public class PaymentApiClient : BaseHttpClient
    {
        public PaymentApiClient() : base("http://localhost:5246/api/")
        {
        }

        public async Task<string> GetVNPayUrlAsync(string appointmentId, double amount)
        {
            try
            {
                var requestData = new { AppointmentId = appointmentId, Amount = amount };

                // Sử dụng hàm PostAsync có sẵn từ BaseHttpClient
                var result = await PostAsync<object, PaymentUrlResponse>("payment/create-url", requestData);

                return result?.PaymentUrl;
            }
            catch
            {
                return null;
            }
        }

        private class PaymentUrlResponse
        {
            public string PaymentUrl { get; set; }
        }
    }
}