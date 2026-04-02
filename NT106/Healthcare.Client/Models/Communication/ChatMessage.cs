using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Communication
{
    [Table("chat_messages")]
    public class ChatMessage : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }

        [Column("sender_id")] public string SenderId { get; set; }

        [Column("receiver_id")] public string ReceiverId { get; set; }

        [Column("message_text")] public string MessageText { get; set; }

        [Column("is_read")] public bool? IsRead { get; set; }

        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}