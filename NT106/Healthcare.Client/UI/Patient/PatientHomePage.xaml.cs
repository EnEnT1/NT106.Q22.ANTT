using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Healthcare.Client.UI.Shell;
using Healthcare.Client.UI.Components;
using System;
using System.Threading.Tasks;

namespace Healthcare.Client.UI.Patient
{
    /// <summary>
    /// Trang chủ của bệnh nhân — Main Content Page.
    /// Được load vào PatientShell.ContentFrame.
    /// </summary>
    public sealed partial class PatientHomePage : Page
    {
        // Tham chiếu tới PatientShell, nhận qua NavigationEventArgs.Parameter
        private PatientShell? _shell;

        public PatientHomePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // PatientShell truyền chính nó qua parameter khi Navigate
            if (e.Parameter is PatientShell shell)
            {
                _shell = shell;
            }
        }

        // ──────────────────────────────────────────────
        // Quick Access Button Handlers
        // ──────────────────────────────────────────────

        private void QuickAccess_BookAppointment_Click(object sender, RoutedEventArgs e)
        {
            _shell?.NavigateToPage(typeof(BookAppointmentPage));
        }

        private void QuickAccess_OnlineConsult_Click(object sender, RoutedEventArgs e)
        {
            // Tạm thời cho khám từ xa dẫn sang kết quả xét nghiệm / tư vấn online tùy project
            _shell?.NavigateToPage(typeof(LabResultsPage));
        }

        private void QuickAccess_Records_Click(object sender, RoutedEventArgs e)
        {
            _shell?.NavigateToPage(typeof(MyRecordsPage));
        }

        private void QuickAccess_LabResults_Click(object sender, RoutedEventArgs e)
        {
            _shell?.NavigateToPage(typeof(LabResultsPage));
        }

        private void QuickAccess_HealthMetrics_Click(object sender, RoutedEventArgs e)
        {
            _shell?.NavigateToPage(typeof(HealthMetricsPage));
        }

        // Card "Tư vấn AI"
        private async void QuickAccess_ChatbotAI_Click(object sender, RoutedEventArgs e)
        {
            await OpenChatbotDialogAsync();
        }

        // Nút nổi chat AI góc phải dưới
        private async void FloatingChatbot_Click(object sender, RoutedEventArgs e)
        {
            await OpenChatbotDialogAsync();
        }

        // Nút nổi cài đặt góc phải dưới
        private async void FloatingSettings_Click(object sender, RoutedEventArgs e)
        {
            await OpenSettingsDialogAsync();
        }

        // ──────────────────────────────────────────────
        // Dialog mở ChatGPT / Chatbot AI
        // ──────────────────────────────────────────────

        private async Task OpenChatbotDialogAsync()
        {
            var chatbot = new ChatbotAIControl
            {
                Width = 560,
                Height = 620
            };

            var dialog = new ContentDialog
            {
                Title = "Tư vấn sức khỏe AI",
                Content = chatbot,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }

        // ──────────────────────────────────────────────
        // Dialog mở Cài đặt / Profile
        // ──────────────────────────────────────────────

        private async Task OpenSettingsDialogAsync()
        {
            var profile = new ProfileControl
            {
                Width = 560,
                Height = 600
            };

            var dialog = new ContentDialog
            {
                Title = "Cài đặt tài khoản",
                Content = profile,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }
    }
}