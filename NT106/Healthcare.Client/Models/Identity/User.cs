using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;


namespace Healthcare.Client.Models.Identity
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("role")] public string Role { get; set; } = string.Empty;
        [Column("full_name")] public string FullName { get; set; } = string.Empty;
        [Column("email")] public string Email { get; set; } = string.Empty;
        [Column("phone")] public string Phone { get; set; } = string.Empty;
        [Column("avatar_url")] public string AvatarUrl { get; set; } = string.Empty;
        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}