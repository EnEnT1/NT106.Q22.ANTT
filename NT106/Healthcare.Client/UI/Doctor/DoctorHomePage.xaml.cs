using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.UI.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class DoctorHomePage : Page
    {
        public class QueueItemViewModel
        {
            public string QueueNumber { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string Symptom { get; set; } = string.Empty;
            public string TimeLabel { get; set; } = string.Empty;
            public string TimeValue { get; set; } = string.Empty;
            public string StatusText { get; set; } = string.Empty;
            public Brush NumberBackground { get; set; } = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249));
            public Brush NumberForeground { get; set; } = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
            public Brush BadgeBackground { get; set; } = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249));
            public Brush BadgeForeground { get; set; } = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
            public string AppointmentId { get; set; } = string.Empty;
            public bool IsPriority { get; set; }
        }

        public class ScheduleItemViewModel
        {
            public string TimeRange { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public Brush DotColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184));
            public Brush TimeColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184));
        }

        private readonly ObservableCollection<QueueItemViewModel> _queueItems = new();
        private readonly ObservableCollection<ScheduleItemViewModel> _scheduleItems = new();
        private readonly List<QueueItemViewModel> _allQueueItems = new();

        public DoctorHomePage()
        {
            this.InitializeComponent();

            QueueListView.ItemsSource = _queueItems;
            ScheduleListView.ItemsSource = _scheduleItems;

            this.Loaded += DoctorHomePage_Loaded;
        }

        private async void DoctorHomePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAllDataAsync();
        }

        private async Task LoadAllDataAsync()
        {
            LoadWelcomeText();
            await LoadQueueAsync();
            await LoadScheduleItemsAsync();
            await LoadStatsAsync();
            ShowAllQueue();
        }

        private void LoadWelcomeText()
        {
            var user = SessionStorage.CurrentUser;
            var hour = DateTime.Now.Hour;

            string greeting = hour < 12 ? "Chào buổi sáng"
                : hour < 18 ? "Chào buổi chiều"
                : "Chào buổi tối";

            TxtWelcome.Text = $"{greeting}, Bác sĩ {user?.FullName ?? "Tâm"}";

            int todayCount = _allQueueItems.Count;
            int onlineCount = 0;

            TxtSubtitle.Text = $"Hôm nay bạn có {todayCount} ca khám trong lịch làm việc.";
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                string? doctorId = SessionStorage.CurrentUser?.Id;

                if (string.IsNullOrWhiteSpace(doctorId))
                    return;

                var client = SupabaseManager.Instance.Client;
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var allAppointmentsResponse = await client.From<Appointment>()
                    .Where(x => x.DoctorId == doctorId)
                    .Get();

                var allAppointments = allAppointmentsResponse.Models ?? new List<Appointment>();

                int completedCount = allAppointments.Count(x => x.Status == "Completed");

                int todayCount = allAppointments.Count(x =>
                    x.AppointmentDate >= today &&
                    x.AppointmentDate < tomorrow);

                int urgentCount = allAppointments.Count(x =>
                    x.AppointmentDate >= today &&
                    x.AppointmentDate < tomorrow &&
                    (x.Status == "In Progress" || x.Status == "Arrived"));

                TxtExamined.Text = completedCount.ToString("D2");
                TxtEmergency.Text = urgentCount.ToString("D2");
                TxtAppointments.Text = todayCount.ToString("D2");

                TxtSubtitle.Text = $"Hôm nay bạn có {todayCount} ca khám, {urgentCount} ca đang cần chú ý.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading stats: " + ex.Message);
            }
        }

        private async Task LoadQueueAsync()
        {
            try
            {
                string? doctorId = SessionStorage.CurrentUser?.Id;

                if (string.IsNullOrWhiteSpace(doctorId))
                    return;

                var client = SupabaseManager.Instance.Client;
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var response = await client.From<Appointment>()
                    .Where(x => x.DoctorId == doctorId &&
                                x.AppointmentDate >= today &&
                                x.AppointmentDate < tomorrow)
                    .Get();

                var appointments = (response.Models ?? new List<Appointment>())
                    .OrderBy(x => x.StartTime)
                    .ToList();

                var patientsResponse = await client.From<User>().Get();

                var patients = (patientsResponse.Models ?? new List<User>())
                    .Where(u => !string.IsNullOrWhiteSpace(u.Id))
                    .GroupBy(u => u.Id)
                    .ToDictionary(g => g.Key, g => g.First().FullName ?? "Bệnh nhân ẩn danh");

                _allQueueItems.Clear();

                int queueCount = 1;

                foreach (var appt in appointments)
                {
                    string patientName = patients.TryGetValue(appt.PatientId, out string? name)
                        ? name
                        : "Bệnh nhân ẩn danh";

                    var statusInfo = GetQueueStatusInfo(appt.Status ?? string.Empty);

                    bool isPriority =
                        appt.Status == "In Progress" ||
                        appt.Status == "Arrived" ||
                        appt.Status == "Pending";

                    _allQueueItems.Add(new QueueItemViewModel
                    {
                        QueueNumber = queueCount.ToString("D2"),
                        PatientName = patientName,
                        Symptom = "Khám tổng quát",
                        TimeLabel = appt.Status == "In Progress" ? "Bắt đầu" : "Dự kiến",
                        TimeValue = appt.StartTime.ToString(@"hh\:mm"),
                        StatusText = statusInfo.Text,
                        NumberBackground = statusInfo.Bg,
                        NumberForeground = statusInfo.Fg,
                        BadgeBackground = statusInfo.Bg,
                        BadgeForeground = statusInfo.Fg,
                        AppointmentId = appt.Id ?? string.Empty,
                        IsPriority = isPriority
                    });

                    queueCount++;
                }

                UpdateQueueButtonText();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading queue: " + ex.Message);
            }
        }

        private (string Text, Brush Bg, Brush Fg) GetQueueStatusInfo(string status)
        {
            return status switch
            {
                "In Progress" => (
                    "ĐANG KHÁM",
                    new SolidColorBrush(Color.FromArgb(255, 219, 234, 254)),
                    new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))
                ),

                "Arrived" => (
                    "ĐÃ ĐẾN",
                    new SolidColorBrush(Color.FromArgb(255, 224, 242, 254)),
                    new SolidColorBrush(Color.FromArgb(255, 14, 165, 233))
                ),

                "Confirmed" => (
                    "CHƯA ĐẾN",
                    new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
                    new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))
                ),

                "Pending" => (
                    "CHỜ DUYỆT",
                    new SolidColorBrush(Color.FromArgb(255, 254, 243, 199)),
                    new SolidColorBrush(Color.FromArgb(255, 180, 83, 9))
                ),

                "Completed" => (
                    "HOÀN TẤT",
                    new SolidColorBrush(Color.FromArgb(255, 220, 252, 231)),
                    new SolidColorBrush(Color.FromArgb(255, 22, 163, 74))
                ),

                "Cancelled" => (
                    "ĐÃ HỦY",
                    new SolidColorBrush(Color.FromArgb(255, 254, 226, 226)),
                    new SolidColorBrush(Color.FromArgb(255, 220, 38, 38))
                ),

                _ => (
                    "KHÔNG RÕ",
                    new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
                    new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))
                )
            };
        }

        private async Task LoadScheduleItemsAsync()
        {
            try
            {
                string? doctorId = SessionStorage.CurrentUser?.Id;

                if (string.IsNullOrWhiteSpace(doctorId))
                    return;

                var client = SupabaseManager.Instance.Client;
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                _scheduleItems.Clear();

                var response = await client.From<Appointment>()
                    .Where(x => x.DoctorId == doctorId &&
                                x.AppointmentDate >= today &&
                                x.AppointmentDate < tomorrow)
                    .Get();

                var appointments = (response.Models ?? new List<Appointment>())
                    .OrderBy(x => x.StartTime)
                    .ToList();

                foreach (var appt in appointments)
                {
                    TimeSpan endTime = appt.EndTime != default
                        ? appt.EndTime
                        : appt.StartTime.Add(TimeSpan.FromMinutes(30));

                    _scheduleItems.Add(new ScheduleItemViewModel
                    {
                        TimeRange = $"{appt.StartTime:hh\\:mm} - {endTime:hh\\:mm}",
                        Title = $"Lịch khám #{ShortId(appt.Id)}",
                        Description = $"Phòng khám: {appt.RoomCode ?? "Chưa cập nhật"}",
                        DotColor = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187)),
                        TimeColor = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187))
                    });
                }

                if (_scheduleItems.Count == 0)
                {
                    _scheduleItems.Add(new ScheduleItemViewModel
                    {
                        TimeRange = "Cả ngày",
                        Title = "Không có lịch trình",
                        Description = "Bạn không có lịch hẹn nào trong hôm nay.",
                        DotColor = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
                        TimeColor = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading schedule: " + ex.Message);
            }
        }

        private static string ShortId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "N/A";

            return id.Length > 6 ? id.Substring(0, 6).ToUpper() : id.ToUpper();
        }

        private void ShowAllQueue()
        {
            _queueItems.Clear();

            foreach (var item in _allQueueItems)
            {
                _queueItems.Add(item);
            }

            SetAllButtonActive();
        }

        private void ShowPriorityQueue()
        {
            _queueItems.Clear();

            var priorityItems = _allQueueItems
                .Where(x => x.IsPriority)
                .ToList();

            foreach (var item in priorityItems)
            {
                _queueItems.Add(item);
            }

            SetPriorityButtonActive();
        }

        private void UpdateQueueButtonText()
        {
            int total = _allQueueItems.Count;
            int priority = _allQueueItems.Count(x => x.IsPriority);

            BtnQueueAll.Content = $"Tất cả ({total})";
            BtnQueuePriority.Content = $"Ưu tiên ({priority})";
        }

        private void SetAllButtonActive()
        {
            BtnQueueAll.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
            BtnQueuePriority.Style = (Style)Application.Current.Resources["DefaultButtonStyle"];
        }

        private void SetPriorityButtonActive()
        {
            BtnQueueAll.Style = (Style)Application.Current.Resources["DefaultButtonStyle"];
            BtnQueuePriority.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        }

        private void BtnQueueAll_Click(object sender, RoutedEventArgs e)
        {
            ShowAllQueue();
        }

        private void BtnQueuePriority_Click(object sender, RoutedEventArgs e)
        {
            ShowPriorityQueue();
        }

        private async void QueueItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not QueueItemViewModel item)
                return;

            ContentDialog dialog = new ContentDialog
            {
                Title = "Thông tin bệnh nhân",
                Content = $"Bệnh nhân: {item.PatientName}\nTriệu chứng: {item.Symptom}\nMã lịch hẹn: {ShortId(item.AppointmentId)}",
                CloseButtonText = "Đóng",
                PrimaryButtonText = "Sang khám",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                this.Frame?.Navigate(typeof(ExaminationPage), item.AppointmentId);
            }
        }

        private async void BtnViewAllQueue_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Hàng chờ khám",
                Content = $"Hiện có {_allQueueItems.Count} bệnh nhân trong danh sách chờ.",
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async void BtnAddSchedule_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Thêm sự kiện",
                Content = "Bạn có thể thêm hoặc chỉnh lịch khám trong trang Quản lý lịch khám.",
                CloseButtonText = "Đóng",
                PrimaryButtonText = "Mở quản lý lịch",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                TriggerShellNavigation("ManageSchedulePage");
            }
        }

        private void BtnViewFullSchedule_Click(object sender, RoutedEventArgs e)
        {
            TriggerShellNavigation("ManageSchedulePage");
        }

        private void BtnViewLabResult_Click(object sender, RoutedEventArgs e)
        {
            TriggerShellNavigation("PatientHistoryPage");
        }

        private void QuickAccess_ManageSchedule(object sender, RoutedEventArgs e)
        {
            TriggerShellNavigation("ManageSchedulePage");
        }

        private void QuickAccess_PatientHistory(object sender, RoutedEventArgs e)
        {
            TriggerShellNavigation("PatientHistoryPage");
        }

        private void QuickAccess_Revenue(object sender, RoutedEventArgs e)
        {
            TriggerShellNavigation("RevenuePage");
        }

        private void TriggerShellNavigation(string pageTag)
        {
            try
            {
                DependencyObject? parent = this.Parent;

                while (parent != null && parent is not DoctorShell)
                {
                    parent = (parent as FrameworkElement)?.Parent;
                }

                if (parent is DoctorShell shell)
                {
                    shell.SelectNavItem(pageTag);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Không tìm thấy DoctorShell cha");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Navigation error: " + ex.Message);
            }
        }
    }
}