using Microsoft.AspNetCore.Mvc;
using Healthcare.Server.Services;
using System.Threading.Tasks;

namespace Healthcare.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;

        public PaymentController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("vnpay-return")]
        public IActionResult VnpayReturn()
        {
            // Lấy toàn bộ tham số từ URL VNPay trả về
            var queryParams = HttpContext.Request.Query;

            // Xác thực chữ ký
            bool isValid = _paymentService.ValidateVnPaySignature(queryParams);

            if (isValid)
            {
                // Cập nhật trạng thái hóa đơn trong Database thành "Đã thanh toán"
                // ... logic Supabase ở đây ...
                return Ok("Thanh toán thành công. Bạn có thể đóng cửa sổ này.");
            }
            else
            {
                return BadRequest("Sai chữ ký bảo mật. Giao dịch bị từ chối.");
            }
        }
    }
}