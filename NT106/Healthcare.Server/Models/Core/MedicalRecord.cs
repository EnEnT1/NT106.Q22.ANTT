using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;
using System.Collections.Generic;

namespace Healthcare.Server.Models.Core
{
    [Table("medical_records")]
    public class MedicalRecord : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = string.Empty;            
        [Column("appointment_id")] public string AppointmentId { get; set; } = string.Empty;
        [Column("doctor_id")] public string DoctorId { get; set; } = string.Empty;
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("diagnosis")] public string Diagnosis { get; set; } = string.Empty;
        [Column("prescription_image_url")] public string PrescriptionImageUrl { get; set; } = string.Empty;
        [Column("ai_medicines")] public List<string> AiMedicines { get; set; } = new List<string>();
        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}