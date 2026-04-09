using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using Windows.UI;

namespace Healthcare.Client.UI.Doctor
{
    /// <summary>
    /// ManageSchedulePage — Doctor on-call schedule management (main content only).
    /// Requires a NavigationView shell with TopNavBar and SideBar already present.
    /// </summary>
    public sealed partial class ManageSchedulePage : Page
    {
        // ─────────────────────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────────────────────

        /// <summary>The Monday of the currently displayed week.</summary>
        private DateTime _currentWeekMonday;

        /// <summary>The month currently displayed in the Month View.</summary>
        private DateTime _currentMonth;

        // Demo data: dates the logged-in doctor has a shift (adjust / replace with real data).
        private readonly HashSet<int> _myShiftDays = new() { 1, 4, 8, 11, 15, 18, 22, 25, 29 };
        // Demo data: night-shift dates.
        private readonly HashSet<int> _nightShiftDays = new() { 3, 7, 14, 21, 28 };
        // Demo data: empty / available slots.
        private readonly HashSet<int> _emptySlotDays = new() { 5, 12, 19, 26 };

        // ─────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────

        public ManageSchedulePage()
        {
            this.InitializeComponent();

            // Initialise to current week (Monday) and current month.
            _currentWeekMonday = GetMonday(DateTime.Today);
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            RefreshWeekView();
            RefreshMonthView();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Refresh so the "today" highlights are always up-to-date each navigation.
            _currentWeekMonday = GetMonday(DateTime.Today);
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            RefreshWeekView();
            RefreshMonthView();
        }

        // ─────────────────────────────────────────────────────────────
        // Tab switching
        // ─────────────────────────────────────────────────────────────

        private void WeekTab_Click(object sender, RoutedEventArgs e)
        {
            WeekViewPanel.Visibility = Visibility.Visible;
            MonthViewPanel.Visibility = Visibility.Collapsed;

            // Active: blue background, white text
            WeekTabButton.Background = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187));
            WeekTabIcon.Foreground   = new SolidColorBrush(Colors.White);
            WeekTabLabel.Foreground  = new SolidColorBrush(Colors.White);

