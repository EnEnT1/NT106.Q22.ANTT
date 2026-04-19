using Healthcare.Client.Helpers;
using Healthcare.Client.UI.Auth;
using Healthcare.Client.UI.Patient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Healthcare.Client.UI.Shell
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PatientShell : Page
    {
        private readonly Dictionary<string, Type> _pages = new()
        {
            { "PatientHomePage", typeof(PatientHomePage) },
            { "BookAppointmentPage", typeof(BookAppointmentPage) },
        };

        public PatientShell()
        {
            InitializeComponent();
            LoadPatientInfo();
            TxtDate.Text = DateTime.Now.ToString("dddd, dd MMMM", new System.Globalization.CultureInfo("vi-VN"));
        }

        private void LoadPatientInfo()
        {
            var user = SessionStorage.CurrentUser;
            if (user == null) return;
            TxtPatientName.Text = user.FullName ?? "Bệnh nhân";
            PatientAvatar.DisplayName = user.FullName ?? "B";
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(PatientHomePage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag && _pages.TryGetValue(tag, out var pageType))
            {
                ContentFrame.Navigate(pageType);
            }
        }

        private async void NavLogout_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Đăng xuất",
                Content = "Bạn có chắc muốn đăng xuất không?",
                PrimaryButtonText = "Đăng xuất",
                CloseButtonText = "Hủy",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                SessionStorage.ClearSession();
                Frame.Navigate(typeof(Healthcare.Client.UI.Auth.LoginPage));
            }
        }
    }
}
