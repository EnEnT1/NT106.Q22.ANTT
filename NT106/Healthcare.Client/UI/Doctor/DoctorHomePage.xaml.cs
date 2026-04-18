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
            try
            {
                var doctorId = SessionStorage.CurrentUser?.Id;
                if (string.IsNullOrEmpty(doctorId)) return;

                var client = SupabaseManager.Instance.Client;

                // Tổng số ca đã khám (Status = 'Completed')
                var examinedResponse = await client.From<Appointment>()
                    .Where(x => x.DoctorId == doctorId && x.Status == "Completed")
                    .Get();
                TxtExamined.Text = examinedResponse.Models.Count.ToString("D2");

                // Ca cấp cứu (Status = 'Emergency' hoặc logic khác, tạm thời giả định là các ca trong hôm nay có status Urgent)
                // Vì model không có IsUrgent, tôi sẽ lọc theo Priority nếu có hoặc tính là 0 nếu không có data
                TxtEmergency.Text = "00"; 

                // Lịch hẹn hôm nay (Status = 'Confirmed' hoặc 'Pending' trong ngày hôm nay)
                var today = DateTime.Today;
                var appointmentsResponse = await client.From<Appointment>()
                    .Where(x => x.DoctorId == doctorId && x.AppointmentDate >= today && x.AppointmentDate < today.AddDays(1))
                    .Get();
                
                TxtAppointments.Text = appointmentsResponse.Models.Count.ToString("D2");
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
                var doctorId = SessionStorage.CurrentUser?.Id;
                if (string.IsNullOrEmpty(doctorId)) return;

                var client = SupabaseManager.Instance.Client;
                var today = DateTime.Today;

                // Lấy danh sách lịch hẹn trong ngày
                var response = await client.From<Appointment>()
                    .Where(x => x.DoctorId == doctorId && x.AppointmentDate >= today && x.AppointmentDate < today.AddDays(1))
                    .Get();

                var appointments = response.Models.OrderBy(x => x.StartTime).ToList();
                
                // Lấy thông tin bệnh nhân tương ứng
                var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
                var patientsResponse = await client.From<User>()
                    .Get(); // Postgrest filter IN is better if supported, but here we can filter locally or fetch all patients
                
                var patients = patientsResponse.Models.ToDictionary(u => u.Id, u => u.FullName);

                _allQueueItems.Clear();

                int queueCount = 1;
                foreach (var appt in appointments)
                {
                    var patientName = patients.ContainsKey(appt.PatientId) ? patients[appt.PatientId] : "Bệnh nhân ẩn danh";
                    
                    var statusInfo = GetQueueStatusInfo(appt.Status);
                    
                    _allQueueItems.Add(new QueueItemViewModel
                    {
                        QueueNumber = queueCount.ToString("D2"),
                        PatientName = patientName,
                        Symptom = "Khám tổng quát", // Tạm thời để mặc định vì Model Appointment không có trường Lý do khám
                        TimeLabel = appt.Status == "In Progress" ? "Bắt đầu" : "Dự kiến",
                        TimeValue = appt.StartTime.ToString(@"hh\:mm"),
                        StatusText = statusInfo.Text,
                        NumberBackground = statusInfo.Bg,
                        NumberForeground = statusInfo.Fg,
                        BadgeBackground = statusInfo.Bg,
                        BadgeForeground = statusInfo.Fg,
                        AppointmentId = appt.Id
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
                "In Progress" => ("ĐANG KHÁM", new SolidColorBrush(Color.FromArgb(255, 219, 234, 254)), new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))),
                "Confirmed" => ("ĐANG ĐỢI", new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)), new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))),
                "Pending" => ("CHỜ DUYỆT", new SolidColorBrush(Color.FromArgb(255, 254, 243, 199)), new SolidColorBrush(Color.FromArgb(255, 180, 83, 9))),
                _ => ("HOÀN TẤT", new SolidColorBrush(Color.FromArgb(255, 220, 252, 231)), new SolidColorBrush(Color.FromArgb(255, 22, 163, 74)))
            };
        }

        private async void LoadScheduleItems()
        {
            try
            {
                var doctorId = SessionStorage.CurrentUser?.Id;
                if (string.IsNullOrEmpty(doctorId)) return;

                var client = SupabaseManager.Instance.Client;
                var today = DateTime.Today;

                _scheduleItems.Clear();

                // Lấy lịch hẹn để làm lịch trình
                var response = await client.From<Appointment>()
                    .Where(x => x.DoctorId == doctorId && x.AppointmentDate >= today && x.AppointmentDate < today.AddDays(1))
                    .Get();

                var appointments = response.Models.OrderBy(x => x.StartTime).ToList();

                foreach (var appt in appointments)
                {
                    _scheduleItems.Add(new ScheduleItemViewModel
                    {
                        TimeRange = $"{appt.StartTime:hh\\:mm} - {appt.StartTime.Add(TimeSpan.FromMinutes(30)):hh\\:mm}",
                        Title = $"Hẹn khám: {appt.Id.Substring(0, Math.Min(appt.Id.Length, 6)).ToUpper()}",
                        Description = $"Phòng khám: {appt.RoomCode ?? "A-101"}",
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
                    Content = $"Bệnh nhân: {item.PatientName}\nTriệu chứng: {item.Symptom}\nMã lịch hẹn: {(item.AppointmentId.Length > 6 ? item.AppointmentId.Substring(0, 6).ToUpper() : item.AppointmentId.ToUpper())}",
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