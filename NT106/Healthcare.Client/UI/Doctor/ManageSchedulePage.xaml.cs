using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Healthcare.Client.Helpers;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.UI.Components;

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class ManageSchedulePage : Page
    {
        private Supabase.Client _supabase;
        private DateTime _currentWeekSunday;
        private DateTime _currentMonthDate;
        private List<Appointment> _allAppointments = new();
        private DispatcherTimer _realtimeTimer;

        public ObservableCollection<AppointmentViewModel> SundayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> MondayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> TuesdayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> WednesdayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> ThursdayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> FridayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> SaturdayList { get; } = new();

        public ManageSchedulePage()
        {
            this.InitializeComponent();
            _supabase = SupabaseManager.Instance.Client;

            // Xác định Chủ Nhật của tuần hiện tại
            _currentWeekSunday = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            _currentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            // Bật đồng hồ thời gian thực
            _realtimeTimer = new DispatcherTimer();
            _realtimeTimer.Interval = TimeSpan.FromSeconds(1);
            _realtimeTimer.Tick += RealtimeTimer_Tick;
            _realtimeTimer.Start();
            RealtimeTimer_Tick(null, null);
        }

        private void RealtimeTimer_Tick(object sender, object e)
        {
            CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");
            CurrentDateText.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await LoadDataFromSupabase();
        }

        private async Task LoadDataFromSupabase()
        {
            try
            {
                string doctorId = SessionStorage.CurrentUser.Id;
                var response = await _supabase.From<Appointment>().Where(x => x.DoctorId == doctorId).Get();
                _allAppointments = response.Models;

                RefreshWeekView();
                RefreshMonthView();
            }
            catch (Exception ex)
            {
                // Bỏ qua log lỗi tạm thời
            }
        }

        private void RefreshWeekView()
        {
            SundayList.Clear(); MondayList.Clear(); TuesdayList.Clear();
            WednesdayList.Clear(); ThursdayList.Clear(); FridayList.Clear(); SaturdayList.Clear();

            // Cập nhật nhãn ngày trên giao diện
            DateTime endOfWeek = _currentWeekSunday.AddDays(6);
            WeekRangeText.Text = $"{_currentWeekSunday:dd/MM} - {endOfWeek:dd/MM/yyyy}";

            SundayDate.Text = _currentWeekSunday.Day.ToString();
            MondayDate.Text = _currentWeekSunday.AddDays(1).Day.ToString();
            TuesdayDate.Text = _currentWeekSunday.AddDays(2).Day.ToString();
            WednesdayDate.Text = _currentWeekSunday.AddDays(3).Day.ToString();
            ThursdayDate.Text = _currentWeekSunday.AddDays(4).Day.ToString();
            FridayDate.Text = _currentWeekSunday.AddDays(5).Day.ToString();
            SaturdayDate.Text = _currentWeekSunday.AddDays(6).Day.ToString();

            // Lọc ra danh sách lịch hẹn trong tuần
            var weekAppts = _allAppointments
                .Where(a => a.AppointmentDate.Date >= _currentWeekSunday.Date && a.AppointmentDate.Date <= endOfWeek.Date)
                .ToList();

            foreach (var appt in weekAppts)
            {
                var vm = new AppointmentViewModel(appt, "Bệnh nhân (ẩn danh)");
                int dow = (int)appt.AppointmentDate.DayOfWeek;
                if (dow == 0) SundayList.Add(vm);
                if (dow == 1) MondayList.Add(vm);
                if (dow == 2) TuesdayList.Add(vm);
                if (dow == 3) WednesdayList.Add(vm);
                if (dow == 4) ThursdayList.Add(vm);
                if (dow == 5) FridayList.Add(vm);
                if (dow == 6) SaturdayList.Add(vm);
            }
        }

        private void RefreshMonthView()
        {
            MonthYearText.Text = $"Tháng {_currentMonthDate.Month}, {_currentMonthDate.Year}";
            MonthCalendarGrid.Children.Clear();

            int daysInMonth = DateTime.DaysInMonth(_currentMonthDate.Year, _currentMonthDate.Month);
            int startDayOfWeek = (int)_currentMonthDate.DayOfWeek; // 0 = CN, 1 = T2...

            int row = 0;
            for (int i = 1; i <= daysInMonth; i++)
            {
                int col = (startDayOfWeek + i - 1) % 7;
                DateTime cellDate = new DateTime(_currentMonthDate.Year, _currentMonthDate.Month, i);
                
                int apptCount = _allAppointments.Count(a => a.AppointmentDate.Date == cellDate.Date);
                bool isToday = (cellDate.Date == DateTime.Today);

                Border cell = new Border
                {
                    Margin = new Thickness(4),
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(isToday ? Color.FromArgb(20, 0, 89, 187) : Color.FromArgb(255, 248, 249, 250)),
                    BorderBrush = new SolidColorBrush(isToday ? Color.FromArgb(255, 0, 89, 187) : Color.FromArgb(255, 248, 249, 250)),
                    BorderThickness = new Thickness(isToday ? 2 : 0),
                    Padding = new Thickness(8)
                };

                StackPanel sp = new StackPanel { Spacing = 6 };
                
                TextBlock txtDay = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 14,
                    FontWeight = isToday ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Medium,
                    Foreground = isToday ? new SolidColorBrush(Color.FromArgb(255, 0, 89, 187)) : new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
                };
                sp.Children.Add(txtDay);

                if (apptCount > 0)
                {
                    Border apptTag = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(255, 191, 217, 253)),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    apptTag.Child = new TextBlock
                    {
                        Text = $"{apptCount} lịch hẹn",
                        FontSize = 11,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187))
                    };
                    sp.Children.Add(apptTag);
                }

                cell.Child = sp;
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                MonthCalendarGrid.Children.Add(cell);

                if (col == 6) row++;
            }
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.CommandParameter?.ToString();
            await UpdateStatus(id, "Confirmed");
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.CommandParameter?.ToString();
            await UpdateStatus(id, "Cancelled");
        }

        private async Task UpdateStatus(string id, string status)
        {
            var appt = await _supabase.From<Appointment>().Where(x => x.Id == id).Single();
            appt.Status = status;
            await appt.Update<Appointment>();
            
            // Reload lại
            await LoadDataFromSupabase();
        }

        private void BtnVideoCall_Click(object sender, RoutedEventArgs e)
        {
            string patientId = (sender as Button)?.CommandParameter?.ToString();
            this.Frame.Navigate(typeof(ExaminationPage), patientId);
        }
       
        private void BtnExamine_Click(object sender, RoutedEventArgs e)
        {
            string apptId = (sender as Button)?.CommandParameter?.ToString();
            this.Frame.Navigate(typeof(ExaminationPage), apptId);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => _ = LoadDataFromSupabase();

        private void WeekTab_Click(object sender, RoutedEventArgs e) 
        { 
            WeekViewPanel.Visibility = Visibility.Visible;
            MonthViewPanel.Visibility = Visibility.Collapsed;

            // Bôi xanh khi đang được chọn
            WeekTabButton.Background = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187));
            WeekTabIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            WeekTabLabel.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            WeekTabLabel.FontWeight = Microsoft.UI.Text.FontWeights.Bold;

            MonthTabButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            MonthTabIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 70, 96, 127));
            MonthTabLabel.Foreground = new SolidColorBrush(Color.FromArgb(255, 70, 96, 127));
            MonthTabLabel.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        }
        
        private void MonthTab_Click(object sender, RoutedEventArgs e) 
        { 
            WeekViewPanel.Visibility = Visibility.Collapsed;
            MonthViewPanel.Visibility = Visibility.Visible;
            RefreshMonthView(); 

            // Bôi xanh khi đang được chọn
            MonthTabButton.Background = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187));
            MonthTabIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            MonthTabLabel.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            MonthTabLabel.FontWeight = Microsoft.UI.Text.FontWeights.Bold;

            WeekTabButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            WeekTabIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 70, 96, 127));
            WeekTabLabel.Foreground = new SolidColorBrush(Color.FromArgb(255, 70, 96, 127));
            WeekTabLabel.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        }



        private void PrevWeek_Click(object sender, RoutedEventArgs e)
        {
            _currentWeekSunday = _currentWeekSunday.AddDays(-7);
            RefreshWeekView();
        }

        private void NextWeek_Click(object sender, RoutedEventArgs e)
        {
            _currentWeekSunday = _currentWeekSunday.AddDays(7);
            RefreshWeekView();
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonthDate = _currentMonthDate.AddMonths(-1);
            RefreshMonthView();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonthDate = _currentMonthDate.AddMonths(1);
            RefreshMonthView();
        }
    }

    public class AppointmentViewModel
    {
        public string Id { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string Time { get; set; }
        public string BaseStatus { get; set; }

        public Visibility IsPending => BaseStatus == "Pending" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsConfirmed => BaseStatus == "Confirmed" ? Visibility.Visible : Visibility.Collapsed;

        public SolidColorBrush BgBrush => new SolidColorBrush(BaseStatus == "Pending" ? Color.FromArgb(255, 255, 248, 225) : Color.FromArgb(255, 243, 248, 255));
        public Color BorderColorCode => BaseStatus == "Pending" ? Color.FromArgb(255, 255, 224, 130) : Color.FromArgb(255, 191, 217, 253);
        public SolidColorBrush TagBgBrush => new SolidColorBrush(BaseStatus == "Pending" ? Color.FromArgb(255, 255, 179, 0) : Color.FromArgb(255, 0, 89, 187));
        public string StatusText => BaseStatus == "Pending" ? "Chờ duyệt" : "Sẵn sàng";

        public AppointmentViewModel(Appointment model, string name)
        {
            Id = model.Id; 
            PatientId = model.PatientId; 
            PatientName = name;
            Time = model.StartTime.ToString(); 
            BaseStatus = model.Status;
        }
    }
}