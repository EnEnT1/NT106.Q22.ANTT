using Healthcare.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Postgrest.Attributes;
using Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Healthcare.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;
        private readonly Supabase.Client _supabaseClient;

        public PaymentController(PaymentService paymentService, Supabase.Client supabaseClient)
        {
            _paymentService = paymentService;
            _supabaseClient = supabaseClient;
        }

        [HttpPost("create-url")]
        public IActionResult CreateUrl([FromBody] PaymentRequestModel request)
        {
            if (!Guid.TryParse(request.AppointmentId, out _))
            {
                return BadRequest(new
                {
                    message = "Lỗi: AppointmentId không đúng định dạng UUID.",
                    invalidId = request.AppointmentId
                });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1") ipAddress = "127.0.0.1";

            var url = _paymentService.CreatePaymentUrl(request.AppointmentId, request.Amount, ipAddress);
            return Ok(new { paymentUrl = url });
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VNPayReturn()
        {
            var query = HttpContext.Request.Query;
            var vnp_TxnRef = query["vnp_TxnRef"].ToString();
            var vnp_ResponseCode = query["vnp_ResponseCode"].ToString();
            // AppointmentId được lưu trong vnp_OrderInfo khi tạo URL
            var vnp_OrderInfo = query["vnp_OrderInfo"].ToString();

            if (!_paymentService.ValidateVnPaySignature(query))
            {
                return Content(GetPaymentResultHtml(false, "Chữ ký thanh toán không hợp lệ!"), "text/html; charset=utf-8");
            }

            // appointmentId nằm trong OrderInfo
            string appointmentId = vnp_OrderInfo;

            if (string.IsNullOrEmpty(appointmentId) || !Guid.TryParse(appointmentId, out _))
            {
                return Content(GetPaymentResultHtml(false, "Không tìm thấy AppointmentId hợp lệ trong giao dịch."), "text/html; charset=utf-8");
            }

            try
            {
                if (vnp_ResponseCode == "00")
                {
                    // 1. Cập nhật transaction
                    var transaction = await _supabaseClient.From<TransactionModel>()
                        .Where(x => x.AppointmentId == appointmentId)
                        .Single();

                    if (transaction != null)
                    {
                        transaction.Status = "Success";
                        await transaction.Update<TransactionModel>();
                    }

                    // 2. Cập nhật appointment
                    var appointment = await _supabaseClient.From<AppointmentModel>()
                        .Where(x => x.Id == appointmentId)
                        .Single();

                    if (appointment != null)
                    {
                        appointment.Status = "Confirmed";
                        await appointment.Update<AppointmentModel>();
                    }

                    return Content(GetPaymentResultHtml(true, "Thanh toán thành công!", appointmentId), "text/html; charset=utf-8");
                }

                return Content(GetPaymentResultHtml(false, "Giao dịch không thành công tại VNPay (Mã phản hồi: " + vnp_ResponseCode + ")."), "text/html; charset=utf-8");
            }
            catch (Exception ex)
            {
                return Content(GetPaymentResultHtml(false, "Lỗi khi cập nhật Database: " + ex.Message), "text/html; charset=utf-8");
            }
        }

        private string GetPaymentResultHtml(bool isSuccess, string details, string appointmentId = "")
        {
            string contentHtml = isSuccess ? $@"
        <div class=""icon success"">✓</div>
        <h1 class=""success"">Thanh Toán Thành Công!</h1>
        <p>Giao dịch của bạn đã được xác nhận. Lịch hẹn của bạn đã chuyển sang trạng thái đã xác nhận (Confirmed).</p>
        <div class=""details"">
            <div class=""detail-row"">
                <span class=""label"">Mã lịch hẹn:</span>
                <span class=""value"">{appointmentId}</span>
            </div>
            <div class=""detail-row"">
                <span class=""label"">Trạng thái:</span>
                <span class=""value"" style=""color: #10b981;"">Đã thanh toán</span>
            </div>
        </div>
        <a href=""#"" onclick=""window.close()"" class=""btn"">Đóng cửa sổ</a>"
        : $@"
        <div class=""icon error"">✗</div>
        <h1 class=""error"">Thanh Toán Thất Bại</h1>
        <p>Có lỗi xảy ra trong quá trình xác thực giao dịch hoặc giao dịch đã bị từ chối từ ngân hàng.</p>
        <div class=""details"">
            <div class=""detail-row"">
                <span class=""label"">Chi tiết:</span>
                <span class=""value"" style=""color: #ef4444;"">{details}</span>
            </div>
        </div>
        <a href=""#"" onclick=""window.close()"" class=""btn"" style=""background-color: #64748b; box-shadow: 0 4px 12px rgba(100, 116, 139, 0.2);"">Đóng cửa sổ</a>";

            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kết Quả Thanh Toán - Healthcare</title>
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@400;600;800&display=swap"" rel=""stylesheet"">
    <style>
        body {{
            font-family: 'Outfit', sans-serif;
            background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }}
        .container {{
            background: rgba(255, 255, 255, 0.85);
            backdrop-filter: blur(12px);
            border: 1px solid rgba(255, 255, 255, 0.4);
            border-radius: 24px;
            padding: 40px;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.05);
            text-align: center;
            max-width: 450px;
            width: 100%;
        }}
        .icon {{
            width: 72px;
            height: 72px;
            border-radius: 50%;
            display: flex;
            justify-content: center;
            align-items: center;
            font-size: 32px;
            margin: 0 auto 24px;
        }}
        .icon.success {{
            background-color: #ecfdf5;
            color: #10b981;
            border: 2px solid #a7f3d0;
        }}
        .icon.error {{
            background-color: #fef2f2;
            color: #ef4444;
            border: 2px solid #fca5a5;
        }}
        h1 {{
            font-size: 24px;
            font-weight: 800;
            margin: 0 0 12px 0;
        }}
        h1.success {{ color: #064e3b; }}
        h1.error {{ color: #7f1d1d; }}
        p {{
            font-size: 15px;
            color: #64748b;
            margin: 0 0 24px 0;
            line-height: 1.6;
        }}
        .details {{
            background-color: rgba(241, 245, 249, 0.6);
            border-radius: 12px;
            padding: 16px;
            font-size: 13.5px;
            color: #334155;
            text-align: left;
            margin-bottom: 24px;
            border: 1px solid rgba(226, 232, 240, 0.8);
        }}
        .detail-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 8px;
        }}
        .detail-row:last-child {{
            margin-bottom: 0;
        }}
        .label {{
            color: #64748b;
            font-weight: 500;
        }}
        .value {{
            font-weight: 600;
            color: #0f172a;
            word-break: break-all;
        }}
        .btn {{
            display: inline-block;
            background-color: #2563eb;
            color: white;
            text-decoration: none;
            padding: 12px 28px;
            border-radius: 12px;
            font-weight: 600;
            font-size: 15px;
            box-shadow: 0 4px 12px rgba(37, 99, 235, 0.2);
            transition: all 0.2s ease;
        }}
        .btn:hover {{
            background-color: #1d4ed8;
            transform: translateY(-1px);
        }}
    </style>
</head>
<body>
    <div class=""container"">
        {contentHtml}
    </div>
</body>
</html>";
        }
    }

    public class PaymentRequestModel
    {
        public string AppointmentId { get; set; }
        public double Amount { get; set; }
    }

    [Table("transactions")]
    public class TransactionModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("appointment_id")]
        public string AppointmentId { get; set; }

        [Column("status")]
        public string Status { get; set; }
    }

    [Table("appointments")]
    public class AppointmentModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("status")]
        public string Status { get; set; }
    }
}