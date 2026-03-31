using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Server.Models.Communication
{
    [Table("notifications")]
    public class Notification : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("user_id")] public string UserId { get; set; } = string.Empty;
        [Column("title")] public string Title { get; set; } = string.Empty;
        [Column("body")] public string Body { get; set; } = string.Empty;
        [Column("is_read")] public bool IsRead { get; set; } = false;
        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}