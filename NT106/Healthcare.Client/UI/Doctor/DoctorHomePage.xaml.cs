using Healthcare.Client.Helpers;
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
            public string QueueNumber { get; set; }
            public string PatientName { get; set; }
            public string Symptom { get; set; }
            public string TimeLabel { get; set; }
            public string TimeValue { get; set; }
            public string StatusText { get; set; }
            public Brush NumberBackground { get; set; }
            public Brush NumberForeground { get; set; }
            public Brush BadgeBackground { get; set; }
            public Brush BadgeForeground { get; set; }
            public string AppointmentId { get; set; }
        }

        public class ScheduleItemViewModel
        {
            public string TimeRange { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public Brush DotColor { get; set; }
            public Brush TimeColor { get; set; }
        }

        private ObservableCollection<QueueItemViewModel> _queueItems = new();
        private ObservableCollection<ScheduleItemViewModel> _scheduleItems = new();
        private List<QueueItemViewModel> _allQueueItems = new();

        public DoctorHomePage()
        {
            this.InitializeComponent();

            QueueListView.ItemsSource = _queueItems;
            ScheduleListView.ItemsSource = _scheduleItems;

            this.Loaded += async (s, e) => await LoadAllDataAsync();
        }

        private async Task LoadAllDataAsync()
        {
            LoadWelcomeText();
            LoadScheduleItems();
            await LoadQueueAsync();
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
        }

        private async Task LoadStatsAsync()
        {
            await Task.CompletedTask;

            TxtExamined.Text = "24";
            TxtEmergency.Text = "03";
            TxtAppointments.Text = "08";
        }

        private async Task LoadQueueAsync()
        {
            await Task.CompletedTask;

            _allQueueItems.Clear();

            _allQueueItems.Add(new QueueItemViewModel
            {
                QueueNumber = "08",
                PatientName = "Nguyễn Văn An",
                Symptom = "Đau thắt ngực • 45 tuổi",
                TimeLabel = "Bắt đầu",
                TimeValue = "09:15 AM",
                StatusText = "ĐANG KHÁM",
                NumberBackground = new SolidColorBrush(Color.FromArgb(255, 219, 234, 254)),
                NumberForeground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
                BadgeBackground = new SolidColorBrush(Color.FromArgb(255, 219, 234, 254)),
                BadgeForeground = new SolidColorBrush(Color.FromArgb(255, 29, 78, 216)),
                AppointmentId = "apt-001"
            });

            _allQueueItems.Add(new QueueItemViewModel
            {
                QueueNumber = "09",
                PatientName = "Lê Thị Bình",
                Symptom = "Khó thở cấp • 68 tuổi",
                TimeLabel = "Dự kiến",
                TimeValue = "Ngay lập tức",
                StatusText = "ƯU TIÊN",
                NumberBackground = new SolidColorBrush(Color.FromArgb(255, 254, 226, 226)),
                NumberForeground = new SolidColorBrush(Color.FromArgb(255, 220, 38, 38)),
                BadgeBackground = new SolidColorBrush(Color.FromArgb(255, 254, 226, 226)),
                BadgeForeground = new SolidColorBrush(Color.FromArgb(255, 153, 0, 10)),
                AppointmentId = "apt-002"
            });

            _allQueueItems.Add(new QueueItemViewModel
            {
                QueueNumber = "10",
                PatientName = "Trần Minh Quân",
                Symptom = "Kiểm tra định kỳ • 32 tuổi",
                TimeLabel = "Dự kiến",
                TimeValue = "09:45 AM",
                StatusText = "ĐANG ĐỢI",
                NumberBackground = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
                NumberForeground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
                BadgeBackground = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
                BadgeForeground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
                AppointmentId = "apt-003"
            });

            _allQueueItems.Add(new QueueItemViewModel
            {
                QueueNumber = "11",
                PatientName = "Phạm Hoàng Nam",
                Symptom = "Tư vấn hậu phẫu • 29 tuổi",
                TimeLabel = "Dự kiến",
                TimeValue = "10:00 AM",
                StatusText = "ĐANG ĐỢI",
                NumberBackground = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
                NumberForeground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
                BadgeBackground = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
                BadgeForeground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
                AppointmentId = "apt-004"
            });

            UpdateQueueButtonText();
        }

        private void LoadScheduleItems()
        {
            _scheduleItems.Clear();

            _scheduleItems.Add(new ScheduleItemViewModel
            {
                TimeRange = "08:00 - 09:00",
                Title = "Giao ban khoa",
                Description = "Phòng họp tầng 4 - Khu A",
                DotColor = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187)),
                TimeColor = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187))
            });

            _scheduleItems.Add(new ScheduleItemViewModel
            {
                TimeRange = "10:30 - 11:30",
                Title = "Hội chẩn trực tuyến",
                Description = "Bệnh nhân: Lê Văn Dũng (Ca khó)",
                DotColor = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
                TimeColor = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
            });

            _scheduleItems.Add(new ScheduleItemViewModel
            {
                TimeRange = "14:00 - 16:00",
                Title = "Khám chuyên sâu",
                Description = "Danh sách đăng ký trước (05 ca)",
                DotColor = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225)),
                TimeColor = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
            });
        }

        private void ShowAllQueue()
        {
            _queueItems.Clear();

            foreach (var item in _allQueueItems)
                _queueItems.Add(item);

            SetAllButtonActive();
        }

        private void ShowPriorityQueue()
        {
            _queueItems.Clear();

            var priorityItems = _allQueueItems
                .Where(x => x.StatusText == "ƯU TIÊN")
                .ToList();

            foreach (var item in priorityItems)
                _queueItems.Add(item);

            SetPriorityButtonActive();
        }

        private void UpdateQueueButtonText()
        {
            int total = _allQueueItems.Count;
            int priority = _allQueueItems.Count(x => x.StatusText == "ƯU TIÊN");

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
            if (e.ClickedItem is QueueItemViewModel item)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Thông tin bệnh nhân",
                    Content = $"Bệnh nhân: {item.PatientName}\nTriệu chứng: {item.Symptom}\nMã lịch hẹn: {item.AppointmentId}",
                    CloseButtonText = "Đóng",
                    PrimaryButtonText = "Sang khám",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    if (this.Frame != null)
                    {
                        this.Frame.Navigate(typeof(ExaminationPage), item.AppointmentId);
                    }
                }
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
                Content = "Chức năng thêm lịch sẽ làm ở bước sau.",
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
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

        private void QuickAccess_Examination(object sender, RoutedEventArgs e)
        {
            TriggerShellNavigation("ExaminationPage");
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
                var parent = this.Parent;

                while (parent != null && !(parent is DoctorShell))
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