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

    public class AiClient
    {
        private readonly HttpClient _httpClient;

        public AiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> AnalyzePrescriptionAsync(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                throw new Exception("Không tìm thấy file ảnh.");

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(imagePath);
            using var streamContent = new StreamContent(fileStream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(streamContent, "file", Path.GetFileName(imagePath));

            HttpResponseMessage response =
                await _httpClient.PostAsync("api/ai/analyze-prescription", form);

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