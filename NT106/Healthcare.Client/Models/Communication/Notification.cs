using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Communication
{
    [Table("notifications")]
    public class Notification : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("body")]
        public string Body { get; set; }

        [Column("is_read")]
        public bool? IsRead { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}