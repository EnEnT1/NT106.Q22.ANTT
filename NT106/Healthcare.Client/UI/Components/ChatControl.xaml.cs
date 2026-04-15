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
        public event EventHandler? StartCallRequested;
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
            RenderQuickNotes();
        }

        public void OnCallStarted()
        {
            BtnStartCall.IsEnabled = false;
            TxtStartCallLabel.Text = "Đang trong cuộc gọi";
            BtnStartCall.Background = new SolidColorBrush(HexToColor("#64748B"));
        }

        public void OnCallEnded()
        {
            BtnStartCall.IsEnabled = true;
            TxtStartCallLabel.Text = "Bắt đầu cuộc gọi";
            BtnStartCall.Background = new SolidColorBrush(HexToColor("#0059BB"));
        }

        public (string Diagnosis, List<string> QuickNotes) GetNotes()
            => (DiagnosisBox.Text, new List<string>(_quickNotes));

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

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
                SwitchTab(btn.Tag?.ToString() ?? "chat");
        }

        public void SwitchTab(string tab)
        {
            _activeTab = tab;

            PanelChat.Visibility = tab == "chat" ? Visibility.Visible : Visibility.Collapsed;
            PanelNotes.Visibility = tab == "notes" ? Visibility.Visible : Visibility.Collapsed;

            bool chatActive = tab == "chat";

            TabChat.BorderThickness = new Thickness(0, 0, 0, chatActive ? 2 : 0);
            TabNotes.BorderThickness = new Thickness(0, 0, 0, chatActive ? 0 : 2);

            TabChat.BorderBrush = new SolidColorBrush(chatActive ? HexToColor("#0059BB") : Colors.Transparent);
            TabNotes.BorderBrush = new SolidColorBrush(!chatActive ? HexToColor("#0059BB") : Colors.Transparent);

            TabChat.Foreground = new SolidColorBrush(chatActive ? HexToColor("#0059BB") : HexToColor("#64748B"));
            TabNotes.Foreground = new SolidColorBrush(!chatActive ? HexToColor("#0059BB") : HexToColor("#64748B"));

            TabChat.FontWeight = chatActive ? FontWeights.Bold : FontWeights.Normal;
            TabNotes.FontWeight = !chatActive ? FontWeights.Bold : FontWeights.Normal;
        }

        private void BtnStartCall_Click(object sender, RoutedEventArgs e)
        {
            StartCallRequested?.Invoke(this, EventArgs.Empty);
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

        private async void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            var input = new TextBox
            {
                PlaceholderText = "Nhập ghi chú nhanh...",
                Width = 260,
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = new ContentDialog
            {
                Title = "Thêm ghi chú nhanh",
                Content = input,
                PrimaryButtonText = "Thêm",
                CloseButtonText = "Huỷ",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary
                && !string.IsNullOrWhiteSpace(input.Text))
            {
                _quickNotes.Add(input.Text.Trim());
                RenderQuickNotes();
            }
        }

        private void RenderQuickNotes()
        {
            QuickNotesList.Children.Clear();

            if (_quickNotes.Count == 0)
            {
                QuickNotesList.Children.Add(new TextBlock
                {
                    Text = "Chưa có ghi chú nhanh",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(HexToColor("#94A3B8"))
                });
                return;
            }

            foreach (var note in _quickNotes)
            {
                var row = new Border
                {
                    Background = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(HexToColor("#F1F5F9")),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, 8, 10, 8)
                };

                var sp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8
                };

                sp.Children.Add(new FontIcon
                {
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Glyph = "\uE73E",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(HexToColor("#0059BB")),
                    VerticalAlignment = VerticalAlignment.Center
                });

                sp.Children.Add(new TextBlock
                {
                    Text = note,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(HexToColor("#1E293B")),
                    VerticalAlignment = VerticalAlignment.Center
                });

                row.Child = sp;
                QuickNotesList.Children.Add(row);
            }
        }

        private void BtnAddMedicine_Click(object sender, RoutedEventArgs e)
        {
            // Tạm thời chưa mở picker DB
            // Sau này có thể làm ContentDialog chọn thuốc từ master_medicines
        }

        private void AddMedicineRow(MasterMedicine med)
        {
            var row = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(HexToColor("#E2E8F0")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(new TextBlock
            {
                Text = $"{med.MedicineName} — {med.DefaultDosage}",
                FontSize = 12,
                Foreground = new SolidColorBrush(HexToColor("#1E293B"))
            });

            var removeBtn = new Button
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4),
                Content = new FontIcon
                {
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Glyph = "\uE711",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(HexToColor("#94A3B8"))
                }
            };

            removeBtn.Click += (_, _) => PrescriptionList.Children.Remove(row);

            Grid.SetColumn(removeBtn, 1);
            grid.Children.Add(removeBtn);

            row.Child = grid;
            PrescriptionList.Children.Add(row);
        }

        private async void BtnSaveNotes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var record = new MedicalRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    AppointmentId = _appointmentId,
                    DoctorId = _currentUserId,
                    PatientId = _patientId,
                    Diagnosis = DiagnosisBox.Text ?? string.Empty,
                    PrescriptionImageUrl = string.Empty,
                    AiMedicines = string.Join(" | ", _quickNotes),
                    CreatedAt = DateTime.Now
                };

                await client.From<MedicalRecord>().Insert(record);

                NotesSaved?.Invoke(this, new MedicalNotesSavedEventArgs
                {
                    Diagnosis = DiagnosisBox.Text ?? string.Empty,
                    QuickNotes = new List<string>(_quickNotes)
                });

                await ShowDialogAsync("Đã lưu", "Ghi chú khám bệnh đã được lưu.");
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi", "Không lưu được ghi chú: " + ex.Message);
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