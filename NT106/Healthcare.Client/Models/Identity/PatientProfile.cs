using Postgrest.Attributes;
using Postgrest.Models;
using System.Collections.Generic;

namespace Healthcare.Client.Models.Identity
{
    [Table("patient_profiles")]
    public class PatientProfile : BaseModel
    {
        [PrimaryKey("patient_id", true)] [Column("patient_id")] public string PatientId { get; set; } = string.Empty;

        [Column("date_of_birth")] public string DateOfBirth { get; set; } = null;

        [Column("gender")] public string Gender { get; set; } = null;

        [Column("blood_type")] public string BloodType { get; set; } = null;

        [Column("height_cm")] public float? HeightCm { get; set; }

        [Column("weight_kg")] public float? WeightKg { get; set; }

        [Column("allergies")] public List<string> Allergies { get; set; } = new();

        [Column("chronic_diseases")] public List<string> ChronicDiseases { get; set; } = new();
    }
}