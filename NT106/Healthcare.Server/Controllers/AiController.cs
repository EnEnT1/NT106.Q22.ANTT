using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Healthcare.Server.Services;

namespace Healthcare.Server.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly AiPrescriptionService _aiPrescriptionService;

        public AiController(AiPrescriptionService aiPrescriptionService)
        {
            _aiPrescriptionService = aiPrescriptionService;
        }

        [HttpPost("analyze-prescription")]
        public async Task<IActionResult> AnalyzePrescription(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn ảnh đơn thuốc.");

            using var stream = file.OpenReadStream();
            var medicines = await _aiPrescriptionService.AnalyzeImageAsync(stream);

            return Ok(new
            {
                success = true,
                medicines = medicines
            });
        }
    }
}