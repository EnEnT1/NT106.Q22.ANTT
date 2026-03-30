using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System.Collections.Generic;

namespace Healthcare.Server.Models.Core
{
    [Table("master_medicines")]
    public class MasterMedicine : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = string.Empty;
        [Column("medicine_name")] public string MedicineName { get; set; } = string.Empty;
        [Column("active_ingredient")] public string ActiveIngredient { get; set; } = string.Empty;
        [Column("default_dosage")] public string DefaultDosage { get; set; } = string.Empty;
        [Column("contraindications")] public List<string> Contraindications { get; set; } = new List<string>();
    }
}