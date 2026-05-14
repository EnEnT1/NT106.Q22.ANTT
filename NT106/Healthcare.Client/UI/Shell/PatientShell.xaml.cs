using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Healthcare.Client.UI.Patient;
using Healthcare.Client.UI.Auth;
using Healthcare.Client.UI.Components;
using Healthcare.Client.Helpers;
using System;
using System.Collections.Generic;

namespace Healthcare.Client.UI.Shell
{
    /// <summary>
    /// Shell page chứa SideBar và TopBar.
    /// PatientHomePage được load vào ContentFrame.
    /// </summary>
    public sealed partial class PatientShell : Page
    {
        // Danh sách tất cả nav buttons để reset active state
        private List<Button> _navButtons;

        public PatientShell()
        {
            this.InitializeComponent();
            LoadUserInfo();
            UpdateDateDisplay();

            // Khởi tạo danh sách nav buttons
            _navButtons = new List<Button>
            {
                NavHome, NavAppointment, NavRecords,
                NavPayment, NavHealthMetrics, NavOnline,
                NavUploadPrescription
            };

            // Subscribe notification panel events
            NotifPanel.UnreadCountChanged += NotifPanel_UnreadCountChanged;
            NotifPanel.NavigationRequested += NotifPanel_NavigationRequested;

            // Navigate tới trang chủ khi load
            ContentFrame.Navigate(typeof(PatientHomePage), this);
            SetActiveButton(NavHome);
        }

        // ──────────────────────────────────────────────
        // Notification Panel Handlers
        // ──────────────────────────────────────────────

        private void NotifPanel_UnreadCountChanged(object sender, int count)
        {
            NotifBadgeText.Text = count.ToString();
            NotifBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NotifPanel_NavigationRequested(object sender, NotificationNavigationRequestedEventArgs e)
        {
            // Close the flyout
            NotificationBellButton.Flyout?.Hide();

            // Navigate to the target page
            if (e.TargetPageType != null)
            {
                NavigateToPage(e.TargetPageType);
            }
        }

        private void LoadUserInfo()
        {
            var user = SessionStorage.CurrentUser;
            if (user == null) return;

            TxtPatientName.Text = user.FullName ?? "Bệnh nhân";
            TxtPatientID.Text = "ID: " + (user.Id?.Length > 6 ? user.Id.Substring(0, 6).ToUpper() : user.Id?.ToUpper());
            PatientAvatar.DisplayName = user.FullName;
        }

        // ──────────────────────────────────────────────
        // Sidebar Navigation Handlers
        // ──────────────────────────────────────────────

        private void NavHome_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(typeof(PatientHomePage));
            SetActiveButton(NavHome);
        }

        private void NavAppointment_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(typeof(BookAppointmentPage));
            SetActiveButton(NavAppointment);
        }

        private void NavRecords_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(typeof(MyRecordsPage));
            SetActiveButton(NavRecords);
        }

        private void NavPayment_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(typeof(PaymentCheckoutPage));
            SetActiveButton(NavPayment);
        }

        private void NavHealthMetrics_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(typeof(HealthMetricsPage));
            SetActiveButton(NavHealthMetrics);
        }

        private void NavOnline_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(typeof(OnlineConsultationPage));
            SetActiveButton(NavOnline);
        }

        private void NavUploadPrescription_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(typeof(UploadPrescriptionPage));
            SetActiveButton(NavUploadPrescription);
        }


        // ──────────────────────────────────────────────
        // Public: Được gọi từ PatientHomePage (Quick Access)
        // ──────────────────────────────────────────────

        public void NavigateToPage(Type pageType)
        {
            NavigateTo(pageType);

            // Cập nhật active state tương ứng trang
            if (pageType == typeof(PatientHomePage))       SetActiveButton(NavHome);
            else if (pageType == typeof(BookAppointmentPage)) SetActiveButton(NavAppointment);
            else if (pageType == typeof(MyRecordsPage))    SetActiveButton(NavRecords);
            else if (pageType == typeof(PaymentCheckoutPage)) SetActiveButton(NavPayment);
            else if (pageType == typeof(HealthMetricsPage)) SetActiveButton(NavHealthMetrics);
            else if (pageType == typeof(OnlineConsultationPage))   SetActiveButton(NavOnline);
            else if (pageType == typeof(UploadPrescriptionPage)) SetActiveButton(NavUploadPrescription);
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────
        // Đăng xuất
        // ──────────────────────────────────────────────
        private async void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Đăng xuất",
                Content = "Bạn có chắc muốn đăng xuất không?",
                PrimaryButtonText = "Đăng xuất",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                SessionStorage.ClearSession();
                Frame.Navigate(typeof(LoginPage));
            }
        }

        // ──────────────────────────────────────────────

        private void NavigateTo(Type pageType)
        {
            if (ContentFrame.CurrentSourcePageType != pageType)
                ContentFrame.Navigate(pageType, this); // truyền shell để trang con có thể gọi lại
        }

        /// <summary>
        /// Đặt button active, reset tất cả các button còn lại về inactive.
        /// </summary>
        private void SetActiveButton(Button activeButton)
        {
            var activeStyle   = (Style)Resources["NavItemActiveStyle"];
            var inactiveStyle = (Style)Resources["NavItemButtonStyle"];

            foreach (var btn in _navButtons)
            {
                btn.Style = (btn == activeButton) ? activeStyle : inactiveStyle;
            }
        }

        /// <summary>
        /// Cập nhật hiển thị ngày hiện tại trên TopBar.
        /// </summary>
        private void UpdateDateDisplay()
        {
            var now = DateTime.Now;
            string[] dayNames = { "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy" };
            string dayName = dayNames[(int)now.DayOfWeek];
            DateTextBlock.Text = $"{dayName}, {now:dd/MM/yyyy}";
        }
    }
}
