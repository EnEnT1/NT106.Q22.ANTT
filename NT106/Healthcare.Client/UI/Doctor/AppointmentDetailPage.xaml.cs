using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Helpers;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class AppointmentDetailPage : Page
    {
        private string _apptId;
        private Appointment _appointment;
        private User _patient;
        private PatientProfile _patientProfile;
        private MedicalRecord _medicalRecord;
        private Supabase.Client _supabase;

        public AppointmentDetailPage()
        {
            this.InitializeComponent();
            _supabase = SupabaseManager.Instance.Client;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is string id)
            {
                _apptId = id;
            }
            else if (e.Parameter is Appointment appt)
            {
                _apptId = appt.Id;
                _appointment = appt;
            }

            if (!string.IsNullOrEmpty(_apptId))
            {
                await LoadData();
            }
        }

        private async Task LoadData()
        {
            try
            {
                // 1. Tải thông tin lịch hẹn trước (Bắt buộc)
                if (_appointment == null)
                {
                    var apptResponse = await _supabase.From<Appointment>().Where(x => x.Id == _apptId).Get();
                    _appointment = apptResponse.Models.FirstOrDefault();
                }

                if (_appointment == null) return;

                // Hiển thị ngay thông tin lịch hẹn cơ bản
                DisplayData();

                // 2. Tải các thông tin bổ trợ (Không bắt buộc, có thể null)
                try {
                    var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;
                    
                    // 1. Fetch User (Patient) - Ưu tiên hàng đầu để lấy tên thực
                    var patientResponse = await client.From<User>()
                        .Filter("id", Postgrest.Constants.Operator.Equals, _appointment.PatientId)
                        .Get();
                    _patient = patientResponse.Models.FirstOrDefault();
                    
                    // Cập nhật tên ngay sau khi có đối tượng patient
                    DisplayData();

                    // 2. Fetch Profile
                    var profileResponse = await client.From<PatientProfile>()
                        .Filter("patient_id", Postgrest.Constants.Operator.Equals, _appointment.PatientId)
                        .Get();
                    _patientProfile = profileResponse.Models.FirstOrDefault();

                    // 3. Fetch Record
                    var recordResponse = await client.From<MedicalRecord>()
                        .Filter("appointment_id", Postgrest.Constants.Operator.Equals, _appointment.Id)
                        .Get();
                    _medicalRecord = recordResponse.Models.FirstOrDefault();
                    
                    // Cuối cùng cập nhật toàn bộ thông tin chi tiết
                    DisplayData();
                } 
                catch (Exception exAux) {
                    System.Diagnostics.Debug.WriteLine($"Error loading auxiliary data: {exAux.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading main appointment data: {ex.Message}");
            }
        }

        private void DisplayData()
        {
            if (_appointment == null) return;

            string safeId = _appointment.Id ?? "";
            string displayId = safeId.Length >= 8 ? safeId.Substring(0, 8) : safeId;
            ApptIdText.Text = $"MÃ THAM CHIẾU: #{displayId.ToUpper()}";
            
            string fullName = _patient?.FullName;
            if (string.IsNullOrEmpty(fullName))
            {
                fullName = "Đang tải thông tin...";
            }
            PatientNameTitleText.Text = fullName;
            ProfileNameText.Text = fullName;
            PatientEmailText.Text = _patient?.Email ?? "Email: --";
            
            DateText.Text = _appointment.AppointmentDate.ToLocalTime().ToString("dd MMMM, yyyy");
            TimeText.Text = _appointment.StartTime.ToString(@"hh\:mm");
            
            // Tính thời gian kết thúc (Mặc định +30 phút)
            var endTime = _appointment.StartTime.Add(TimeSpan.FromMinutes(30));
            EndTimeText.Text = endTime.ToString(@"hh\:mm");
            
            RoomCodeText.Text = _appointment.RoomCode ?? "CHƯA CÓ MÃ";

            // Logic Ẩn nút Hủy nếu đã hoàn thành hoặc vắng mặt
            BtnTopCancel.Visibility = (_appointment.Status == "Completed" || _appointment.Status == "Missed") ? Visibility.Collapsed : Visibility.Visible;
            BtnExamineEdit.Visibility = (_appointment.Status == "Completed" || _appointment.Status == "Cancelled" || _appointment.Status == "Missed") ? Visibility.Collapsed : Visibility.Visible;

            // Logic Gọi Video (Chỉ Online và ĐÚNG GIỜ)
            bool isOnline = _appointment.ExaminationType == "Online";
            DateTime appointmentFullDateTime = _appointment.AppointmentDate.ToLocalTime().Date.Add(_appointment.StartTime);
            DateTime appointmentEndFullDateTime = appointmentFullDateTime.AddMinutes(30);
            bool isOnTime = DateTime.Now >= appointmentFullDateTime.AddMinutes(-5) && DateTime.Now <= appointmentEndFullDateTime;

            if (isOnline && _appointment.Status != "Missed" && _appointment.Status != "Cancelled" && _appointment.Status != "Completed")
            {
                BtnCallAction.IsEnabled = isOnTime;
                CallBtnLabel.Text = isOnTime ? "Gọi Video" : "Chưa đến giờ";
                BtnCallAction.Opacity = isOnTime ? 1.0 : 0.5;
            }
            else
            {
                BtnCallAction.IsEnabled = false;
                CallBtnLabel.Text = isOnline ? "Gọi Video" : "Khám tại chỗ";
                BtnCallAction.Opacity = 0.5;
            }

            // Avatar Logic (PersonPicture with default anonymous silhouette placeholder)
            if (!string.IsNullOrEmpty(_patient?.AvatarUrl))
            {
                PatientAvatar.ProfilePicture = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(_patient.AvatarUrl));
            }
            else
            {
                PatientAvatar.ProfilePicture = null;
                PatientAvatar.DisplayName = null;
                PatientAvatar.Initials = null;
            }

            // Status Badge & Top Buttons
            if (_appointment.Status == "Pending")
            {
                StatusText.Text = "CHỜ XÁC NHẬN";
                StatusBadge.Background = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)); // Amber
                BtnTopApprove.Visibility = Visibility.Visible;
                BtnTopCancel.Visibility = Visibility.Visible;
                BtnTopVideoCall.Visibility = Visibility.Collapsed;
            }
            else if (_appointment.Status == "Confirmed" || _appointment.Status == "Arrived")
            {
                StatusText.Text = _appointment.Status == "Arrived" ? "ĐÃ ĐẾN (SẴN SÀNG)" : "ĐÃ XÁC NHẬN";
                StatusBadge.Background = _appointment.Status == "Arrived" ? 
                    new SolidColorBrush(Color.FromArgb(255, 14, 165, 233)) : 
                    new SolidColorBrush(Color.FromArgb(255, 2, 132, 199));
                    
                BtnTopApprove.Visibility = Visibility.Collapsed;
                BtnTopCancel.Visibility = Visibility.Visible;
                BtnTopVideoCall.Visibility = _appointment.ExaminationType == "Online" ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (_appointment.Status == "Cancelled")
            {
                StatusText.Text = "ĐÃ HỦY";
                StatusBadge.Background = new SolidColorBrush(Color.FromArgb(255, 225, 29, 72)); // Rose
                BtnTopApprove.Visibility = Visibility.Collapsed;
                BtnTopCancel.Visibility = Visibility.Collapsed;
                BtnTopVideoCall.Visibility = Visibility.Collapsed;
            }
            else if (_appointment.Status == "Missed")
            {
                StatusText.Text = "VẮNG MẶT";
                StatusBadge.Background = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)); // Slate gray
                BtnTopApprove.Visibility = Visibility.Collapsed;
                BtnTopCancel.Visibility = Visibility.Collapsed;
                BtnTopVideoCall.Visibility = Visibility.Collapsed;
            }
            else
            {
                StatusText.Text = _appointment.Status.ToUpper();
                StatusBadge.Background = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            }

            // Type Badge
            TypeText.Text = (_appointment.ExaminationType ?? "Offline").ToUpper();
            TypeBadge.Background = new SolidColorBrush(TypeText.Text == "ONLINE" ? Color.FromArgb(255, 20, 184, 166) : Color.FromArgb(255, 148, 163, 184));

            // Profile info
            if (_patientProfile != null)
            {
                GenderText.Text = string.IsNullOrEmpty(_patientProfile.Gender) ? "--" : _patientProfile.Gender;
                BloodTypeText.Text = string.IsNullOrEmpty(_patientProfile.BloodType) ? "--" : _patientProfile.BloodType;
                
                if (!string.IsNullOrEmpty(_patientProfile.DateOfBirth) && DateTime.TryParse(_patientProfile.DateOfBirth, out DateTime dob))
                {
                    int age = DateTime.Today.Year - dob.Year;
                    if (dob.Date > DateTime.Today.AddYears(-age)) age--;
                    AgeText.Text = $"{age} tuổi";
                }
            }

            // Clinical Notes
            if (_medicalRecord != null)
            {
                DiagnosisText.Text = _medicalRecord.Diagnosis ?? "Bác sĩ chưa có ghi chú cho cuộc khám này.";
                NoteDateText.Text = _medicalRecord.CreatedAt.ToString("MMM dd, hh:mm tt");
                MedicineText.Text = (_medicalRecord.AiMedicines != null && _medicalRecord.AiMedicines.Count > 0) ? "Đã kê thuốc" : "Không có thuốc";
            }

            // Timeline
            TimelineBookedTime.Text = $"{_appointment.CreatedAt.ToString("dd MMM, yyyy - hh:mm tt")}";
            
            if (_appointment.Status == "Cancelled")
            {
                TimelineEndTitle.Text = "ĐÃ HỦY";
                TimelineEndTitle.Foreground = new SolidColorBrush(Color.FromArgb(255, 159, 18, 57));
                TimelineEndIconBg.Fill = new SolidColorBrush(Color.FromArgb(255, 159, 18, 57));
                TimelineEndIcon.Glyph = "\xE711";
                TimelineEndTime.Text = "Giao dịch đã bị chấm dứt";
                TimelineEndReason.Visibility = Visibility.Visible;
                TimelineEndReasonText.Text = "Lý do: Lịch hẹn không được thực hiện hoặc đã bị bác sĩ/bệnh nhân hủy.";
            }
            else if (_appointment.Status == "Missed")
            {
                TimelineEndTitle.Text = "VẮNG MẶT";
                TimelineEndTitle.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
                TimelineEndIconBg.Fill = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
                TimelineEndIcon.Glyph = "\xE711";
                TimelineEndTime.Text = "Không thực hiện cuộc hẹn";
                TimelineEndReason.Visibility = Visibility.Visible;
                TimelineEndReasonText.Text = "Lý do: Cuộc hẹn đã bị quá giờ mà bệnh nhân không tham gia khám.";
            }
            else if (_appointment.Status == "Confirmed" || _appointment.Status == "Arrived")
            {
                TimelineEndTitle.Text = _appointment.Status == "Arrived" ? "ĐÃ ĐẾN PHÒNG KHÁM" : "ĐÃ XÁC NHẬN";
                TimelineEndTitle.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
                TimelineEndIconBg.Fill = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
                TimelineEndIcon.Glyph = "\xE73E";
                TimelineEndTime.Text = _appointment.Status == "Arrived" ? "Bệnh nhân đã đến và sẵn sàng khám" : "Bác sĩ đã chấp nhận lịch hẹn";
                TimelineEndReason.Visibility = Visibility.Collapsed;
            }
            else
            {
                TimelineEndTitle.Text = "ĐANG CHỜ";
                TimelineEndTitle.Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105));
                TimelineEndIconBg.Fill = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184));
                TimelineEndIcon.Glyph = "\xE814";
                TimelineEndTime.Text = "Chờ bác sĩ duyệt lịch";
                TimelineEndReason.Visibility = Visibility.Collapsed;
            }
        }

        private void Breadcrumb_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => Frame.Navigate(typeof(ManageSchedulePage));

        private void BtnBack_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(ManageSchedulePage));

         private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            // Logic xem lịch sử sẽ được cập nhật sau
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            try {
                if (_appointment == null) return;
                
                _appointment.Status = "Confirmed";
                await _supabase.From<Appointment>().Update(_appointment);
                
                // Sau khi duyệt, tải lại dữ liệu để cập nhật UI
                await LoadData();
            } catch (Exception ex) {
                // Xử lý lỗi nếu cần
            }
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            try {
                _appointment.Status = "Cancelled";
                await _supabase.From<Appointment>().Update(_appointment);
                await LoadData();
            } catch { }
        }

        private void BtnVideoCall_Click(object sender, RoutedEventArgs e) 
        {
            if (_patient != null)
                Frame.Navigate(typeof(ExaminationPage), _appointment.Id);
        }

        private void BtnExamine_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(ExaminationPage), _appointment.Id);
    }
}
