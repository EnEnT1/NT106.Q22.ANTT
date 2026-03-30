using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Communication
{
    [Table("chat_messages")]
    public class ChatMessage : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("sender_id")] public string SenderId { get; set; } = string.Empty;
        [Column("receiver_id")] public string ReceiverId { get; set; } = string.Empty;
        [Column("message_text")] public string MessageText { get; set; } = string.Empty;
        [Column("is_read")] public bool IsRead { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}