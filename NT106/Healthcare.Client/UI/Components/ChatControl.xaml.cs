using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
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
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Windows.UI;
using System.Linq;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class ChatControl : UserControl
    {
        public event EventHandler<MedicalNotesSavedEventArgs>? NotesSaved;

        private string _appointmentId = string.Empty;
        private string _currentUserId = string.Empty;
        private string _patientId = string.Empty;
        private string _otherUserId = string.Empty;
        private string _otherUserName = string.Empty;
        private bool _isDoctorUser = false;
        private bool _isSending = false;

        private VideoCallControl? _videoCall;
        private readonly HashSet<string> _renderedMessageIds = new();

        private readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri(Healthcare.Client.APIClient.BaseHttpClient.ServerBaseUrl)
        };

        public ChatControl()
        {
            this.InitializeComponent();
            this.Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Cleanup();
        }

        public async Task InitializeAsync(string appointmentId, string currentUserId, string patientId, VideoCallControl videoCall)
        {
            _appointmentId = appointmentId;
            _currentUserId = currentUserId;
            _patientId = patientId;
            _videoCall = videoCall;

            _otherUserId = string.Empty;
            _otherUserName = string.Empty;
            _renderedMessageIds.Clear();

            _isDoctorUser = _currentUserId != _patientId;

            // Subscribe to Daily.co chat messages
            if (_videoCall != null)
            {
                _videoCall.ChatMessageReceived += VideoCall_ChatMessageReceived;
            }

            if (_isDoctorUser)
            {
                _otherUserId = _patientId;
            }
            else
            {
                try
                {
                    var client = SupabaseManager.Instance.Client;
                    var apptResult = await client.From<Appointment>().Where(a => a.Id == _appointmentId).Single();
                    if (apptResult != null)
                    {
                        _otherUserId = apptResult.DoctorId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChatControl] Failed to fetch doctor ID: {ex.Message}");
                }
            }

            if (!string.IsNullOrEmpty(_otherUserId))
            {
                try
                {
                    var client = SupabaseManager.Instance.Client;
                    var userRes = await client.From<User>().Where(u => u.Id == _otherUserId).Single();
                    if (userRes != null && !string.IsNullOrEmpty(userRes.FullName))
                    {
                        _otherUserName = userRes.FullName;
                        if (_otherUserName.StartsWith("ệnh nhân", StringComparison.OrdinalIgnoreCase))
                        {
                            _otherUserName = "B" + _otherUserName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChatControl] Failed to fetch other user name: {ex.Message}");
                }
            }

            await LoadChatHistoryAsync();
        }

        public void Cleanup()
        {
            if (_videoCall != null)
            {
                _videoCall.ChatMessageReceived -= VideoCall_ChatMessageReceived;
                _videoCall = null;
            }
        }

        private void VideoCall_ChatMessageReceived(object? sender, ChatMessageEventArgs e)
        {
            // Tránh render lại tin nhắn chính mình gửi (vì e.IsLocal = true khi ta gửi trong Daily)
            if (e.IsLocal) return;

            DispatcherQueue.TryEnqueue(() =>
            {
                var msg = new ChatMessageItem
                {
                    Id = Guid.NewGuid().ToString(),
                    SenderId = _otherUserId,
                    ReceiverId = _currentUserId,
                    MessageText = e.Text,
                    CreatedAt = DateTime.UtcNow
                };
                AppendBubble(msg, false);
            });
        }

        private async Task LoadChatHistoryAsync()
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var response = await client
                    .From<ChatMessageItem>()
                    .Filter("sender_id", Postgrest.Constants.Operator.In, new List<string> { _currentUserId, _otherUserId })
                    .Filter("receiver_id", Postgrest.Constants.Operator.In, new List<string> { _currentUserId, _otherUserId })
                    .Order("created_at", Postgrest.Constants.Ordering.Ascending)
                    .Get();

                MessageList.Children.Clear();
                _renderedMessageIds.Clear();

                foreach (var msg in response.Models)
                {
                    bool isSelf = msg.SenderId == _currentUserId;
                    AppendBubble(msg, isSelf);
                }

                ScrollToBottom();
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi", "Không tải được lịch sử chat: " + ex.Message);
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void ChatInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            if (_isSending)
                return;

            var text = ChatInput.Text.Trim();

            if (string.IsNullOrEmpty(text))
                return;

            if (string.IsNullOrWhiteSpace(_otherUserId))
            {
                await ShowDialogAsync("Lỗi", "Không xác định được người nhận tin nhắn.");
                return;
            }

            _isSending = true;
            ChatInput.Text = string.Empty;
            BtnSend.IsEnabled = false;

            var msg = new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = _currentUserId,
                ReceiverId = _otherUserId,
                MessageText = text,
                IsRead = false,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };

            // 1. Hiển thị lên giao diện cục bộ ngay
            AppendBubble(msg, true);

            try
            {
                // 2. Gửi qua Daily.co peer-to-peer (Instant Delivery)
                if (_videoCall != null)
                {
                    await _videoCall.SendChatMessageAsync(text);
                }

                // 3. Gửi lên Database lưu lịch sử
                bool sentDirectly = false;
                try
                {
                    var supabase = SupabaseManager.Instance.Client;
                    await supabase.From<ChatMessageItem>().Insert(msg);
                    sentDirectly = true;
                }
                catch (Exception directEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Chat Direct] DB Save Failed: {directEx.Message}");
                }

                if (!sentDirectly)
                {
                    await _httpClient.PostAsJsonAsync("api/chat/send", new
                    {
                        Id = msg.Id,
                        SenderId = msg.SenderId,
                        ReceiverId = msg.ReceiverId,
                        MessageText = msg.MessageText,
                        IsRead = msg.IsRead,
                        CreatedAt = msg.CreatedAt
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatControl] Save message err: {ex.Message}");
            }
            finally
            {
                BtnSend.IsEnabled = true;
                _isSending = false;
            }
        }

        private void AppendBubble(ChatMessageItem msg, bool isSelf)
        {
            if (!string.IsNullOrWhiteSpace(msg.Id) && !_renderedMessageIds.Add(msg.Id))
                return;

            MessageList.Children.Add(BuildBubble(msg, isSelf));
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.UpdateLayout();
            ChatScrollViewer.ChangeView(null, ChatScrollViewer.ScrollableHeight, null);
        }

        private UIElement BuildBubble(ChatMessageItem msg, bool isSelf)
        {
            var wrapper = new StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = isSelf ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 280,
                Margin = new Thickness(0, 4, 0, 4)
            };

            var time = msg.CreatedAt.ToLocalTime();

            string defaultOtherName = _isDoctorUser ? "Bệnh nhân" : "Bác sĩ";
            string nameToShow = !string.IsNullOrEmpty(_otherUserName) ? _otherUserName : defaultOtherName;

            wrapper.Children.Add(new TextBlock
            {
                Text = isSelf
                    ? $"Bạn • {time:HH:mm}"
                    : $"{nameToShow} • {time:HH:mm}",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(HexToColor("#64748B")),
                HorizontalAlignment = isSelf ? HorizontalAlignment.Right : HorizontalAlignment.Left
            });

            wrapper.Children.Add(new Border
            {
                Background = new SolidColorBrush(isSelf ? HexToColor("#0059BB") : HexToColor("#E7E8E9")),
                CornerRadius = new CornerRadius(isSelf ? 12 : 2, isSelf ? 2 : 12, 12, 12),
                Padding = new Thickness(12, 8, 12, 8),
                Child = new TextBlock
                {
                    Text = msg.MessageText ?? string.Empty,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(isSelf ? Colors.White : HexToColor("#1E293B"))
                }
            });

            return wrapper;
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
                Convert.ToByte(hex.Substring(4, 2), 16)
            );
        }
    }

    [Table("chat_messages")]
    public class ChatMessageItem : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("sender_id")]
        public string SenderId { get; set; } = string.Empty;

        [Column("receiver_id")]
        public string ReceiverId { get; set; } = string.Empty;

        [Column("message_text")]
        public string MessageText { get; set; } = string.Empty;

        [Column("is_read")]
        public bool? IsRead { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class MedicalNotesSavedEventArgs : EventArgs
    {
        public string Diagnosis { get; set; } = string.Empty;

        public List<string> QuickNotes { get; set; } = new();
    }
}
