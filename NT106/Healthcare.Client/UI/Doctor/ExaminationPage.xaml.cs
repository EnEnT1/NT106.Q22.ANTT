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
using System.Diagnostics;
using Windows.UI;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class ExaminationPage : Page
    {
        private string _appointmentId = string.Empty;
        private string _patientId = string.Empty;
        private string _currentUserId = string.Empty;
        private string _activeNav = "video";
        private readonly System.Collections.Generic.List<string> _quickNotes = new();

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
            try
            {
                var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;
                var appointmentResponse = await client.From<Appointment>().Where(a => a.Id == _appointmentId).Single();
                
                if (appointmentResponse != null)
                {
                    _patientId = appointmentResponse.PatientId;
                    await LoadPatientInfoAsync();
                    
                    await VideoCall.InitializeAsync(_appointmentId, _patientId, appointmentResponse.RoomCode);
                    await Chat.InitializeAsync(_appointmentId, _currentUserId, _patientId);
                }
                else
                {
                    await new ContentDialog { Title = "Lỗi", Content = "Không tìm thấy thông tin lịch hẹn này.", CloseButtonText = "Đóng", XamlRoot = this.XamlRoot }.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DoctorExam] Init Error: {ex.Message}");
            }
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
                TxtPatientMeta.Text = "ID: #MD8829  •  45 Tuổi";
                TxtCondition.Text = "Cao huyết áp";
                _patientId = "mock-patient-id";
            }
        }

        private static string BuildPatientMeta(User? user, PatientProfile? profile)
        {
            if (user == null && profile == null)
                return "–";

            string idText = user != null ? $"ID: #{user.Id.Substring(0, Math.Min(user.Id.Length, 6)).ToUpper()}" : "ID: –";

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

            string oldNav = _activeNav;
            _activeNav = btn.Tag?.ToString() ?? "info";
            
            if (oldNav == _activeNav) return;

            UpdateNavStyles();

            // All panels initially collapsed
            InfoPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Collapsed;
            NotesPanel.Visibility = Visibility.Collapsed;
            PrescriptionPanel.Visibility = Visibility.Collapsed;

            // Activate panel based on tag
            switch (_activeNav)
            {
                case "info":
                    InfoPanel.Visibility = Visibility.Visible;
                    break;
                case "chat":
                    ChatPanel.Visibility = Visibility.Visible;
                    break;
                case "notes":
                    NotesPanel.Visibility = Visibility.Visible;
                    RenderQuickNotes();
                    break;
                case "prescription":
                    PrescriptionPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdateNavStyles()
        {
            // Reset all icons to default color
            IconInfo.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            IconChat.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            IconNotes.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            IconPrescription.Foreground = new SolidColorBrush(HexToColor("#64748B"));

            // Set active icon color
            var activeBrush = new SolidColorBrush(HexToColor("#0284C7"));
            switch (_activeNav)
            {
                case "info": IconInfo.Foreground = activeBrush; break;
                case "chat": IconChat.Foreground = activeBrush; break;
                case "notes": IconNotes.Foreground = activeBrush; break;
                case "prescription": IconPrescription.Foreground = activeBrush; break;
            }
        }

        private void VideoCall_CallStarted(object sender, EventArgs e)
        {
            _activeNav = "info";
            UpdateNavStyles();
        }

        private void VideoCall_CallEnded(object sender, EventArgs e)
        {
            // In the new layout, we might not want to hide the whole VideoCall control, 
            // but just show that it ended. Or we can hide it.
            // Keeping it visible as a black screen with status message for now.
        }


        private async void Chat_NotesSaved(object sender, MedicalNotesSavedEventArgs e)
        {
            // Logic này có thể bỏ qua hoặc cập nhật nếu cần đồng bộ thông tin khác
        }

        private async void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            var input = new TextBox { PlaceholderText = "Nhập ghi chú nhanh...", Width = 260, TextWrapping = TextWrapping.Wrap };
            var dialog = new ContentDialog { Title = "Thêm ghi chú nhanh", Content = input, PrimaryButtonText = "Thêm", CloseButtonText = "Huỷ", DefaultButton = ContentDialogButton.Primary, XamlRoot = this.XamlRoot };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(input.Text))
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
                QuickNotesList.Children.Add(new TextBlock { Text = "Chưa có ghi chú nhanh", FontSize = 12, Foreground = new SolidColorBrush(HexToColor("#94A3B8")) });
                return;
            }
            foreach (var note in _quickNotes)
            {
                var row = new Border { Background = new SolidColorBrush(Colors.White), CornerRadius = new CornerRadius(8), BorderBrush = new SolidColorBrush(HexToColor("#F1F5F9")), BorderThickness = new Thickness(1, 1, 1, 1), Padding = new Thickness(10, 8, 10, 8) };
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                sp.Children.Add(new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE73E", FontSize = 12, Foreground = new SolidColorBrush(HexToColor("#0059BB")), VerticalAlignment = VerticalAlignment.Center });
                sp.Children.Add(new TextBlock { Text = note, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(HexToColor("#1E293B")), VerticalAlignment = VerticalAlignment.Center });
                row.Child = sp;
                QuickNotesList.Children.Add(row);
            }
        }

        private void BtnAddMedicine_Click(object sender, RoutedEventArgs e) { /* Picker DB Logic logic here */ }

        private async void BtnSaveNotes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;
                var record = new MedicalRecord {
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
                await new ContentDialog { Title = "Đã lưu", Content = "Ghi chú khám bệnh đã được lưu.", CloseButtonText = "Đóng", XamlRoot = this.XamlRoot }.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog { Title = "Lỗi", Content = "Không lưu được ghi chú: " + ex.Message, CloseButtonText = "Đóng", XamlRoot = this.XamlRoot }.ShowAsync();
            }
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
                try
                {
                    var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;
                    
                    // 1. Lưu hồ sơ bệnh án
                    var medicalRecord = new MedicalRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        AppointmentId = _appointmentId,
                        PatientId = _patientId,
                        DoctorId = _currentUserId,
                        Diagnosis = DiagnosisBox.Text ?? string.Empty,
                        AiMedicines = string.Join(" | ", _quickNotes),
                        CreatedAt = DateTime.Now
                    };

                    await client.From<MedicalRecord>().Insert(medicalRecord);

                    // 2. Cập nhật Appointment status = Completed
                    await client.From<Appointment>()
                        .Where(x => x.Id == _appointmentId)
                        .Set(x => x.Status, "Completed")
                        .Update();

                    VideoCall.Cleanup();
                    Chat.Cleanup();

                    // Quay về trang chủ bác sĩ
                    Frame.Navigate(typeof(ManageSchedulePage)); 
                }
                catch (Exception ex)
                {
                    await new ContentDialog
                    {
                        Title = "Lỗi lưu dữ liệu",
                        Content = $"Không thể lưu kết quả khám: {ex.Message}",
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
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