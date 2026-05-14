using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        [HttpPost("chat")]
        public IActionResult Chat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new
                {
                    success = false,
                    reply = "Vui lòng nhập câu hỏi."
                });
            }

            string reply = GenerateHealthReply(request.Message);

            return Ok(new
            {
                success = true,
                reply = reply
            });
        }

        private string GenerateHealthReply(string message)
        {
            string lowerMessage = message.ToLower();

            if (lowerMessage.Contains("đau đầu") || lowerMessage.Contains("dau dau"))
            {
                return "Bạn nên nghỉ ngơi, uống đủ nước và theo dõi tình trạng đau đầu. Nếu đau kéo dài, đau dữ dội, buồn nôn hoặc chóng mặt, bạn nên đặt lịch khám bác sĩ.";
            }

            if (lowerMessage.Contains("sốt") || lowerMessage.Contains("sot"))
            {
                return "Bạn nên đo nhiệt độ, uống nhiều nước và nghỉ ngơi. Nếu sốt cao trên 38.5°C, sốt kéo dài hoặc kèm khó thở, bạn nên liên hệ bác sĩ.";
            }

            if (lowerMessage.Contains("ho"))
            {
                return "Bạn nên uống nước ấm, giữ ấm cổ và theo dõi triệu chứng. Nếu ho kéo dài, ho có đờm xanh/vàng, đau ngực hoặc khó thở thì nên đi khám.";
            }

            if (lowerMessage.Contains("đau bụng") || lowerMessage.Contains("dau bung"))
            {
                return "Bạn nên theo dõi vị trí đau, mức độ đau và các triệu chứng đi kèm. Nếu đau dữ dội, nôn ói, sốt hoặc tiêu chảy kéo dài, bạn nên đi khám ngay.";
            }

            if (lowerMessage.Contains("đặt lịch") || lowerMessage.Contains("dat lich"))
            {
                return "Bạn có thể vào trang đặt lịch khám, chọn bác sĩ, ngày giờ phù hợp rồi xác nhận lịch hẹn.";
            }

            if (lowerMessage.Contains("thuốc") || lowerMessage.Contains("thuoc"))
            {
                return "Bạn không nên tự ý dùng thuốc khi chưa có chỉ định. Hãy đọc kỹ đơn thuốc hoặc hỏi bác sĩ/dược sĩ nếu chưa rõ cách dùng.";
            }

            return "Tôi có thể hỗ trợ tư vấn sức khỏe cơ bản. Tuy nhiên, thông tin này không thay thế chẩn đoán của bác sĩ. Bạn nên đặt lịch khám nếu triệu chứng kéo dài hoặc nghiêm trọng.";
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}