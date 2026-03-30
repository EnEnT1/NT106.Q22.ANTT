using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Core
{
    [Table("master_medicines")]
    public class MasterMedicine : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("medicine_name")] public string MedicineName { get; set; } = string.Empty;
        [Column("active_ingredient")] public string ActiveIngredient { get; set; } = string.Empty;
        [Column("default_dosage")] public string DefaultDosage { get; set; } = string.Empty;
        [Column("contraindications")] public List<string> Contraindications { get; set; } = new List<string>();
    }
}