            // Inactive: transparent background, muted text
            MonthTabButton.Background = new SolidColorBrush(Colors.Transparent);
            MonthTabIcon.Foreground   = new SolidColorBrush(Color.FromArgb(255, 70, 96, 127));
            MonthTabLabel.Foreground  = new SolidColorBrush(Color.FromArgb(255, 70, 96, 127));
        }

        private void MonthTab_Click(object sender, RoutedEventArgs e)
        {
            WeekViewPanel.Visibility = Visibility.Collapsed;
            MonthViewPanel.Visibility = Visibility.Visible;

            // Active: blue background, white text
            MonthTabButton.Background = new SolidColorBrush(Color.FromArgb(255, 0, 89, 187));
            MonthTabIcon.Foreground   = new SolidColorBrush(Colors.White);
            MonthTabLabel.Foreground  = new SolidColorBrush(Colors.White);

            // Inactive: transparent background, muted text
            WeekTabButton.Background = new SolidColorBrush(Colors.Transparent);
            WeekTabIcon.Foreground   = new SolidColorBrush(Color.FromArgb(255, 70, 96, 127));
            WeekTabLabel.Foreground  = new SolidColorBrush(Color.FromArgb(255, 70, 96, 127));
        }

        // ─────────────────────────────────────────────────────────────
        // Week navigation
        // ─────────────────────────────────────────────────────────────

        private void PrevWeek_Click(object sender, RoutedEventArgs e)
        {
            _currentWeekMonday = _currentWeekMonday.AddDays(-7);
            RefreshWeekView();
        }

        private void NextWeek_Click(object sender, RoutedEventArgs e)
        {
            _currentWeekMonday = _currentWeekMonday.AddDays(7);
            RefreshWeekView();
        }

        /// <summary>
        /// Updates all date labels in the week grid to match <see cref="_currentWeekMonday"/>.
        /// </summary>
        private void RefreshWeekView()
        {
            // Monday is index 0, so Sun = Monday - 1, Tue = +1, etc.
            DateTime sun = _currentWeekMonday.AddDays(-1);
            DateTime mon = _currentWeekMonday;
            DateTime tue = _currentWeekMonday.AddDays(1);
            DateTime wed = _currentWeekMonday.AddDays(2);
            DateTime thu = _currentWeekMonday.AddDays(3);
            DateTime fri = _currentWeekMonday.AddDays(4);
            DateTime sat = _currentWeekMonday.AddDays(5);

            // Update date number labels.
            SundayDate.Text    = sun.Day.ToString();
            MondayDate.Text    = mon.Day.ToString();
            TuesdayDate.Text   = tue.Day.ToString();
            WednesdayDate.Text = wed.Day.ToString();
            ThursdayDate.Text  = thu.Day.ToString();
            FridayDate.Text    = fri.Day.ToString();
            SaturdayDate.Text  = sat.Day.ToString();

            // Week range label, e.g. "20 – 26 tháng 1, 2025"
            string rangeText;
            if (sun.Month == sat.Month)
                rangeText = $"{sun.Day} – {sat.Day} tháng {sat.Month}, {sat.Year}";
            else
                rangeText = $"{sun.Day}/{sun.Month} – {sat.Day}/{sat.Month}/{sat.Year}";

            WeekRangeText.Text = rangeText;

            // Week-of-year counter (ISO: first week contains Thursday).
            int weekNum = GetIsoWeekNumber(mon);
            WeekNumberText.Text = $"Tuần {weekNum} · Tháng {mon.Month} năm {mon.Year}";

            // Highlight today's column header with gradient if today falls in this week.
            HighlightTodayInWeekView(sun, mon, tue, wed, thu, fri, sat);

            // Update the upcoming shift banner to show the next morning shift relative to today.
            DateTime tomorrow = DateTime.Today.AddDays(1);
            UpcomingShiftText.Text = $"Khoa Nội - {FormatDay(tomorrow)}, 08:00 - 16:00";
        }

        private void HighlightTodayInWeekView(
            DateTime sun, DateTime mon, DateTime tue, DateTime wed,
            DateTime thu, DateTime fri, DateTime sat)
        {
            // ── Reset ALL column backgrounds to transparent ──
            SundayColumnBorder.Background    = new SolidColorBrush(Colors.Transparent);
            MondayColumnBorder.Background    = new SolidColorBrush(Colors.Transparent);
            TuesdayColumnBorder.Background   = new SolidColorBrush(Colors.Transparent);
            WednesdayColumnBorder.Background = new SolidColorBrush(Colors.Transparent);
            ThursdayColumnBorder.Background  = new SolidColorBrush(Colors.Transparent);
            FridayColumnBorder.Background    = new SolidColorBrush(Colors.Transparent);
            SaturdayColumnBorder.Background  = new SolidColorBrush(Colors.Transparent);

            // ── Reset all day-headers to grey ──
            ResetDayHeader(SundayHeader,    SundayDayName,    SundayDate);
            ResetDayHeader(MondayHeader,    MondayDayName,    MondayDate);
            ResetDayHeader(TuesdayHeader,   TuesdayDayName,   TuesdayDate);
            ResetDayHeader(WednesdayHeader, WednesdayDayName, WednesdayDate);
            ResetDayHeader(ThursdayHeader,  ThursdayDayName,  ThursdayDate);
            ResetDayHeader(FridayHeader,    FridayDayName,    FridayDate);
            ResetDayHeader(SaturdayHeader,  SaturdayDayName,  SaturdayDate);

            // ── Apply today highlight ──
            DateTime today = DateTime.Today;
            if      (today == sun) { ApplyTodayColumn(SundayColumnBorder,    SundayHeader,    SundayDayName,    SundayDate); }
            else if (today == mon) { ApplyTodayColumn(MondayColumnBorder,    MondayHeader,    MondayDayName,    MondayDate); }
            else if (today == tue) { ApplyTodayColumn(TuesdayColumnBorder,   TuesdayHeader,   TuesdayDayName,   TuesdayDate); }
            else if (today == wed) { ApplyTodayColumn(WednesdayColumnBorder, WednesdayHeader, WednesdayDayName, WednesdayDate); }
            else if (today == thu) { ApplyTodayColumn(ThursdayColumnBorder,  ThursdayHeader,  ThursdayDayName,  ThursdayDate); }
            else if (today == fri) { ApplyTodayColumn(FridayColumnBorder,    FridayHeader,    FridayDayName,    FridayDate); }
            else if (today == sat) { ApplyTodayColumn(SaturdayColumnBorder,  SaturdayHeader,  SaturdayDayName,  SaturdayDate); }
        }

        /// <summary>Resets a day-header border and its text blocks to the default grey style.</summary>
        private static void ResetDayHeader(Border header, TextBlock dayName, TextBlock dateText)
        {
            header.Background   = new SolidColorBrush(Color.FromArgb(255, 237, 238, 239));
            header.CornerRadius = new CornerRadius(8);
            header.Padding      = new Thickness(8, 10, 8, 10);
            header.Margin       = new Thickness(0, 0, 0, 12);

            dayName.Foreground  = new SolidColorBrush(Color.FromArgb(255, 113, 119, 134)); // OutlineBrush
            dateText.Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
        }

        /// <summary>
        /// Applies the "today" treatment: light-blue column background + blue-gradient header
        /// with white text.
        /// </summary>
        private static void ApplyTodayColumn(
            Border columnBorder, Border header, TextBlock dayName, TextBlock dateText)
        {
            // Light-blue column background
            columnBorder.Background = new SolidColorBrush(Color.FromArgb(15, 0, 89, 187));

            // Blue gradient header
            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new Windows.Foundation.Point(0, 0);
            gradient.EndPoint   = new Windows.Foundation.Point(1, 1);
            gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 0, 89, 187),  Offset = 0 });
            gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 0, 112, 234), Offset = 1 });
            header.Background = gradient;

            // White text on the blue gradient
            dayName.Foreground  = new SolidColorBrush(Color.FromArgb(204, 255, 255, 255)); // #CCFFFFFF
            dateText.Foreground = new SolidColorBrush(Colors.White);
        }

        // ─────────────────────────────────────────────────────────────
        // Month navigation
        // ─────────────────────────────────────────────────────────────

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            RefreshMonthView();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            RefreshMonthView();
        }

        /// <summary>
        /// Rebuilds the month calendar grid for <see cref="_currentMonth"/>.
        /// Each cell is a <see cref="Border"/> added into a 7-column Grid via
        /// attached Grid.Column / Grid.Row properties.
        /// </summary>
        private void RefreshMonthView()
        {
            // Update header labels.
            MonthYearText.Text = $"Tháng {_currentMonth.Month}, {_currentMonth.Year}";
            MonthSubText.Text  = $"Xem toàn cảnh lịch trực cả tháng {_currentMonth.Month}/{_currentMonth.Year}";

            // Clear existing cells.
            MonthCalendarGrid.Items.Clear();

            int daysInMonth    = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            // Day-of-week of the 1st (0=Sunday … 6=Saturday).
            int firstDow       = (int)new DateTime(_currentMonth.Year, _currentMonth.Month, 1).DayOfWeek;
            DateTime today     = DateTime.Today;

            // Total cells needed (leading blanks + days).
            int totalCells = firstDow + daysInMonth;
            int totalRows  = (int)Math.Ceiling(totalCells / 7.0);

            for (int cellIndex = 0; cellIndex < totalRows * 7; cellIndex++)
            {
                int col = cellIndex % 7;
                int row = cellIndex / 7;
                int day = cellIndex - firstDow + 1; // =1 on the first real day

                var cell = BuildMonthCell(day, col, row, daysInMonth, today);
                MonthCalendarGrid.Items.Add(cell);
            }
        }

        /// <summary>Builds a single day cell for the month grid.</summary>
        private Border BuildMonthCell(int day, int col, int row, int daysInMonth, DateTime today)
        {
            bool isValidDay  = day >= 1 && day <= daysInMonth;
            bool isToday     = isValidDay && _currentMonth.Year == today.Year
                                          && _currentMonth.Month == today.Month
                                          && day == today.Day;
            bool isMyShift   = isValidDay && _myShiftDays.Contains(day);
            bool isNight     = isValidDay && _nightShiftDays.Contains(day);
            bool isEmpty     = isValidDay && _emptySlotDays.Contains(day);
            bool isSunday    = col == 0;

            // ── Container ──
            var cell = new Border
            {
                Margin        = new Thickness(2),
                CornerRadius  = new CornerRadius(10),
                MinHeight     = 72,
                Padding       = new Thickness(6, 8, 6, 8),
            };

            // Position inside Grid.
            Grid.SetColumn(cell, col);
            Grid.SetRow(cell, row);

            if (!isValidDay)
            {
                cell.Background = new SolidColorBrush(Colors.Transparent);
                return cell;
            }

            // ── Background colour by state ──
            if (isToday)
            {
                var grad = new LinearGradientBrush();
                grad.StartPoint = new Windows.Foundation.Point(0, 0);
                grad.EndPoint   = new Windows.Foundation.Point(1, 1);
                grad.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 0, 89, 187),  Offset = 0 });
                grad.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 0, 112, 234), Offset = 1 });
                cell.Background = grad;
            }
            else if (isMyShift)
                cell.Background = new SolidColorBrush(Color.FromArgb(30, 0, 89, 187));
            else if (isNight)
                cell.Background = new SolidColorBrush(Color.FromArgb(20, 186, 26, 26));
            else if (isEmpty)
                cell.Background = new SolidColorBrush(Color.FromArgb(25, 46, 125, 50));
            else
                cell.Background = new SolidColorBrush(Color.FromArgb(255, 243, 244, 245));

            // ── Inner stack ──
            var stack = new StackPanel { Spacing = 4 };

            // Day number
            var dayText = new TextBlock
            {
                Text       = day.ToString(),
                FontSize   = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = isToday  ? new SolidColorBrush(Colors.White)
                           : isSunday ? new SolidColorBrush(Color.FromArgb(255, 186, 26, 26))
                                      : (Brush)new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)),
            };
            stack.Children.Add(dayText);

            // Shift indicator dots
            if (isValidDay && (isMyShift || isNight || isEmpty))
            {
                var dotPanel = new StackPanel
                {
                    Orientation         = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing             = 4,
                };

                if (isMyShift)
                    dotPanel.Children.Add(MakeDot(isToday ? Colors.White
                                                           : Color.FromArgb(255, 0, 89, 187)));
                if (isNight)
                    dotPanel.Children.Add(MakeDot(Color.FromArgb(255, 186, 26, 26)));
                if (isEmpty)
                    dotPanel.Children.Add(MakeDot(Color.FromArgb(255, 46, 125, 50)));

                stack.Children.Add(dotPanel);

                // Small shift label
                string label = isMyShift ? "Ca bạn"
                             : isNight   ? "Ca đêm"
                                         : "Trống";
                stack.Children.Add(new TextBlock
                {
                    Text                = label,
                    FontSize            = 9,
                    FontWeight          = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping        = TextWrapping.NoWrap,
                    Foreground          = isToday ? new SolidColorBrush(Color.FromArgb(220, 255, 255, 255))
                                                  : new SolidColorBrush(Color.FromArgb(180, 70, 96, 127)),
                });
            }

            cell.Child = stack;
            return cell;
        }

        private static Border MakeDot(Color color) => new Border
        {
            Width        = 6,
            Height       = 6,
            CornerRadius = new CornerRadius(3),
            Background   = new SolidColorBrush(color),
        };

        // ─────────────────────────────────────────────────────────────
        // Banner
        // ─────────────────────────────────────────────────────────────

        /// <summary>"Xem chi tiết" button on the upcoming shift banner.</summary>
        private async void ViewShiftDetail_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title          = "Chi tiết ca trực",
                Content        = "Khoa Nội Tổng Quát\nNgày mai: 08:00 – 16:00\nPhòng: P.201",
                CloseButtonText = "Đóng",
                XamlRoot       = this.XamlRoot,
            };
            await dialog.ShowAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // Header action buttons
        // ─────────────────────────────────────────────────────────────

        private async void RegisterShift_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title          = "Đăng ký ca trực",
                Content        = "Chức năng đăng ký ca trực đang được phát triển.",
                CloseButtonText = "Đóng",
                XamlRoot       = this.XamlRoot,
            };
            await dialog.ShowAsync();
        }

        private async void SwapShift_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title          = "Đổi ca trực",
                Content        = "Chức năng đổi ca trực đang được phát triển.",
                CloseButtonText = "Đóng",
                XamlRoot       = this.XamlRoot,
            };
            await dialog.ShowAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // Calendar card actions
        // ─────────────────────────────────────────────────────────────

        private async void SwapShiftCard_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title           = "Yêu cầu đổi ca",
                Content         = "Bạn có muốn gửi yêu cầu đổi ca 08:00–16:00 Thứ Hai không?",
                PrimaryButtonText = "Gửi yêu cầu",
                CloseButtonText = "Hủy",
                XamlRoot        = this.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // TODO: Submit swap request to backend / ViewModel.
            }
        }

        private async void RegisterEmptySlot_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title             = "Xác nhận đăng ký",
                Content           = "Bạn có muốn đăng ký ca trực này không?",
                PrimaryButtonText = "Đăng ký",
                CloseButtonText   = "Hủy",
                XamlRoot          = this.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // TODO: Register shift via ViewModel / service and refresh UI.
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        /// <summary>Returns the Monday for the week containing <paramref name="date"/>.</summary>
        private static DateTime GetMonday(DateTime date)
        {
            int dow = (int)date.DayOfWeek; // 0=Sun
            // Shift so Monday = 0
            int diff = (dow == 0) ? -6 : 1 - dow;
            return date.AddDays(diff).Date;
        }

        /// <summary>ISO 8601 week number.</summary>
        private static int GetIsoWeekNumber(DateTime date)
        {
            // .NET's Calendar week-of-year with ISO 8601 rules.
            return System.Globalization.ISOWeek.GetWeekOfYear(date);
        }

        private static string FormatDay(DateTime d)
        {
            string[] dayNames = { "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư",
                                  "Thứ năm", "Thứ sáu", "Thứ bảy" };
            string dayName = dayNames[(int)d.DayOfWeek];
            return $"{dayName}, {d.Day:D2}/{d.Month:D2}";
        }
    }
}
