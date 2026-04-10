using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Communication;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.APIClient;
using Healthcare.Client.UI.Auth;
using System;
using System.Threading.Tasks;

namespace Healthcare.Client.UI.Admin
{
    public sealed partial class AdminHomePage : Page
    {
        private string _currentTab = "Patient";

        public AdminHomePage()
        {
            this.InitializeComponent();
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            try
            {
                switch (_currentTab)
                {
                    case "Patient":
                        AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<PatientProfile>();
                        ActionButtonsPanel.Visibility = Visibility.Visible;
                        break;
                    case "Doctor":
                        AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<DoctorProfile>();
                        ActionButtonsPanel.Visibility = Visibility.Visible;
                        break;
                    default:
                        if (_currentTab == "Appointment") AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<Appointment>();
                        else AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<Transaction>();
                        ActionButtonsPanel.Visibility = Visibility.Collapsed;
                        break;
                }
            }
            catch (Exception ex) { await ShowDialogAsync("Lỗi", ex.Message); }
        }

        private void AdminNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer.Tag != null)
            {
                _currentTab = args.InvokedItemContainer.Tag.ToString();
                HeaderTitle.Text = args.InvokedItem.ToString();
                LoadDataAsync();
            }
        }

        
        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng về trang đăng nhập
            Frame.Navigate(typeof(LoginPage));
        }

        private async void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            TextBox emailBox = new TextBox { Header = "Email", Margin = new Thickness(0, 0, 0, 10) };
            PasswordBox passBox = new PasswordBox { Header = "Mật khẩu", Margin = new Thickness(0, 0, 0, 10) };
            TextBox nameBox = new TextBox { Header = "Họ tên", Margin = new Thickness(0, 0, 0, 10) };
            StackPanel panel = new StackPanel { Children = { emailBox, passBox, nameBox } };

            string role = _currentTab == "Doctor" ? "Doctor" : "Patient";
            ContentDialog dialog = new ContentDialog { Title = $"Thêm {role}", Content = panel, PrimaryButtonText = "Tạo", CloseButtonText = "Hủy", XamlRoot = this.XamlRoot };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                bool success = await new AdminApiClient().CreateUserViaServerAsync(emailBox.Text, passBox.Password, nameBox.Text, role);
                if (success) { await ShowDialogAsync("Thành công", "Đã tạo tài khoản."); LoadDataAsync(); }
            }
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AdminDataGrid.SelectedItem == null) return;
            await ExecuteDelete(AdminDataGrid.SelectedItem);
        }

        private async void AdminDataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (AdminDataGrid.SelectedItem != null) await ExecuteDelete(AdminDataGrid.SelectedItem);
        }

        private async Task ExecuteDelete(object item)
        {
            if (item is not PatientProfile && item is not DoctorProfile) return;
            ContentDialog dialog = new ContentDialog { Title = "Xác nhận xóa", Content = "Xóa vĩnh viễn tài khoản này?", PrimaryButtonText = "Xóa", CloseButtonText = "Hủy", XamlRoot = this.XamlRoot };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                string id = (item is PatientProfile p) ? p.PatientId : ((DoctorProfile)item).DoctorId;
                if (await new AdminApiClient().DeleteUserViaServerAsync(id)) { LoadDataAsync(); }
            }
        }

        private async Task ShowDialogAsync(string t, string m)
        {
            await new ContentDialog { Title = t, Content = m, CloseButtonText = "Đóng", XamlRoot = this.XamlRoot }.ShowAsync();
        }
    }
}