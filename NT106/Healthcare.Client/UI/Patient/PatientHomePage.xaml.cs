using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Healthcare.Client.UI.Shell;
using Healthcare.Client.UI.Components;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Helpers;
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
        private string _upcomingAppointmentId = string.Empty;

        private readonly ObservableCollection<SuggestedDoctorViewModel> _suggestedDoctors = new();

        public PatientHomePage()
        {
            this.InitializeComponent();
            this.Loaded += PatientHomePage_Loaded;
            this.Unloaded += PatientHomePage_Unloaded;

            SuggestedDoctorsListView.ItemsSource = _suggestedDoctors;
        }

        private void PatientHomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            DeviceTestCardControl.StopDeviceTesting();
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
            LoadWelcomeText();
            await LoadUpcomingAppointmentAsync();
            await LoadHealthMetricsAsync();
            await LoadSuggestedDoctorsAsync();
        }

        private void LoadWelcomeText()
        {
            var user = SessionStorage.CurrentUser;
            var hour = DateTime.Now.Hour;

            string greeting = hour < 12 ? "Chào buổi sáng"
                : hour < 18 ? "Chào buổi chiều"
                : "Chào buổi tối";

            WelcomeTextBlock.Text = $"{greeting}, {(user != null ? (user.FullName ?? "Bệnh nhân") : "Bệnh nhân")}";
        }

        private async Task LoadUpcomingAppointmentAsync()
        {
            try
            {
                var user = SessionStorage.CurrentUser;
                if (user == null || string.IsNullOrWhiteSpace(user.Id))
                {
                    _upcomingAppointmentId = string.Empty;
                    AppointmentActiveState.Visibility = Visibility.Collapsed;
                    AppointmentEmptyState.Visibility = Visibility.Visible;
                    return;
                }

                var client = SupabaseManager.Instance.Client;

                var apptResponse = await client
                    .From<Appointment>()
                    .Where(x => x.PatientId == user.Id)
                    .Get();

                var appointments = apptResponse.Models ?? new List<Appointment>();

                var now = AppointmentDateTimeHelper.NowVietnam;
                var upcomingAppt = appointments
                    .Where(x => AppointmentDateTimeHelper.GetEnd(x) >= now &&
                                x.Status != "Completed" &&
                                x.Status != "Cancelled")
                    .OrderBy(AppointmentDateTimeHelper.GetStart)
                    .FirstOrDefault();

                if (upcomingAppt == null)
                {
                    _upcomingAppointmentId = string.Empty;
                    AppointmentActiveState.Visibility = Visibility.Collapsed;
                    AppointmentEmptyState.Visibility = Visibility.Visible;
                    return;
                }

                _upcomingAppointmentId = upcomingAppt.Id;

                string doctorName = "Bác sĩ";
                string specialty = "Chuyên khoa";
                string initials = "BS";

                if (!string.IsNullOrWhiteSpace(upcomingAppt.DoctorId))
                {
                    var doctorUserResponse = await client
                        .From<User>()
                        .Where(x => x.Id == upcomingAppt.DoctorId)
                        .Get();

                    var doctorUser = doctorUserResponse.Models?.FirstOrDefault();
                    if (doctorUser != null)
                    {
                        doctorName = doctorUser.FullName ?? "Bác sĩ";
                        if (!doctorName.StartsWith("BS", StringComparison.OrdinalIgnoreCase))
                        {
                            doctorName = "BS. " + doctorName;
                        }

                        initials = GetInitials(doctorUser.FullName);
                    }

                    var doctorProfileResponse = await client
                        .From<DoctorProfile>()
                        .Where(x => x.DoctorId == upcomingAppt.DoctorId)
                        .Get();

                    var doctorProfile = doctorProfileResponse.Models?.FirstOrDefault();
                    if (doctorProfile != null)
                    {
                        var spec = GetDoctorSpecialty(doctorProfile);
                        if (!string.IsNullOrWhiteSpace(spec))
                        {
                            specialty = spec;
                        }
                    }
                }

                var apptDate = AppointmentDateTimeHelper.ToVietnamDate(upcomingAppt.AppointmentDate);
                string[] dayNames = { "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy" };
                string dayOfWeekStr = dayNames[(int)apptDate.DayOfWeek];

                AppointmentDateTextBlock.Text = $"{dayOfWeekStr}, {apptDate:dd} Tháng {apptDate:MM}";
                AppointmentTimeTextBlock.Text = AppointmentDateTimeHelper.GetStart(upcomingAppt).ToString("hh:mm tt");

                AppointmentDoctorNameTextBlock.Text = doctorName;
                AppointmentDoctorSpecialtyTextBlock.Text = specialty;
                AppointmentDoctorPicture.Initials = initials;

                AppointmentActiveState.Visibility = Visibility.Visible;
                AppointmentEmptyState.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading upcoming appointment: " + ex.Message);
                _upcomingAppointmentId = string.Empty;
                AppointmentActiveState.Visibility = Visibility.Collapsed;
                AppointmentEmptyState.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadHealthMetricsAsync()
        {
            try
            {
                var user = SessionStorage.CurrentUser;
                if (user == null || string.IsNullOrWhiteSpace(user.Id))
                {
                    HealthIndicatorsCard.Visibility = Visibility.Collapsed;
                    Grid.SetColumnSpan(UpcomingAppointmentCard, 2);
                    return;
                }

                var client = SupabaseManager.Instance.Client;
                var healthMetricsResponse = await client
                    .From<HealthMetric>()
                    .Where(x => x.PatientId == user.Id)
                    .Get();

                var healthMetrics = healthMetricsResponse.Models ?? new List<HealthMetric>();

                if (healthMetrics.Count == 0)
                {
                    HealthIndicatorsCard.Visibility = Visibility.Collapsed;
                    Grid.SetColumnSpan(UpcomingAppointmentCard, 2);
                }
                else
                {
                    HealthIndicatorsCard.Visibility = Visibility.Visible;
                    Grid.SetColumnSpan(UpcomingAppointmentCard, 1);

                    var latestBp = healthMetrics
                        .Where(x => x.MetricType != null &&
                                    (x.MetricType.Equals("blood_pressure", StringComparison.OrdinalIgnoreCase) ||
                                     x.MetricType.Equals("huyết áp", StringComparison.OrdinalIgnoreCase)))
                        .OrderByDescending(x => x.MeasuredAt)
                        .FirstOrDefault();

                    var latestHr = healthMetrics
                        .Where(x => x.MetricType != null &&
                                    (x.MetricType.Equals("heart_rate", StringComparison.OrdinalIgnoreCase) ||
                                     x.MetricType.Equals("nhịp tim", StringComparison.OrdinalIgnoreCase)))
                        .OrderByDescending(x => x.MeasuredAt)
                        .FirstOrDefault();

                    if (latestBp == null && latestHr == null)
                    {
                        HealthIndicatorsCard.Visibility = Visibility.Collapsed;
                        Grid.SetColumnSpan(UpcomingAppointmentCard, 2);
                        return;
                    }

                    var latestTime = healthMetrics.Max(x => x.MeasuredAt);
                    var timeDiff = DateTime.UtcNow - latestTime.ToUniversalTime();

                    string timeDiffStr;
                    if (timeDiff.TotalMinutes < 60)
                    {
                        timeDiffStr = $"Cập nhật {(int)Math.Max(1, timeDiff.TotalMinutes)} phút trước";
                    }
                    else if (timeDiff.TotalHours < 24)
                    {
                        timeDiffStr = $"Cập nhật {(int)timeDiff.TotalHours} giờ trước";
                    }
                    else
                    {
                        timeDiffStr = $"Cập nhật {latestTime.ToLocalTime():dd/MM/yyyy}";
                    }

                    HealthMetricsUpdateTimeTextBlock.Text = timeDiffStr;

                    if (latestBp != null)
                    {
                        var diastolic = healthMetrics
                            .Where(x => x.MetricType != null &&
                                        (x.MetricType.Equals("diastolic", StringComparison.OrdinalIgnoreCase) ||
                                         x.MetricType.Equals("huyết áp tâm trương", StringComparison.OrdinalIgnoreCase)))
                            .OrderByDescending(x => x.MeasuredAt)
                            .FirstOrDefault();

                        BloodPressureValueTextBlock.Text = diastolic != null
                            ? $"{(int)latestBp.Value}/{(int)diastolic.Value}"
                            : $"{(int)latestBp.Value}/80";

                        BloodPressureStatusTextBlock.Text = EvaluateBloodPressure(BloodPressureValueTextBlock.Text);
                    }
                    else
                    {
                        BloodPressureValueTextBlock.Text = "--/--";
                        BloodPressureStatusTextBlock.Text = "Chưa đo";
                    }

                    if (latestHr != null)
                    {
                        HeartRateValueTextBlock.Text = $"{(int)latestHr.Value}";
                        HeartRateStatusTextBlock.Text = latestHr.Value >= 60 && latestHr.Value <= 100 ? "Ổn định" : "Cần theo dõi";
                    }
                    else
                    {
                        HeartRateValueTextBlock.Text = "--";
                        HeartRateStatusTextBlock.Text = "Chưa đo";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading health metrics: " + ex.Message);
                HealthIndicatorsCard.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(UpcomingAppointmentCard, 2);
            }
        }

        private static string EvaluateBloodPressure(string bpText)
        {
            var parts = bpText.Split('/');
            if (parts.Length != 2) return "Bình thường";
            if (!int.TryParse(parts[0].Trim(), out int sys)) return "Bình thường";
            if (!int.TryParse(parts[1].Trim(), out int dia)) return "Bình thường";

            if (sys < 120 && dia < 80) return "Bình thường";
            if (sys < 130 && dia < 80) return "Cao - GĐ 1";
            if (sys < 140 || dia < 90) return "Cao - GĐ 2";

            return "Tăng huyết áp";
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

        private async void QuickAccess_OnlineConsult_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_upcomingAppointmentId))
            {
                await new ContentDialog
                {
                    Title = "Chưa có lịch khám online",
                    Content = "Bạn cần có một lịch khám online sắp tới để tham gia cuộc gọi.",
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            Frame.Navigate(typeof(OnlineConsultationPage), _upcomingAppointmentId);
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
                Title = "Trợ lý sức khỏe",
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
