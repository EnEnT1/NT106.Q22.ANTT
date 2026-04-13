// ============================================================
//  ChatControl.xaml.cs
//  Healthcare.Client — UI/Components/ChatControl
//
//  UserControl tự quản lý:
//    - Load lịch sử chat từ Supabase
//    - Subscribe Realtime nhận message mới
//    - Gửi message (có stub Encrypt)
//    - Quick notes (lưu vào memory, flush khi Finish)
//    - Tab Chat ↔ Ghi chú
//    - Ghi chú y khoa + đơn thuốc (Notes panel)
//
//  Cách dùng từ ExaminationPage:
//    await Chat.InitializeAsync(appointmentId, currentUserId, patientId);
//
//  Events ra ngoài:
//    Chat.StartCallRequested += OnStartCallRequested;
// ============================================================

using Healthcare.Client.Models.Communication;
using Healthcare.Client.Models.Core;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Helpers;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class ChatControl : UserControl
    {
        // ─────────────────────────────────────────────────────────
        //  Events
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Phát ra khi user bấm "Bắt đầu cuộc gọi".
        /// ExaminationPage lắng nghe và gọi VideoCall.StartCallAsync().
        /// </summary>
        public event EventHandler? StartCallRequested;

        /// <summary>
        /// Phát ra khi bác sĩ lưu ghi chú / đơn thuốc.
        /// </summary>
        public event EventHandler<MedicalNotesSavedEventArgs>? NotesSaved;

        // ─────────────────────────────────────────────────────────
        //  Dependencies
        // ─────────────────────────────────────────────────────────

        // TODO — DB:
        // private readonly SupabaseDbService       _db       = SupabaseManager.Instance.Db;
        // TODO — Realtime:
        // private readonly SupabaseRealtimeService _realtime = SupabaseManager.Instance.Realtime;

        // ─────────────────────────────────────────────────────────
        //  State
        // ─────────────────────────────────────────────────────────

        private string _appointmentId = string.Empty;
        private string _currentUserId = string.Empty;  // doctor userId
        private string _patientId = string.Empty;

        private string _activeTab = "chat";

        // Quick notes buffer — flush to MedicalRecord khi Finish
        private readonly List<string> _quickNotes = new();

        // ─────────────────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────────────────

        public ChatControl()
        {
            this.InitializeComponent();
            this.Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Cleanup();

        // ─────────────────────────────────────────────────────────
        //  PUBLIC API
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Gọi từ ExaminationPage.OnNavigatedTo sau khi có appointmentId.
        /// </summary>
        public async Task InitializeAsync(string appointmentId, string currentUserId, string patientId)
        {
            _appointmentId = appointmentId;
            _currentUserId = currentUserId;
            _patientId = patientId;

            await LoadChatHistoryAsync();
            SubscribeRealtime();
            RenderQuickNotes();
        }

        /// <summary>
        /// Gọi từ ExaminationPage khi cuộc gọi đã bắt đầu —
        /// đổi label nút thành "Đang trong cuộc gọi".
        /// </summary>
        public void OnCallStarted()
        {
            BtnStartCall.IsEnabled = false;
            TxtStartCallLabel.Text = "Đang trong cuộc gọi";
            BtnStartCall.Background = new SolidColorBrush(HexToColor("#64748B"));
        }

        /// <summary>
        /// Gọi từ ExaminationPage khi cuộc gọi kết thúc —
        /// reset nút.
        /// </summary>
        public void OnCallEnded()
        {
            BtnStartCall.IsEnabled = true;
            TxtStartCallLabel.Text = "Bắt đầu cuộc gọi";
            BtnStartCall.Background = new SolidColorBrush(HexToColor("#0059BB"));
        }

        /// <summary>
        /// Trả về nội dung ghi chú và quick notes để ExaminationPage
        /// lưu vào MedicalRecord khi Finish.
        /// </summary>
        public (string Diagnosis, List<string> QuickNotes) GetNotes()
            => (DiagnosisBox.Text, new List<string>(_quickNotes));

        public void Cleanup()
        {
            // TODO — Realtime: _realtime?.Unsubscribe(_chatChannel);
        }

        // ─────────────────────────────────────────────────────────
        //  DATA
        // ─────────────────────────────────────────────────────────

        public class ChatMessage
        {
            public string AppointmentId { get; set; }
            public string SenderId { get; set; }
            public string Content { get; set; }   
            public DateTime CreatedAt { get; set; }
        }


        private async Task LoadChatHistoryAsync()
        {
            // ══════════════════════════════════════════════════════
            // TODO — DB: Query lịch sử chat
            //
            //   var messages = await _db.GetAsync<ChatMessage>(q =>
            //       q.Eq("appointment_id", _appointmentId)
            //        .Order("created_at", ascending: true));
            //
            //   MessageList.Children.Clear();
            //   foreach (var msg in messages)
            //   {
            //       // TODO — Crypto: msg.Content = RSAManager.Decrypt(msg.Content);
            //       bool isSelf = msg.SenderId == _currentUserId;
            //       MessageList.Children.Add(BuildBubble(msg, isSelf));
            //   }
            //   ScrollToBottom();
            // ══════════════════════════════════════════════════════

            // Mock data — xoá khi có DB
            await Task.Delay(0);
            LoadMockMessages();
        }

        private void SubscribeRealtime()
        {
            // ══════════════════════════════════════════════════════
            // TODO — Realtime: Lắng nghe message mới từ bệnh nhân
            //
            //   _chatChannel = await _realtime.SubscribeToTableAsync<ChatMessage>(
            //       table:  "chat_messages",
            //       filter: $"appointment_id=eq.{_appointmentId}",
            //       onInsert: msg =>
            //       {
            //           DispatcherQueue.TryEnqueue(() =>
            //           {
            //               // Skip nếu là message của chính mình
            //               // (đã append optimistically khi gửi)
            //               if (msg.SenderId == _currentUserId) return;
            //
            //               // TODO — Crypto: msg.Content = RSAManager.Decrypt(msg.Content);
            //               AppendBubble(msg, isSelf: false);
            //           });
            //       });
            // ══════════════════════════════════════════════════════
        }

        // ─────────────────────────────────────────────────────────
        //  TAB SWITCHING
        // ─────────────────────────────────────────────────────────

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

            // Tab styles
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

        // ─────────────────────────────────────────────────────────
        //  START CALL BUTTON
        // ─────────────────────────────────────────────────────────

        private void BtnStartCall_Click(object sender, RoutedEventArgs e)
        {
            // Notify ExaminationPage → nó gọi VideoCall.StartCallAsync()
            StartCallRequested?.Invoke(this, EventArgs.Empty);
        }

        // ─────────────────────────────────────────────────────────
        //  SEND MESSAGE
        // ─────────────────────────────────────────────────────────

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
            => await SendMessageAsync();

        private async void ChatInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                await SendMessageAsync();
        }

        private async Task SendMessageAsync()
        {
            var text = ChatInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            ChatInput.Text = string.Empty;

            var msg = new ChatMessage
            {
                AppointmentId = _appointmentId,
                SenderId = _currentUserId,
                Content = text,
                CreatedAt = DateTime.Now
            };

            // Optimistic append
            AppendBubble(msg, isSelf: true);

            // ══════════════════════════════════════════════════════
            // TODO — DB + Crypto: Lưu message lên Supabase
            //
            //   var encrypted = RSAManager.Encrypt(text, patientPublicKey);
            //   msg.Content = encrypted;
            //   await _db.AddAsync(msg);
            //
            //   // Realtime sẽ push message tới bệnh nhân tự động
            // ══════════════════════════════════════════════════════

            await Task.CompletedTask;
        }

        // ─────────────────────────────────────────────────────────
        //  BUBBLE BUILDER
        // ─────────────────────────────────────────────────────────

        private void AppendBubble(ChatMessage msg, bool isSelf)
        {
            MessageList.Children.Add(BuildBubble(msg, isSelf));
            ScrollToBottom();
        }

        private UIElement BuildBubble(ChatMessage msg, bool isSelf)
        {
            var wrapper = new StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = isSelf ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 240,
                Margin = new Thickness(0, 4, 0, 4)
            };

            // Sender + time
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

            // Bubble
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

        // ─────────────────────────────────────────────────────────
        //  QUICK NOTES
        // ─────────────────────────────────────────────────────────

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

            var notes = _quickNotes.Count > 0
                ? _quickNotes
                : new List<string> { "Kê đơn thuốc Amlodipine 5mg", "Hẹn tái khám sau 7 ngày" };

            foreach (var note in notes)
            {
                var row = new Border
                {
                    Background = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(HexToColor("#F1F5F9")),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, 8, 10, 8)
                };
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
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

        // ─────────────────────────────────────────────────────────
        //  NOTES PANEL
        // ─────────────────────────────────────────────────────────

        private void BtnAddMedicine_Click(object sender, RoutedEventArgs e)
        {
            // ══════════════════════════════════════════════════════
            // TODO — DB: Mở dialog tìm trong MasterMedicine
            //
            //   var picker = new MedicinePicker();  // ContentDialog custom
            //   picker.XamlRoot = this.XamlRoot;
            //   var selected = await picker.ShowAsync();
            //   if (selected != null)
            //   {
            //       AddMedicineRow(selected);
            //   }
            // ══════════════════════════════════════════════════════
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
            // ══════════════════════════════════════════════════════
            // TODO — DB: Lưu MedicalRecord
            //
            //   var record = new MedicalRecord
            //   {
            //       AppointmentId = _appointmentId,
            //       PatientId     = _patientId,
            //       DoctorId      = _currentUserId,
            //       Diagnosis     = DiagnosisBox.Text,
            //       Notes         = string.Join("\n", _quickNotes),
            //       CreatedAt     = DateTime.UtcNow
            //   };
            //   await _db.AddAsync(record);
            // ══════════════════════════════════════════════════════

            NotesSaved?.Invoke(this, new MedicalNotesSavedEventArgs
            {
                Diagnosis = DiagnosisBox.Text,
                QuickNotes = new List<string>(_quickNotes)
            });

            var d = new ContentDialog
            {
                Title = "Đã lưu",
                Content = "Ghi chú khám bệnh đã được lưu.",
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };
            await d.ShowAsync();
        }

        // ─────────────────────────────────────────────────────────
        //  MOCK DATA (xoá khi có DB)
        // ─────────────────────────────────────────────────────────

        private void LoadMockMessages()
        {
            var mocks = new[]
            {
                (Id: "patient", Text: "Chào bác sĩ, tôi thấy hơi chóng mặt từ sáng nay.",
                 Time: DateTime.Now.AddMinutes(-5)),
                (Id: "doctor",  Text: "Ông đã đo huyết áp lúc đó chưa ạ?",
                 Time: DateTime.Now.AddMinutes(-4)),
                (Id: "patient", Text: "Vừa đo xong là 145/92 thưa bác sĩ.",
                 Time: DateTime.Now.AddMinutes(-3)),
            };
            MessageList.Children.Clear();
            foreach (var m in mocks)
                AppendBubble(new ChatMessage
                {
                    SenderId = m.Id,
                    Content = m.Text,
                    CreatedAt = m.Time,
                    AppointmentId = _appointmentId
                }, isSelf: m.Id == "doctor");
        }

        // ─────────────────────────────────────────────────────────
        //  UTILITY
        // ─────────────────────────────────────────────────────────

        private static Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  EventArgs
    // ─────────────────────────────────────────────────────────────

    public class MedicalNotesSavedEventArgs : EventArgs
    {
        public string Diagnosis { get; set; } = string.Empty;
        public List<string> QuickNotes { get; set; } = new();
    }
}
