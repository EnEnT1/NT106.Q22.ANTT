using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Server.Models.Core
{
    [Table("doctor_time_slots")]
    public class TimeSlot : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("doctor_id")] public string DoctorId { get; set; }

        [Column("slot_date")] public DateTime SlotDate { get; set; }

        [Column("start_time")] public TimeSpan StartTime { get; set; }

        [Column("end_time")] public TimeSpan EndTime { get; set; }

        [Column("status")] public string Status { get; set; } = "Available"; // Available, Booked, Unavailable

        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
