using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Healthcare.Server.Services;
using System.Threading.Tasks;
using System;

namespace Healthcare.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly AiPrescriptionService _aiService;

        public AiController(AiPrescriptionService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzePrescription(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("Không tìm thấy file ảnh.");

            try
            {
                using var stream = imageFile.OpenReadStream();
                var result = await _aiService.AnalyzeImageAsync(stream);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}