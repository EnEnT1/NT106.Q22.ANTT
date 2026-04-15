using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Core
{
    [Table("health_metrics")]
    public class HealthMetric : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }

        [Column("patient_id")] public string PatientId { get; set; }

        [Column("metric_type")] public string MetricType { get; set; }

        [Column("value")] public double Value { get; set; } 
        [Column("unit")] public string Unit { get; set; }

        [Column("measured_at")] public DateTime MeasuredAt { get; set; }
    }
}