using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Healthcare.Server.Services
{
    public class AiPrescriptionService
    {
        private readonly string _apiKey;

        public AiPrescriptionService()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                      ?? throw new Exception("Thiếu OPENAI_API_KEY trong file .env!");
        }

        public async Task<List<string>> AnalyzeImageAsync(Stream imageStream)
        {
            await Task.Delay(2000);
            return new List<string> { "Paracetamol 500mg - 2 viên/ngày", "Amoxicillin 250mg - 1 viên/ngày" };
        }
    }
}