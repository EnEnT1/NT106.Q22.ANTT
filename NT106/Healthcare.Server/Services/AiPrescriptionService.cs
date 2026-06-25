using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            _apiKey = _configuration["Gemini:ApiKey"]
                      ?? throw new Exception("Thiếu Gemini:ApiKey trong appsettings.json");

            _model = _configuration["Gemini:Model"]
                     ?? "gemini-3.5-flash";

            _httpClient = new HttpClient();
        }

        public async Task<PrescriptionData> AnalyzeImageAsync(Stream imageStream)
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
Bạn là AI chuyên đọc đơn thuốc tiếng Việt.
Hãy trích xuất TOÀN BỘ thông tin từ ảnh đơn thuốc và trả về JSON đúng format sau (KHÔNG thêm bất kỳ text nào ngoài JSON):
{
  ""clinicName"": ""tên phòng khám/bệnh viện"",
  ""clinicAddress"": ""địa chỉ phòng khám"",
  ""clinicPhone"": ""số điện thoại phòng khám"",
  ""patientName"": ""họ tên bệnh nhân"",
  ""patientAge"": ""tuổi"",
  ""patientGender"": ""Nam hoặc Nữ"",
  ""patientAddress"": ""địa chỉ bệnh nhân"",
  ""diagnosis"": ""chẩn đoán bệnh"",
  ""doctorName"": ""tên bác sĩ"",
  ""prescriptionDate"": ""ngày/tháng/năm trên đơn (định dạng dd/MM/yyyy)"",
  ""medicines"": [
    {
      ""name"": ""Tên thuốc và hàm lượng"",
      ""quantity"": ""số viên/chai tổng cộng (chỉ số và đơn vị, ví dụ: 10 viên)"",
      ""timesPerDay"": ""số lần uống mỗi ngày (chỉ số nguyên, ví dụ: 2)"",
      ""quantityPerTime"": ""số lượng mỗi lần (chỉ số và đơn vị, ví dụ: 1 viên)"",
      ""note"": ""ghi chú thêm nếu có (ví dụ: sau ăn, khi sốt, ...)""
    }
  ],
  ""doctorAdvice"": ""lời dặn của bác sĩ nếu có""
}
Nếu không đọc được thông tin nào thì để chuỗi rỗng. medicines trả mảng rỗng [] nếu không đọc được.";

            // Gemini Vision API — generateContent
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0,
                    responseMimeType = "application/json"
                }
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Lỗi Gemini: " + responseText);

            using JsonDocument doc = JsonDocument.Parse(responseText);
            // Gemini response: candidates[0].content.parts[0].text
            string modelReply = doc.RootElement
                                   .GetProperty("candidates")[0]
                                   .GetProperty("content")
                                   .GetProperty("parts")[0]
                                   .GetProperty("text")
                                   .GetString();

            if (string.IsNullOrWhiteSpace(modelReply))
                return new PrescriptionData();

            try
            {
                // Trim markdown code fences nếu model trả về ```json ... ```
                string cleaned = modelReply.Trim();
                if (cleaned.StartsWith("```"))
                {
                    int start = cleaned.IndexOf('{');
                    int end = cleaned.LastIndexOf('}');
                    if (start >= 0 && end > start)
                        cleaned = cleaned.Substring(start, end - start + 1);
                }

                var data = JsonSerializer.Deserialize<PrescriptionData>(
                    cleaned,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return data ?? new PrescriptionData();
            }
            catch
            {
                return new PrescriptionData();
            }
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = "Bạn là trợ lý sức khỏe tên là Elizabeth của ứng dụng Healthcare. Hãy tư vấn sức khỏe cơ bản một cách thân thiện và chuyên nghiệp. Nhắc nhở người dùng rằng lời khuyên này không thay thế chẩn đoán của bác sĩ." } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userMessage } }
                    }
                },
                generationConfig = new { temperature = 0.7 }
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Lỗi Gemini: " + responseText);

            using JsonDocument doc = JsonDocument.Parse(responseText);
            return doc.RootElement
                      .GetProperty("candidates")[0]
                      .GetProperty("content")
                      .GetProperty("parts")[0]
                      .GetProperty("text")
                      .GetString() ?? "Xin lỗi, tôi không thể xử lý câu trả lời lúc này.";
        }
    }
}

// ===== Data Models =====

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