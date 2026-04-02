using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.Models.Core;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.APIClient;
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
                // Dùng SupabaseDbService để kéo dữ liệu trực tiếp từ Database
                switch (_currentTab)
                {
                    case "Patient":
                        AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<PatientProfile>();
                        break;
                    case "Doctor":
                        AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<DoctorProfile>();
                        break;
                    case "Appointment":
                        AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<Appointment>();
                        break;
                    case "Transaction":
                        AdminDataGrid.ItemsSource = await SupabaseDbService.GetAllAsync<Transaction>();
                        break;
                }
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi tải dữ liệu", ex.Message);
            }
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

        private async void AdminDataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var selectedItem = AdminDataGrid.SelectedItem;
            if (selectedItem == null) return;

            if (selectedItem is not PatientProfile && selectedItem is not DoctorProfile)
                return;

            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Cảnh báo xóa vĩnh viễn",
                Content = "Bạn có chắc chắn muốn xóa tài khoản này? Hành động này sẽ xóa hoàn toàn thông tin đăng nhập và hồ sơ của họ trên toàn hệ thống.",
                PrimaryButtonText = "Xóa ngay",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string userIdToDelete = string.Empty;

                if (selectedItem is PatientProfile p)
                    userIdToDelete = p.PatientId;
                else if (selectedItem is DoctorProfile d)
                    userIdToDelete = d.DoctorId;

                if (!string.IsNullOrEmpty(userIdToDelete))
                {
                    // GỌI API LÊN SERVER CỦA BẠN ĐỂ XÓA (Cách Pro)
                    bool isSuccess = await AdminApiClient.DeleteUserViaServerAsync(userIdToDelete);

                    if (isSuccess)
                    {
                        await ShowDialogAsync("Thành công", "Đã xóa tài khoản khỏi hệ thống.");
                        LoadDataAsync(); // Refresh lại bảng
                    }
                    else
                    {
                        await ShowDialogAsync("Thất bại", "Không thể xóa tài khoản. Vui lòng kiểm tra lại Server ASP.NET Core đã chạy chưa.");
                    }
                }
            }
        }

        private async Task ShowDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
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