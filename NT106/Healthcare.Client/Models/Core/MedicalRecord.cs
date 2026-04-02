using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Core
{
    [Table("medical_records")]
    public class MedicalRecord : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }

        [Column("appointment_id")] public string AppointmentId { get; set; }

        [Column("doctor_id")] public string DoctorId { get; set; }

        [Column("patient_id")] public string PatientId { get; set; }

        [Column("diagnosis")] public string Diagnosis { get; set; }
        [Column("prescription_image_url")] public string PrescriptionImageUrl { get; set; }

        [Column("ai_medicines")] public string AiMedicines { get; set; }

        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}