using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Server.Models.Core
{
    [Table("transactions")]
    public class Transaction : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = string.Empty;
        [Column("appointment_id")] public string AppointmentId { get; set; } = string.Empty;
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("amount")] public decimal Amount { get; set; } = 0;
        [Column("payment_method")] public string PaymentMethod { get; set; } = string.Empty;
        [Column("transaction_ref")] public string TransactionRef { get; set; } = string.Empty;
        [Column("status")] public string Status { get; set; } = string.Empty;
        [Column("paid_at")] public DateTime? PaidAt { get; set; } = null;
    }
}