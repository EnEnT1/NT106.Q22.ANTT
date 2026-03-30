using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Core
{
    [Table("lab_results")]
    public class LabResult : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("appointment_id")] public string AppointmentId { get; set; } = string.Empty;
        [Column("test_type")] public string TestType { get; set; } = string.Empty;
        [Column("file_url")] public string FileUrl { get; set; } = string.Empty;
        [Column("doctor_notes")] public string DoctorNotes { get; set; } = string.Empty;
        [Column("uploaded_at")] public DateTime UploadedAt { get; set; }
    }
}