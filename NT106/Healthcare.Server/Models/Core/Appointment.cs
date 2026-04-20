using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Server.Models.Core
{
    [Table("appointments")]
    public class Appointment : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = string.Empty;        
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("doctor_id")] public string DoctorId { get; set; } = string.Empty;
        [Column("appointment_date")] public DateTime AppointmentDate { get; set; } = DateTime.UtcNow;
        [Column("start_time")] public TimeSpan StartTime { get; set; }
        [Column("end_time")] public TimeSpan EndTime { get; set; }
        [Column("status")] public string Status { get; set; } = string.Empty;
        [Column("examination_type")] public string ExaminationType { get; set; } = string.Empty;
        [Column("room_code")] public string RoomCode { get; set; } = string.Empty;
        [Column("slot_id")] public string SlotId { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}