using Healthcare.Client.Models.Communication;
using Healthcare.Client.Cryptography;
using Healthcare.Client.Models.Identity; 
using System;
using System.Threading.Tasks;
using Supabase.Realtime;

namespace Healthcare.Client.SupabaseIntegration
{
    public class SupabaseRealtimeService
    {
        private readonly Supabase.Client _client;
        private RealtimeChannel _chatChannel;

        public event EventHandler<ChatMessage> OnMessageReceived;

        public SupabaseRealtimeService(Supabase.Client client)
        {
            _client = client;
        }

        //1. Hàm gửi tin nhắn mã hóa -> gửi Supabase
        public async Task<bool> SendMessageAsync(string senderId, string receiverId, string rawMessage)
        {
            try
            {
                //1.1 Lấy Public Key của người nhận từ Supabase
                var receiverQuery = await _client.From<User>().Where(x => x.Id == receiverId).Single();
                if (receiverQuery == null || string.IsNullOrEmpty(receiverQuery.PublicKey))
                    return false; 

                string receiverPublicKey = receiverQuery.PublicKey;

                //1.2 Mã hóa tin nhắn bằng Public Key của receiver
                string encryptedContent = RSAManager.Encrypt(rawMessage, receiverPublicKey);

                //1.3 Gửi tin nhắn mã hóa lên Supabase
                var msg = new ChatMessage
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    MessageText = encryptedContent
                };

                var res = await _client.From<ChatMessage>().Insert(msg);
                return res.ResponseMessage.IsSuccessStatusCode;
            }
            catch (Exception) { return false; }
        }

        //2. Hàm sẽ được gọi khi vào trang Chat, để bắt đầu nghe tin nhắn mới
        public async Task StartListeningForMessages(string myUserId, string myPrivateKey)
        {
            Console.WriteLine("[WebSockets] Lang nghe tin nhan...");

            // 2.1 Tạo kênh và đăng ký theo dõi trực tiếp bảng "chat_messages" ở schema "public"
            _chatChannel = _client.Realtime.Channel("realtime", "public", "chat_messages");

            // 2.2 Thực hiện 
            _chatChannel.AddPostgresChangeHandler(
                Supabase.Realtime.PostgresChanges.PostgresChangesOptions.ListenType.Inserts,
                (sender, change) =>
                {
                    var newMsg = change.Model<ChatMessage>();
                    if (newMsg != null && newMsg.ReceiverId == myUserId)
                    {
                        newMsg.MessageText = RSAManager.Decrypt(newMsg.MessageText, myPrivateKey);
                        OnMessageReceived?.Invoke(this, newMsg);
                    }
                });
            await _chatChannel.Subscribe();
        }
    }
}