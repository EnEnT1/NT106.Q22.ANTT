using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Server.Models.Core
{
    [Table("health_metrics")]
    public class HealthMetric : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = string.Empty;
        [Column("patient_id")] public string PatientId { get; set; } = string.Empty;
        [Column("metric_type")] public string MetricType { get; set; } = string.Empty;
        [Column("value")] public string Value { get; set; } = string.Empty;
        [Column("unit")] public string Unit { get; set; } = string.Empty;
        [Column("measured_at")] public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    }
}