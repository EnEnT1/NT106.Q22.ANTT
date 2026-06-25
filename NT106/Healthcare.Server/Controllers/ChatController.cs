using Microsoft.AspNetCore.Mvc;
using Healthcare.Server.SupabaseIntegration;
using Healthcare.Server.Models.Communication;
using System;
using System.Threading.Tasks;

namespace Healthcare.Server.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly SupabaseAdminHelper _supabaseAdmin;

        public ChatController(SupabaseAdminHelper supabaseAdmin)
        {
            _supabaseAdmin = supabaseAdmin;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.SenderId) || string.IsNullOrWhiteSpace(message.ReceiverId))
            {
                return BadRequest("Thông tin tin nhắn không hợp lệ.");
            }

            try
            {
                if (string.IsNullOrEmpty(message.Id))
                {
                    message.Id = Guid.NewGuid().ToString();
                }
                if (message.CreatedAt == default)
                {
                    message.CreatedAt = DateTime.UtcNow;
                }

                var response = await _supabaseAdmin.AdminClient.From<ChatMessage>().Insert(message);
                
                if (response.Models != null && response.Models.Count > 0)
                {
                    return Ok(new { success = true, data = response.Models[0] });
                }
                
                return Ok(new { success = true, message = "Đã lưu tin nhắn thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatController Error]: {ex.Message}");
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
    }
}
