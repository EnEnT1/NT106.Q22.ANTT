using Postgrest.Attributes;
using Postgrest.Models;

namespace Healthcare.Client.Models.Identity
{
    [Table("doctor_profiles")]
    public class DoctorProfile : BaseModel
    {
        [Column("doctor_id")]
        public string DoctorId { get; set; }

        [Column("specialty")]
        public string Specialty { get; set; }

        [Column("experience_years")]
        public int? ExperienceYears { get; set; }

        [Column("consultation_fee")]
        public decimal? ConsultationFee { get; set; }

        [Column("rating_average")]
        public float? RatingAverage { get; set; }

        [Column("biography")]
        public string Biography { get; set; }
    }
}