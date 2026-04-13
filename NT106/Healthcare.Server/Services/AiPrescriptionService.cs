using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Healthcare.Server.Services
{
    public class AiPrescriptionService
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly HttpClient _httpClient;

        public AiPrescriptionService(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = _configuration["OpenAI:ApiKey"]
                      ?? throw new Exception("Thiếu OpenAI:ApiKey trong appsettings.json");

            _model = _configuration["OpenAI:Model"]
                     ?? "gpt-4o-mini";

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<List<string>> AnalyzeImageAsync(Stream imageStream)
        {
            if (imageStream == null || imageStream.Length == 0)
                throw new Exception("Ảnh không hợp lệ.");

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                await imageStream.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            string base64Image = Convert.ToBase64String(imageBytes);

            string prompt = @"
Bạn là AI hỗ trợ đọc đơn thuốc.
Hãy trích xuất danh sách thuốc từ ảnh đơn thuốc.
Chỉ trả về JSON đúng format:
{
  ""medicines"": [
    ""Tên thuốc - liều dùng"",
    ""Tên thuốc - liều dùng""
  ]
}
Nếu không đọc được thì trả:
{
  ""medicines"": []
}";

            var requestBody = new
            {
                model = _model,
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = prompt
                            },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:image/png;base64,{base64Image}"
                                }
                            }
                        }
                    }
                },
                temperature = 0
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("chat/completions", content);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Lỗi OpenAI: " + responseText);

            using JsonDocument doc = JsonDocument.Parse(responseText);

            string modelReply = doc.RootElement
                                   .GetProperty("choices")[0]
                                   .GetProperty("message")
                                   .GetProperty("content")
                                   .GetString();

            if (string.IsNullOrWhiteSpace(modelReply))
                return new List<string>();

            try
            {
                using JsonDocument resultDoc = JsonDocument.Parse(modelReply);

                if (resultDoc.RootElement.TryGetProperty("medicines", out JsonElement medicinesElement)
                    && medicinesElement.ValueKind == JsonValueKind.Array)
                {
                    List<string> medicines = new List<string>();

                    foreach (JsonElement item in medicinesElement.EnumerateArray())
                    {
                        string value = item.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            medicines.Add(value);
                    }

                    return medicines;
                }
            }
            catch
            {
                // nếu model không trả đúng JSON thì fallback bên dưới
            }

            return new List<string> { modelReply };
        }
    }
}