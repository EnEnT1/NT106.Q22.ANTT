using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.Foundation;
namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class RevenuePage : Page
    {
        private string _selectedPeriod = "month";   // "month" | "quarter" | "year"
        private string? _selectedDoctorId = null;   // null = tất cả
        private string? _selectedServiceType = null;

        private int _currentPage = 0;
        private int _totalPages = 1;
        private const int PageSize = 10;

        private List<TransactionItem> _allTransactions = new();
        private List<MonthlyChartPoint> _chartData = new();
        private List<ServiceSegment> _donutData = new();

        public RevenuePage()
        {
            this.InitializeComponent();
            this.Loaded += RevenuePage_Loaded;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadDataAsync();
        }

        private void RevenuePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_chartData.Count > 0)
            {
                DrawBarChart();
                DrawDonutChart();
            }
        }

        private async Task LoadDataAsync()
        {
            SetLoading(true);

            try
            {
                if (ServiceFilter != null && ServiceFilter.SelectedIndex < 0)
                {
                    ServiceFilter.SelectedIndex = 0;
                }

                if (DoctorFilter.ItemsSource == null)
                {
                    await LoadDoctorFilterAsync();
                }

                var (fromDate, toDate) = GetDateRange(_selectedPeriod);

                _allTransactions = await LoadTransactionsFromDatabaseAsync(fromDate, toDate);

                await CalculateAndRenderKpis(fromDate, toDate);

                _chartData = BuildChartData(fromDate, toDate);
                _donutData = BuildDonutData();

                TxtChartSubtitle.Text = BuildChartSubtitle(fromDate, toDate);

                _currentPage = 0;
                RenderTransactionRows();
                RenderBreakdownList();

                await Task.Delay(50);
                DrawBarChart();
                DrawDonutChart();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Không thể tải dữ liệu: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void ServiceFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServiceFilter.SelectedItem is ComboBoxItem item)
            {
                _selectedServiceType = item.Content?.ToString() switch
                {
                    "Khám trực tuyến" => "online",
                    "Khám tại phòng mạch" => "clinic",
                    "Xét nghiệm" => "lab",
                    "Đơn thuốc" => "rx",
                    _ => null
                };
            }
            else
            {
                _selectedServiceType = null;
            }

            _currentPage = 0;
            await LoadDataAsync();
        }

        private async Task<List<TransactionItem>> LoadTransactionsFromDatabaseAsync(DateTime fromDate, DateTime toDate)
        {
            var result = new List<TransactionItem>();

            try
            {
                var client = SupabaseManager.Instance.Client;

                var transactionResponse = await client
                    .From<Transaction>()
                    .Get();

                var transactions = transactionResponse.Models
                    .Where(t => t.PaidAt.HasValue
                             && t.PaidAt.Value >= fromDate
                             && t.PaidAt.Value <= toDate)
                    .ToList();

                var appointmentResponse = await client
                    .From<Appointment>()
                    .Get();

                var appointments = appointmentResponse.Models;

                var userResponse = await client
                    .From<User>()
                    .Get();

                var users = userResponse.Models;

                foreach (var transaction in transactions)
                {
                    var appointment = appointments.FirstOrDefault(a => a.Id == transaction.AppointmentId);
                    var patientUser = users.FirstOrDefault(u => u.Id == transaction.PatientId);

                    result.Add(new TransactionItem
                    {
                        Id = transaction.Id,
                        PatientName = patientUser?.FullName ?? "Bệnh nhân",
                        ServiceType = InferServiceType(transaction, appointment),
                        Status = transaction.Status,
                        Amount = transaction.Amount,
                        CreatedAt = transaction.PaidAt ?? appointment?.CreatedAt ?? DateTime.Now,
                        DoctorId = appointment?.DoctorId
                    });
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Lỗi tải dữ liệu doanh thu: " + ex.Message);
            }

            return result;
        }

        private string InferServiceType(Transaction transaction, Appointment? appointment)
        {
            var paymentMethod = transaction.PaymentMethod?.Trim().ToLower() ?? "";

            if (paymentMethod.Contains("online"))
                return "online";

            if (paymentMethod.Contains("lab"))
                return "lab";

            if (paymentMethod.Contains("rx") || paymentMethod.Contains("pharmacy") || paymentMethod.Contains("medicine"))
                return "rx";

            return "clinic";
        }

        private async Task CalculateAndRenderKpis(DateTime fromDate, DateTime toDate)
        {
            var filtered = ApplyFilters(_allTransactions);
            var paid = filtered.Where(t => t.Status == "completed").ToList();

            var span = toDate - fromDate;
            var prevFrom = fromDate - span;
            var prevTo = fromDate.AddSeconds(-1);

            var prevTransactions = await LoadTransactionsFromDatabaseAsync(prevFrom, prevTo);
            var prevPaid = prevTransactions.Where(t => t.Status == "completed").ToList();

            decimal totalRevenue = paid.Sum(t => t.Amount);
            decimal prevRevenue = prevPaid.Sum(t => t.Amount);
            double revenueGrowth = prevRevenue == 0 ? 0
                : (double)((totalRevenue - prevRevenue) / prevRevenue * 100);

            TxtTotalRevenue.Text = totalRevenue.ToString("N0");
            SetGrowthBadge(TxtTotalGrowth, Icon_TotalGrowth, Badge_TotalGrowth, revenueGrowth);

            int completedVisits = paid.Count;
            int prevCompleted = prevPaid.Count;
            double visitsGrowth = prevCompleted == 0 ? 0
                : (double)(completedVisits - prevCompleted) / prevCompleted * 100;

            TxtCompletedVisits.Text = completedVisits.ToString("N0");
            TxtVisitsGrowth.Text = FormatGrowth(visitsGrowth);
            TxtVisitsGrowth.Foreground = new SolidColorBrush(
                visitsGrowth >= 0 ? HexToColor("#16A34A") : HexToColor("#DC2626"));

            decimal avgRevenue = completedVisits == 0 ? 0 : totalRevenue / completedVisits;
            decimal prevAvg = prevCompleted == 0 ? 0 : prevRevenue / prevCompleted;
            double avgGrowth = prevAvg == 0 ? 0
                : (double)((avgRevenue - prevAvg) / prevAvg * 100);

            TxtAvgRevenue.Text = avgRevenue.ToString("N0");
            SetGrowthBadge(TxtAvgGrowth, Icon_AvgGrowth, Badge_AvgGrowth, avgGrowth);

            TxtMonthlyGrowth.Text = FormatGrowth(revenueGrowth);
            GrowthProgressBar.Value = Math.Min(Math.Abs(revenueGrowth), 100);
            GrowthProgressBar.Foreground = new SolidColorBrush(
                revenueGrowth >= 0 ? HexToColor("#10B981") : HexToColor("#EF4444"));
        }

        private List<MonthlyChartPoint> BuildChartData(DateTime from, DateTime to)
        {
            var result = new List<MonthlyChartPoint>();
            var paid = ApplyFilters(_allTransactions).Where(t => t.Status == "completed");

            var cursor = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            while (cursor <= end)
            {
                var month = cursor;
                var group = paid.Where(t => t.CreatedAt.Year == month.Year
                                         && t.CreatedAt.Month == month.Month).ToList();

                result.Add(new MonthlyChartPoint
                {
                    Label = $"Th {month.Month}",
                    DirectAmount = (double)group
                        .Where(t => t.ServiceType != "online")
                        .Sum(t => t.Amount),
                    OnlineAmount = (double)group
                        .Where(t => t.ServiceType == "online")
                        .Sum(t => t.Amount),
                });

                cursor = cursor.AddMonths(1);
            }

            return result;
        }

        private List<ServiceSegment> BuildDonutData()
        {
            var paid = ApplyFilters(_allTransactions).Where(t => t.Status == "completed").ToList();
            decimal total = paid.Sum(t => t.Amount);

            if (total == 0) return new();

            var colors = new Dictionary<string, string>
            {
                ["clinic"] = "#0059BB",
                ["online"] = "#BFD9FD",
                ["lab"] = "#C084FC",
                ["rx"] = "#FB923C",
            };

            var labels = new Dictionary<string, string>
            {
                ["clinic"] = "Khám tại phòng mạch",
                ["online"] = "Khám trực tuyến",
                ["lab"] = "Xét nghiệm",
                ["rx"] = "Đơn thuốc",
            };

            return colors.Keys.Select(key => new ServiceSegment
            {
                Label = labels[key],
                Color = colors[key],
                Share = (double)(paid.Where(t => t.ServiceType == key).Sum(t => t.Amount) / total),
            }).Where(s => s.Share > 0).ToList();
        }

        private void RenderTransactionRows()
        {
            TransactionRows.Children.Clear();
            PageButtons.Children.Clear();

            var filtered = ApplyFilters(_allTransactions)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            _totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
            _currentPage = Math.Clamp(_currentPage, 0, _totalPages - 1);

            var page = filtered
                .Skip(_currentPage * PageSize)
                .Take(PageSize)
                .ToList();

            EmptyState.Visibility = page.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var tx in page)
                TransactionRows.Children.Add(BuildTransactionRow(tx));

            int shown = filtered.Count == 0 ? 0 : Math.Min(PageSize, filtered.Count - _currentPage * PageSize);
            TxtPaginationInfo.Text = $"Hiển thị {shown} trong số {filtered.Count} giao dịch";

            for (int i = 0; i < _totalPages; i++)
            {
                int pageIndex = i;
                bool isActive = (i == _currentPage);

                var btn = new Border
                {
                    Background = new SolidColorBrush(isActive ? HexToColor("#0059BB") : Colors.Transparent),
                    CornerRadius = new CornerRadius(6),
                    Width = 28,
                    Height = 28
                };

                var lbl = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 12,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = new SolidColorBrush(isActive ? Colors.White : HexToColor("#64748B")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                btn.Child = lbl;
                btn.Tapped += (s, e) =>
                {
                    _currentPage = pageIndex;
                    RenderTransactionRows();
                };

                PageButtons.Children.Add(btn);
            }

            PrevPageButton.IsEnabled = _currentPage > 0;
            NextPageButton.IsEnabled = _currentPage < _totalPages - 1;
        }

        private Border BuildTransactionRow(TransactionItem tx)
        {
            var row = new Border
            {
                Padding = new Thickness(28, 16, 28, 16),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(HexToColor("#F8FAFC")),
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

            var dateStack = new StackPanel();
            dateStack.Children.Add(new TextBlock
            {
                Text = tx.CreatedAt.ToString("dd 'Th' MM, yyyy"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(HexToColor("#1E293B"))
            });
            dateStack.Children.Add(new TextBlock
            {
                Text = tx.CreatedAt.ToString("HH:mm"),
                FontSize = 10,
                Foreground = new SolidColorBrush(HexToColor("#94A3B8"))
            });
            Grid.SetColumn(dateStack, 0);

            var patientStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            var avatar = new Ellipse { Width = 32, Height = 32, Fill = new SolidColorBrush(HexToColor("#E2E8F0")) };
            patientStack.Children.Add(avatar);
            patientStack.Children.Add(new TextBlock
            {
                Text = tx.PatientName ?? "–",
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(HexToColor("#1E293B")),
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn(patientStack, 1);

            var (badgeBg, badgeFg, badgeTxt) = GetServiceBadgeStyle(tx.ServiceType);
            var serviceBadge = new Border
            {
                Background = new SolidColorBrush(HexToColor(badgeBg)),
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(12, 4, 12, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = badgeTxt,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(HexToColor(badgeFg))
                }
            };
            Grid.SetColumn(serviceBadge, 2);

            var amountTxt = new TextBlock
            {
                Text = tx.Amount.ToString("N0"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(HexToColor("#1E293B")),
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(amountTxt, 3);

            var (statusBg, statusDot, statusTxt) = GetStatusBadgeStyle(tx.Status);
            var statusContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusContent.Children.Add(new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(HexToColor(statusDot)),
                VerticalAlignment = VerticalAlignment.Center
            });
            statusContent.Children.Add(new TextBlock
            {
                Text = statusTxt,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(HexToColor(statusDot == "#16A34A" ? "#15803D" : "#C2410C"))
            });

            var statusBadge = new Border
            {
                Background = new SolidColorBrush(HexToColor(statusBg)),
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(12, 4, 12, 4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = statusContent
            };
            Grid.SetColumn(statusBadge, 4);

            grid.Children.Add(dateStack);
            grid.Children.Add(patientStack);
            grid.Children.Add(serviceBadge);
            grid.Children.Add(amountTxt);
            grid.Children.Add(statusBadge);

            row.Child = grid;
            return row;
        }

        private void RenderBreakdownList()
        {
            BreakdownList.Children.Clear();

            foreach (var seg in _donutData)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                left.Children.Add(new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(HexToColor(seg.Color)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                left.Children.Add(new TextBlock
                {
                    Text = seg.Label,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(HexToColor("#475569")),
                    VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(left, 0);

                var right = new TextBlock
                {
                    Text = $"{seg.Share * 100:F0}%",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(HexToColor("#1E293B"))
                };
                Grid.SetColumn(right, 1);

                row.Children.Add(left);
                row.Children.Add(right);
                BreakdownList.Children.Add(row);
            }
        }

        private void DrawBarChart()
        {
            RevenueBarChart.Children.Clear();
            if (_chartData.Count == 0) return;

            double canvasW = RevenueBarChart.ActualWidth > 0 ? RevenueBarChart.ActualWidth : 600;
            double canvasH = RevenueBarChart.ActualHeight > 0 ? RevenueBarChart.ActualHeight : 220;
            double chartH = canvasH - 28;

            double maxVal = _chartData.Max(p => p.DirectAmount + p.OnlineAmount);
            if (maxVal == 0) maxVal = 1;

            double barArea = canvasW / _chartData.Count;
            double barW = barArea * 0.50;
            double gap = barArea * 0.25;

            Color primaryColor = HexToColor("#0059BB");
            Color onlineColor = HexToColor("#BFD9FD");

            for (int i = 0; i < _chartData.Count; i++)
            {
                var pt = _chartData[i];
                double x = i * barArea + gap;

                double onlineH = (pt.OnlineAmount / maxVal) * chartH * 0.85;
                double directH = (pt.DirectAmount / maxVal) * chartH * 0.85;

                if (onlineH > 0)
                {
                    var onlineRect = new Rectangle
                    {
                        Width = barW,
                        Height = onlineH,
                        Fill = new SolidColorBrush(onlineColor),
                        RadiusX = 4,
                        RadiusY = 4,
                        Opacity = 0.85
                    };
                    Canvas.SetLeft(onlineRect, x);
                    Canvas.SetTop(onlineRect, chartH - onlineH);
                    RevenueBarChart.Children.Add(onlineRect);
                }

                if (directH > 0)
                {
                    var directRect = new Rectangle
                    {
                        Width = barW,
                        Height = directH,
                        Fill = new SolidColorBrush(primaryColor),
                        RadiusX = 4,
                        RadiusY = 4
                    };
                    Canvas.SetLeft(directRect, x);
                    Canvas.SetTop(directRect, chartH - onlineH - directH);
                    RevenueBarChart.Children.Add(directRect);
                }

                bool isLast = (i == _chartData.Count - 1);
                var lbl = new TextBlock
                {
                    Text = pt.Label,
                    FontSize = 10,
                    FontWeight = isLast ? FontWeights.ExtraBold : FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(isLast ? HexToColor("#0059BB") : HexToColor("#94A3B8")),
                    Width = barW,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(lbl, x);
                Canvas.SetTop(lbl, chartH + 8);
                RevenueBarChart.Children.Add(lbl);
            }
        }

        private void RevenueBarChart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_chartData.Count > 0) DrawBarChart();
        }

        private void DrawDonutChart()
        {
            DonutChart.Children.Clear();

            double cx = 80, cy = 80, outerR = 72, innerR = 50;
            double startAngle = -90.0;

            var segments = _donutData.Count > 0
                ? _donutData
                : new List<ServiceSegment> { new() { Share = 1, Color = "#E2E8F0" } };

            foreach (var seg in segments)
            {
                double sweep = seg.Share * 360.0;
                DonutChart.Children.Add(
                    BuildDonutSegment(cx, cy, outerR, innerR, startAngle, sweep, HexToColor(seg.Color)));
                startAngle += sweep;
            }

            var pct = new TextBlock
            {
                Text = "100%",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(HexToColor("#1E293B")),
                Width = 80,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(pct, cx - 40);
            Canvas.SetTop(pct, cy - 16);
            DonutChart.Children.Add(pct);

            var sub = new TextBlock
            {
                Text = "CƠ CẤU",
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                CharacterSpacing = 150,
                Foreground = new SolidColorBrush(HexToColor("#94A3B8")),
                Width = 80,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(sub, cx - 40);
            Canvas.SetTop(sub, cy + 4);
            DonutChart.Children.Add(sub);
        }

        private static Path BuildDonutSegment(
            double cx, double cy, double outerR, double innerR,
            double startDeg, double sweepDeg, Color color)
        {
            if (sweepDeg >= 360) sweepDeg = 359.9999;

            double startRad = DegToRad(startDeg);
            double endRad = DegToRad(startDeg + sweepDeg);
            bool isLarge = sweepDeg > 180;

            var outerStart = Pt(cx, cy, outerR, startRad);
            var outerEnd = Pt(cx, cy, outerR, endRad);
            var innerStart = Pt(cx, cy, innerR, endRad);
            var innerEnd = Pt(cx, cy, innerR, startRad);

            var fig = new PathFigure { StartPoint = outerStart, IsClosed = true };
            fig.Segments.Add(new ArcSegment
            {
                Point = outerEnd,
                Size = new Size(outerR, outerR),
                IsLargeArc = isLarge,
                SweepDirection = SweepDirection.Clockwise
            });
            fig.Segments.Add(new LineSegment { Point = innerStart });
            fig.Segments.Add(new ArcSegment
            {
                Point = innerEnd,
                Size = new Size(innerR, innerR),
                IsLargeArc = isLarge,
                SweepDirection = SweepDirection.Counterclockwise
            });

            var geo = new PathGeometry();
            geo.Figures.Add(fig);

            return new Path
            {
                Data = geo,
                Fill = new SolidColorBrush(color)
            };
        }

        private async void BtnPeriod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            _selectedPeriod = btn.Tag?.ToString() ?? "month";
            _currentPage = 0;

            foreach (var b in new[] { BtnThisMonth, BtnThisQuarter, BtnThisYear })
            {
                b.Style = b == btn
                    ? (Style)Resources["FilterActiveStyle"]
                    : (Style)Resources["FilterInactiveStyle"];
            }

            await LoadDataAsync();
        }

        private async void DoctorFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoctorFilter.SelectedItem is DoctorFilterItem item)
            {
                _selectedDoctorId = string.IsNullOrWhiteSpace(item.Id) ? null : item.Id;
            }
            else
            {
                _selectedDoctorId = null;
            }

            _currentPage = 0;
            await LoadDataAsync();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowInfoDialog(
                "Xuất báo cáo",
                "Tính năng xuất báo cáo đang được phát triển.\nSẽ hỗ trợ xuất CSV và Excel trong phiên bản tiếp theo.");
        }

        private void ViewAllTransactions_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate đến trang danh sách giao dịch đầy đủ
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                RenderTransactionRows();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages - 1)
            {
                _currentPage++;
                RenderTransactionRows();
            }
        }

        private List<TransactionItem> ApplyFilters(List<TransactionItem> source)
        {
            var q = source.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_selectedDoctorId))
                q = q.Where(t => t.DoctorId == _selectedDoctorId);

            if (!string.IsNullOrWhiteSpace(_selectedServiceType))
                q = q.Where(t => string.Equals(t.ServiceType, _selectedServiceType, StringComparison.OrdinalIgnoreCase));

            return q.ToList();
        }

        private static (DateTime from, DateTime to) GetDateRange(string period)
        {
            var now = DateTime.Now;

            return period switch
            {
                "quarter" => (
                    new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1),
                    new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 3, 1).AddMonths(1).AddSeconds(-1)
                ),
                "year" => (
                    new DateTime(now.Year, 1, 1),
                    new DateTime(now.Year, 12, 31, 23, 59, 59)
                ),
                _ => (
                    new DateTime(now.Year, now.Month, 1),
                    new DateTime(now.Year, now.Month, 1).AddMonths(1).AddSeconds(-1)
                )
            };
        }

        private static string BuildChartSubtitle(DateTime from, DateTime to)
            => $"Th {from.Month}/{from.Year} – Th {to.Month}/{to.Year}";

        private static (string bg, string fg, string text) GetServiceBadgeStyle(string? type) => type switch
        {
            "online" => ("#EFF4FF", "#3B5BDB", "Khám trực tuyến"),
            "clinic" => ("#DBEAFE", "#1D4ED8", "Phòng mạch"),
            "lab" => ("#F3E8FF", "#6D28D9", "Xét nghiệm"),
            "rx" => ("#FFEDD5", "#C2410C", "Đơn thuốc"),
            _ => ("#F1F5F9", "#64748B", type ?? "–")
        };

        private static (string bg, string dot, string text) GetStatusBadgeStyle(string? status) => status switch
        {
            "completed" => ("#F0FDF4", "#16A34A", "Đã thanh toán"),
            "processing" => ("#FFF7ED", "#EA580C", "Đang xử lý"),
            "cancelled" => ("#FEF2F2", "#DC2626", "Đã huỷ"),
            _ => ("#F8FAFC", "#94A3B8", status ?? "–")
        };

        private void SetGrowthBadge(TextBlock txt, FontIcon icon, Border badge, double growth)
        {
            txt.Text = FormatGrowth(growth);

            bool positive = growth >= 0;
            var color = HexToColor(positive ? "#16A34A" : "#DC2626");

            txt.Foreground = new SolidColorBrush(color);
            icon.Foreground = new SolidColorBrush(color);
            icon.Glyph = positive ? "\uE96D" : "\uE96E";
            badge.Background = new SolidColorBrush(HexToColor(positive ? "#F0FDF4" : "#FEF2F2"));
        }

        private static string FormatGrowth(double g)
            => g >= 0 ? $"+{g:F1}%" : $"{g:F1}%";

        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            MainScrollViewer.IsEnabled = !isLoading;
        }

        private async Task ShowErrorDialog(string message)
        {
            var d = new ContentDialog
            {
                Title = "Lỗi",
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };
            await d.ShowAsync();
        }

        private async Task ShowInfoDialog(string title, string message)
        {
            var d = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };
            await d.ShowAsync();
        }

        private static double DegToRad(double d) => d * Math.PI / 180.0;

        private static Windows.Foundation.Point Pt(double cx, double cy, double r, double rad)
            => new(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));

        private static Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16)
            );
        }

        private static List<TransactionItem> GenerateMockTransactions(DateTime from, DateTime to)
        {
            var rnd = new Random(42);
            var types = new[] { "clinic", "online", "lab", "rx" };
            var statuses = new[] { "completed", "completed", "completed", "processing", "cancelled" };
            var names = new[] { "Lê Thị Mai Anh", "Nguyễn Văn Bình", "Trần Hoàng Yến", "Phạm Minh Đức", "Võ Thị Lan", "Đặng Minh Khoa" };

            var list = new List<TransactionItem>();
            var cursor = from;

            while (cursor <= to)
            {
                int count = rnd.Next(1, 6);

                for (int i = 0; i < count; i++)
                {
                    list.Add(new TransactionItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        PatientName = names[rnd.Next(names.Length)],
                        ServiceType = types[rnd.Next(types.Length)],
                        Status = statuses[rnd.Next(statuses.Length)],
                        Amount = rnd.Next(3, 30) * 100_000m,
                        CreatedAt = cursor.AddHours(rnd.Next(8, 18)).AddMinutes(rnd.Next(0, 59)),
                        DoctorId = "mock-doctor-id",
                    });
                }

                cursor = cursor.AddDays(1);
            }

            return list.OrderByDescending(t => t.CreatedAt).ToList();
        }

        private async Task LoadDoctorFilterAsync()
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var doctorResponse = await client
                    .From<DoctorProfile>()
                    .Get();

                var userResponse = await client
                    .From<User>()
                    .Get();

                var doctors = doctorResponse.Models;
                var users = userResponse.Models;

                var doctorItems = new List<DoctorFilterItem>
                {
                    new DoctorFilterItem
                    {
                        Id = "",
                        FullName = "Tất cả bác sĩ"
                    }
                };

                foreach (var doctor in doctors)
                {
                    var user = users.FirstOrDefault(u => u.Id == doctor.DoctorId);

                    doctorItems.Add(new DoctorFilterItem
                    {
                        Id = doctor.DoctorId,
                        FullName = user?.FullName ?? "Bác sĩ"
                    });
                }

                DoctorFilter.ItemsSource = doctorItems;
                DoctorFilter.DisplayMemberPath = "FullName";
                DoctorFilter.SelectedValuePath = "Id";
                DoctorFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Không tải được danh sách bác sĩ: " + ex.Message);
            }
        }
    }

    internal class TransactionItem
    {
        public string Id { get; set; } = "";
        public string? PatientName { get; set; }
        public string? ServiceType { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DoctorId { get; set; }
    }

    internal class MonthlyChartPoint
    {
        public string Label { get; set; } = "";
        public double DirectAmount { get; set; }
        public double OnlineAmount { get; set; }
    }

    internal class ServiceSegment
    {
        public string Label { get; set; } = "";
        public string Color { get; set; } = "#CBD5E1";
        public double Share { get; set; }
    }

    internal class DoctorFilterItem
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}