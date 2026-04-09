using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Core
{
    [Table("appointments")]
    public class Appointment : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }

        [Column("patient_id")] public string PatientId { get; set; }

        [Column("doctor_id")] public string DoctorId { get; set; }

        [Column("appointment_date")] public DateTime AppointmentDate { get; set; }

        [Column("start_time")] public TimeSpan StartTime { get; set; }

        [Column("status")] public string Status { get; set; }

        [Column("room_code")] public string RoomCode { get; set; }

        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}