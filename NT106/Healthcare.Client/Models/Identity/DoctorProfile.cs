using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Identity
{
    [Table("doctor_profiles")]
    public class DoctorProfile : BaseModel
    {
        [PrimaryKey("doctor_id", false)] public string DoctorId { get; set; } = string.Empty;
        [Column("specialty")] public string Specialty { get; set; } = string.Empty;
        [Column("experience_years")] public int ExperienceYears { get; set; } = int.MaxValue;
        [Column("consultation_fee")] public decimal ConsultationFee { get; set; } = decimal.MaxValue;
        [Column("rating_average")] public float RatingAverage { get; set; } = float.MaxValue;
        [Column("biography")] public string Biography { get; set; } = string.Empty;
    }
}