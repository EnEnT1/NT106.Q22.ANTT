using Healthcare.Client.Helpers;
using Healthcare.Client.UI.Auth;
using Healthcare.Client.UI.Doctor;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Models.Identity;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;

namespace Healthcare.Client.UI.Shell
{
    public sealed partial class DoctorShell : Page
    {
        // Nút nav đang active hiện tại
        private Button? _activeNavButton;

        public DoctorShell()
        {
            this.InitializeComponent();
            LoadDoctorInfo();
            LoadTopBarInfo();
            // Mặc định mở Trang chủ
            NavigateTo(typeof(DoctorHomePage), NavHome);
        }

        // ── Load thông tin bác sĩ từ SessionStorage vào sidebar ──
        private async void LoadDoctorInfo()
        {
            var user = SessionStorage.CurrentUser;
            if (user == null) return;

            TxtDoctorName.Text  = user.FullName ?? "Bác sĩ";
            DoctorAvatar.DisplayName = user.FullName ?? "Bác sĩ";
            TxtDepartment.Text  = "Khoa Nội Tổng Quát"; // Mặc định trước khi load DB

            try
            {
                var profileResponse = await SupabaseManager.Instance.Client
                    .From<DoctorProfile>()
                    .Where(x => x.DoctorId == user.Id)
                    .Get();

                var profile = profileResponse.Models.FirstOrDefault();
                if (profile != null && !string.IsNullOrEmpty(profile.Specialty))
                {
                    TxtDepartment.Text = profile.Specialty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading doctor profile: " + ex.Message);
            }
        }

        // ── Load ngày + ca trực lên TopBar ──
        private void LoadTopBarInfo()
        {
            TxtDate.Text  = DateTime.Now.ToString("dddd, dd/MM/yyyy",
                                new System.Globalization.CultureInfo("vi-VN"));
            
            // Tính toán ca trực dựa trên giờ hiện tại
            var hour = DateTime.Now.Hour;
            if (hour < 12)
            {
                TxtShift.Text = "CA TRỰC: SÁNG";
            }
            else if (hour < 18)
            {
                TxtShift.Text = "CA TRỰC: CHIỀU";
            }
            else
            {
                TxtShift.Text = "CA TRỰC: TỐI";
            }
        }

        // ── Điều hướng + cập nhật trạng thái active của nav button ──
        private void NavigateTo(Type pageType, Button sender)
        {
            // Reset active style cho nút cũ
            if (_activeNavButton != null)
            {
                _activeNavButton.Style = (Style)Resources["NavItemButtonStyle"];

                // Reset icon foreground
                if (_activeNavButton.Content is StackPanel sp &&
                    sp.Children[0] is FontIcon icon)
                {
                    icon.Foreground = (SolidColorBrush)Resources["SlateGray500"];
                }
                // Reset label foreground
                if (_activeNavButton.Content is StackPanel sp2 &&
                    sp2.Children[1] is TextBlock lbl)
                {
                    lbl.Foreground = (SolidColorBrush)Resources["SlateGray500"];
                    lbl.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                }
            }

            // Áp active style cho nút mới
            sender.Style = (Style)Resources["NavItemActiveStyle"];
            if (sender.Content is StackPanel spNew)
            {
                var accentBrush = (SolidColorBrush)Resources["PrimaryBrush"];
                if (spNew.Children[0] is FontIcon iconNew)
                    iconNew.Foreground = accentBrush;
                if (spNew.Children[1] is TextBlock lblNew)
                {
                    lblNew.Foreground  = accentBrush;
                    lblNew.FontWeight  = Microsoft.UI.Text.FontWeights.Bold;
                }
            }

            _activeNavButton = sender;
            ContentFrame.Navigate(pageType);
        }

        // ── Công khai: cho phép page con yêu cầu navigate ──
        public void SelectPage(string pageTag)
        {
            switch (pageTag)
            {
                case "DoctorHomePage":     NavigateTo(typeof(DoctorHomePage),     NavHome);     break;
                case "ManageSchedulePage": NavigateTo(typeof(ManageSchedulePage), NavSchedule); break;
                case "PatientHistoryPage": NavigateTo(typeof(PatientHistoryPage), NavPatients); break;
                case "RevenuePage":        NavigateTo(typeof(RevenuePage),        NavRevenue);  break;
            }
        }

        // ── Nav Click handlers ──
        private void NavHome_Click(object sender, RoutedEventArgs e)
            => NavigateTo(typeof(DoctorHomePage), NavHome);

        private void NavSchedule_Click(object sender, RoutedEventArgs e)
            => NavigateTo(typeof(ManageSchedulePage), NavSchedule);

        private void NavPatients_Click(object sender, RoutedEventArgs e)
            => NavigateTo(typeof(PatientHistoryPage), NavPatients);

        private void NavRevenue_Click(object sender, RoutedEventArgs e)
            => NavigateTo(typeof(RevenuePage), NavRevenue);

        // ── Notification button ──
        private void BtnNotification_Click(object sender, RoutedEventArgs e)
        {
            // TODO: mở NotificationPanel flyout
        }

        // ── Settings button ──
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navigate sang Settings page
        }

        // ── Đăng xuất ──
        private async void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title           = "Đăng xuất",
                Content         = "Bạn có chắc muốn đăng xuất không?",
                PrimaryButtonText  = "Đăng xuất",
                CloseButtonText    = "Hủy",
                XamlRoot        = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                SessionStorage.ClearSession();
                Frame.Navigate(typeof(LoginPage));
            }
        }
    }
}
