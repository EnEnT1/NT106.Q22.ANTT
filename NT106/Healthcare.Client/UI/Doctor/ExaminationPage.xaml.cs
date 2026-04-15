using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.UI.Components;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class ExaminationPage : Page
    {
        private string _appointmentId = string.Empty;
        private string _patientId = string.Empty;
        private string _currentUserId = string.Empty;
        private string _activeNav = "video";

        public ExaminationPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string appointmentId)
                _appointmentId = appointmentId;

            _currentUserId = SessionStorage.CurrentUser?.Id ?? "mock-doctor-id";

            UpdateNavStyles();
            await InitializePageAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            VideoCall.Cleanup();
            Chat.Cleanup();
        }

        private async Task InitializePageAsync()
        {
            await LoadPatientInfoAsync();
            await VideoCall.InitializeAsync(_appointmentId, _patientId);
            await Chat.InitializeAsync(_appointmentId, _currentUserId, _patientId);
        }

        private async Task LoadPatientInfoAsync()
        {
            try
            {
                var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;

                var appointmentResponse = await client
                    .From<Appointment>()
                    .Get();

                var appointment = appointmentResponse.Models
                    .FirstOrDefault(a => a.Id == _appointmentId);

                if (appointment == null)
                {
                    TxtPatientName.Text = "Không tìm thấy lịch hẹn";
                    TxtPatientMeta.Text = "–";
                    TxtCondition.Text = "–";
                    return;
                }

                _patientId = appointment.PatientId;

                var userResponse = await client
                    .From<User>()
                    .Get();

                var patientUser = userResponse.Models
                    .FirstOrDefault(u => u.Id == _patientId);

                var patientProfileResponse = await client
                    .From<PatientProfile>()
                    .Get();

                var patientProfile = patientProfileResponse.Models
                    .FirstOrDefault(p => p.PatientId == _patientId);

                TxtPatientName.Text = patientUser?.FullName ?? "Bệnh nhân";
                TxtPatientMeta.Text = BuildPatientMeta(patientUser, patientProfile);
                TxtCondition.Text = BuildCondition(patientProfile);
            }
            catch
            {
                TxtPatientName.Text = "Lê Văn Dũng";
                TxtPatientMeta.Text = "ID: #MD-8829  •  45 Tuổi";
                TxtCondition.Text = "Cao huyết áp";
                _patientId = "mock-patient-id";
            }
        }

        private static string BuildPatientMeta(User? user, PatientProfile? profile)
        {
            if (user == null && profile == null)
                return "–";

            string idText = user != null ? $"ID: #{user.Id}" : "ID: –";

            string ageText = "Tuổi: –";
            if (profile != null && !string.IsNullOrWhiteSpace(profile.DateOfBirth))
            {
                if (DateTime.TryParse(profile.DateOfBirth, out var dob))
                {
                    var age = DateTime.Now.Year - dob.Year;
                    if (dob > DateTime.Now.AddYears(-age)) age--;
                    ageText = $"{age} tuổi";
                }
            }

            return $"{idText}  •  {ageText}";
        }

        private static string BuildCondition(PatientProfile? profile)
        {
            if (profile == null) return "Chưa rõ";

            if (profile.ChronicDiseases != null && profile.ChronicDiseases.Count > 0)
                return string.Join(", ", profile.ChronicDiseases);

            if (profile.Allergies != null && profile.Allergies.Count > 0)
                return "Dị ứng: " + string.Join(", ", profile.Allergies);

            return "Chưa rõ";
        }

        private void BtnNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            _activeNav = btn.Tag?.ToString() ?? "video";
            UpdateNavStyles();

            if (_activeNav == "video")
            {
                VideoCall.Visibility = Visibility.Visible;
            }
            else if (_activeNav == "chat")
            {
                Chat.SwitchTab("chat");
            }
            else if (_activeNav == "notes")
            {
                Chat.SwitchTab("notes");
            }
            else if (_activeNav == "history")
            {
                // TODO: sau này navigate sang PatientHistoryPage hoặc mở panel history
            }
        }

        private void UpdateNavStyles()
        {
            var navItems = new[]
            {
                (Btn: BtnNavVideo, Tag: "video"),
                (Btn: BtnNavChat, Tag: "chat"),
                (Btn: BtnNavNotes, Tag: "notes"),
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
                        {
                            fi.Foreground = new SolidColorBrush(
                                active ? HexToColor("#0059BB") : HexToColor("#64748B"));
                        }
                    }
                }
            }
        }

        private void VideoCall_CallStarted(object sender, EventArgs e)
        {
            Chat.OnCallStarted();
            _activeNav = "video";
            UpdateNavStyles();
        }

        private void VideoCall_CallEnded(object sender, EventArgs e)
        {
            Chat.OnCallEnded();
            VideoCall.Visibility = Visibility.Collapsed;
            _activeNav = "chat";
            UpdateNavStyles();
            Chat.SwitchTab("chat");
        }

        private async void Chat_StartCallRequested(object sender, EventArgs e)
        {
            VideoCall.Visibility = Visibility.Visible;
            _activeNav = "video";
            UpdateNavStyles();
            await VideoCall.StartCallAsync();
        }

        private void Chat_NotesSaved(object sender, MedicalNotesSavedEventArgs e)
        {
            // Có thể dùng e.Diagnosis và e.QuickNotes để cập nhật UI sau này :v chỗ này cũng chưa ro
        }

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
                var (diagnosis, quickNotes) = Chat.GetNotes();

                // TODO:
                // - lưu MedicalRecord thật
                // - cập nhật Appointment status = completed

                VideoCall.Cleanup();
                Chat.Cleanup();

                // TODO: Frame.Navigate(typeof(DoctorHomePage));
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
}