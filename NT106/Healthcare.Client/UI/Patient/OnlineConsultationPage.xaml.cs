using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Healthcare.Client.UI.Patient
{
    public class OnlineAppointmentViewModel
    {
        public string Id { get; set; }
        public string DoctorName { get; set; }
        public string AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public string FullDateTime => $"{AppointmentDate} {AppointmentTime}";
        public string Status { get; set; }
        public string RawStatus { get; set; }
        public bool IsJoinable { get; set; }
        public string BadgeText { get; set; }
        public string BadgeBg { get; set; }
        public string BadgeForeground { get; set; }
    }

    public sealed partial class OnlineConsultationPage : Page
    {
        public ObservableCollection<OnlineAppointmentViewModel> UpcomingAppointments { get; } = new();
        public ObservableCollection<OnlineAppointmentViewModel> HistoryAppointments { get; } = new();

        public OnlineConsultationPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadAppointmentsAsync();
        }

        private async Task LoadAppointmentsAsync()
        {
            try
            {
                var patientId = SessionStorage.CurrentUser?.Id;
                if (string.IsNullOrEmpty(patientId)) return;

                var client = Healthcare.Client.SupabaseIntegration.SupabaseManager.Instance.Client;

                // 1. Lấy danh sách Appointment Online của Patient
                var response = await client.From<Appointment>()
                    .Where(a => a.PatientId == patientId)
                    .Where(a => a.ExaminationType == "Online")
                    .Get();

                var appointments = response.Models.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ToList();

                // 2. Lấy thông tin Bác sĩ (Optimization: Lấy tất cả user 1 lần hoặc cache)
                var userResponse = await client.From<User>().Get();
                var doctors = userResponse.Models.ToDictionary(u => u.Id, u => u.FullName);

                UpcomingAppointments.Clear();
                HistoryAppointments.Clear();

                // Sắp xếp lịch khám theo thời gian tăng dần để tìm cái gần nhất
                var sortedAppts = appointments.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToList();
                bool foundNearest = false;

                foreach (var app in sortedAppts)
                {
                    // Logic tính toán xem đã tới giờ hay chưa (Cho phép vào sớm 15p)
                    var appDateTime = app.AppointmentDate.Date.Add(app.StartTime);
                    var endTime = app.AppointmentDate.Date.Add(app.EndTime);
                    bool isJoinable = (DateTime.Now >= appDateTime.AddMinutes(-15)) && (DateTime.Now <= endTime);

                    var vm = new OnlineAppointmentViewModel
                    {
                        Id = app.Id,
                        DoctorName = doctors.ContainsKey(app.DoctorId) ? doctors[app.DoctorId] : "Bác sĩ",
                        AppointmentDate = app.AppointmentDate.ToString("dd/MM/yyyy"),
                        AppointmentTime = app.StartTime.ToString(@"hh\:mm"),
                        Status = MapStatus(app.Status),
                        RawStatus = app.Status,
                        IsJoinable = isJoinable,
                        BadgeText = isJoinable ? "Đang diễn ra" : "Sắp diễn ra",
                        BadgeBg = isJoinable ? "#DCFCE7" : "#EFF6FF",
                        BadgeForeground = isJoinable ? "#166534" : "#1D4ED8"
                    };

                    // 1. Luôn thêm vào danh sách tổng hợp bên dưới (Sắp xếp lại ở bước sau nếu cần)
                    HistoryAppointments.Add(vm);

                    // 2. Chỉ lấy 1 lịch khám CHƯA KẾT THÚC và gần nhất để hiện ở mục "Sắp diễn ra"
                    bool isUpcomingStatus = app.Status == "Confirmed" || app.Status == "Paid" || app.Status == "Success";
                    if (!foundNearest && isUpcomingStatus && DateTime.Now <= endTime)
                    {
                        UpcomingAppointments.Add(vm);
                        foundNearest = true;
                    }
                }

                // Sắp xếp lại danh sách tổng hợp theo thời gian mới nhất lên đầu
                var historyList = HistoryAppointments.OrderByDescending(h => DateTime.ParseExact(h.AppointmentDate, "dd/MM/yyyy", null).Add(TimeSpan.Parse(h.AppointmentTime))).ToList();
                HistoryAppointments.Clear();
                foreach (var h in historyList) HistoryAppointments.Add(h);

                UpcomingEmptyState.Visibility = UpcomingAppointments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // Silently fail or log
            }
        }

        private string MapStatus(string status)
        {
            return status switch
            {
                "Confirmed" => "Đã xác nhận",
                "Paid" => "Đã thanh toán",
                "Success" => "Thành công",
                "Completed" => "Đã hoàn thành",
                "Cancelled" => "Đã hủy",
                _ => status
            };
        }

        private void BtnTestUI_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng thẳng sang trang khám với ID ảo để test UI
            Frame.Navigate(typeof(PatientExaminationPage), "test-appointment-id");
        }

        private async void BtnJoin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string appointmentId)
            {
                var appointment = UpcomingAppointments.FirstOrDefault(a => a.Id == appointmentId);
                if (appointment != null)
                {
                    await ShowConfirmDialog(appointment);
                }
            }
        }

        private async void HistoryItem_Click(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Grid grid && grid.Tag is string appointmentId)
            {
                var appointment = HistoryAppointments.FirstOrDefault(a => a.Id == appointmentId);
                // Với lịch sử, nếu đã hoàn thành mới cho vào xem lại
                if (appointment != null && appointment.RawStatus == "Completed")
                {
                    await ShowConfirmDialog(appointment);
                }
            }
        }

        private async Task ShowConfirmDialog(OnlineAppointmentViewModel appointment)
        {
            var stack = new StackPanel { Spacing = 16, Margin = new Thickness(0, 10, 0, 0) };
            
            var infoGrid = new Grid();
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var leftCol = new StackPanel();
            leftCol.Children.Add(new TextBlock { Text = "Bác sĩ", FontSize = 12, Foreground = new SolidColorBrush(Microsoft.UI.Colors.SlateGray) });
            leftCol.Children.Add(new TextBlock { Text = appointment.DoctorName, FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 16 });
            
            var rightCol = new StackPanel();
            rightCol.Children.Add(new TextBlock { Text = "Thời gian", FontSize = 12, Foreground = new SolidColorBrush(Microsoft.UI.Colors.SlateGray) });
            rightCol.Children.Add(new TextBlock { Text = appointment.FullDateTime, FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 16 });
            
            Grid.SetColumn(leftCol, 0);
            Grid.SetColumn(rightCol, 1);
            infoGrid.Children.Add(leftCol);
            infoGrid.Children.Add(rightCol);
            
            stack.Children.Add(infoGrid);
            stack.Children.Add(new TextBlock { 
                Text = "Bạn có muốn tham gia buổi khám này không?", 
                FontSize = 14, 
                Margin = new Thickness(0, 8, 0, 0) 
            });
            
            var warningBorder = new Border {
                Background = new SolidColorBrush(ColorHelper.FromArgb(25, 245, 158, 11)), // Amber 50 approx
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 245, 158, 11)),
                BorderThickness = new Thickness(1)
            };
            var warningStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            warningStack.Children.Add(new FontIcon { Glyph = "\uE7BA", FontSize = 16, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 245, 158, 11)) });
            warningStack.Children.Add(new TextBlock { 
                Text = "Cảnh báo: Vui lòng kiểm tra kết nối mạng và thiết bị trước khi tham gia.", 
                FontSize = 12, 
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 180, 83, 9)),
                TextWrapping = TextWrapping.Wrap,
                Width = 300
            });
            warningBorder.Child = warningStack;
            stack.Children.Add(warningBorder);

            var dialog = new ContentDialog
            {
                Title = "Xác nhận tham gia khám",
                Content = stack,
                PrimaryButtonText = "Tham gia",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Frame.Navigate(typeof(PatientExaminationPage), appointment.Id);
            }
        }
    }
}
