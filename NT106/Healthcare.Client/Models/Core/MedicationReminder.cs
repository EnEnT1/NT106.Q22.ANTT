using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Core
{
    [Table("medication_reminders")]
    public class MedicationReminder : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }

        [Column("patient_id")] public string PatientId { get; set; }

        [Column("medicine_name")] public string MedicineName { get; set; }

        [Column("dosage")] public string Dosage { get; set; }

        [Column("time_to_take")] public TimeSpan TimeToTake { get; set; }

        [Column("is_active")] public bool? IsActive { get; set; }

        [Column("start_date")] public DateTime StartDate { get; set; }

        [Column("end_date")] public DateTime? EndDate { get; set; }
    }
}