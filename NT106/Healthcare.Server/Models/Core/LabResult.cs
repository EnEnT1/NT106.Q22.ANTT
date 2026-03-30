using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Server.Models.Core
{
    [Table("lab_results")]
    public class LabResult : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = string.Empty;
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("appointment_id")] public string AppointmentId { get; set; } = string.Empty;
        [Column("test_type")] public string TestType { get; set; } = string.Empty;
        [Column("file_url")] public string FileUrl { get; set; } = string.Empty;
        [Column("doctor_notes")] public string DoctorNotes { get; set; } = string.Empty;
        [Column("uploaded_at")] public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}