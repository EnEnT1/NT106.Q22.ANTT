using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Core
{
    [Table("lab_results")]
    public class LabResult : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }

        [Column("patient_id")] public string PatientId { get; set; }

        [Column("appointment_id")] public string AppointmentId { get; set; }

        [Column("test_type")] public string TestType { get; set; }

        [Column("file_url")] public string FileUrl { get; set; }

        [Column("doctor_notes")] public string DoctorNotes { get; set; }

        [Column("uploaded_at")] public DateTime UploadedAt { get; set; }
    }
}