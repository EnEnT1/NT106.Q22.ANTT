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
                return BadRequest(new { message = "Chữ ký không hợp lệ!" });
            }

            // appointmentId nằm trong OrderInfo
            string appointmentId = vnp_OrderInfo;

            if (string.IsNullOrEmpty(appointmentId) || !Guid.TryParse(appointmentId, out _))
            {
                return BadRequest(new { message = "Không tìm thấy AppointmentId hợp lệ trong giao dịch." });
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

                    return Ok(new { message = "Thanh toán thành công!", appointmentId = appointmentId });
                }

                return BadRequest(new { message = "Giao dịch không thành công tại VNPay." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi cập nhật Database: " + ex.Message });
            }
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