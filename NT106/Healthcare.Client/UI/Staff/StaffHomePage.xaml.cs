using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.Models.Core;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.UI.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
namespace Healthcare.Client.UI.Staff
{
    public sealed partial class StaffHomePage : Page
    {
        private string _currentTab = "Appointment";
        private List<StaffAppointmentDisplay> _allAppointments = new();
        private List<StaffTransactionDisplay> _allTransactions = new();

        public class StaffAppointmentDisplay
        {
            public string Id { get; set; }
            public string PatientName { get; set; }
            public string DoctorName { get; set; }
            public DateTime AppointmentDate { get; set; }
            public string Status { get; set; }
            public string RoomCode { get; set; }
            public Brush StatusBrush => Status == "Confirmed" ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) :
                                       Status == "Pending" ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 88, 12)) :
                                       Status == "Arrived" ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 165, 233)) :
                                       new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139));
            
            public Visibility IsCheckInVisible => Status == "Confirmed" ? Visibility.Visible : Visibility.Collapsed;
        }

        public class StaffTransactionDisplay
        {
            public string Id { get; set; }
            public string PatientName { get; set; }
            public decimal Amount { get; set; }
            public string PaymentMethod { get; set; }
            public string Status { get; set; }
            public DateTime? PaidAt { get; set; }
        }

        public StaffHomePage()
        {
            this.InitializeComponent();
            LoadDataAsync();
        }

        private void UpdateTableHeaders()
        {
            switch (_currentTab)
            {
                case "Appointment":
                    StaffDataGrid.ItemTemplate = (DataTemplate)Resources["AppointmentTemplate"];
                    ColHeader0.Text = "MÃ LỊCH HẸN";
                    ColHeader1.Text = "BỆNH NHÂN / BÁC SĨ";
                    ColHeader2.Text = "NGÀY HẸN / TRẠNG THÁI";
                    ColHeader3.Text = "MÃ PHÒNG";
                    break;
                case "Transaction":
                    StaffDataGrid.ItemTemplate = (DataTemplate)Resources["TransactionTemplate"];
                    ColHeader0.Text = "MÃ GIAO DỊCH";
                    ColHeader1.Text = "NGƯỜI THANH TOÁN / PT";
                    ColHeader2.Text = "SỐ TIỀN / TRẠNG THÁI";
                    ColHeader3.Text = "THỜI GIAN";
                    break;
            }
        }

        private async void LoadDataAsync()
        {
            try
            {
                StaffDataGrid.ItemsSource = null;
                UpdateTableHeaders();

                switch (_currentTab)
                {
                    case "Appointment":
                        await LoadAppointmentsAsync();
                        break;
                    case "Transaction":
                        await LoadTransactionsAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi hệ thống", $"Không thể tải dữ liệu: {ex.Message}");
            }
        }

        private async Task LoadAppointmentsAsync()
        {
            var appointments = await SupabaseDbService.GetAllAsync<Appointment>();
            var allUsers = await SupabaseDbService.GetAllAsync<User>();
            
            _allAppointments = appointments
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new StaffAppointmentDisplay
                {
                    Id = a.Id,
                    PatientName = allUsers.FirstOrDefault(u => u.Id == a.PatientId)?.FullName ?? "N/A",
                    DoctorName = allUsers.FirstOrDefault(u => u.Id == a.DoctorId)?.FullName ?? "Bác sĩ ẩn",
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    RoomCode = a.RoomCode
                }).ToList();

            StaffDataGrid.ItemsSource = _allAppointments;
        }

        private async Task LoadTransactionsAsync()
        {
            var transactions = await SupabaseDbService.GetAllAsync<Transaction>();
            var allUsers = await SupabaseDbService.GetAllAsync<User>();

            _allTransactions = transactions
                .OrderByDescending(t => t.PaidAt)
                .Select(t => new StaffTransactionDisplay
                {
                    Id = t.Id,
                    PatientName = allUsers.FirstOrDefault(u => u.Id == t.PatientId)?.FullName ?? "N/A",
                    Amount = t.Amount,
                    PaymentMethod = t.PaymentMethod,
                    Status = t.Status,
                    PaidAt = t.PaidAt
                }).ToList();

            StaffDataGrid.ItemsSource = _allTransactions;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string query = sender.Text.ToLower().Trim();
            
            if (string.IsNullOrEmpty(query))
            {
                if (_currentTab == "Appointment") StaffDataGrid.ItemsSource = _allAppointments;
                else StaffDataGrid.ItemsSource = _allTransactions;
                return;
            }

            if (_currentTab == "Appointment")
            {
                StaffDataGrid.ItemsSource = _allAppointments
                    .Where(a => (a.PatientName?.ToLower().Contains(query) ?? false) || 
                               (a.Id?.ToLower().Contains(query) ?? false))
                    .ToList();
            }
            else
            {
                StaffDataGrid.ItemsSource = _allTransactions
                    .Where(t => (t.PatientName?.ToLower().Contains(query) ?? false) || 
                               (t.Id?.ToLower().Contains(query) ?? false))
                    .ToList();
            }
        }

        private async void StaffDataGrid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (_currentTab == "Transaction" && StaffDataGrid.SelectedItem is StaffTransactionDisplay selectedTx)
            {
                if (selectedTx.Status == "Paid")
                {
                    await ShowDialogAsync("Thông báo", "Giao dịch này đã được thanh toán rồi.");
                    return;
                }

                ContentDialog confirmDialog = new ContentDialog
                {
                    Title = "Xác nhận Thanh toán tại Quầy",
                    Content = $"Bạn có chắc chắn muốn xác nhận đã thu số tiền {selectedTx.Amount:N0} VNĐ cho bệnh nhân {selectedTx.PatientName} không?",
                    PrimaryButtonText = "Xác nhận Đã thu",
                    CloseButtonText = "Hủy",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        var tx = (await SupabaseManager.Instance.Client
                            .From<Transaction>()
                            .Where(t => t.Id == selectedTx.Id)
                            .Get()).Models.FirstOrDefault();

                        if (tx != null)
                        {
                            tx.Status = "Paid";
                            tx.PaidAt = DateTime.Now;
                            tx.PaymentMethod = "Tiền mặt (tại quầy)";
                            
                            await tx.Update<Transaction>();
                            await ShowDialogAsync("Thành công", "Đã xác nhận thanh toán thành công.");
                            LoadDataAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowDialogAsync("Lỗi", "Không thể cập nhật giao dịch: " + ex.Message);
                    }
                }
            }
            else if (_currentTab == "Appointment" && StaffDataGrid.SelectedItem is StaffAppointmentDisplay selectedApp)
            {
                // Giữ lại fallback: hiện thông tin chi tiết nếu double click
                await ShowDialogAsync("Lịch hẹn", $"Mã phòng khám cho {selectedApp.PatientName} là: {selectedApp.RoomCode}");
            }
        }

        private async void BtnCheckIn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string apptId)
            {
                var apptDisplay = _allAppointments.FirstOrDefault(a => a.Id == apptId);
                if (apptDisplay == null) return;

                ContentDialog confirmDialog = new ContentDialog
                {
                    Title = "Xác nhận Check-in",
                    Content = $"Xác nhận bệnh nhân {apptDisplay.PatientName} đã đến phòng khám? (Mã phòng: {apptDisplay.RoomCode})",
                    PrimaryButtonText = "Check-in",
                    CloseButtonText = "Hủy",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        var appt = (await SupabaseManager.Instance.Client
                            .From<Appointment>()
                            .Where(a => a.Id == apptId)
                            .Get()).Models.FirstOrDefault();

                        if (appt != null)
                        {
                            appt.Status = "Arrived";
                            
                            await appt.Update<Appointment>();
                            await ShowDialogAsync("Check-in Thành công", $"Đã xác nhận bệnh nhân đến. Vui lòng hướng dẫn bệnh nhân đến Phòng: {apptDisplay.RoomCode}.");
                            LoadDataAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowDialogAsync("Lỗi", "Không thể cập nhật trạng thái: " + ex.Message);
                    }
                }
            }
        }

        private void StaffNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer?.Tag != null)
            {
                _currentTab = args.InvokedItemContainer.Tag.ToString();
                HeaderTitle.Text = args.InvokedItem.ToString();
                SearchBox.Text = string.Empty; // Reset search when changing tabs
                LoadDataAsync();
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadDataAsync();
        }

        private async void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog logoutDialog = new ContentDialog
            {
                Title = "Xác nhận đăng xuất",
                Content = "Bạn có chắc chắn muốn thoát khỏi phiên làm việc này không?",
                PrimaryButtonText = "Đăng xuất",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await logoutDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Frame.Navigate(typeof(LoginPage));
            }
        }

        private async Task ShowDialogAsync(string title, string message)
        {
            ContentDialog dialog = new ContentDialog { Title = title, Content = message, CloseButtonText = "Đóng", XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
        }
    }
}
