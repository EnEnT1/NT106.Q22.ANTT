using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Healthcare.Client.UI.Shell;
using System;

namespace Healthcare.Client.UI.Patient
{
    /// <summary>
    /// Trang chủ của bệnh nhân — Main Content Page.
    /// Được load vào PatientShell.ContentFrame.
    /// </summary>
    public sealed partial class PatientHomePage : Page
    {
        // Tham chiếu tới PatientShell, nhận qua NavigationEventArgs.Parameter
        private PatientShell _shell;

        public PatientHomePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // PatientShell truyền chính nó qua parameter khi Navigate
            if (e.Parameter is PatientShell shell)
                _shell = shell;
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
    }
}
