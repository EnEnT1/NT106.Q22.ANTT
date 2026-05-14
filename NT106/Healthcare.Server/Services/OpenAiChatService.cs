using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Healthcare.Server.Services
{
    public class OpenAiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public OpenAiChatService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();

            _apiKey = configuration["OpenAI:ApiKey"] ?? "";
            _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        }

        public async Task<string> AskAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return "Server chưa cấu hình OpenAI API key.";
            }

            var requestBody = new
            {
                model = _model,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "Bạn là chatbot hỗ trợ sức khỏe cơ bản trong ứng dụng Healthcare. Trả lời bằng tiếng Việt, ngắn gọn, dễ hiểu. Không chẩn đoán thay bác sĩ. Luôn khuyên người dùng đi khám nếu triệu chứng nặng, kéo dài, đau dữ dội, khó thở, sốt cao, ngất, chảy máu hoặc có dấu hiệu nguy hiểm."
                    },
                    new
                    {
                        role = "user",
                        content = message
                    }
                },
                temperature = 0.3
            };

            string json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/chat/completions"
            );

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return "OpenAI API lỗi: " + responseJson;
            }

            using var doc = JsonDocument.Parse(responseJson);

            string? reply = doc
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return string.IsNullOrWhiteSpace(reply)
                ? "AI chưa có câu trả lời phù hợp."
                : reply;
        }
    }
}