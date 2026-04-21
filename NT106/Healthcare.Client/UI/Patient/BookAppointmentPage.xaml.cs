using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Healthcare.Client.Models.Core;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Helpers;

namespace Healthcare.Client.UI.Patient
{
    public class TimeSlotViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string Id { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string StartTimeStr => DateTime.Today.Add(StartTime).ToString("HH:mm");
        public string DurationStr => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public TimeSlot OriginalModel { get; set; }

        private bool _isSelected;
        public bool IsSelected 
        { 
            get => _isSelected; 
            set 
            { 
                _isSelected = value; 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(TimeColor))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BorderColor))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BackgroundColor))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BorderThickness))); 
            } 
        }
        public string TimeColor => IsSelected ? "#2563EB" : "#334155";
        public string BorderColor => IsSelected ? "#2563EB" : "#E2E8F0";
        public string BackgroundColor => IsSelected ? "#EFF6FF" : "White";
        public string BorderThickness => IsSelected ? "1.5" : "1";

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }

    public class DateViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public DateTime DateValue { get; set; }
        public string DayOfWeekStr { get; set; } // Th 2, Th 3...
        public string DayStr { get; set; }       // 12, 13...

        private bool _isSelected;
        public bool IsSelected 
        { 
            get => _isSelected; 
            set 
            { 
                _isSelected = value; 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(DayOfWeekColor))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(DayColor))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BorderColor))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BackgroundColor))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BorderThickness))); 
            } 
        }
        public string DayOfWeekColor => IsSelected ? "#BFDBFE" : "#64748B";
        public string DayColor => IsSelected ? "White" : "#0F172A";
        public string BorderColor => IsSelected ? "#2563EB" : "#E2E8F0";
        public string BackgroundColor => IsSelected ? "#2563EB" : "White";
        public string BorderThickness => IsSelected ? "2" : "1";

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }

    public class DoctorViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Specialty { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsRandom { get; set; } // Bác sĩ ngẫu nhiên flag
        public decimal ConsultationFee { get; set; } // Phí khám bệnh
        public string ConsultationFeeFormatted => string.Format("{0:N0} VNĐ", ConsultationFee);
        public User OriginalModel { get; set; }

        private bool _isSelected;
        public bool IsSelected 
        { 
            get => _isSelected; 
            set 
            { 
                _isSelected = value; 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BorderColor))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BackgroundColor))); 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BorderThickness))); 
            } 
        }
        public string BorderColor => IsSelected ? "#2563EB" : "#E2E8F0";
        public string BackgroundColor => IsSelected ? "#EFF6FF" : "White";
        public string BorderThickness => IsSelected ? "1.5" : "1";

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }

    public class UserAppointmentViewModel
    {
        public string Id { get; set; }
        public string DoctorName { get; set; }
        public string Specialty { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string TimeStr { get; set; }
        public string DateStr => AppointmentDate.ToString("dd/MM/yyyy");
        public string Status { get; set; }
        public string Type { get; set; } // Online/Offline
        
        public string StatusText => (Status == "Confirmed" || Status == "Paid" || Status == "Success") ? "Đã xác nhận" : 
                                   Status == "Cancelled" ? "Đã hủy" : "Đang chờ";

        public string StatusColorHex => (Status == "Confirmed" || Status == "Paid" || Status == "Success") ? "#166534" :
                                        Status == "Cancelled" ? "#991B1B" : "#854D0E";

        public string StatusBgHex => (Status == "Confirmed" || Status == "Paid" || Status == "Success") ? "#DCFCE7" :
                                     Status == "Cancelled" ? "#FEE2E2" : "#FEF9C3";
    }

    public sealed partial class BookAppointmentPage : Page
    {
        private Supabase.Client _supabase;
        
        public System.Collections.ObjectModel.ObservableCollection<DateViewModel> AvailableDates { get; set; } = new();
        public System.Collections.ObjectModel.ObservableCollection<DoctorViewModel> AvailableDoctors { get; set; } = new();
        public System.Collections.ObjectModel.ObservableCollection<UserAppointmentViewModel> UserAppointments { get; set; } = new();
        private List<DoctorViewModel> _allDoctorsList = new();
        private string _currentTransactionId = null;
        private string _vnpayUrl = null;
        private Microsoft.UI.Xaml.DispatcherTimer _countdownTimer;
        private Microsoft.UI.Xaml.DispatcherTimer _paymentCheckTimer;
        private int _countdownSeconds = 30;
        private bool _isInitializing = true;

        public string FormatCurrency(decimal amount) => string.Format("{0:N0} VNĐ", amount);
        public Visibility ShowIfEmpty(int count) => count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility HideIfEmpty(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public static BookAppointmentPage Current { get; private set; }

        public BookAppointmentPage()
        {
            Current = this;
            this.InitializeComponent();
            _supabase = SupabaseManager.Instance.Client;

            this.Loaded += (s, e) =>
            {
                _isInitializing = false;
                GenerateDates();
                LoadDoctors();
                LoadUserAppointments();
                ExaminationType_Checked(null, null); // Sync initial payment visibility
            };
        }

        private async void LoadUserAppointments()
        {
            try 
            {
                var user = SessionStorage.CurrentUser;
                if (user == null) return;

                var response = await _supabase.From<Appointment>()
                    .Where(x => x.PatientId == user.Id)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                var appts = response.Models;

                DispatcherQueue.TryEnqueue(() => {
                    UserAppointments.Clear();
                    foreach (var appt in appts)
                    {
                        // Note: Lấy thông tin bác sĩ đồng bộ ở đây có thể chậm, 
                        // nhưng để an toàn hãy lấy data trước rồi Add vào list trên UI thread
                    }
                });

                // Tối ưu hơn: Map data trước khi đưa lên UI thread
                var viewModels = new List<UserAppointmentViewModel>();
                foreach (var appt in appts)
                {
                    try {
                        var docData = await _supabase.From<User>().Where(x => x.Id == appt.DoctorId).Single();
                        var profile = await _supabase.From<DoctorProfile>().Where(x => x.DoctorId == appt.DoctorId).Single();

                        viewModels.Add(new UserAppointmentViewModel
                        {
                            Id = appt.Id,
                            DoctorName = docData?.FullName ?? "Bác sĩ",
                            Specialty = profile?.Specialty ?? "Chuyên khoa",
                            AvatarUrl = docData?.AvatarUrl,
                            AppointmentDate = appt.AppointmentDate,
                            TimeStr = $"{appt.StartTime:hh\\:mm} - {appt.EndTime:hh\\:mm}",
                            Status = appt.Status ?? "Confirmed",
                            Type = appt.ExaminationType
                        });
                    } catch { /* Skip and continue if one fails */ }
                }

                DispatcherQueue.TryEnqueue(() => {
                    UserAppointments.Clear();
                    foreach (var vm in viewModels)
                    {
                        UserAppointments.Add(vm);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading appointments: " + ex.Message);
            }
        }

        private void BtnNewAppointment_Click(object sender, RoutedEventArgs e)
        {
            ToggleBookingView(true);
        }

        private void BtnBackToList_Click(object sender, RoutedEventArgs e)
        {
            ToggleBookingView(false);
            LoadUserAppointments(); // Refresh list on back
        }

        private void ToggleBookingView(bool isBooking)
        {
            DashboardContainer.Visibility = isBooking ? Visibility.Collapsed : Visibility.Visible;
            BookingFlowContainer.Visibility = isBooking ? Visibility.Visible : Visibility.Collapsed;
            
            if (isBooking)
            {
                SetStep(1); // Reset to step 1
            }
        }

        private void GenerateDates()
        {
            var today = DateTime.Today;
            string[] vnDays = { "CN", "Th 2", "Th 3", "Th 4", "Th 5", "Th 6", "Th 7" };
            
            for (int i = 0; i < 14; i++)
            {
                var d = today.AddDays(i);
                AvailableDates.Add(new DateViewModel
                {
                    DateValue = d,
                    DayOfWeekStr = vnDays[(int)d.DayOfWeek],
                    DayStr = d.Day.ToString("00")
                });
            }
            DatesGridView.ItemsSource = AvailableDates;
            // Do NOT set SelectedIndex here to avoid triggering SelectionChanged during init
        }

        private async void DatesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (e.RemovedItems != null)
                foreach (var item in e.RemovedItems)
                    if (item is DateViewModel v) v.IsSelected = false;

            if (e.AddedItems != null)
                foreach (var item in e.AddedItems)
                    if (item is DateViewModel v) v.IsSelected = true;

            await LoadSlotsAsync();
            UpdateSummary();
        }

        private async Task LoadSlotsAsync()
        {
            var selectedDoctorVM = DoctorsGridView.SelectedItem as DoctorViewModel;
            var selectedDateVM = DatesGridView.SelectedItem as DateViewModel;

            if (selectedDoctorVM == null || selectedDateVM == null || selectedDoctorVM.IsRandom) 
            {
                TimeSlotsGridView.ItemsSource = null;
                // TimeSlotsGridView.ItemsSource = null; // Removed check for NoSlotsText
                return;
            }

            try
            {
                var response = await _supabase.From<TimeSlot>()
                    .Where(x => x.DoctorId == selectedDoctorVM.Id)
                    .Where(x => x.SlotDate == selectedDateVM.DateValue.Date)
                    .Where(x => x.Status == "Available")
                    .Get();

                var slots = response.Models.OrderBy(s => s.StartTime).Select(s => new TimeSlotViewModel
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    OriginalModel = s
                }).ToList();

                TimeSlotsGridView.ItemsSource = slots;
            }
            catch (Exception ex)
            {
                await ShowDialog("Lỗi", "Không thể tải khung giờ: " + ex.Message);
            }
        }

        private void UpdateSummary()
        {
            var doc = DoctorsGridView?.SelectedItem as DoctorViewModel;
            var date = DatesGridView?.SelectedItem as DateViewModel;
            var slot = TimeSlotsGridView?.SelectedItem as TimeSlotViewModel;

            if (SumDoctorName != null) SumDoctorName.Text = doc?.FullName ?? "--";
            if (SumSpecialty != null) SumSpecialty.Text = doc?.Specialty ?? "--";
            
            // Cập nhật phí khám (lấy từ ConsultationFee của bác sĩ, hiển thị trên TextBlock SumFee)
            if (SumFee != null)
            {
                if (doc != null)
                {
                    SumFee.Text = string.Format("{0:N0} VNĐ", doc.ConsultationFee);
                }
                else
                {
                    SumFee.Text = "--";
                }
            }

            if (date != null && slot != null && SumTime != null)
            {
                SumTime.Text = $"{slot.StartTimeStr}\n{date.DayOfWeekStr}, {date.DateValue:dd/MM/yyyy}";
            }
            else if (SumTime != null)
            {
                SumTime.Text = "--";
            }
        }

        private void TimeSlotsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (e.RemovedItems != null)
                foreach (var item in e.RemovedItems)
                    if (item is TimeSlotViewModel v) v.IsSelected = false;

            if (e.AddedItems != null)
                foreach (var item in e.AddedItems)
                    if (item is TimeSlotViewModel v) v.IsSelected = true;

            UpdateSummary();
        }

        private async void DoctorsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (e.RemovedItems != null)
                foreach (var item in e.RemovedItems)
                    if (item is DoctorViewModel v) v.IsSelected = false;

            if (e.AddedItems != null)
                foreach (var item in e.AddedItems)
                    if (item is DoctorViewModel v) v.IsSelected = true;

            await LoadSlotsAsync();
            UpdateSummary();
        }

        private async void LoadDoctors()
        {
            try
            {
                var response = await _supabase.From<User>()
                    .Where(x => x.Role == "Doctor")
                    .Get();

                var profilesResponse = await _supabase.From<DoctorProfile>().Get();
                var profiles = profilesResponse.Models;

                AvailableDoctors.Clear();
                _allDoctorsList.Clear();

                foreach (var md in response.Models)
                {
                    var profile = profiles.FirstOrDefault(p => p.DoctorId == md.Id);

                    var vm = new DoctorViewModel
                    {
                        Id = md.Id,
                        FullName = "BS. " + md.FullName,
                        Specialty = profile?.Specialty ?? "Chưa cập nhật",
                        ConsultationFee = profile?.ConsultationFee ?? 500000,
                        AvatarUrl = md.AvatarUrl, // Để null để PersonPicture tự động hiện Initials thay vì dùng ảnh placeholder
                        IsRandom = false,
                        OriginalModel = md
                    };
                    AvailableDoctors.Add(vm);
                    _allDoctorsList.Add(vm);
                }

                DoctorsGridView.ItemsSource = AvailableDoctors;
                DoctorsGridView.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                await ShowDialog("Lỗi", "Không thể tải danh sách bác sĩ: " + ex.Message);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
                this.Frame.GoBack();
        }

        private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            var selectedDoctorVM = DoctorsGridView.SelectedItem as DoctorViewModel;
            var selectedSlotVM = TimeSlotsGridView.SelectedItem as TimeSlotViewModel;
            var selectedDateVM = DatesGridView.SelectedItem as DateViewModel;

            if (selectedDoctorVM == null || selectedSlotVM == null || selectedDateVM == null)
            {
                await ShowDialog("Thiếu thông tin", "Vui lòng chọn đầy đủ bác sĩ, ngày và khung giờ khám.");
                return;
            }

            if (TypeOnline.IsChecked != true && TypeOffline.IsChecked != true)
            {
                await ShowDialog("Thiếu thông tin", "Vui lòng chọn Hình thức khám.");
                return;
            }

            if (SessionStorage.CurrentUser == null)
            {
                await ShowDialog("Lỗi", "Vui lòng đăng nhập lại.");
                return;
            }

            try
            {
                // 1. Cập nhật trạng thái slot thành 'Booked' với điều kiện trạng thái hiện tại là 'Available'
                // Điều này giúp tránh Race Condition (double booking)
                var slotResponse = await _supabase.From<TimeSlot>()
                    .Where(x => x.Id == selectedSlotVM.Id)
                    .Where(x => x.Status == "Available")
                    .Set(x => x.Status, "Booked")
                    .Update();

                if (slotResponse.Models.Count == 0)
                {
                    await ShowDialog("Lỗi", "Khung giờ này vừa có người khác đặt. Vui lòng chọn khung giờ khác.");
                    await LoadSlotsAsync(); // Refresh lại danh sách
                    return;
                }

                string paymentMethod = PaymentOnline.IsChecked == true ? "Online" : "Manual";
                
                string finalDoctorId = selectedDoctorVM.Id;
                if (selectedDoctorVM.IsRandom)
                {
                    var actualDoctors = AvailableDoctors.Where(d => !d.IsRandom).ToList();
                    if (actualDoctors.Any())
                    {
                        var rand = new Random();
                        finalDoctorId = actualDoctors[rand.Next(actualDoctors.Count)].Id;
                    }
                }

                var appt = new Appointment
                {
                    PatientId = SessionStorage.CurrentUser.Id,
                    DoctorId = finalDoctorId,
                    AppointmentDate = selectedDateVM.DateValue.Date,
                    StartTime = selectedSlotVM.StartTime,
                    EndTime = selectedSlotVM.EndTime,
                    SlotId = selectedSlotVM.Id,
                    Status = "Confirmed", // Lịch được xác nhận ngay lập tức
                    ExaminationType = TypeOnline.IsChecked == true ? "Online" : "Offline",
                    CreatedAt = DateTime.UtcNow,
                    RoomCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                };

                // Nhận lại Object đã insert kèm ID mới từ Supabase
                var apptResponse = await _supabase.From<Appointment>().Insert(appt);
                var insertedAppt = apptResponse.Models.FirstOrDefault();

                if (insertedAppt == null)
                {
                    await ShowDialog("Lỗi", "Lỗi rớt mạng hoặc CSDL từ chối tạo lịch hẹn. Vui lòng thử lại!");
                    return;
                }

                // Cập nhật trạng thái Slot thành Booked
                await _supabase.From<TimeSlot>()
                    .Where(x => x.Id == selectedSlotVM.Id)
                    .Set(x => x.Status, "Booked")
                    .Update();

                // Tính phí (lấy từ Appointment Summary đã nối vào DoctorViewModel)
                decimal feeToPay = selectedDoctorVM.ConsultationFee;

                var transaction = new Transaction
                {
                    AppointmentId = insertedAppt.Id,
                    PatientId = SessionStorage.CurrentUser.Id,
                    Amount = feeToPay, 
                    PaymentMethod = paymentMethod,
                    Status = "Pending",
                    TransactionRef = "TXA" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                    PaidAt = null
                };
                var transResponse = await _supabase.From<Transaction>().Insert(transaction);
                var insertedTrans = transResponse.Models.FirstOrDefault();

                string transId = insertedTrans?.Id ?? transaction.Id;
                _currentTransactionId = transId;

                if (paymentMethod == "Online")
                {
                    // Chuyển sang step 2 với thông tin thanh toán
                    DlgAmount.Text = string.Format("{0:N0} VNĐ", transaction.Amount);
                    DlgRef.Text = transaction.TransactionRef;

                    // Lấy VNPay URL từ server
                    var paymentClient = new Healthcare.Client.APIClient.PaymentApiClient();
                    _vnpayUrl = await paymentClient.GetVNPayUrlAsync(insertedAppt.Id, (double)transaction.Amount);
                    if (string.IsNullOrEmpty(_vnpayUrl))
                        _vnpayUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_TxnRef={transaction.TransactionRef}";

                    // Điền bảng tóm tắt bước 3
                    SuccDoctorName.Text = SumDoctorName.Text;
                    SuccTime.Text = SumTime.Text;
                    SuccRef.Text = transaction.TransactionRef;

                    SetStep(2);
                }
                else
                {
                    // Thanh toán tại quầy: hiển bước 2 dạng Offline
                    SuccessMessage.Text = "Lịch hẹn của bạn đã được ghi nhận. Vui lòng thanh toán tại quầy khi đến khám!";
                    SuccDoctorName.Text = SumDoctorName.Text;
                    SuccTime.Text = SumTime.Text;
                    SuccRef.Text = transaction.TransactionRef;
                    SetStep(2, isOffline: true);
                    StartOfflineCountdown();
                }
            }
            catch (Exception ex)
            {
                await ShowDialog("Lỗi", "Không thể đăng ký lịch hẹn: " + ex.Message);
            }
        }

        private void SetStep(int step, bool isOffline = false)
        {
            StepFormPanel.Visibility    = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            StepPaymentPanel.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            StepSuccessPanel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
            
            // Chỉ hiện cột tóm tắt ở bước 1
            if (RightSummaryColumn != null)
                RightSummaryColumn.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;

            if (step == 2)
            {
                StepPaymentOnlinePanel.Visibility  = isOffline ? Visibility.Collapsed : Visibility.Visible;
                StepPaymentOfflinePanel.Visibility = isOffline ? Visibility.Visible   : Visibility.Collapsed;
            }

            // Cập nhật step indicator
            Step1Circle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step >= 1 ? (byte)37  : (byte)226, step >= 1 ? (byte)99  : (byte)232, step >= 1 ? (byte)235 : (byte)240));
            Step2Circle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step >= 2 ? (byte)37  : (byte)226, step >= 2 ? (byte)99  : (byte)232, step >= 2 ? (byte)235 : (byte)240));
            Step3Circle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step >= 3 ? (byte)37  : (byte)226, step >= 3 ? (byte)99  : (byte)232, step >= 3 ? (byte)235 : (byte)240));

            // Cập nhật đường nối (Progress Lines)
            if (StepLine1 != null)
                StepLine1.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step >= 2 ? (byte)37 : (byte)226, step >= 2 ? (byte)99 : (byte)232, step >= 2 ? (byte)235 : (byte)240));
            
            if (StepLine2 != null)
                StepLine2.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step >= 3 ? (byte)37 : (byte)226, step >= 3 ? (byte)99 : (byte)232, step >= 3 ? (byte)235 : (byte)240));

            Step2Num.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step >= 2 ? (byte)255 : (byte)100, step >= 2 ? (byte)255 : (byte)116, step >= 2 ? (byte)255 : (byte)139));
            Step3Num.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step >= 3 ? (byte)255 : (byte)100, step >= 3 ? (byte)255 : (byte)116, step >= 3 ? (byte)255 : (byte)139));

            Step1Label.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step == 1 ? (byte)37  : (byte)100, step == 1 ? (byte)99  : (byte)116, step == 1 ? (byte)235 : (byte)139));
            Step2Label.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step == 2 ? (byte)37  : (byte)100, step == 2 ? (byte)99  : (byte)116, step == 2 ? (byte)235 : (byte)139));
            Step3Label.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, step == 3 ? (byte)37  : (byte)100, step == 3 ? (byte)99  : (byte)116, step == 3 ? (byte)235 : (byte)139));
        }

        private async void BtnOpenVNPay_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_vnpayUrl))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(_vnpayUrl));
                StartPaymentCheckTimer();
            }
        }

        private void StartPaymentCheckTimer()
        {
            if (_paymentCheckTimer == null)
            {
                _paymentCheckTimer = new Microsoft.UI.Xaml.DispatcherTimer();
                _paymentCheckTimer.Interval = TimeSpan.FromSeconds(3); // poll every 3 seconds
                _paymentCheckTimer.Tick += async (s, e) =>
                {
                    try
                    {
                        var response = await _supabase.From<Transaction>()
                            .Where(t => t.Id == _currentTransactionId)
                            .Single();

                        if (response != null && response.Status == "Success")
                        {
                            _paymentCheckTimer.Stop();
                            BtnPaymentDone.IsEnabled = true;
                            if (BtnPaymentDoneText != null)
                            {
                                BtnPaymentDoneText.Text = "✓ Đã nhận được thanh toán, tiếp tục";
                            }
                        }
                    }
                    catch { /* ignore polling errors */ }
                };
            }
            
            if (!_paymentCheckTimer.IsEnabled)
            {
                _paymentCheckTimer.Start();
                if (BtnPaymentDoneText != null)
                {
                    BtnPaymentDoneText.Text = "⏳ Đang chờ xác nhận thanh toán...";
                }
            }
        }

        private void BtnPaymentDone_Click(object sender, RoutedEventArgs e)
        {
            SuccessMessage.Text = "Lịch hẹn của bạn đã được ghi nhận. Cảm ơn bạn đã hoàn tất thanh toán!";
            SetStep(3);
        }

        private void StartOfflineCountdown()
        {
            _countdownSeconds = 30;
            CountdownText.Text = $"Tự động chuyển sang bước tiếp theo sau {_countdownSeconds} giây...";

            _countdownTimer = new Microsoft.UI.Xaml.DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += (s, e) =>
            {
                _countdownSeconds--;
                if (_countdownSeconds > 0)
                {
                    CountdownText.Text = $"Tự động chuyển sang bước tiếp theo sau {_countdownSeconds} giây...";
                }
                else
                {
                    _countdownTimer.Stop();
                    SetStep(3);
                }
            };
            _countdownTimer.Start();
        }

        private void BtnOfflineProceed_Click(object sender, RoutedEventArgs e)
        {
            _countdownTimer?.Stop();
            SetStep(3);
        }

        private void BtnGoHome_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack) this.Frame.GoBack();
            else this.Frame.Navigate(typeof(PatientHomePage));
        }

        private async Task ShowDialog(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void ExaminationType_Checked(object sender, RoutedEventArgs e)
        {
            if (PaymentMethodSection != null && TypeOnline != null && TypeOffline != null)
            {
                PaymentMethodSection.Visibility = Visibility.Visible;

                if (TypeOnline.IsChecked == true)
                {
                    PaymentOnline.IsChecked = true;
                    PaymentManual.Visibility = Visibility.Collapsed;
                }
                else if (TypeOffline.IsChecked == true)
                {
                    PaymentManual.Visibility = Visibility.Visible;
                }
            }
        }

        private void SearchDoctorBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string query = sender.Text.ToLowerInvariant();
                AvailableDoctors.Clear();

                foreach (var doc in _allDoctorsList)
                {
                    if (doc.FullName.ToLowerInvariant().Contains(query) || 
                        doc.Specialty.ToLowerInvariant().Contains(query))
                    {
                        AvailableDoctors.Add(doc);
                    }
                }
            }
        }
    }
}
