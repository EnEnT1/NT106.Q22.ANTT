using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;
namespace Healthcare.Server.Models.Security
{
    [Table("audit_logs")]
    public class AuditLog : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("actor_id")] public string ActorId { get; set; } = string.Empty;
        [Column("action")] public string Action { get; set; } = string.Empty;
        [Column("target_table")] public string TargetTable { get; set; } = string.Empty;
        [Column("target_record_id")] public string TargetRecordId { get; set; } = string.Empty;
        [Column("old_values")] public string OldValues { get; set; } = string.Empty;
        [Column("new_values")] public string NewValues { get; set; } = string.Empty;
        [Column("ip_address")] public string IpAddress { get; set; } = string.Empty;
        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}