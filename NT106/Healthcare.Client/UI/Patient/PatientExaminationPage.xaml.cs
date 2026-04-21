using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.UI.Components;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI;

namespace Healthcare.Client.UI.Patient
{
    public sealed partial class PatientExaminationPage : Page
    {
        private string _appointmentId = string.Empty;
        private string _doctorId = string.Empty;
        private string _currentUserId = string.Empty;
        private string _activeNav = "video";

        public PatientExaminationPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string appointmentId)
                _appointmentId = appointmentId;

            _currentUserId = SessionStorage.CurrentUser?.Id ?? string.Empty;

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
                    _doctorId = appointmentResponse.DoctorId;
                    await LoadDoctorInfoAsync();
                    
                    await VideoCall.InitializeAsync(_appointmentId, _doctorId, appointmentResponse.RoomCode);
                    await Chat.InitializeAsync(_appointmentId, _currentUserId, _doctorId);
                    await LoadMedicalDataAsync();
                }
                else
                {
                    await new ContentDialog { Title = "Lỗi", Content = "Không tìm thấy thông tin lịch hẹn này.", CloseButtonText = "Đóng", XamlRoot = this.XamlRoot }.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PatientExam] Init Error: {ex.Message}");
            }
        }

        private async Task LoadDoctorInfoAsync()
        {
            try
            {
                var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;

                var appointmentResponse = await client.From<Appointment>().Get();
                var appointment = appointmentResponse.Models.FirstOrDefault(a => a.Id == _appointmentId);

                if (appointment == null) return;

                _doctorId = appointment.DoctorId;

                var userResponse = await client.From<User>().Get();
                var doctorUser = userResponse.Models.FirstOrDefault(u => u.Id == _doctorId);

                TxtDoctorName.Text = doctorUser?.FullName ?? "Bác sĩ";
                TxtDoctorID.Text = doctorUser?.Id?.Length > 6 ? doctorUser.Id.Substring(0, 6).ToUpper() : doctorUser?.Id?.ToUpper();
                DoctorAvatar.DisplayName = doctorUser?.FullName;
            }
            catch { /* Handle error */ }
        }

        private async Task LoadMedicalDataAsync()
        {
            try
            {
                var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;
                var recordResponse = await client.From<MedicalRecord>()
                    .Where(r => r.AppointmentId == _appointmentId)
                    .Get();

                var record = recordResponse.Models.FirstOrDefault();
                if (record != null)
                {
                    TxtDiagnosis.Text = string.IsNullOrEmpty(record.Diagnosis) ? "Chưa có chẩn đoán." : record.Diagnosis;
                    // Logic to render prescription can be added here
                }
            }
            catch { }
        }

        private void BtnNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            string oldNav = _activeNav;
            _activeNav = btn.Tag?.ToString() ?? "info";
            
            if (oldNav == _activeNav) return;

            UpdateNavStyles();

            InfoPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Collapsed;
            NotesPanel.Visibility = Visibility.Collapsed;
            PrescriptionPanel.Visibility = Visibility.Collapsed;

            switch (_activeNav)
            {
                case "info": InfoPanel.Visibility = Visibility.Visible; break;
                case "chat": ChatPanel.Visibility = Visibility.Visible; break;
                case "notes": NotesPanel.Visibility = Visibility.Visible; break;
                case "prescription": PrescriptionPanel.Visibility = Visibility.Visible; break;
            }
        }

        private void UpdateNavStyles()
        {
            IconInfo.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            IconChat.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            IconNotes.Foreground = new SolidColorBrush(HexToColor("#64748B"));
            IconPrescription.Foreground = new SolidColorBrush(HexToColor("#64748B"));

            var activeBrush = new SolidColorBrush(HexToColor("#2563EB"));
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

        private void VideoCall_CallEnded(object sender, EventArgs e) { }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private static Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(255, Convert.ToByte(hex.Substring(0, 2), 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
        }
    }
}
