using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Healthcare.Client.Models.Communication
{
    [Table("webrtc_signals")]
    public class WebrtcSignal : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("room_code")]
        public string RoomCode { get; set; }

        [Column("sender_id")]
        public string SenderId { get; set; }

        [Column("signal_type")]
        public string SignalType { get; set; }

        [Column("payload")]
        public string Payload { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}