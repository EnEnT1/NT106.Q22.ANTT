using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Healthcare.Client.APIClient
{
    public class AiAnalyzeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Medicines { get; set; }
    }

    public class AiClient : BaseHttpClient
    {
        public AiClient() : base("http://localhost:5246/api/")
        {
        }

        public async Task<List<string>> AnalyzePrescriptionAsync(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                throw new Exception("Không tìm thấy file ảnh.");

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(imagePath);
            using var streamContent = new StreamContent(fileStream);

            // Tự động xác định Content-Type dựa trên đuôi file
            string extension = Path.GetExtension(imagePath).ToLower();
            string contentType = extension switch
            {
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "image/jpeg"
            };

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(streamContent, "file", Path.GetFileName(imagePath));

            // Vì AiClient kế thừa BaseHttpClient đã có sẵn HttpClient _httpClient được cấu hình BaseAddress
            // và đã trỏ vào http://localhost:5246/api/ nên endpoint chỉ cần 'ai/analyze-prescription'
            HttpResponseMessage response = await _httpClient.PostAsync("ai/analyze-prescription", form);

            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Lỗi từ server: " + json);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<AiAnalyzeResponse>(json, options);

            return result?.Medicines ?? new List<string>();
        }
    }
}