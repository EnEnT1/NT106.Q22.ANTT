using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.Models.Core;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.APIClient;
using Healthcare.Client.UI.Auth;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Healthcare.Client.UI.Admin
{
    public sealed partial class AdminHomePage : Page
    {
        private string _currentTab = "Patient"; // Lưu trữ tab hiện tại đang được chọn
        private readonly AdminApiClient _adminApiClient = new AdminApiClient();

        public AdminHomePage()
        {
            this.InitializeComponent();
            LoadDataAsync(); // Tải dữ liệu lần đầu khi vào trang
        }

        private async void LoadDataAsync()
        {
            try
            {
                // Làm trống danh sách cũ trong khi đang tải dữ liệu mới
                AdminDataGrid.ItemsSource = null;

                switch (_currentTab)
                {
                    case "Patient":
                        // Truy vấn bảng User và lọc theo vai trò Bệnh nhân để lấy đầy đủ Họ tên/Email
                        var patients = await SupabaseManager.Instance.Client
                            .From<User>()
                            .Where(u => u.Role == "Patient")
                            .Get();
                        AdminDataGrid.ItemsSource = patients.Models;
                        ActionButtonsPanel.Visibility = Visibility.Visible;
                        break;

                    case "Doctor":
                        // Truy vấn bảng User và lọc theo vai trò Bác sĩ
                        var doctors = await SupabaseManager.Instance.Client
                            .From<User>()
                            .Where(u => u.Role == "Doctor")
                            .Get();
                        AdminDataGrid.ItemsSource = doctors.Models;
                        ActionButtonsPanel.Visibility = Visibility.Visible;
                        break;

                    case "Appointment":
                        // Hiển thị danh sách tất cả các lịch hẹn
                        AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<Appointment>();
                        ActionButtonsPanel.Visibility = Visibility.Collapsed;
                        break;

                    case "Transaction":
                        // Hiển thị lịch sử giao dịch thanh toán
                        AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<Transaction>();
                        ActionButtonsPanel.Visibility = Visibility.Collapsed;
                        break;

                    default:
                        ActionButtonsPanel.Visibility = Visibility.Collapsed;
                        break;
                }
            }
            catch (Exception ex) 
            { 
                await ShowDialogAsync("Lỗi hệ thống", $"Không thể tải dữ liệu: {ex.Message}"); 
            }
        }

        private void AdminNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            // Xử lý khi người dùng nhấn vào các mục trên thanh điều hướng bên trái
            if (args.InvokedItemContainer?.Tag != null)
            {
                _currentTab = args.InvokedItemContainer.Tag.ToString();
                HeaderTitle.Text = args.InvokedItem.ToString();
                LoadDataAsync();
            }
        }

        private async void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            // Hiển thị hộp thoại xác nhận khi người dùng nhấn Đăng xuất
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
                // Nếu chọn Đăng xuất, điều hướng quay lại trang Đăng nhập
                Frame.Navigate(typeof(LoginPage));
            }
        }

        private async void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            // Tạo các ô nhập liệu cho hộp thoại thêm mới
            TextBox emailBox = new TextBox { Header = "Địa chỉ Email", PlaceholderText = "ví dụ: vancuong@gmail.com", Margin = new Thickness(0, 0, 0, 12) };
            PasswordBox passBox = new PasswordBox { Header = "Mật khẩu", PlaceholderText = "Nhập mật khẩu ít nhất 6 ký tự", Margin = new Thickness(0, 0, 0, 12) };
            TextBox nameBox = new TextBox { Header = "Họ và Tên", PlaceholderText = "Nhập tên đầy đủ", Margin = new Thickness(0, 0, 0, 12) };
            
            StackPanel panel = new StackPanel { Padding = new Thickness(0, 8, 0, 0) };
            panel.Children.Add(nameBox);
            panel.Children.Add(emailBox);
            panel.Children.Add(passBox);

            string roleLabel = _currentTab == "Doctor" ? "Bác sĩ" : "Bệnh nhân";
            string roleValue = _currentTab == "Doctor" ? "Doctor" : "Patient";

            // Hiển thị hộp thoại tạo tài khoản mới
            ContentDialog dialog = new ContentDialog
            {
                Title = $"Tạo tài khoản {roleLabel} mới",
                Content = panel,
                PrimaryButtonText = "Xác nhận tạo",
                CloseButtonText = "Hủy bỏ",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                // Kiểm tra tính hợp lệ của dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(emailBox.Text) || string.IsNullOrWhiteSpace(passBox.Password) || string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    await ShowDialogAsync("Thông báo", "Vui lòng nhập đầy đủ tất cả các trường thông tin.");
                    return;
                }

                try
                {
                    // Gọi API Server để tạo tài khoản đồng bộ cả Auth và Database
                    bool success = await _adminApiClient.CreateUserViaServerAsync(emailBox.Text, passBox.Password, nameBox.Text, roleValue);
                    if (success)
                    {
                        await ShowDialogAsync("Thành công", $"Hệ thống đã tạo tài khoản {roleLabel} thành công.");
                        LoadDataAsync();
                    }
                    else
                    {
                        await ShowDialogAsync("Thất bại", "Không thể tạo tài khoản. Có thể email đã tồn tại trên hệ thống.");
                    }
                }
                catch (Exception ex)
                {
                    await ShowDialogAsync("Lỗi", ex.Message);
                }
            }
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem đã có dòng nào được chọn hay chưa
            if (AdminDataGrid.SelectedItem == null)
            {
                await ShowDialogAsync("Thông báo", "Vui lòng chọn một dòng trong danh sách để thực hiện xóa.");
                return;
            }
            await ExecuteDelete(AdminDataGrid.SelectedItem);
        }

        private async void AdminDataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Hỗ trợ xóa nhanh bằng cách nhấn đúp chuột vào dòng
            if (AdminDataGrid.SelectedItem != null) 
                await ExecuteDelete(AdminDataGrid.SelectedItem);
        }

        private async Task ExecuteDelete(object item)
        {
            // Chỉ cho phép xóa các dòng là Người dùng (Bệnh nhân/Bác sĩ)
            if (item is not User user) return;

            ContentDialog dialog = new ContentDialog
            {
                Title = "Xác nhận xóa tài khoản",
                Content = $"Bạn có chắc chắn muốn xóa vĩnh viễn người dùng '{user.FullName}' khỏi hệ thống?\nHành động này sẽ xóa sạch dữ liệu và không thể khôi phục.",
                PrimaryButtonText = "Xóa ngay",
                CloseButtonText = "Giữ lại",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try
                {
                    // Gọi API Server để xóa người dùng khỏi hệ thống
                    if (await _adminApiClient.DeleteUserViaServerAsync(user.Id))
                    {
                        LoadDataAsync(); // Tải lại danh sách sau khi xóa thành công
                    }
                    else
                    {
                        await ShowDialogAsync("Thất bại", "Hệ thống gặp sự cố khi đang xóa người dùng này.");
                    }
                }
                catch (Exception ex)
                {
                    await ShowDialogAsync("Lỗi", ex.Message);
                }
            }
        }

        private async Task ShowDialogAsync(string title, string message)
        {
            // Hàm tiện ích để hiển thị thông báo nhanh
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}