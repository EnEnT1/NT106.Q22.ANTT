using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Healthcare.Client.APIClient
{
    public class AiClient
    {
        private readonly HttpClient _httpClient;

        public AiClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5246/")
            };
        }

        // Hàm này dùng cho UploadPrescriptionPage đang truyền _selectedFilePath
        public async Task<List<PrescriptionMedicineDto>> AnalyzePrescriptionAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new Exception("Chưa chọn file ảnh đơn thuốc.");
            }

            if (!File.Exists(filePath))
            {
                throw new Exception("Không tìm thấy file ảnh đơn thuốc.");
            }

            await using var stream = File.OpenRead(filePath);
            string fileName = Path.GetFileName(filePath);

            return await AnalyzePrescriptionAsync(stream, fileName);
        }

        // Hàm này dùng khi đã có Stream và fileName
        public async Task<List<PrescriptionMedicineDto>> AnalyzePrescriptionAsync(Stream imageStream, string fileName = "prescription.jpg")
        {
            using var form = new MultipartFormDataContent();

            using var fileContent = new StreamContent(imageStream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            form.Add(fileContent, "file", fileName);

            using var response = await _httpClient.PostAsync("api/ai/analyze-prescription", form);

            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Lỗi phân tích đơn thuốc: " + json);
            }

            var result = JsonSerializer.Deserialize<AnalyzePrescriptionResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            return result?.Medicines ?? new List<PrescriptionMedicineDto>();
        }
    }

    public class AnalyzePrescriptionResponse
    {
        public bool Success { get; set; }

        public List<PrescriptionMedicineDto> Medicines { get; set; } = new();
    }

    public class PrescriptionMedicineDto
    {
        public string Name { get; set; } = string.Empty;

        public string Dosage { get; set; } = string.Empty;

        public string Frequency { get; set; } = string.Empty;

        public string Duration { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;
    }
}