using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Core
{
    [Table("transactions")]
    public class Transaction : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("appointment_id")] public string AppointmentId { get; set; } = string.Empty;
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("amount")] public decimal Amount { get; set; }
        [Column("payment_method")] public string PaymentMethod { get; set; } = string.Empty;
        [Column("transaction_ref")] public string TransactionRef { get; set; } = string.Empty;
        [Column("status")] public string Status { get; set; } = string.Empty;
        [Column("paid_at")] public DateTime? PaidAt { get; set; }
    }
}