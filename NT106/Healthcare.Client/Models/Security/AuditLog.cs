using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Security
{
    [Table("audit_logs")]
    public class AuditLog : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("actor_id")]
        public string ActorId { get; set; }

        [Column("action")]
        public string Action { get; set; }

        [Column("target_table")]
        public string TargetTable { get; set; }

        [Column("target_record_id")]
        public string TargetRecordId { get; set; }

        [Column("old_values")]
        public string OldValues { get; set; }

        [Column("new_values")]
        public string NewValues { get; set; }

        [Column("ip_address")]
        public string IpAddress { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}