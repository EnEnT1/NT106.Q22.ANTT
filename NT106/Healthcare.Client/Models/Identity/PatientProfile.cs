using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;


namespace Healthcare.Client.Models.Identity
{
    [Table("patient_profiles")]
    public class PatientProfile : BaseModel
    {
        [PrimaryKey("patient_id", false)] public string PatientId { get; set; } = string.Empty;
        [Column("blood_type")] public string BloodType { get; set; } = string.Empty;
        [Column("height_cm")] public float HeightCm { get; set; }
        [Column("weight_kg")] public float WeightKg { get; set; }
        [Column("allergies")] public List<string> Allergies { get; set; } = new List<string>();
        [Column("chronic_diseases")] public List<string> ChronicDiseases { get; set; } = new List<string>();
    }
}