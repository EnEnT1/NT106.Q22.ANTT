using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Communication
{
    [Table("reviews")]
    public class Review : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("appointment_id")]
        public string AppointmentId { get; set; }

        [Column("patient_id")]
        public string PatientId { get; set; }

        [Column("doctor_id")]
        public string DoctorId { get; set; }

        [Column("rating")]
        public int? Rating { get; set; }

        [Column("comment")]
        public string Comment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}