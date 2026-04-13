using Postgrest.Attributes;
using Postgrest.Models;
using System.Collections.Generic; 

namespace Healthcare.Client.Models.Identity
{
    [Table("patient_profiles")]
    public class PatientProfile : BaseModel
    {
        [Column("patient_id")]
        public string PatientId { get; set; }

        [Column("date_of_birth")] public string DateOfBirth { get; set; }

        [Column("gender")] public string Gender { get; set; }

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