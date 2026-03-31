using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Core
{
    [Table("health_metrics")]
    public class HealthMetric : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("metric_type")] public string MetricType { get; set; } = string.Empty;
        [Column("value")] public string Value { get; set; } = string.Empty;
        [Column("unit")] public string Unit { get; set; } = string.Empty;
        [Column("measured_at")] public DateTime MeasuredAt { get; set; } 
    }
}