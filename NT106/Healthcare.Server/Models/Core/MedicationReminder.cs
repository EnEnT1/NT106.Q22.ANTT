using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Server.Models.Core
{
    [Table("medication_reminders")]
    public class MedicationReminder : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = string.Empty;
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("medicine_name")] public string MedicineName { get; set; } = string.Empty;
        [Column("dosage")] public string Dosage { get; set; } = string.Empty;
        [Column("time_to_take")] public TimeSpan TimeToTake { get; set; } = TimeSpan.Zero;
        [Column("is_active")] public bool IsActive { get; set; } = false;
        [Column("start_date")] public DateTime StartDate { get; set; } = DateTime.UtcNow;
        [Column("end_date")] public DateTime? EndDate { get; set; } = null;
    }
}