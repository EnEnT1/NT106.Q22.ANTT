using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Communication
{
    [Table("notifications")]
    public class Notification : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("user_id")] public string UserId { get; set; } = string.Empty;
        [Column("title")] public string Title { get; set; } = String.Empty;
        [Column("body")] public string Body { get; set; } = String.Empty;
        [Column("is_read")] public bool IsRead { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}