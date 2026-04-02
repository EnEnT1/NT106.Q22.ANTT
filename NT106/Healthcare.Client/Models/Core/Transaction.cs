using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Core
{
    [Table("transactions")]
    public class Transaction : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }

        [Column("appointment_id")] public string AppointmentId { get; set; }

        [Column("patient_id")] public string PatientId { get; set; }

        [Column("amount")] public decimal Amount { get; set; }
        [Column("payment_method")] public string PaymentMethod { get; set; }

        [Column("transaction_ref")] public string TransactionRef { get; set; }

        [Column("status")] public string Status { get; set; }

        [Column("paid_at")] public DateTime? PaidAt { get; set; }
    }
}