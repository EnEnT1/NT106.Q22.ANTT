// ============================================================
//  ExaminationPage.xaml.cs
//  Healthcare.Client — UI/Doctor/ExaminationPage
//
//  Vai trò: Layout shell + điều phối giữa các component.
//  KHÔNG chứa logic chat, video, hay timer — tất cả nằm trong
//  VideoCallControl và ChatControl.
//
//  Luồng chính:
//    OnNavigatedTo(appointmentId)
//      → LoadPatientInfoAsync()
//      → VideoCall.InitializeAsync(appointmentId, patientId)
//      → Chat.InitializeAsync(appointmentId, currentUserId, patientId)
// ============================================================

using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Helpers;
using Healthcare.Client.UI.Components;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class ExaminationPage : Page
    {
        // ─────────────────────────────────────────────────────────
        //  Dependencies
        // ─────────────────────────────────────────────────────────

        // TODO — DB: private readonly SupabaseDbService _db = SupabaseManager.Instance.Db;

        // ─────────────────────────────────────────────────────────
        //  State
        // ─────────────────────────────────────────────────────────

        private string _appointmentId = string.Empty;
        private string _patientId = string.Empty;
        private string _currentUserId = string.Empty;
        private string _activeNav = "video";

        // ─────────────────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────────────────

        public ExaminationPage()
        {
            this.InitializeComponent();
        }

        // ─────────────────────────────────────────────────────────
        //  NAVIGATION
        // ─────────────────────────────────────────────────────────

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Nhận appointmentId từ DoctorHomePage (hàng chờ)
            if (e.Parameter is string appointmentId)
                _appointmentId = appointmentId;

            // TODO — Session: _currentUserId = SessionStorage.CurrentUser.Id;
            _currentUserId = "mock-doctor-id";

            await InitializePageAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            VideoCall.Cleanup();
            Chat.Cleanup();
        }

        // ─────────────────────────────────────────────────────────
        //  INIT
        // ─────────────────────────────────────────────────────────

        private async Task InitializePageAsync()
        {
            // 1. Load thông tin bệnh nhân → hiển thị sidebar trái
            await LoadPatientInfoAsync();

            // 2. Khởi tạo VideoCallControl (chuẩn bị local camera)
            await VideoCall.InitializeAsync(_appointmentId, _patientId);

            // 3. Khởi tạo ChatControl (load lịch sử + subscribe realtime)
            await Chat.InitializeAsync(_appointmentId, _currentUserId, _patientId);
        }

        // ─────────────────────────────────────────────────────────
        //  PATIENT INFO
        // ─────────────────────────────────────────────────────────

        private async Task LoadPatientInfoAsync()
        {
            // ══════════════════════════════════════════════════════
            // TODO — DB: Query Appointment → PatientProfile
            //
            //   var appt = (await _db.GetAsync<Appointment>(q =>
            //       q.Eq("id", _appointmentId))).FirstOrDefault();
            //   if (appt == null) return;
            //
            //   _patientId = appt.PatientId;
            //
            //   var profile = (await _db.GetAsync<PatientProfile>(q =>
            //       q.Eq("user_id", _patientId))).FirstOrDefault();
            //   if (profile == null) return;
            //
            //   TxtPatientName.Text = profile.FullName;
            //   TxtPatientMeta.Text = $"ID: #{profile.PatientCode}  •  {profile.Age} Tuổi";
            //   TxtCondition.Text   = profile.PrimaryCondition ?? "–";
            //
            //   if (!string.IsNullOrEmpty(profile.AvatarUrl))
            //   {
            //       var brush = new ImageBrush();
            //       brush.ImageSource = new BitmapImage(new Uri(profile.AvatarUrl));
            //       AvatarBorder.Background = brush;
            //   }
            // ══════════════════════════════════════════════════════

            // Mock — xoá khi có DB
            await Task.Delay(0);
            _patientId = "mock-patient-id";
            TxtPatientName.Text = "Lê Văn Dũng";
            TxtPatientMeta.Text = "ID: #MD-8829  •  45 Tuổi";
            TxtCondition.Text = "Cao huyết áp";
        }

        // ─────────────────────────────────────────────────────────
        //  LEFT NAV — context switching
        // ─────────────────────────────────────────────────────────

        private void BtnNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            _activeNav = btn.Tag?.ToString() ?? "video";
            UpdateNavStyles();

            // Khi click Chat/Notes từ sidebar → tự switch tab bên ChatControl
            if (_activeNav == "chat") Chat.SwitchTab("chat");
            if (_activeNav == "notes") Chat.SwitchTab("notes");
        }

        private void UpdateNavStyles()
        {
            var navItems = new[]
            {
                (Btn: BtnNavVideo,   Tag: "video"),
                (Btn: BtnNavChat,    Tag: "chat"),
                (Btn: BtnNavNotes,   Tag: "notes"),
                (Btn: BtnNavHistory, Tag: "history"),
            };

            foreach (var (btn, tag) in navItems)
            {
                bool active = tag == _activeNav;
                btn.Background = new SolidColorBrush(active ? Colors.White : Colors.Transparent);

                if (btn.Content is StackPanel sp)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is TextBlock tb)
                        {
                            tb.Foreground = new SolidColorBrush(
                                active ? HexToColor("#0059BB") : HexToColor("#64748B"));
                            tb.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
                        }
                        if (child is FontIcon fi)
                            fi.Foreground = new SolidColorBrush(
                                active ? HexToColor("#0059BB") : HexToColor("#64748B"));
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        //  VIDEO CALL EVENTS (từ VideoCallControl)
        // ─────────────────────────────────────────────────────────

        private void VideoCall_CallStarted(object sender, EventArgs e)
        {
            // Notify ChatControl để đổi trạng thái nút
            Chat.OnCallStarted();
        }

        private void VideoCall_CallEnded(object sender, EventArgs e)
        {
            Chat.OnCallEnded();
            VideoCall.Visibility = Visibility.Collapsed;
            Grid.SetColumn(Chat, 1);
            Grid.SetColumnSpan(Chat, 2);
        }

        // ─────────────────────────────────────────────────────────
        //  CHAT EVENTS (từ ChatControl)
        // ─────────────────────────────────────────────────────────

        private async void Chat_StartCallRequested(object sender, EventArgs e)
        {
            // ChatControl báo user bấm "Bắt đầu cuộc gọi"
            // → ExaminationPage gọi VideoCall để start
            VideoCall.Visibility = Visibility.Visible;
            Grid.SetColumn(Chat, 2);
            Grid.SetColumnSpan(Chat, 1);
            await VideoCall.StartCallAsync();
        }

        private void Chat_NotesSaved(object sender, MedicalNotesSavedEventArgs e)
        {
            // Ghi chú đã lưu — có thể update UI sidebar nếu cần
            // e.Diagnosis, e.QuickNotes
        }

        // ─────────────────────────────────────────────────────────
        //  FOOTER ACTIONS
        // ─────────────────────────────────────────────────────────

        private async void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Kết thúc khám",
                Content = "Kết thúc buổi khám và lưu toàn bộ ghi chú?",
                PrimaryButtonText = "Kết thúc",
                CloseButtonText = "Huỷ",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                // Lấy notes từ ChatControl
                var (diagnosis, quickNotes) = Chat.GetNotes();

                // ══════════════════════════════════════════════════════
                // TODO — DB: Lưu MedicalRecord + cập nhật Appointment
                //
                //   await _db.AddAsync(new MedicalRecord
                //   {
                //       AppointmentId = _appointmentId,
                //       PatientId     = _patientId,
                //       DoctorId      = _currentUserId,
                //       Diagnosis     = diagnosis,
                //       Notes         = string.Join("\n", quickNotes),
                //       CreatedAt     = DateTime.UtcNow
                //   });
                //
                //   await _db.UpdateAsync<Appointment>(
                //       q => q.Eq("id", _appointmentId),
                //       new { status = "completed" });
                // ══════════════════════════════════════════════════════

                VideoCall.Cleanup();
                Chat.Cleanup();

                // TODO — Navigate: Frame.Navigate(typeof(DoctorHomePage));
            }
        }

        private async void BtnEmergency_Click(object sender, RoutedEventArgs e)
        {
            var d = new ContentDialog
            {
                Title = "Khẩn cấp",
                Content = "Đã gửi tín hiệu khẩn cấp.\nVui lòng hướng dẫn bệnh nhân liên hệ 115 ngay.",
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };
            await d.ShowAsync();
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
}
