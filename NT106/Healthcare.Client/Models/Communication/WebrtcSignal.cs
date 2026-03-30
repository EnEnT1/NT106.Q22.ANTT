using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;

namespace Healthcare.Client.Models.Communication
{
    [Table("webrtc_signals")]
    public class WebrtcSignal : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Column("room_code")] public string RoomCode { get; set; } = string.Empty;
        [Column("sender_id")] public string SenderId { get; set; } = string.Empty;
        [Column("signal_type")] public string SignalType { get; set; } = string.Empty; 
        [Column("payload")] public string Payload { get; set; } = string.Empty;
        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}