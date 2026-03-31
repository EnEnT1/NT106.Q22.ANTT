using Microsoft.AspNetCore.Mvc.RazorPages;
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Server.Models.Identity
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = string.Empty;
        [Column("role")] public string Role { get; set; } = string.Empty;
        [Column("full_name")] public string FullName { get; set; } = string.Empty;
        [Column("email")] public string Email { get; set; } = string.Empty;
        [Column("phone")] public string Phone { get; set; } = string.Empty;
        [Column("avatar_url")] public string AvatarUrl { get; set; } = string.Empty;
        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}