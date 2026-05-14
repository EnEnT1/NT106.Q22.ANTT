using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Healthcare.Client.UI.Shell;
using Healthcare.Client.UI.Components;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Patient
{
    public sealed partial class PatientHomePage : Page
    {
        private PatientShell? _shell;

        private readonly ObservableCollection<SuggestedDoctorViewModel> _suggestedDoctors = new();

        public PatientHomePage()
        {
            this.InitializeComponent();
            this.Loaded += PatientHomePage_Loaded;

            SuggestedDoctorsListView.ItemsSource = _suggestedDoctors;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is PatientShell shell)
            {
                _shell = shell;
            }
        }

        private async void PatientHomePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSuggestedDoctorsAsync();
        }

        private async Task LoadSuggestedDoctorsAsync()
        {
            try
            {
                _suggestedDoctors.Clear();

                var client = SupabaseManager.Instance.Client;

                var doctorProfileResponse = await client
                    .From<DoctorProfile>()
                    .Get();

                var userResponse = await client
                    .From<User>()
                    .Get();

                var doctorProfiles = doctorProfileResponse.Models ?? new List<DoctorProfile>();
                var users = userResponse.Models ?? new List<User>();

                foreach (var doctorProfile in doctorProfiles.Take(8))
                {
                    var user = users.FirstOrDefault(u => u.Id == doctorProfile.DoctorId);

                    if (user == null)
                        continue;

                    string fullName = string.IsNullOrWhiteSpace(user.FullName)
                        ? "Bác sĩ"
                        : user.FullName;

                    string specialty = GetDoctorSpecialty(doctorProfile);

                    _suggestedDoctors.Add(new SuggestedDoctorViewModel
                    {
                        DoctorId = doctorProfile.DoctorId ?? string.Empty,
                        FullName = fullName,
                        DisplayName = fullName.StartsWith("BS", StringComparison.OrdinalIgnoreCase)
                            ? fullName
                            : "BS. " + fullName,
                        Initials = GetInitials(fullName),
                        Specialty = string.IsNullOrWhiteSpace(specialty)
                            ? "Chưa cập nhật chuyên khoa"
                            : specialty,
                        StatusColor = new SolidColorBrush(ParseColor("#22C55E"))
                    });
                }

                SuggestedDoctorEmptyState.Visibility =
                    _suggestedDoctors.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                SuggestedDoctorsListView.Visibility =
                    _suggestedDoctors.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                SuggestedDoctorEmptyState.Visibility = Visibility.Visible;
                SuggestedDoctorsListView.Visibility = Visibility.Collapsed;
            }
        }

        private void SuggestedDoctorBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string doctorId)
            {
                _shell?.NavigateToPage(typeof(BookAppointmentPage));
            }
        }

        private static string GetDoctorSpecialty(DoctorProfile doctorProfile)
        {
            string[] possibleNames =
            {
                "Specialty",
                "Specialization",
                "Department",
                "DepartmentName",
                "Major",
                "Expertise"
            };

            foreach (string name in possibleNames)
            {
                PropertyInfo? prop = doctorProfile.GetType().GetProperty(name);

                if (prop == null)
                    continue;

                object? value = prop.GetValue(doctorProfile);

                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                    return value.ToString()!;
            }

            return string.Empty;
        }

        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "BS";

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
                return parts[0].Substring(0, 1).ToUpper();

            return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpper();
        }

        private static Color ParseColor(string hex)
        {
            hex = hex.TrimStart('#');

            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16)
            );
        }

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

        private async void QuickAccess_ChatbotAI_Click(object sender, RoutedEventArgs e)
        {
            await OpenChatbotDialogAsync();
        }

        private async Task OpenChatbotDialogAsync()
        {
            var chatbot = new ChatbotAIControl
            {
                Width = 460,
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

        private async Task OpenSettingsDialogAsync()
        {
            var profile = new ProfileControl
            {
                Width = 460,
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

    public class SuggestedDoctorViewModel
    {
        public string DoctorId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Initials { get; set; } = "BS";

        public string Specialty { get; set; } = "Chưa cập nhật chuyên khoa";

        public Brush StatusColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
    }
}