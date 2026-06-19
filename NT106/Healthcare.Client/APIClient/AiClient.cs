using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                BaseAddress = new Uri(BaseHttpClient.ServerBaseUrl)
            };
        }

        /// <summary>Gửi file lên server, nhận về PrescriptionData đầy đủ.</summary>
        public async Task<PrescriptionData> AnalyzePrescriptionAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new Exception("Chưa chọn file ảnh đơn thuốc.");
            if (!File.Exists(filePath))
                throw new Exception("Không tìm thấy file ảnh đơn thuốc.");

            await using var stream = File.OpenRead(filePath);
            string fileName = Path.GetFileName(filePath);
            return await AnalyzePrescriptionAsync(stream, fileName);
        }

        public async Task<PrescriptionData> AnalyzePrescriptionAsync(Stream imageStream, string fileName = "prescription.jpg")
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(imageStream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(fileContent, "file", fileName);

            using var response = await _httpClient.PostAsync("api/ai/analyze-prescription", form);
            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Lỗi phân tích đơn thuốc: " + json);

            var result = JsonSerializer.Deserialize<AnalyzePrescriptionResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data ?? new PrescriptionData();
        }
    }

    // ===== Response wrapper =====
    public class AnalyzePrescriptionResponse
    {
        public bool Success { get; set; }
        public PrescriptionData Data { get; set; } = new();
        public List<string> Medicines { get; set; } = new(); // backward-compat
    }

    // ===== Data Models (mirror server) =====
    public class PrescriptionData
    {
        [JsonPropertyName("clinicName")]       public string ClinicName       { get; set; } = "";
        [JsonPropertyName("clinicAddress")]    public string ClinicAddress    { get; set; } = "";
        [JsonPropertyName("clinicPhone")]      public string ClinicPhone      { get; set; } = "";
        [JsonPropertyName("patientName")]      public string PatientName      { get; set; } = "";
        [JsonPropertyName("patientAge")]       public string PatientAge       { get; set; } = "";
        [JsonPropertyName("patientGender")]    public string PatientGender    { get; set; } = "";
        [JsonPropertyName("patientAddress")]   public string PatientAddress   { get; set; } = "";
        [JsonPropertyName("diagnosis")]        public string Diagnosis        { get; set; } = "";
        [JsonPropertyName("doctorName")]       public string DoctorName       { get; set; } = "";
        [JsonPropertyName("prescriptionDate")] public string PrescriptionDate { get; set; } = "";
        [JsonPropertyName("medicines")]        public List<MedicineItem> Medicines { get; set; } = new();
        [JsonPropertyName("doctorAdvice")]     public string DoctorAdvice     { get; set; } = "";
    }

    public class MedicineItem
    {
        [JsonPropertyName("name")]             public string Name             { get; set; } = "";
        [JsonPropertyName("quantity")]         public string Quantity         { get; set; } = "";
        [JsonPropertyName("timesPerDay")]      public string TimesPerDay      { get; set; } = "";
        [JsonPropertyName("quantityPerTime")]  public string QuantityPerTime  { get; set; } = "";
        [JsonPropertyName("note")]             public string Note             { get; set; } = "";
    }
}