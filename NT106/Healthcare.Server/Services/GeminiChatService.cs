using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Healthcare.Server.Services
{
    /// <summary>
    /// Dịch vụ chat sức khỏe sử dụng Google Gemini API thay thế OpenAI.
    /// </summary>
    public class GeminiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiChatService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Gemini:ApiKey"] ?? "";
            _model  = configuration["Gemini:Model"]  ?? "gemini-1.5-flash";
        }

        public async Task<string> AskAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return "Server chưa cấu hình Gemini API key.";

            // Gemini generateContent endpoint
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new { text = "Bạn là trợ lý sức khỏe của ứng dụng Healthcare. Trả lời bằng tiếng Việt, ngắn gọn, dễ hiểu. Không chẩn đoán thay bác sĩ. Luôn khuyên người dùng đi khám nếu triệu chứng nặng, kéo dài, đau dữ dội, khó thở, sốt cao, ngất, chảy máu hoặc có dấu hiệu nguy hiểm." }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = message } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 512
                }
            };

            string json = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return "Gemini API lỗi: " + responseJson;

            using var doc = JsonDocument.Parse(responseJson);

            // Gemini response path: candidates[0].content.parts[0].text
            string? reply = doc
                .RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return string.IsNullOrWhiteSpace(reply)
                ? "Trợ lý chưa có câu trả lời phù hợp."
                : reply;
        }
    }
}
