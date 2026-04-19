using Healthcare.Client.Models.Core;
using Healthcare.Client.SupabaseIntegration;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Postgrest.Attributes;
using Postgrest.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class ChatControl : UserControl
    {
        public event EventHandler<MedicalNotesSavedEventArgs>? NotesSaved;

        private string _appointmentId = string.Empty;
        private string _currentUserId = string.Empty;
        private string _patientId = string.Empty;

        private string _activeTab = "chat";
        private readonly List<string> _quickNotes = new();

        public ChatControl()
        {
            this.InitializeComponent();
            this.Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Cleanup();

        public async Task InitializeAsync(string appointmentId, string currentUserId, string patientId)
        {
            _appointmentId = appointmentId;
            _currentUserId = currentUserId;
            _patientId = patientId;

            await LoadChatHistoryAsync();
            SubscribeRealtime();
        }

        public void Cleanup()
        {
            // TODO: unsubscribe realtime nếu sau này có channel thật
        }

        private async Task LoadChatHistoryAsync()
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var response = await client
                    .From<ChatMessageItem>()
                    .Get();

                var messages = response.Models
                    .Where(m => m.AppointmentId == _appointmentId)
                    .OrderBy(m => m.CreatedAt)
                    .ToList();

                MessageList.Children.Clear();

                foreach (var msg in messages)
                {
                    bool isSelf = msg.SenderId == _currentUserId;
                    MessageList.Children.Add(BuildBubble(msg, isSelf));
                }

                ScrollToBottom();
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi", "Không tải được lịch sử chat: " + ex.Message);
            }
        }

        private void SubscribeRealtime()
        {
            // TODO:
            // Sau này nối Supabase realtime ở đây.
            // Hiện tại để trống trước để app chạy ổn định.
        }


        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void ChatInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            var text = ChatInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            ChatInput.Text = string.Empty;

            var msg = new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                AppointmentId = _appointmentId,
                SenderId = _currentUserId,
                Content = text,
                CreatedAt = DateTime.Now
            };

            AppendBubble(msg, true);

            try
            {
                var client = SupabaseManager.Instance.Client;
                await client.From<ChatMessageItem>().Insert(msg);
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi", "Không gửi được tin nhắn: " + ex.Message);
            }
        }

        private void AppendBubble(ChatMessageItem msg, bool isSelf)
        {
            MessageList.Children.Add(BuildBubble(msg, isSelf));
            ScrollToBottom();
        }

        private UIElement BuildBubble(ChatMessageItem msg, bool isSelf)
        {
            var wrapper = new StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = isSelf ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 240,
                Margin = new Thickness(0, 4, 0, 4)
            };

            wrapper.Children.Add(new TextBlock
            {
                Text = isSelf
                    ? $"Bạn • {msg.CreatedAt:HH:mm}"
                    : $"Bệnh nhân • {msg.CreatedAt:HH:mm}",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(HexToColor("#64748B")),
                HorizontalAlignment = isSelf
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left
            });

            wrapper.Children.Add(new Border
            {
                Background = new SolidColorBrush(
                    isSelf ? HexToColor("#0059BB") : HexToColor("#E7E8E9")),
                CornerRadius = new CornerRadius(
                    isSelf ? 12 : 2,
                    isSelf ? 2 : 12,
                    12, 12),
                Padding = new Thickness(12, 8, 12, 8),
                Child = new TextBlock
                {
                    Text = msg.Content ?? string.Empty,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(
                        isSelf ? Colors.White : HexToColor("#1E293B"))
                }
            });

            return wrapper;
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.UpdateLayout();
            ChatScrollViewer.ChangeView(null, ChatScrollViewer.ScrollableHeight, null);
        }

        private async Task ShowDialogAsync(string title, string message)
        {
            var d = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            await d.ShowAsync();
        }

        private static Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
    }

    [Table("chat_messages")]
    public class ChatMessageItem : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("appointment_id")]
        public string AppointmentId { get; set; } = string.Empty;

        [Column("sender_id")]
        public string SenderId { get; set; } = string.Empty;

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class MedicalNotesSavedEventArgs : EventArgs
    {
        public string Diagnosis { get; set; } = string.Empty;
        public List<string> QuickNotes { get; set; } = new();
    }
}