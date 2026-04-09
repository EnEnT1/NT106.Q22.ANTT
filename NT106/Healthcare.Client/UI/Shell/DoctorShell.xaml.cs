using Healthcare.Client.Helpers;
using Healthcare.Client.UI.Auth;
using Healthcare.Client.UI.Doctor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Healthcare.Client.UI.Shell
{
    public sealed partial class DoctorShell : Page
    {
        // Map Tag → Type để navigate
        private readonly Dictionary<string, Type> _pages = new()
        {
            { "DoctorHomePage",      typeof(DoctorHomePage) },
            { "ManageSchedulePage",  typeof(ManageSchedulePage) },
            { "ExaminationPage",     typeof(ExaminationPage) },
            { "PatientHistoryPage",  typeof(PatientHistoryPage) },
            { "RevenuePage",         typeof(RevenuePage) },
        };

        public DoctorShell()
        {
            this.InitializeComponent();
            LoadDoctorInfo();
            LoadTopBarInfo();
        }

        // ── Load thông tin bác sĩ từ SessionStorage vào sidebar ──
        private void LoadDoctorInfo()
        {
            var user = SessionStorage.CurrentUser;
            if (user == null) return;

            TxtDoctorName.Text = user.FullName ?? "Bác sĩ";
            TxtDepartment.Text = user.FullName ?? "Khoa Nội Tổng Quát";
        }

        // ── Load ngày + ca trực lên TopBar ──
        private void LoadTopBarInfo()
        {
            TxtDate.Text = DateTime.Now.ToString("dddd, dd MMMM",
                                new System.Globalization.CultureInfo("vi-VN"));
            TxtShift.Text = "CA TRỰC: SÁNG"; // TODO: lấy từ DB theo ngày
        }

        // ── Khi NavigationView load xong, chọn mặc định Home ──
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(DoctorHomePage));
        }

        // ── Khi user click menu item ──
        private void NavView_SelectionChanged(NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item
                && item.Tag is string tag
                && _pages.TryGetValue(tag, out var pageType))
            {
                ContentFrame.Navigate(pageType);
            }
        }
        public void SelectNavItem(string pageTag)
        {
            if (_pages.TryGetValue(pageTag, out var pageType))
            {
                var targetItem = NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == pageTag);

                if (targetItem != null)
                {
                    NavView.SelectedItem = targetItem;
                }
                else
                {
                    ContentFrame.Navigate(pageType);
                }
            }
        }


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
        private async void NavLogout_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Đăng xuất",
                Content = "Bạn có chắc muốn đăng xuất không?",
                PrimaryButtonText = "Đăng xuất",
                CloseButtonText = "Hủy",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                SessionStorage.ClearSession();
                // Navigate về LoginPage
                Frame.Navigate(typeof(LoginPage));
            }
        }
    }
}
