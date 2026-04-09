using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Core;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.UI.Shell;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class DoctorHomePage : Page
    {
        // ViewModel đơn giản cho Queue item
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
            public string AppointmentId { get; set; } // để navigate sang ExaminationPage
        }

        // ViewModel cho Schedule item
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

        public DoctorHomePage()
        {
            this.InitializeComponent();
            QueueListView.ItemsSource = _queueItems;
            ScheduleListView.ItemsSource = _scheduleItems;

            // Load data khi page mở
            this.Loaded += async (s, e) => await LoadAllDataAsync();
        }

        // ══════════════════════════════════════════
        // DATA LOADING
        // ══════════════════════════════════════════

        private async Task LoadAllDataAsync()
        {
            LoadWelcomeText();
            LoadScheduleItems(); // mock data trước, sau thay bằng Supabase
            await LoadQueueAsync();
            await LoadStatsAsync();
        }

        private void LoadWelcomeText()
        {
            var user = SessionStorage.CurrentUser;
            var hour = DateTime.Now.Hour;
            var greeting = hour < 12 ? "Chào buổi sáng" :
                           hour < 18 ? "Chào buổi chiều" : "Chào buổi tối";

            TxtWelcome.Text = $"{greeting}, Bác sĩ {user?.FullName ?? ""}";
        }

        private async Task LoadStatsAsync()
        {
            // TODO: gọi SupabaseDbService lấy số liệu thực
            // Tạm thời dùng mock
            TxtExamined.Text = "24";
            TxtEmergency.Text = "03";
            TxtAppointments.Text = "08";
        }

        private async Task LoadQueueAsync()
        {
            _queueItems.Clear();

            // TODO: thay bằng SupabaseDbService.GetTodayQueueAsync(doctorId)
            // Mock data theo đúng design
            _queueItems.Add(new QueueItemViewModel
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

            _queueItems.Add(new QueueItemViewModel
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

            _queueItems.Add(new QueueItemViewModel
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

            _queueItems.Add(new QueueItemViewModel
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
        }

        private void LoadScheduleItems()
        {
            _scheduleItems.Clear();

            // TODO: thay bằng Supabase data
            _scheduleItems.Add(new ScheduleItemViewModel
            {
                TimeRange = "08:00 - 09:00",
                Title = "Giao ban khoa",
                Description = "Phòng họp tầng 4 - Khu A",
                DotColor = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187)),
                TimeColor = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187)),
            });

            _scheduleItems.Add(new ScheduleItemViewModel
            {
                TimeRange = "10:30 - 11:30",
                Title = "Hội chẩn trực tuyến",
                Description = "Bệnh nhân: Lê Văn Dũng (Ca khó)",
                DotColor = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
                TimeColor = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
            });

            _scheduleItems.Add(new ScheduleItemViewModel
            {
                TimeRange = "14:00 - 16:00",
                Title = "Khám chuyên sâu",
                Description = "Danh sách đăng ký trước (05 ca)",
                DotColor = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225)),
                TimeColor = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
            });
        }

        // ══════════════════════════════════════════
        // EVENT HANDLERS
        // ══════════════════════════════════════════

        // Click vào 1 bệnh nhân trong queue → navigate sang ExaminationPage
        private void QueueItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is QueueItemViewModel item)
            {
                // Lấy DoctorShell → navigate ContentFrame sang ExaminationPage
                // và truyền appointmentId
                var shell = this.Parent as Frame;
                // TODO: Frame.Navigate(typeof(ExaminationPage), item.AppointmentId);
            }
        }

        private void BtnViewAllQueue_Click(object sender, RoutedEventArgs e)
        {
            // TODO: navigate sang trang Queue đầy đủ (nếu có)
        }

        private void BtnAddSchedule_Click(object sender, RoutedEventArgs e)
        {
            // TODO: mở dialog thêm sự kiện
        }

        private void BtnViewFullSchedule_Click(object sender, RoutedEventArgs e)
        {
            // Navigate sang ManageSchedulePage
        }

        private void BtnViewLabResult_Click(object sender, RoutedEventArgs e)
        {
            // Navigate sang PatientHistoryPage với patient ID liên quan
        }

        // Quick Access shortcuts → đổi tab NavigationView ở Shell
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

        // Helper: tìm DoctorShell cha và trigger navigate
        private void TriggerShellNavigation(string pageTag)
        {
            try
            {
                // Bubble up tìm DoctorShell
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
                System.Diagnostics.Debug.WriteLine($" Navigation error: {ex.Message}");
            }
        }
    }
}
