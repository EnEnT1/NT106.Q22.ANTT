using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Identity
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id", true)] public string Id { get; set; }

        [Column("role")] public string Role { get; set; }

        [Column("full_name")] public string FullName { get; set; }

        [Column("email")] public string Email { get; set; }

        [Column("phone")] public string Phone { get; set; }

        [Column("avatar_url")] public string AvatarUrl { get; set; }

        [Column("created_at")] public DateTime CreatedAt { get; set; }

        [Column("public_key")] public string PublicKey { get; set; }
        [Column("encrypted_private_key")] public string EncryptedPrivateKey { get; set; }
    }
}