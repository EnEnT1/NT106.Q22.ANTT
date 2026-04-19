using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Healthcare.Client.Helpers;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Healthcare.Client.UI.Patient
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PatientHomePage : Page
    {
        public PatientHomePage()
        {
            this.InitializeComponent();
            LoadCurrentUser();
            LoadDashboardData();
        }

        private void LoadCurrentUser()
        {
            var user = SessionStorage.CurrentUser;

            if (user == null)
            {
                GreetingText.Text = "Chào bạn";
                SubGreetingText.Text = "Không tìm thấy thông tin đăng nhập.";

                FullNameText.Text = "Họ tên: Chưa có dữ liệu";
                EmailText.Text = "Email: Chưa có dữ liệu";
                PhoneText.Text = "Số điện thoại: Chưa có dữ liệu";
                RoleText.Text = "Vai trò: Chưa có dữ liệu";
                return;
            }

            GreetingText.Text = $"Chào, {user.FullName}";
            SubGreetingText.Text = "Chúc bạn có một ngày thật khỏe mạnh.";

            FullNameText.Text = $"Họ tên: {user.FullName}";
            EmailText.Text = $"Email: {user.Email}";
            PhoneText.Text = $"Số điện thoại: {user.Phone}";
            RoleText.Text = $"Vai trò: {user.Role}";
        }

        private void LoadDashboardData()
        {
            AppointmentCountText.Text = "2";
            RecordCountText.Text = "5";
            LabCountText.Text = "1";

            LatestAppointmentTitleText.Text = "Khám tổng quát định kỳ";
            LatestAppointmentDateText.Text = "Ngày khám: 20/04/2026 - 08:30";
            LatestAppointmentDoctorText.Text = "Bác sĩ: Minh Tâm";
            LatestAppointmentStatusText.Text = "Trạng thái: Sắp tới";
        }

        private void BookAppointment_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(BookAppointmentPage));
        }

        private async void MyRecords_Click(object sender, RoutedEventArgs e)
        {
            await ShowMessage("Đi tới trang Hồ sơ của tôi");
        }

        private async void HealthMetrics_Click(object sender, RoutedEventArgs e)
        {
            await ShowMessage("Đi tới trang Chỉ số sức khỏe");
        }

        private async void LabResults_Click(object sender, RoutedEventArgs e)
        {
            await ShowMessage("Đi tới trang Kết quả xét nghiệm");
        }

        private async Task ShowMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Thông báo",
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
