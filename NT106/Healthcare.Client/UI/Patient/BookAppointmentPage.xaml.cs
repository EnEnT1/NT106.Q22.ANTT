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
            DoctorComboBox.SelectionChanged += async (s, e) => await LoadSlotsAsync();
        }

        public class TimeSlotViewModel
        {
            public string Id { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public string StartTimeStr => DateTime.Today.Add(StartTime).ToString("HH:mm");
            public string DurationStr => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
            public TimeSlot OriginalModel { get; set; }
        }

        private async Task LoadSlotsAsync()
        {
            var selectedDoctor = DoctorComboBox.SelectedItem as User;
            if (selectedDoctor == null) return;

            try
            {
                var response = await _supabase.From<TimeSlot>()
                    .Where(x => x.DoctorId == selectedDoctor.Id)
                    .Where(x => x.SlotDate == ApptDatePicker.Date.DateTime.Date)
                    .Where(x => x.Status == "Available")
                    .Get();

                var slots = response.Models.OrderBy(s => s.StartTime).Select(s => new TimeSlotViewModel
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    OriginalModel = s
                }).ToList();

                SlotsGridView.ItemsSource = slots;
                NoSlotsText.Visibility = slots.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                await ShowDialog("Lỗi", "Không thể tải khung giờ: " + ex.Message);
            }
        }

        private async void ApptDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            await LoadSlotsAsync();
        }

        private void SlotsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Có thể thêm logic xử lý khi chọn slot ở đây
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
            var selectedSlotVM = SlotsGridView.SelectedItem as TimeSlotViewModel;

            if (selectedDoctor == null || selectedSlotVM == null)
            {
                await ShowDialog("Thiếu thông tin", "Vui lòng chọn bác sĩ và khung giờ khám.");
                return;
            }

            if (SessionStorage.CurrentUser == null)
            {
                await ShowDialog("Lỗi", "Vui lòng đăng nhập lại.");
                return;
            }

            try
            {
                // 1. Cập nhật trạng thái slot thành 'Booked' với điều kiện trạng thái hiện tại là 'Available'
                // Điều này giúp tránh Race Condition (double booking)
                var slotResponse = await _supabase.From<TimeSlot>()
                    .Where(x => x.Id == selectedSlotVM.Id)
                    .Where(x => x.Status == "Available")
                    .Set(x => x.Status, "Booked")
                    .Update();

                if (slotResponse.Models.Count == 0)
                {
                    await ShowDialog("Lỗi", "Khung giờ này vừa có người khác đặt. Vui lòng chọn khung giờ khác.");
                    await LoadSlotsAsync(); // Refresh lại danh sách
                    return;
                }

                string apptId = Guid.NewGuid().ToString();
                string paymentMethod = PaymentOnline.IsChecked == true ? "Online" : "Manual";

                var appt = new Appointment
                {
                    Id = apptId,
                    PatientId = SessionStorage.CurrentUser.Id,
                    DoctorId = selectedDoctor.Id,
                    AppointmentDate = ApptDatePicker.Date.DateTime.Date,
                    StartTime = selectedSlotVM.StartTime,
                    EndTime = selectedSlotVM.EndTime,
                    SlotId = selectedSlotVM.Id,
                    Status = "Confirmed", // Lịch được xác nhận ngay lập tức
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
                    Amount = 250000, 
                    PaymentMethod = paymentMethod,
                    Status = "Pending",
                    TransactionRef = "APPT-" + apptId.Substring(0, 6).ToUpper(),
                    PaidAt = null
                };

                await _supabase.From<Appointment>().Insert(appt);
                await _supabase.From<Transaction>().Insert(transaction);

                await ShowDialog("Thành công", "Lịch hẹn của bạn đã được xác nhận thành công!");
                
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
