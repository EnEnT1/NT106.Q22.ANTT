using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Server.Models.Identity
{
    [Table("doctor_profiles")]
    public class DoctorProfile : BaseModel
    {
        [PrimaryKey("doctor_id", false)] public string DoctorId { get; set; } = string.Empty;
        [Column("specialty")] public string Specialty { get; set; } = string.Empty;
        [Column("experience_years")] public int ExperienceYears { get; set; } = 0;
        [Column("consultation_fee")] public decimal ConsultationFee { get; set; } = 0;
        [Column("rating_average")] public float RatingAverage { get; set; } = 0;
        [Column("biography")] public string Biography { get; set; } = string.Empty;
    }
}