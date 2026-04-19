using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Helpers;

namespace Healthcare.Client.UI.Patient
{
    public sealed partial class BookAppointmentPage : Page
    {
        private Supabase.Client _supabase;

        public BookAppointmentPage()
        {
            this.InitializeComponent();
            _supabase = SupabaseManager.Instance.Client;
            LoadDoctors();
            
            ApptDatePicker.Date = DateTimeOffset.Now;
            ApptTimePicker.Time = DateTime.Now.TimeOfDay;
        }

        private async void LoadDoctors()
        {
            try
            {
                var response = await _supabase.From<User>()
                    .Where(x => x.Role == "Doctor")
                    .Get();

                DoctorComboBox.ItemsSource = response.Models;
            }
            catch (Exception ex)
            {
                await ShowDialog("Lỗi", "Không thể tải danh sách bác sĩ: " + ex.Message);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
                this.Frame.GoBack();
        }

        private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            var selectedDoctor = DoctorComboBox.SelectedItem as User;
            if (selectedDoctor == null)
            {
                await ShowDialog("Thiếu thông tin", "Vui lòng chọn bác sĩ.");
                return;
            }

            if (SessionStorage.CurrentUser == null)
            {
                await ShowDialog("Lỗi", "Vui lòng đăng nhập lại.");
                return;
            }

            try
            {
                string apptId = Guid.NewGuid().ToString();
                string paymentMethod = PaymentOnline.IsChecked == true ? "Online" : "Manual";

                var appt = new Appointment
                {
                    Id = apptId,
                    PatientId = SessionStorage.CurrentUser.Id,
                    DoctorId = selectedDoctor.Id,
                    AppointmentDate = ApptDatePicker.Date.DateTime,
                    StartTime = ApptTimePicker.Time,
                    Status = "Pending",
                    ExaminationType = TypeOnline.IsChecked == true ? "Online" : "Offline",
                    CreatedAt = DateTime.UtcNow,
                    RoomCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                };

                // Tạo Giao dịch đi kèm
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    AppointmentId = apptId,
                    PatientId = SessionStorage.CurrentUser.Id,
                    Amount = 250000, // Phí khám mặc định
                    PaymentMethod = paymentMethod,
                    Status = "Pending",
                    TransactionRef = "APPT-" + apptId.Substring(0, 6).ToUpper(),
                    PaidAt = null
                };

                await _supabase.From<Appointment>().Insert(appt);
                await _supabase.From<Transaction>().Insert(transaction);

                await ShowDialog("Thành công", "Lịch hẹn của bạn đã được đăng ký thành công và đang chờ bác sĩ duyệt.");
                
                if (this.Frame.CanGoBack)
                    this.Frame.GoBack();
            }
            catch (Exception ex)
            {
                await ShowDialog("Lỗi", "Không thể đăng ký lịch hẹn: " + ex.Message);
            }
        }

        private async Task ShowDialog(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
