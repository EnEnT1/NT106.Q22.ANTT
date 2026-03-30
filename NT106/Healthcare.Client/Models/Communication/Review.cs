using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Communication
{
    [Table("reviews")]
    public class Review : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("appointment_id")] public string AppointmentId { get; set; } = string.Empty;
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("doctor_id")] public string DoctorId { get; set; } = string.Empty;
        [Column("rating")] public int Rating { get; set; } = 0;
        [Column("comment")] public string Comment { get; set; } = string.Empty;
        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   
    }
}