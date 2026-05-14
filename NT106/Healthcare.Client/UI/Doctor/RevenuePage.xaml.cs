using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Microsoft.UI;
using Microsoft.UI.Text;
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

namespace Healthcare.Client.UI.Doctor
{
    public sealed partial class RevenuePage : Page
    {
        private readonly ObservableCollection<HealthResultItemViewModel> _visibleResults = new();

        private List<HealthResultItemViewModel> _allResults = new();
        private List<HealthResultItemViewModel> _filteredResults = new();

        private int _currentPage = 0;
        private const int PageSize = 10;

        public RevenuePage()
        {
            this.InitializeComponent();
            ResultListView.ItemsSource = _visibleResults;
            this.Loaded += RevenuePage_Loaded;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadDataAsync();
        }

        private async void RevenuePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_allResults.Count == 0)
            {
                await LoadDataAsync();
            }
        }

        private async Task LoadDataAsync()
        {
            SetLoading(true);

            try
            {
                var client = SupabaseManager.Instance.Client;

                var recordResponse = await client.From<MedicalRecord>().Get();
                var records = recordResponse.Models ?? new List<MedicalRecord>();

                var appointmentResponse = await client.From<Appointment>().Get();
                var appointments = appointmentResponse.Models ?? new List<Appointment>();

                var userResponse = await client.From<User>().Get();
                var users = userResponse.Models ?? new List<User>();

                var transactionResponse = await client.From<Transaction>().Get();
                var transactions = transactionResponse.Models ?? new List<Transaction>();

                _allResults = records
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(record =>
                    {
                        var appointment = appointments.FirstOrDefault(a => a.Id == record.AppointmentId);
                        var patient = users.FirstOrDefault(u => u.Id == record.PatientId);
                        var doctor = users.FirstOrDefault(u => u.Id == record.DoctorId || u.Id == appointment?.DoctorId);
                        var transaction = transactions.FirstOrDefault(t =>
                            t.AppointmentId == record.AppointmentId ||
                            t.PatientId == record.PatientId);

                        string status = NormalizePaymentStatus(transaction?.Status);
                        decimal amount = transaction?.Amount ?? 0;

                        return new HealthResultItemViewModel
                        {
                            Id = record.Id ?? string.Empty,
                            PatientId = record.PatientId ?? string.Empty,
                            PatientName = string.IsNullOrWhiteSpace(patient?.FullName) ? "Bệnh nhân" : patient.FullName,
                            Initials = GetInitials(patient?.FullName),
                            ExamDate = record.CreatedAt,
                            ExamDateText = record.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                            DoctorNameText = "BS. " + (string.IsNullOrWhiteSpace(doctor?.FullName) ? ShortId(record.DoctorId) : doctor.FullName),
                            DiagnosisText = string.IsNullOrWhiteSpace(record.Diagnosis)
                                ? "Chưa có chẩn đoán"
                                : "Chẩn đoán: " + record.Diagnosis,
                            MedicineText = string.IsNullOrWhiteSpace(record.AiMedicines)
                                ? "Thuốc/đơn thuốc: Chưa cập nhật"
                                : "Thuốc/đơn thuốc: " + record.AiMedicines,
                            PaymentStatus = status,
                            PaymentStatusText = GetPaymentStatusText(status),
                            Amount = amount,
                            AmountText = amount > 0 ? amount.ToString("N0") + " VNĐ" : "Chưa ghi nhận",
                            AvatarBackground = new SolidColorBrush(ParseColor("#E0F2FE")),
                            AvatarForeground = new SolidColorBrush(ParseColor("#0369A1")),
                            StatusBackground = new SolidColorBrush(ParseColor(GetPaymentStatusBackground(status))),
                            StatusForeground = new SolidColorBrush(ParseColor(GetPaymentStatusForeground(status)))
                        };
                    })
                    .ToList();

                _currentPage = 0;
                ApplyFilters();
                RenderKpis();
            }
            catch (Exception ex)
            {
                await ShowInfoDialog("Lỗi", "Không tải được kết quả khám: " + ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void ApplyFilters()
        {
            string keyword = SearchBox.Text.Trim().ToLower();
            string statusFilter = GetSelectedComboText(StatusFilter);
            string dateFilter = GetSelectedComboText(DateFilter);

            var query = _allResults.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.PatientName.ToLower().Contains(keyword) ||
                    x.DiagnosisText.ToLower().Contains(keyword) ||
                    x.MedicineText.ToLower().Contains(keyword) ||
                    x.Id.ToLower().Contains(keyword));
            }

            if (statusFilter == "Đã thanh toán")
            {
                query = query.Where(x => x.PaymentStatus == "completed");
            }
            else if (statusFilter == "Đang xử lý")
            {
                query = query.Where(x => x.PaymentStatus == "processing");
            }
            else if (statusFilter == "Chưa thanh toán")
            {
                query = query.Where(x => x.PaymentStatus == "unpaid");
            }

            DateTime now = DateTime.Now;

            if (dateFilter == "Hôm nay")
            {
                query = query.Where(x => x.ExamDate.Date == DateTime.Today);
            }
            else if (dateFilter == "Tháng này")
            {
                query = query.Where(x => x.ExamDate.Year == now.Year && x.ExamDate.Month == now.Month);
            }
            else if (dateFilter == "Năm nay")
            {
                query = query.Where(x => x.ExamDate.Year == now.Year);
            }

            _filteredResults = query
                .OrderByDescending(x => x.ExamDate)
                .ToList();

            RenderResultList();
        }

        private void RenderKpis()
        {
            TxtTotalResults.Text = _allResults.Count.ToString("N0");
            TxtPatientCount.Text = _allResults.Select(x => x.PatientId).Distinct().Count().ToString("N0");
            TxtMedicineCount.Text = _allResults.Count(x => !x.MedicineText.Contains("Chưa cập nhật")).ToString("N0");

            decimal totalAmount = _allResults.Sum(x => x.Amount);
            TxtTotalPaidAmount.Text = totalAmount > 0 ? totalAmount.ToString("N0") + " VNĐ" : "0 VNĐ";
        }

        private void RenderResultList()
        {
            _visibleResults.Clear();

            int totalItems = _filteredResults.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            if (_currentPage < 0)
                _currentPage = 0;

            if (_currentPage >= totalPages)
                _currentPage = totalPages - 1;

            var pageItems = _filteredResults
                .Skip(_currentPage * PageSize)
                .Take(PageSize)
                .ToList();

            foreach (var item in pageItems)
            {
                _visibleResults.Add(item);
            }

            EmptyState.Visibility = pageItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ResultListView.Visibility = pageItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            TxtResultSubtitle.Text = $"{totalItems} kết quả khám phù hợp";
            TxtPaginationInfoTop.Text = $"{totalItems} kết quả";

            int shownFrom = totalItems == 0 ? 0 : _currentPage * PageSize + 1;
            int shownTo = Math.Min((_currentPage + 1) * PageSize, totalItems);

            TxtPaginationInfo.Text = $"Hiển thị {shownFrom} - {shownTo} / {totalItems} kết quả";
            TxtPageNumber.Text = $"{_currentPage + 1} / {totalPages}";

            PrevPageButton.IsEnabled = _currentPage > 0;
            NextPageButton.IsEnabled = _currentPage < totalPages - 1;
        }

        private async void ResultListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not HealthResultItemViewModel item)
                return;

            string content =
                $"Bệnh nhân: {item.PatientName}\n" +
                $"Ngày khám: {item.ExamDateText}\n" +
                $"Bác sĩ: {item.DoctorNameText}\n\n" +
                $"{item.DiagnosisText}\n\n" +
                $"{item.MedicineText}\n\n" +
                $"Trạng thái thanh toán: {item.PaymentStatusText}\n" +
                $"Chi phí: {item.AmountText}";

            await ShowInfoDialog("Chi tiết kết quả khám", content);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPage = 0;
            ApplyFilters();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            _currentPage = 0;
            ApplyFilters();
        }

        private void DateFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            _currentPage = 0;
            ApplyFilters();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowInfoDialog(
                "Xuất báo cáo",
                "Tính năng xuất báo cáo kết quả khám đang được phát triển.\nHiện tại bạn có thể xem trực tiếp danh sách kết quả khám trên màn hình này.");
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                RenderResultList();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = Math.Max(1, (int)Math.Ceiling(_filteredResults.Count / (double)PageSize));

            if (_currentPage < totalPages - 1)
            {
                _currentPage++;
                RenderResultList();
            }
        }

        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            MainScrollViewer.IsEnabled = !isLoading;
        }

        private async Task ShowInfoDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }

        private static string GetSelectedComboText(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string NormalizePaymentStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "unpaid";

            string value = status.Trim().ToLower();

            return value switch
            {
                "completed" => "completed",
                "paid" => "completed",
                "success" => "completed",
                "processing" => "processing",
                "pending" => "processing",
                "cancelled" => "unpaid",
                "failed" => "unpaid",
                _ => value
            };
        }

        private static string GetPaymentStatusText(string status)
        {
            return status switch
            {
                "completed" => "Đã thanh toán",
                "processing" => "Đang xử lý",
                "unpaid" => "Chưa thanh toán",
                _ => "Không rõ"
            };
        }

        private static string GetPaymentStatusBackground(string status)
        {
            return status switch
            {
                "completed" => "#DCFCE7",
                "processing" => "#FEF3C7",
                "unpaid" => "#F1F5F9",
                _ => "#F1F5F9"
            };
        }

        private static string GetPaymentStatusForeground(string status)
        {
            return status switch
            {
                "completed" => "#15803D",
                "processing" => "#B45309",
                "unpaid" => "#64748B",
                _ => "#64748B"
            };
        }

        private static string ShortId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "N/A";

            return id.Length > 6 ? id.Substring(0, 6).ToUpper() : id.ToUpper();
        }

        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "BN";

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
    }

    public class HealthResultItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Initials { get; set; } = "BN";

        public DateTime ExamDate { get; set; }
        public string ExamDateText { get; set; } = string.Empty;
        public string DoctorNameText { get; set; } = string.Empty;
        public string DiagnosisText { get; set; } = string.Empty;
        public string MedicineText { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = "unpaid";
        public string PaymentStatusText { get; set; } = "Chưa thanh toán";

        public decimal Amount { get; set; }
        public string AmountText { get; set; } = string.Empty;

        public Brush AvatarBackground { get; set; } = new SolidColorBrush(Colors.LightGray);
        public Brush AvatarForeground { get; set; } = new SolidColorBrush(Colors.Black);
        public Brush StatusBackground { get; set; } = new SolidColorBrush(Colors.LightGray);
        public Brush StatusForeground { get; set; } = new SolidColorBrush(Colors.Black);
    }
}