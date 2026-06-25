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
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.UI;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using System.Linq;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class ChatControl : UserControl
    {
        public event EventHandler<MedicalNotesSavedEventArgs>? NotesSaved;

        private string _appointmentId = string.Empty;
        private string _currentUserId = string.Empty;
        private string _patientId = string.Empty;
        private string _otherUserName = string.Empty;
        private bool _isDoctorUser = false;

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

            _isDoctorUser = _currentUserId != _patientId;
            TextTabDoctor.Text = _isDoctorUser ? "Bệnh nhân" : "Bác sĩ";

            string otherUserId = string.Empty;
            if (_isDoctorUser)
            {
                otherUserId = _patientId;
            }
            else
            {
                try
                {
                    var client = SupabaseManager.Instance.Client;
                    var apptResult = await client.From<Appointment>().Where(a => a.Id == _appointmentId).Single();
                    if (apptResult != null)
                    {
                        otherUserId = apptResult.DoctorId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChatControl] Failed to fetch doctor ID: {ex.Message}");
                }
            }

            if (!string.IsNullOrEmpty(otherUserId))
            {
                try
                {
                    var client = SupabaseManager.Instance.Client;
                    var userRes = await client.From<User>().Where(u => u.Id == otherUserId).Single();
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
            await SubscribeRealtimeAsync();
            InitializeAiChat();
        }

        public void Cleanup()
        {
            _chatChannel?.Unsubscribe();
        }

        private void InitializeAiChat()
        {
            AiMessageList.Children.Clear();
            var welcome = new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = "AI",
                ReceiverId = _currentUserId,
                MessageText = "Xin chào! Tôi là Trợ lý Y tế AI. Tôi có thể hỗ trợ giải đáp các câu hỏi cơ bản về sức khỏe và triệu chứng của bạn.",
                CreatedAt = DateTime.UtcNow
            };
            AiMessageList.Children.Add(BuildBubble(welcome, false, "Trợ lý AI"));
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

                DoctorMessageList.Children.Clear();

                foreach (var msg in response.Models)
                {
                    bool isSelf = msg.SenderId == _currentUserId;
                    DoctorMessageList.Children.Add(BuildBubble(msg, isSelf));
                }

                ScrollDoctorToBottom();
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
                try
                {
                    await client.Realtime.ConnectAsync();
                }
                catch { }

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
                                AppendDoctorBubble(msg, false);
                            });
                        }
                    });

                await _chatChannel.Subscribe();
            }
            catch
            {
               
            }
        }

        // Tab Switching Logic
        private void BtnTabDoctor_Click(object sender, RoutedEventArgs e)
        {
            PanelDoctorChat.Visibility = Visibility.Visible;
            PanelAiChat.Visibility = Visibility.Collapsed;

            // Highlight Doctor Tab
            BtnTabDoctor.BorderBrush = new SolidColorBrush(HexToColor("#0059BB"));
            BtnTabDoctor.BorderThickness = new Thickness(0, 0, 0, 2);
            IconTabDoctor.Foreground = new SolidColorBrush(HexToColor("#0059BB"));
            TextTabDoctor.Foreground = new SolidColorBrush(HexToColor("#0059BB"));
            TextTabDoctor.FontWeight = FontWeights.Bold;

            // Unhighlight AI Tab
            BtnTabAi.BorderBrush = new SolidColorBrush(Colors.Transparent);
            BtnTabAi.BorderThickness = new Thickness(0);
            IconTabAi.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            TextTabAi.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            TextTabAi.FontWeight = FontWeights.Medium;
        }

        private void BtnTabAi_Click(object sender, RoutedEventArgs e)
        {
            PanelDoctorChat.Visibility = Visibility.Collapsed;
            PanelAiChat.Visibility = Visibility.Visible;

            // Highlight AI Tab
            BtnTabAi.BorderBrush = new SolidColorBrush(HexToColor("#10B981"));
            BtnTabAi.BorderThickness = new Thickness(0, 0, 0, 2);
            IconTabAi.Foreground = new SolidColorBrush(HexToColor("#10B981"));
            TextTabAi.Foreground = new SolidColorBrush(HexToColor("#10B981"));
            TextTabAi.FontWeight = FontWeights.Bold;

            // Unhighlight Doctor Tab
            BtnTabDoctor.BorderBrush = new SolidColorBrush(Colors.Transparent);
            BtnTabDoctor.BorderThickness = new Thickness(0);
            IconTabDoctor.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            TextTabDoctor.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            TextTabDoctor.FontWeight = FontWeights.Medium;
        }

        // Send Doctor Message
        private async void BtnSendDoctor_Click(object sender, RoutedEventArgs e)
        {
            await SendDoctorMessageAsync();
        }

        private async void DoctorChatInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await SendDoctorMessageAsync();
            }
        }

        private async Task SendDoctorMessageAsync()
        {
            var text = DoctorChatInput.Text.Trim();

            if (string.IsNullOrEmpty(text))
                return;

            DoctorChatInput.Text = string.Empty;
            BtnSendDoctor.IsEnabled = false;

            var msg = new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = _currentUserId,
                ReceiverId = _patientId,
                MessageText = text,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            AppendDoctorBubble(msg, true);

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

                if (!sentViaHttp)
                {
                    var supabase = SupabaseManager.Instance.Client;
                    await supabase.From<ChatMessageItem>().Insert(msg);
                }
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi", "Không gửi được tin nhắn: " + ex.Message);
            }
            finally
            {
                BtnSendDoctor.IsEnabled = true;
            }
        }

        // Send AI Message
        private async void BtnSendAi_Click(object sender, RoutedEventArgs e)
        {
            await SendAiMessageAsync();
        }

        private async void AiChatInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await SendAiMessageAsync();
            }
        }

        private async Task SendAiMessageAsync()
        {
            var text = AiChatInput.Text.Trim();

            if (string.IsNullOrEmpty(text))
                return;

            AiChatInput.Text = string.Empty;
            BtnSendAi.IsEnabled = false;

            var msg = new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = _currentUserId,
                ReceiverId = "AI",
                MessageText = text,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            AppendAiBubble(msg, true);

            try
            {
                await SendMessageToAiAsync(text);
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi", "Không kết nối được trợ lý AI: " + ex.Message);
            }
            finally
            {
                BtnSendAi.IsEnabled = true;
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
                    AppendAiResponseBubble("AI hiện chưa phản hồi được. Vui lòng thử lại sau.");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<AiChatResponse>();

                AppendAiResponseBubble(result?.Reply ?? "AI chưa có câu trả lời phù hợp.");
            }
            catch (Exception ex)
            {
                AppendAiResponseBubble("Không kết nối được với hệ thống hỗ trợ. Vui lòng kiểm tra lại kết nối mạng.");
                System.Diagnostics.Debug.WriteLine($"[Ai Chat Error]: {ex.Message}");
            }
        }

        private void AppendDoctorBubble(ChatMessageItem msg, bool isSelf)
        {
            DoctorMessageList.Children.Add(BuildBubble(msg, isSelf));
            ScrollDoctorToBottom();
        }

        private void AppendAiBubble(ChatMessageItem msg, bool isSelf)
        {
            AiMessageList.Children.Add(BuildBubble(msg, isSelf, isSelf ? "Bạn" : "Trợ lý AI"));
            ScrollAiToBottom();
        }

        private void AppendAiResponseBubble(string content)
        {
            var msg = new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = "AI",
                ReceiverId = _currentUserId,
                MessageText = content,
                CreatedAt = DateTime.UtcNow
            };

            AiMessageList.Children.Add(BuildBubble(msg, false, "Trợ lý AI"));
            ScrollAiToBottom();
        }

        private void ScrollDoctorToBottom()
        {
            DoctorScrollViewer.UpdateLayout();
            DoctorScrollViewer.ChangeView(null, DoctorScrollViewer.ScrollableHeight, null);
        }

        private void ScrollAiToBottom()
        {
            AiScrollViewer.UpdateLayout();
            AiScrollViewer.ChangeView(null, AiScrollViewer.ScrollableHeight, null);
        }

        private UIElement BuildBubble(ChatMessageItem msg, bool isSelf, string? displayName = null)
        {
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

            var time = ParseDateTimeToLocal(msg.CreatedAt);

            string defaultOtherName = _isDoctorUser ? "Bệnh nhân" : "Bác sĩ";
            string nameToShow = !string.IsNullOrEmpty(_otherUserName) ? _otherUserName : defaultOtherName;

            wrapper.Children.Add(new TextBlock
            {
                Text = isSelf
                    ? $"Bạn • {time:HH:mm}"
                    : $"{displayName ?? nameToShow} • {time:HH:mm}",
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

        private UIElement BuildTtsBubble(string text, DateTime createdAt, bool isSelf)
        {
            var time = ParseDateTimeToLocal(createdAt);

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

            var btn = new Button
            {
                Background = new SolidColorBrush(isSelf ? HexToColor("#0F172A") : HexToColor("#0059BB")),
                Foreground = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(isSelf ? 12 : 2, isSelf ? 2 : 12, 12, 12),
                Padding = new Thickness(14, 9, 14, 9),
                BorderThickness = new Thickness(0),
                IsEnabled = !isSelf
            };

            var btnContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            btnContent.Children.Add(new FontIcon
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = isSelf ? "\uE8F4" : "\uE768",
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
                string ttsText = text;
                btn.Click += (s, e) => PlayTtsAsync(ttsText);
            }

            wrapper.Children.Add(btn);
            return wrapper;
        }

        private async void PlayTtsAsync(string text)
        {
            try
            {
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

        private static DateTime ParseDateTimeToLocal(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Unspecified)
            {
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            return dt.ToLocalTime();
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