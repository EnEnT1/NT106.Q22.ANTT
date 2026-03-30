using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Core
{
    [Table("medication_reminders")]
    public class MedicationReminder : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("medicine_name")] public string MedicineName { get; set; } = string.Empty;
        [Column("dosage")] public string Dosage { get; set; } = string.Empty;
        [Column("time_to_take")] public TimeSpan TimeToTake { get; set; }
        [Column("is_active")] public bool IsActive { get; set; }
        [Column("start_date")] public DateTime StartDate { get; set; }
        [Column("end_date")] public DateTime? EndDate { get; set; }
    }
}