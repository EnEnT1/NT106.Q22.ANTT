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
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
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

        private MediaPlayer? _ttsPlayer;

        private readonly List<string> _quickNotes = new();

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
                    .Filter("sender_id", Postgrest.Constants.Operator.In, new List<string> { _currentUserId, _patientId })
                    .Filter("receiver_id", Postgrest.Constants.Operator.In, new List<string> { _currentUserId, _patientId })
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
                            msg.SenderId == _patientId &&
                            msg.ReceiverId == _currentUserId)
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
                SenderId = _currentUserId,
                ReceiverId = _patientId,
                MessageText = text,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            AppendBubble(msg, true);

            try
            {
                bool sentViaHttp = false;
                try
                {
                    var response = await _httpClient.PostAsJsonAsync("api/chat/send", new
                    {
                        Id       = msg.Id,
                        SenderId = msg.SenderId,
                        ReceiverId = msg.ReceiverId,
                        MessageText = msg.MessageText,
                        IsRead   = msg.IsRead,
                        CreatedAt = msg.CreatedAt
                    });
                    sentViaHttp = response.IsSuccessStatusCode;
                }
                catch (Exception httpEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Chat HTTP] Failed: {httpEx.Message}");
                }

                // Fallback: insert directly into Supabase when HTTP server is unavailable
                if (!sentViaHttp)
                {
                    var supabase = SupabaseManager.Instance.Client;
                    await supabase.From<ChatMessageItem>().Insert(msg);
                }

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
                AppendAiMessage("Không kết nối được với hệ thống hỗ trợ. Vui lòng kiểm tra lại kết nối mạng.");
                System.Diagnostics.Debug.WriteLine($"[Ai Chat Error]: {ex.Message}");
            }
        }

        private void AppendAiMessage(string content)
        {
            var msg = new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = "AI",
                ReceiverId = _currentUserId,
                MessageText = content,
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
            // TTS messages get a special play-button bubble
            if (msg.MessageText?.StartsWith("[TTS] ") == true)
            {
                string ttsText = msg.MessageText.Substring(6); // strip "[TTS] "
                return BuildTtsBubble(ttsText, msg.CreatedAt, isSelf);
            }

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
                    Text = msg.MessageText ?? string.Empty,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(isSelf ? Colors.White : HexToColor("#1E293B"))
                }
            });

            return wrapper;
        }

        /// <summary>
        /// Builds a TTS voice-message bubble with a play button.
        /// isSelf = doctor sent (shows "Đã gửi"), !isSelf = patient receives (shows play button).
        /// </summary>
        private UIElement BuildTtsBubble(string text, DateTime createdAt, bool isSelf)
        {
            var time = createdAt.ToLocalTime();

            var wrapper = new StackPanel
            {
                Spacing = 4,
                HorizontalAlignment = isSelf ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 260,
                Margin = new Thickness(0, 4, 0, 4)
            };

            wrapper.Children.Add(new TextBlock
            {
                Text = isSelf ? $"Giọng nói bác sĩ • {time:HH:mm}" : $"Bác sĩ • {time:HH:mm}",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(HexToColor("#64748B")),
                HorizontalAlignment = isSelf ? HorizontalAlignment.Right : HorizontalAlignment.Left
            });

            // Play button (only interactive on patient side)
            var btn = new Button
            {
                Background = new SolidColorBrush(isSelf ? HexToColor("#0F172A") : HexToColor("#0059BB")),
                Foreground = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(isSelf ? 12 : 2, isSelf ? 2 : 12, 12, 12),
                Padding = new Thickness(14, 9, 14, 9),
                BorderThickness = new Thickness(0),
                IsEnabled = !isSelf  // doctor side is just an indicator, not clickable
            };

            var btnContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            btnContent.Children.Add(new FontIcon
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = isSelf ? "\uE8F4" : "\uE768",   // sent icon / speaker
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.White)
            });
            btnContent.Children.Add(new TextBlock
            {
                Text = isSelf ? "Đã gửi giọng nói" : "▶ Nghe giọng bác sĩ",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White)
            });
            btn.Content = btnContent;

            if (!isSelf)
            {
                string ttsText = text; // capture for closure
                btn.Click += (s, e) => PlayTtsAsync(ttsText);
            }

            wrapper.Children.Add(btn);
            return wrapper;
        }

        private async void PlayTtsAsync(string text)
        {
            try
            {
                // Stop any currently playing audio
                _ttsPlayer?.Pause();
                _ttsPlayer = null;

                using var synth = new SpeechSynthesizer();
                var stream = await synth.SynthesizeTextToStreamAsync(text);

                _ttsPlayer = new MediaPlayer();
                _ttsPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
                _ttsPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TTS] Playback error: {ex.Message}");
            }
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

    public class AiChatResponse
    {
        public bool Success { get; set; }

        public string Reply { get; set; } = string.Empty;
    }
}