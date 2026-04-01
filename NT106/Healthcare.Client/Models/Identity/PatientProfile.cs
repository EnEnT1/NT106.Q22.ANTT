using Postgrest.Attributes;
using Postgrest.Models;
using System.Collections.Generic; // Bắt buộc phải có để dùng List

namespace Healthcare.Client.Models.Identity
{
    [Table("patient_profiles")]
    public class PatientProfile : BaseModel
    {
        [Column("patient_id")]
        public string PatientId { get; set; }

        [Column("blood_type")]
        public string BloodType { get; set; }

        [Column("height_cm")]
        public float? HeightCm { get; set; }

        [Column("weight_kg")]
        public float? WeightKg { get; set; }

        [Column("allergies")]
        public List<string> Allergies { get; set; }

        [Column("chronic_diseases")]
        public List<string> ChronicDiseases { get; set; }
    }
}