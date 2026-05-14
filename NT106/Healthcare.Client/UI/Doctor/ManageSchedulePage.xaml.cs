using Microsoft.UI;
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
        private List<Transaction> _allTransactions = new();
        private List<User> _allPatients = new();
        private DateTime? _filteredDate = null;
        private DispatcherTimer _realtimeTimer;
        private int _currentPage = 1;
        private const int PageSize = 40;

        public ObservableCollection<AppointmentViewModel> SundayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> MondayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> TuesdayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> WednesdayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> ThursdayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> FridayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> SaturdayList { get; } = new();
        public ObservableCollection<AppointmentViewModel> AllAppointmentsListVM { get; } = new();
        public ObservableCollection<SlotViewModel> DoctorSlotsList { get; } = new();

        public ManageSchedulePage()
        {
            this.InitializeComponent();
            _supabase = SupabaseManager.Instance.Client;

            _currentWeekSunday = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            _currentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            _realtimeTimer = new DispatcherTimer();
            _realtimeTimer.Interval = TimeSpan.FromSeconds(1);
            _realtimeTimer.Tick += RealtimeTimer_Tick;
            _realtimeTimer.Start();
            RealtimeTimer_Tick(null, null);

            FilterSlotDatePicker.Date = DateTimeOffset.Now;
        }

        private void RealtimeTimer_Tick(object? sender, object? e)
        {
            CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");
            CurrentDateText.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            bool ok = await EnsureDoctorSessionAsync(e.Parameter);
            if (!ok)
                return;

            await LoadDataFromSupabase();
        }

        private async Task<bool> EnsureDoctorSessionAsync(object parameter)
        {
            try
            {
                if (SessionStorage.CurrentUser != null &&
                    !string.IsNullOrWhiteSpace(SessionStorage.CurrentUser.Id))
                {
                    return true;
                }

                if (parameter is User navUser)
                {
                    SessionStorage.CurrentUser = navUser;
                    return true;
                }

                if (parameter is string doctorIdFromNav && !string.IsNullOrWhiteSpace(doctorIdFromNav))
                {
                    var userResponse = await _supabase.From<User>().Get();
                    var matchedUser = userResponse.Models.FirstOrDefault(u => u.Id == doctorIdFromNav);

                    if (matchedUser != null)
                    {
                        SessionStorage.CurrentUser = matchedUser;
                        return true;
                    }
                }

                var response = await _supabase.From<User>().Get();
                var firstDoctor = response.Models
                    .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u.Role)
                                      && u.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase));

                if (firstDoctor != null)
                {
                    SessionStorage.CurrentUser = firstDoctor;
                    return true;
                }

                await new ContentDialog
                {
                    Title = "Không tìm thấy phiên đăng nhập",
                    Content = "Không có bác sĩ nào trong SessionStorage và cũng không tìm được bác sĩ phù hợp trong database.",
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return false;
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Lỗi khởi tạo phiên",
                    Content = ex.Message,
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return false;
            }
        }

        private async Task LoadDataFromSupabase()
        {
            try
            {
                if (SessionStorage.CurrentUser == null || string.IsNullOrWhiteSpace(SessionStorage.CurrentUser.Id))
                {
                    await new ContentDialog
                    {
                        Title = "Lỗi đăng nhập",
                        Content = "Không tìm thấy thông tin bác sĩ đang đăng nhập.",
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                    return;
                }

                string doctorId = SessionStorage.CurrentUser.Id;

                var response = await _supabase
                    .From<Appointment>()
                    .Where(x => x.DoctorId == doctorId)
                    .Get();

                _allAppointments = response.Models ?? new List<Appointment>();

                var pIds = _allAppointments.Select(a => a.PatientId).Distinct().ToList();
                if (pIds.Any())
                {
                    var uRes = await _supabase.From<User>()
                        .Filter("id", Postgrest.Constants.Operator.In, pIds)
                        .Get();

                    _allPatients = uRes.Models ?? new List<User>();
                }

                var transResponse = await _supabase
                    .From<Transaction>()
                    .Get();

                _allTransactions = transResponse.Models ?? new List<Transaction>();

                RefreshWeekView();
                RefreshMonthView();
                RefreshListView();
                await LoadDoctorSlots();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Lỗi tải dữ liệu",
                    Content = ex.Message,
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }

        private void RefreshListView()
        {
            AllAppointmentsListVM.Clear();
            var query = _allAppointments.AsEnumerable();

            if (_filteredDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate.Date == _filteredDate.Value.Date);
            }

            var fullList = query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.StartTime)
                .ToList();

            int totalItems = fullList.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / PageSize);
            if (totalPages == 0)
                totalPages = 1;

            if (_currentPage > totalPages)
                _currentPage = totalPages;

            if (_currentPage < 1)
                _currentPage = 1;

            var paged = fullList
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            foreach (var appt in paged)
            {
                var patient = _allPatients.FirstOrDefault(u => u.Id == appt.PatientId);
                var trans = _allTransactions.FirstOrDefault(t => t.AppointmentId == appt.Id);

                AllAppointmentsListVM.Add(
                    new AppointmentViewModel(
                        appt,
                        patient?.FullName ?? "Bệnh nhân (ẩn danh)",
                        trans?.PaymentMethod
                    )
                );
            }

            if (PageInfoText != null)
                PageInfoText.Text = $"Trang {_currentPage} / {totalPages}";

            if (BtnPrevPage != null)
                BtnPrevPage.IsEnabled = _currentPage > 1;

            if (BtnNextPage != null)
                BtnNextPage.IsEnabled = _currentPage < totalPages;
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                RefreshListView();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            RefreshListView();
        }

        private void HighlightTodayColumn()
        {
            var accentBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 91, 108, 246));
            var accentLightBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 238, 240, 254));
            var transparentBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            var whiteBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
            var textPrimaryBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 30, 41, 59));
            var outlineBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 148, 163, 184));

            var headers = new[] { SundayHeader, MondayHeader, TuesdayHeader, WednesdayHeader, ThursdayHeader, FridayHeader, SaturdayHeader };
            var circles = new[] { SundayCircle, MondayCircle, TuesdayCircle, WednesdayCircle, ThursdayCircle, FridayCircle, SaturdayCircle };
            var dates = new[] { SundayDate, MondayDate, TuesdayDate, WednesdayDate, ThursdayDate, FridayDate, SaturdayDate };
            var labels = new[] { SundayLabel, MondayLabel, TuesdayLabel, WednesdayLabel, ThursdayLabel, FridayLabel, SaturdayLabel };

            for (int i = 0; i < 7; i++)
            {
                if (headers[i] != null)
                    headers[i].Background = transparentBrush;

                if (circles[i] != null)
                    circles[i].Background = transparentBrush;

                if (dates[i] != null)
                    dates[i].Foreground = textPrimaryBrush;

                if (labels[i] != null)
                    labels[i].Foreground = outlineBrush;
            }

            int todayDow = (int)DateTime.Today.DayOfWeek;
            DateTime colDate = _currentWeekSunday.AddDays(todayDow);

            if (colDate.Date == DateTime.Today)
            {
                if (headers[todayDow] != null)
                    headers[todayDow].Background = accentLightBrush;

                if (circles[todayDow] != null)
                    circles[todayDow].Background = accentBrush;

                if (dates[todayDow] != null)
                    dates[todayDow].Foreground = whiteBrush;

                if (labels[todayDow] != null)
                    labels[todayDow].Foreground = accentBrush;
            }
        }

        private void RefreshWeekView()
        {
            SundayList.Clear();
            MondayList.Clear();
            TuesdayList.Clear();
            WednesdayList.Clear();
            ThursdayList.Clear();
            FridayList.Clear();
            SaturdayList.Clear();

            DateTime endOfWeek = _currentWeekSunday.AddDays(6);
            WeekRangeText.Text = $"{_currentWeekSunday:dd/MM} - {endOfWeek:dd/MM/yyyy}";

            SundayDate.Text = _currentWeekSunday.Day.ToString();
            MondayDate.Text = _currentWeekSunday.AddDays(1).Day.ToString();
            TuesdayDate.Text = _currentWeekSunday.AddDays(2).Day.ToString();
            WednesdayDate.Text = _currentWeekSunday.AddDays(3).Day.ToString();
            ThursdayDate.Text = _currentWeekSunday.AddDays(4).Day.ToString();
            FridayDate.Text = _currentWeekSunday.AddDays(5).Day.ToString();
            SaturdayDate.Text = _currentWeekSunday.AddDays(6).Day.ToString();

            HighlightTodayColumn();

            var weekAppts = _allAppointments
                .Where(a => a.AppointmentDate.Date >= _currentWeekSunday.Date
                         && a.AppointmentDate.Date <= endOfWeek.Date)
                .OrderBy(a => a.StartTime)
                .ToList();

            foreach (var appt in weekAppts)
            {
                var patient = _allPatients.FirstOrDefault(u => u.Id == appt.PatientId);
                var trans = _allTransactions.FirstOrDefault(t => t.AppointmentId == appt.Id);
                var vm = new AppointmentViewModel(appt, patient?.FullName ?? "Bệnh nhân (ẩn danh)", trans?.PaymentMethod);
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
            int startDayOfWeek = (int)_currentMonthDate.DayOfWeek;

            int row = 0;
            for (int i = 1; i <= daysInMonth; i++)
            {
                int col = (startDayOfWeek + i - 1) % 7;
                DateTime cellDate = new DateTime(_currentMonthDate.Year, _currentMonthDate.Month, i);

                int apptCount = _allAppointments.Count(a => a.AppointmentDate.Date == cellDate.Date);
                bool isToday = cellDate.Date == DateTime.Today;

                Border cell = new Border
                {
                    Margin = new Thickness(4),
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(isToday
                        ? Color.FromArgb(20, 0, 89, 187)
                        : Color.FromArgb(255, 248, 249, 250)),
                    BorderBrush = new SolidColorBrush(isToday
                        ? Color.FromArgb(255, 0, 89, 187)
                        : Color.FromArgb(255, 248, 249, 250)),
                    BorderThickness = new Thickness(isToday ? 2 : 0),
                    Padding = new Thickness(8)
                };

                StackPanel sp = new StackPanel { Spacing = 6 };

                TextBlock txtDay = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 14,
                    FontWeight = isToday
                        ? Microsoft.UI.Text.FontWeights.Bold
                        : Microsoft.UI.Text.FontWeights.Medium,
                    Foreground = isToday
                        ? new SolidColorBrush(Color.FromArgb(255, 0, 89, 187))
                        : new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
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

                cell.Tapped += (s, e) =>
                {
                    _filteredDate = cellDate;
                    _currentPage = 1;
                    ListViewPanel.Visibility = Visibility.Visible;
                    WeekViewPanel.Visibility = Visibility.Collapsed;
                    MonthViewPanel.Visibility = Visibility.Collapsed;
                    SlotManagementPanel.Visibility = Visibility.Collapsed;
                    RefreshListView();
                    UpdateTabStyles(ListTabButton);
                };

                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                MonthCalendarGrid.Children.Add(cell);

                if (col == 6)
                    row++;
            }
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            string? id = (sender as Button)?.CommandParameter?.ToString();
            await UpdateStatus(id, "Confirmed");
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            string? id = (sender as Button)?.CommandParameter?.ToString();
            await UpdateStatus(id, "Cancelled");
        }

        private async Task UpdateStatus(string? id, string status)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            try
            {
                var appt = await _supabase
                    .From<Appointment>()
                    .Where(x => x.Id == id)
                    .Single();

                if (appt == null)
                    return;

                appt.Status = status;
                await appt.Update<Appointment>();

                await LoadDataFromSupabase();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Lỗi cập nhật",
                    Content = ex.Message,
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }

        private void BtnVideoCall_Click(object sender, RoutedEventArgs e)
        {
            string? patientId = (sender as Button)?.CommandParameter?.ToString();
            this.Frame.Navigate(typeof(ExaminationPage), patientId);
        }

        private void BtnExamine_Click(object sender, RoutedEventArgs e)
        {
            string? apptId = (sender as Button)?.CommandParameter?.ToString();
            this.Frame.Navigate(typeof(ExaminationPage), apptId);
        }

        private void AppointmentCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var vm = (sender as FrameworkElement)?.DataContext as AppointmentViewModel;

            if (vm != null)
            {
                this.Frame.Navigate(typeof(AppointmentDetailPage), vm.Id);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadDataFromSupabase();
        }

        private void ListTab_Click(object sender, RoutedEventArgs e)
        {
            _filteredDate = null;
            _currentPage = 1;
            ListViewPanel.Visibility = Visibility.Visible;
            WeekViewPanel.Visibility = Visibility.Collapsed;
            MonthViewPanel.Visibility = Visibility.Collapsed;
            SlotManagementPanel.Visibility = Visibility.Collapsed;
            RefreshListView();

            UpdateTabStyles(ListTabButton);
        }

        private void WeekTab_Click(object sender, RoutedEventArgs e)
        {
            ListViewPanel.Visibility = Visibility.Collapsed;
            WeekViewPanel.Visibility = Visibility.Visible;
            MonthViewPanel.Visibility = Visibility.Collapsed;
            SlotManagementPanel.Visibility = Visibility.Collapsed;

            UpdateTabStyles(WeekTabButton);
        }

        private void MonthTab_Click(object sender, RoutedEventArgs e)
        {
            ListViewPanel.Visibility = Visibility.Collapsed;
            WeekViewPanel.Visibility = Visibility.Collapsed;
            MonthViewPanel.Visibility = Visibility.Visible;
            SlotManagementPanel.Visibility = Visibility.Collapsed;
            RefreshMonthView();

            UpdateTabStyles(MonthTabButton);
        }

        private async void SlotTab_Click(object sender, RoutedEventArgs e)
        {
            ListViewPanel.Visibility = Visibility.Collapsed;
            WeekViewPanel.Visibility = Visibility.Collapsed;
            MonthViewPanel.Visibility = Visibility.Collapsed;
            SlotManagementPanel.Visibility = Visibility.Visible;

            await LoadDoctorSlots();
            UpdateTabStyles(SlotTabButton);
        }

        private async Task LoadDoctorSlots()
        {
            if (SessionStorage.CurrentUser == null || string.IsNullOrEmpty(SessionStorage.CurrentUser.Id))
                return;

            try
            {
                var query = _supabase.From<TimeSlot>()
                    .Where(x => x.DoctorId == SessionStorage.CurrentUser.Id);

                if (FilterSlotDatePicker.Date != null)
                {
                    var filterDate = FilterSlotDatePicker.Date.DateTime.Date;
                    query = query.Where(s => s.SlotDate == filterDate);
                }

                var response = await query.Get();
                var allSlots = response.Models ?? new List<TimeSlot>();

                DoctorSlotsList.Clear();

                foreach (var s in allSlots.OrderBy(x => x.SlotDate).ThenBy(x => x.StartTime))
                {
                    DoctorSlotsList.Add(new SlotViewModel(s));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDoctorSlots Error: {ex.Message}");
            }
        }

        private async void BtnAutoCreateSlots_Click(object sender, RoutedEventArgs e)
        {
            if (SessionStorage.CurrentUser == null)
                return;

            var date = NewSlotDatePicker.Date.DateTime.Date;
            var start = NewSlotStartTime.Time;
            var end = NewSlotEndTime.Time;
            var duration = TimeSpan.FromMinutes(SlotDurationBox.Value);
            var buffer = TimeSpan.FromMinutes(BufferDurationBox.Value);

            if (end <= start)
            {
                await ShowMsg("Lỗi", "Giờ kết thúc phải sau giờ bắt đầu.");
                return;
            }

            var slotsToCreate = new List<TimeSlot>();
            var current = start;

            while (current + duration <= end)
            {
                slotsToCreate.Add(new TimeSlot
                {
                    DoctorId = SessionStorage.CurrentUser.Id,
                    SlotDate = date,
                    StartTime = current,
                    EndTime = current + duration,
                    Status = "Available"
                });

                current += duration + buffer;
            }

            if (!slotsToCreate.Any())
            {
                await ShowMsg("Lỗi", "Không thể tạo khung giờ nào với thiết lập hiện tại.");
                return;
            }

            try
            {
                await _supabase.From<TimeSlot>().Insert(slotsToCreate);

                // Đồng bộ bộ lọc ngày sang ngày vừa tạo để bác sĩ thấy ngay
                FilterSlotDatePicker.Date = NewSlotDatePicker.Date;

                await ShowMsg("Thành công", $"Đã tạo {slotsToCreate.Count} khung giờ cho ngày {date:dd/MM/yyyy}");
                await LoadDoctorSlots();
            }
            catch (Exception ex)
            {
                await ShowMsg("Lỗi", "Không thể tạo khung giờ: " + ex.Message);
            }
        }

        private async void BtnDeleteSlot_Click(object sender, RoutedEventArgs e)
        {
            string? id = (sender as Button)?.CommandParameter?.ToString();

            if (string.IsNullOrEmpty(id))
                return;

            // Hỏi xác nhận trước khi xoá
            var confirmDialog = new ContentDialog
            {
                Title = "Xác nhận xoá",
                Content = "Bạn có chắc muốn xoá khung giờ này không?",
                PrimaryButtonText = "Xoá",
                CloseButtonText = "Huỷ",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            try
            {
                await _supabase.From<TimeSlot>().Where(x => x.Id == id).Delete();
                await LoadDoctorSlots();
                await ShowMsg("Thành công", "Đã xoá khung giờ thành công.");
            }
            catch (Exception ex)
            {
                await ShowMsg("Lỗi", "Không thể xoá khung giờ: " + ex.Message);
            }
        }

        private async void BtnDeleteAllAvailableSlots_Click(object sender, RoutedEventArgs e)
        {
            if (SessionStorage.CurrentUser == null)
                return;

            var date = FilterSlotDatePicker.Date.DateTime.Date;

            // Hỏi xác nhận trước khi xoá hàng loạt
            var confirmDialog = new ContentDialog
            {
                Title = "Xác nhận xoá tất cả",
                Content = $"Bạn có chắc muốn xoá tất cả khung giờ trống ngày {date:dd/MM/yyyy}?",
                PrimaryButtonText = "Xoá tất cả",
                CloseButtonText = "Huỷ",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            try
            {
                await _supabase.From<TimeSlot>()
                    .Where(x => x.DoctorId == SessionStorage.CurrentUser.Id)
                    .Where(x => x.SlotDate == date)
                    .Where(x => x.Status == "Available")
                    .Delete();

                await LoadDoctorSlots();
                await ShowMsg("Thành công", $"Đã xoá tất cả khung giờ trống ngày {date:dd/MM/yyyy}.");
            }
            catch (Exception ex)
            {
                await ShowMsg("Lỗi", "Không thể xoá: " + ex.Message);
            }
        }

        private void FilterSlotDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            _ = LoadDoctorSlots();
        }

        private async Task ShowMsg(string title, string content)
        {
            await new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }

        private void UpdateTabStyles(Button activeBtn)
        {
            var buttons = new[] { ListTabButton, WeekTabButton, MonthTabButton, SlotTabButton };
            var icons = new[] { ListTabIcon, WeekTabIcon, MonthTabIcon, SlotTabIcon };
            var labels = new[] { ListTabLabel, WeekTabLabel, MonthTabLabel, SlotTabLabel };

            for (int i = 0; i < buttons.Length; i++)
            {
                bool isActive = buttons[i] == activeBtn;

                buttons[i].Background = new SolidColorBrush(
                    isActive ? Color.FromArgb(255, 58, 141, 255) : Colors.Transparent
                );

                if (icons[i] != null)
                    icons[i].Foreground = new SolidColorBrush(
                        isActive ? Colors.White : Color.FromArgb(255, 100, 116, 139)
                    );

                if (labels[i] != null)
                    labels[i].Foreground = new SolidColorBrush(
                        isActive ? Colors.White : Color.FromArgb(255, 100, 116, 139)
                    );

                if (labels[i] != null)
                    labels[i].FontWeight = isActive
                        ? Microsoft.UI.Text.FontWeights.Bold
                        : Microsoft.UI.Text.FontWeights.SemiBold;
            }
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

    public class SlotViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
        public string DateFormatted { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public SolidColorBrush StatusBg => new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Status == "Available"
                ? Windows.UI.Color.FromArgb(255, 5, 150, 105)
                : (Status == "Booked"
                    ? Windows.UI.Color.FromArgb(255, 0, 89, 187)
                    : Windows.UI.Color.FromArgb(255, 100, 116, 139)));

        public Visibility CanDeleteVisibility =>
            Status == "Available" ? Visibility.Visible : Visibility.Collapsed;

        public SlotViewModel(TimeSlot model)
        {
            Id = model.Id ?? string.Empty;
            TimeRange = $"{model.StartTime:hh\\:mm} - {model.EndTime:hh\\:mm}";
            DateFormatted = model.SlotDate.ToString("dd/MM/yyyy");
            Status = model.Status ?? string.Empty;

            StatusText = Status switch
            {
                "Available" => "Trống",
                "Booked" => "Đã đặt",
                "Unavailable" => "Khoá",
                _ => "Không rõ"
            };
        }
    }

    public class AppointmentViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string BaseStatus { get; set; } = string.Empty;
        public string TypeText { get; set; } = string.Empty;
        public string PaymentText { get; set; } = string.Empty;
        public string DateFormatted { get; set; } = string.Empty;

        public Visibility IsPending =>
            BaseStatus == "Pending" ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsConfirmed =>
            (BaseStatus == "Confirmed" || BaseStatus == "Arrived")
                ? Visibility.Visible
                : Visibility.Collapsed;

        public Visibility IsOnlineCallVisible =>
            ((BaseStatus == "Confirmed" || BaseStatus == "Arrived") && TypeText == "Online")
                ? Visibility.Visible
                : Visibility.Collapsed;

        public SolidColorBrush BgBrush => new SolidColorBrush(
            BaseStatus == "Pending"
                ? Color.FromArgb(255, 255, 248, 225)
                : Color.FromArgb(255, 243, 248, 255));

        public Color BorderColorCode =>
            BaseStatus == "Pending"
                ? Color.FromArgb(255, 255, 224, 130)
                : Color.FromArgb(255, 191, 217, 253);

        public SolidColorBrush TagBgBrush => new SolidColorBrush(
            BaseStatus switch
            {
                "Pending" => Color.FromArgb(255, 255, 179, 0),
                "Confirmed" => Color.FromArgb(255, 0, 89, 187),
                "Arrived" => Color.FromArgb(255, 14, 165, 233),
                "Completed" => Color.FromArgb(255, 5, 150, 105),
                "Cancelled" => Color.FromArgb(255, 220, 38, 38),
                _ => Color.FromArgb(255, 100, 116, 139)
            });

        public string StatusText => BaseStatus switch
        {
            "Pending" => "Chờ duyệt",
            "Confirmed" => "Chưa đến",
            "Arrived" => "Đã đến",
            "Completed" => "Đã hoàn thành",
            "Cancelled" => "Đã hủy",
            _ => "Không rõ"
        };

        public SolidColorBrush TypeBgBrush => new SolidColorBrush(
            TypeText == "Online"
                ? Color.FromArgb(255, 124, 58, 237)
                : Color.FromArgb(255, 5, 150, 105));

        public AppointmentViewModel(Appointment model, string name, string? paymentMethod = null)
        {
            Id = model.Id ?? string.Empty;
            PatientId = model.PatientId ?? string.Empty;
            PatientName = name ?? "Bệnh nhân (ẩn danh)";
            Time = $"{model.StartTime:hh\\:mm} - {model.EndTime:hh\\:mm}";
            BaseStatus = model.Status ?? string.Empty;
            DateFormatted = model.AppointmentDate.ToString("dd/MM/yyyy");
            TypeText = model.ExaminationType ?? "Offline";

            PaymentText = paymentMethod == "Online"
                ? "Chuyển khoản"
                : (paymentMethod == "Manual" ? "Tại quầy" : "Chưa rõ");
        }
    }
}