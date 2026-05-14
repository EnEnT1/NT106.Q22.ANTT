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
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Windows.UI;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class ChatControl : UserControl
    {
        public event EventHandler<MedicalNotesSavedEventArgs>? NotesSaved;

        private string _appointmentId = string.Empty;
        private string _currentUserId = string.Empty;
        private string _patientId = string.Empty;

        private RealtimeChannel? _chatChannel;

        private readonly List<string> _quickNotes = new();

        private readonly HttpClient _httpClient = new HttpClient
        {
            // Nhớ sửa port này theo Healthcare.Server/Properties/launchSettings.json
            BaseAddress = new Uri("https://localhost:5001/")
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

        public async Task InitializeAsync(string appointmentId, string currentUserId, string patientId)
        {
            _appointmentId = appointmentId;
            _currentUserId = currentUserId;
            _patientId = patientId;

            await LoadChatHistoryAsync();
            await SubscribeRealtimeAsync();
        }

        public void Cleanup()
        {
            _chatChannel?.Unsubscribe();
        }

        private async Task LoadChatHistoryAsync()
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var response = await client
                    .From<ChatMessageItem>()
                    .Where(m => m.AppointmentId == _appointmentId)
                    .Order("created_at", Postgrest.Constants.Ordering.Ascending)
                    .Get();

                MessageList.Children.Clear();

                foreach (var msg in response.Models)
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

        private async Task SubscribeRealtimeAsync()
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                _chatChannel = client.Realtime.Channel("chat_" + _appointmentId, "public", "chat_messages");

                _chatChannel.AddPostgresChangeHandler(
                    PostgresChangesOptions.ListenType.Inserts,
                    (sender, change) =>
                    {
                        var msg = change.Model<ChatMessageItem>();

                        if (msg != null &&
                            msg.AppointmentId == _appointmentId &&
                            msg.SenderId != _currentUserId &&
                            msg.SenderId != "AI")
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                AppendBubble(msg, false);
                            });
                        }
                    });

                await _chatChannel.Subscribe();
            }
            catch
            {
               
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
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            var text = ChatInput.Text.Trim();

            if (string.IsNullOrEmpty(text))
                return;

            ChatInput.Text = string.Empty;
            BtnSend.IsEnabled = false;

            var msg = new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                AppointmentId = _appointmentId,
                SenderId = _currentUserId,
                Content = text,
                CreatedAt = DateTime.UtcNow
            };

            AppendBubble(msg, true);

            try
            {
                var client = SupabaseManager.Instance.Client;
                await client.From<ChatMessageItem>().Insert(msg);

                await SendMessageToAiAsync(text);
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi", "Không gửi được tin nhắn: " + ex.Message);
            }
            finally
            {
                BtnSend.IsEnabled = true;
            }
        }

        private async Task SendMessageToAiAsync(string userMessage)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/ai/chat", new
                {
                    message = userMessage
                });

                if (!response.IsSuccessStatusCode)
                {
                    AppendAiMessage("AI hiện chưa phản hồi được. Vui lòng thử lại sau.");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<AiChatResponse>();

                AppendAiMessage(result?.Reply ?? "AI chưa có câu trả lời phù hợp.");
            }
            catch (Exception ex)
            {
                AppendAiMessage("Không kết nối được AI: " + ex.Message);
            }
        }

        private void AppendAiMessage(string content)
        {
            var msg = new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                AppointmentId = _appointmentId,
                SenderId = "AI",
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            MessageList.Children.Add(BuildBubble(msg, false, "AI"));
            ScrollToBottom();
        }

        private void AppendBubble(ChatMessageItem msg, bool isSelf)
        {
            MessageList.Children.Add(BuildBubble(msg, isSelf));
            ScrollToBottom();
        }

        private UIElement BuildBubble(ChatMessageItem msg, bool isSelf, string? displayName = null)
        {
            var wrapper = new StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = isSelf ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 280,
                Margin = new Thickness(0, 4, 0, 4)
            };

            var time = msg.CreatedAt.ToLocalTime();

            wrapper.Children.Add(new TextBlock
            {
                Text = isSelf
                    ? $"Bạn • {time:HH:mm}"
                    : $"{displayName ?? "Bác sĩ/Bệnh nhân"} • {time:HH:mm}",
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
                    Text = msg.Content ?? string.Empty,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(isSelf ? Colors.White : HexToColor("#1E293B"))
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
                Convert.ToByte(hex.Substring(4, 2), 16)
            );
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

    public class AiChatResponse
    {
        public bool Success { get; set; }

        public string Reply { get; set; } = string.Empty;
    }
}