using Postgrest.Attributes;
using Postgrest.Models;
using System.Collections.Generic;

namespace Healthcare.Client.Models.Core
{
    [Table("master_medicines")]
    public class MasterMedicine : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }

        [Column("medicine_name")] public string MedicineName { get; set; }

        [Column("active_ingredient")] public string ActiveIngredient { get; set; }

        [Column("default_dosage")] public string DefaultDosage { get; set; }

        [Column("contraindications")] public List<string> Contraindications { get; set; }
    }
}