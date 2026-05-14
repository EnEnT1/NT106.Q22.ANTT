using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Healthcare.Client.Models.Communication;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Helpers;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

namespace Healthcare.Client.UI.Components
{
    // ══════════════════════════════════════════════════════════════════════
    // Notification Category Enum
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Loại thông báo — quyết định icon và màu badge, cũng như trang điều hướng.</summary>
    public enum NotificationCategory
    {
        Appointment,    // Lịch hẹn   → BookAppointmentPage / ExaminationPage
        LabResult,      // Xét nghiệm → LabResultsPage
        Payment,        // Thanh toán  → PaymentCheckoutPage / RevenuePage
        Medication,     // Nhắc thuốc → (no navigation, informational)
        Message,        // Tin nhắn   → ChatControl
        System          // Hệ thống   → (no navigation)
    }

    // ══════════════════════════════════════════════════════════════════════
    // ViewModel
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel đại diện cho một thông báo trong danh sách.
    /// Tất cả computed properties phục vụ x:Bind trực tiếp.
    /// </summary>
    public class NotificationViewModel
    {
        // ── Data ──────────────────────────────────────────────────────────
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public NotificationCategory Category { get; set; } = NotificationCategory.System;

        /// <summary>Payload tuỳ chọn — chứa ID trang cần điều hướng (vd: labResultId, appointmentId).</summary>
        public string? NavigationPayload { get; set; }

        /// <summary>Tham chiếu về model gốc từ Supabase (nếu cần dùng thêm).</summary>
        public Notification? SourceRecord { get; set; }

        // ── Computed: Typography ──────────────────────────────────────────
        public FontWeight TitleWeight => IsRead ? FontWeights.Normal : FontWeights.SemiBold;

        // ── Computed: Row background ──────────────────────────────────────
        public SolidColorBrush RowBackground => IsRead
            ? new SolidColorBrush(Colors.Transparent)
            : new SolidColorBrush(Color.FromArgb(10, 0, 89, 187));  // Hơi xanh nhạt cho chưa đọc

        // ── Computed: Unread dot ──────────────────────────────────────────
        public Visibility UnreadDotVisibility => IsRead ? Visibility.Collapsed : Visibility.Visible;

        // ── Computed: Icon ────────────────────────────────────────────────
        public string IconGlyph => Category switch
        {
            NotificationCategory.Appointment => "\uE787",   // Calendar
            NotificationCategory.LabResult => "\uE9F9",   // Microscope / lab
            NotificationCategory.Payment => "\uE8A0",   // Payment card
            NotificationCategory.Medication => "\uECAA",   // Pill
            NotificationCategory.Message => "\uE8BD",   // Chat bubble
            NotificationCategory.System => "\uE7BA",   // Info
            _ => "\uEA8F"    // Bell
        };

        public SolidColorBrush IconBackground => Category switch
        {
            NotificationCategory.Appointment => new SolidColorBrush(Color.FromArgb(255, 191, 217, 253)), // blue
            NotificationCategory.LabResult => new SolidColorBrush(Color.FromArgb(255, 209, 228, 255)), // light blue
            NotificationCategory.Payment => new SolidColorBrush(Color.FromArgb(255, 255, 243, 205)), // yellow
            NotificationCategory.Medication => new SolidColorBrush(Color.FromArgb(255, 218, 232, 255)), // soft blue
            NotificationCategory.Message => new SolidColorBrush(Color.FromArgb(255, 225, 227, 228)), // grey
            NotificationCategory.System => new SolidColorBrush(Color.FromArgb(255, 216, 226, 255)), // primary-fixed
            _ => new SolidColorBrush(Color.FromArgb(255, 225, 227, 228))
        };

        public SolidColorBrush IconForeground => Category switch
        {
            NotificationCategory.Appointment => new SolidColorBrush(Color.FromArgb(255, 0, 89, 187)),
            NotificationCategory.LabResult => new SolidColorBrush(Color.FromArgb(255, 0, 68, 147)),
            NotificationCategory.Payment => new SolidColorBrush(Color.FromArgb(255, 133, 100, 4)),
            NotificationCategory.Medication => new SolidColorBrush(Color.FromArgb(255, 0, 89, 187)),
            NotificationCategory.Message => new SolidColorBrush(Color.FromArgb(255, 65, 71, 84)),
            NotificationCategory.System => new SolidColorBrush(Color.FromArgb(255, 0, 89, 187)),
            _ => new SolidColorBrush(Color.FromArgb(255, 65, 71, 84))
        };

        // ── Computed: Time ────────────────────────────────────────────────
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - CreatedAt;
                if (diff.TotalMinutes < 1) return "Vừa xong";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
                return CreatedAt.ToString("dd/MM/yyyy");
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Navigation Request EventArgs
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Được raise khi người dùng click một thông báo cần điều hướng sang trang khác.
    /// Shell (DoctorShell / PatientShell) subscribe sự kiện này để gọi Frame.Navigate().
    /// </summary>
    public class NotificationNavigationRequestedEventArgs : EventArgs
    {
        /// <summary>Kiểu trang đích để Shell gọi Frame.Navigate(typeof(...)).</summary>
        public Type TargetPageType { get; init; } = typeof(Page);

        /// <summary>Tham số truyền vào trang đích (vd: appointmentId, labResultId).</summary>
        public object? Parameter { get; init; }
    }

    // ══════════════════════════════════════════════════════════════════════
    // UserControl Code-Behind
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Panel thông báo dạng Flyout — gắn vào nút chuông trên TopBar của
    /// <see cref="Healthcare.Client.UI.Shell.DoctorShell"/> và
    /// <see cref="Healthcare.Client.UI.Shell.PatientShell"/>.
    ///
    /// Luồng dữ liệu:
    ///   1. Supabase Realtime (SupabaseRealtimeService) push INSERT vào bảng notifications
    ///   2. LoadAsync() hoặc handler realtime cập nhật _allItems
    ///   3. ApplyFilter() lọc theo tab đang chọn → đổ vào _displayed
    ///   4. Người dùng click → raise NavigationRequested → Shell navigate sang Page tương ứng
    /// </summary>
    public sealed partial class NotificationPanel : UserControl
    {
        // ── Events ────────────────────────────────────────────────────────

        /// <summary>
        /// Shell subscribe sự kiện này để thực hiện điều hướng Frame.
        /// </summary>
        public event EventHandler<NotificationNavigationRequestedEventArgs>? NavigationRequested;

        /// <summary>
        /// Raised khi số thông báo chưa đọc thay đổi — TopBar dùng để cập nhật badge.
        /// </summary>
        public event EventHandler<int>? UnreadCountChanged;

        // ── Fields ────────────────────────────────────────────────────────

        private readonly ObservableCollection<NotificationViewModel> _displayed = new();
        private List<NotificationViewModel> _allItems = new();
        private string _activeTab = "All"; // "All" | "Unread"

        // ── Constructor ───────────────────────────────────────────────────

        public NotificationPanel()
        {
            InitializeComponent();
            NotificationsListView.ItemsSource = _displayed;
            Loaded += NotificationPanel_Loaded;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────

        private async void NotificationPanel_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAsync();
            SubscribeRealtime();
        }

        // ── Data Loading ──────────────────────────────────────────────────

        /// <summary>
        /// Tải thông báo từ Supabase cho user hiện tại.
        /// </summary>
        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                // ── Kết nối Supabase thật ─────────────────────────────────
                // var userId = SessionStorage.CurrentUser?.Id ?? string.Empty;
                // var records = await SupabaseDbService.Instance
                //     .GetAsync<Notification>(n => n.UserId == userId);
                //
                // _allItems = records
                //     .OrderByDescending(n => n.CreatedAt)
                //     .Select(MapToViewModel)
                //     .ToList();
                // ─────────────────────────────────────────────────────────

                // ── Dữ liệu mẫu (xóa khi kết nối Supabase thật) ──────────
                _allItems = GetSampleData();
                // ─────────────────────────────────────────────────────────

                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationPanel] LoadAsync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribe Supabase Realtime — nhận thông báo mới ngay lập tức mà không cần refresh.
        /// </summary>
        private void SubscribeRealtime()
        {
            // TODO: Kết nối SupabaseRealtimeService để lắng nghe INSERT trên bảng notifications
            //
            // SupabaseRealtimeService.Instance.OnNotificationReceived += (sender, notification) =>
            // {
            //     DispatcherQueue.TryEnqueue(() =>
            //     {
            //         var vm = MapToViewModel(notification);
            //         _allItems.Insert(0, vm);
            //         ApplyFilter();
            //     });
            // };
        }

        // ── Mapping ───────────────────────────────────────────────────────

        /// <summary>
        /// Chuyển đổi model Supabase <see cref="Notification"/> → <see cref="NotificationViewModel"/>.
        /// </summary>
        private static NotificationViewModel MapToViewModel(Notification n) => new()
        {
            Id = n.Id?.ToString() ?? string.Empty,
            Title = n.Title ?? string.Empty,
            Body = n.Body ?? string.Empty,
            IsRead = n.IsRead ?? false,
            CreatedAt = n.CreatedAt,
            Category = ParseCategory(n.Type),
            NavigationPayload = n.RelatedEntityId?.ToString(),
            SourceRecord = n
        };

        private static NotificationCategory ParseCategory(string? type) => type?.ToLower() switch
        {
            "appointment" => NotificationCategory.Appointment,
            "lab_result" => NotificationCategory.LabResult,
            "payment" => NotificationCategory.Payment,
            "medication" => NotificationCategory.Medication,
            "message" => NotificationCategory.Message,
            _ => NotificationCategory.System
        };

        // ── Filter ────────────────────────────────────────────────────────

        /// <summary>Lọc danh sách theo tab đang chọn và làm mới UI.</summary>
        private void ApplyFilter()
        {
            _displayed.Clear();

            var source = _activeTab == "Unread"
                ? _allItems.Where(n => !n.IsRead)
                : _allItems;

            foreach (var item in source)
                _displayed.Add(item);

            // Empty state
            EmptyStatePanel.Visibility = _displayed.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            RefreshUnreadBadge();
        }

        private void RefreshUnreadBadge()
        {
            int count = _allItems.Count(n => !n.IsRead);
            UnreadCountText.Text = count.ToString();
            UnreadBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            UnreadCountChanged?.Invoke(this, count);
        }

        // ── Event Handlers ────────────────────────────────────────────────

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                _activeTab = tag;
                ApplyFilter();
            }
        }

        private void MarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _allItems)
                item.IsRead = true;

            // TODO: Gọi Supabase batch update
            // await SupabaseDbService.Instance.MarkAllNotificationsReadAsync(SessionStorage.CurrentUser.Id);

            ApplyFilter();
        }

        private void NotificationSettings_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Mở trang cài đặt thông báo / dialog
        }

        /// <summary>
        /// Xử lý click vào một thông báo:
        /// 1. Đánh dấu đã đọc
        /// 2. Raise <see cref="NavigationRequested"/> để Shell điều hướng sang trang tương ứng
        /// </summary>
        private async void NotificationItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not NotificationViewModel vm) return;

            // Đánh dấu đã đọc
            if (!vm.IsRead)
            {
                vm.IsRead = true;

                // TODO: Cập nhật Supabase
                // await SupabaseDbService.Instance.UpdateAsync<Notification>(
                //     vm.Id, n => n.IsRead = true);

                ApplyFilter();
            }

            // Xác định trang đích theo Category
            var args = ResolveNavigation(vm);
            if (args is not null)
                NavigationRequested?.Invoke(this, args);
        }

        private void ViewAll_Click(object sender, RoutedEventArgs e)
        {
            // "Xem tất cả" không điều hướng sang trang riêng trong thiết kế này;
            // có thể mở rộng sau nếu cần một NotificationsPage toàn màn hình.
            // Ví dụ: NavigationRequested?.Invoke(this, new(...) { TargetPageType = typeof(NotificationsPage) });
        }

        // ── Navigation Resolution ─────────────────────────────────────────

        /// <summary>
        /// Ánh xạ Category + Payload → trang đích và tham số.
        ///
        /// Shell đã biết cách navigate:
        ///   • PatientShell: BookAppointmentPage, LabResultsPage, PaymentCheckoutPage, MyRecordsPage
        ///   • DoctorShell : ExaminationPage, PatientHistoryPage, RevenuePage
        ///
        /// UserControl này import namespace cần thiết bằng cách dùng Type trực tiếp —
        /// không cần biết Shell đang là loại nào.
        /// </summary>
        private static NotificationNavigationRequestedEventArgs? ResolveNavigation(NotificationViewModel vm)
        {
            return vm.Category switch
            {
                NotificationCategory.Appointment =>
                    new()
                    {
                        TargetPageType = typeof(Healthcare.Client.UI.Patient.BookAppointmentPage),
                        Parameter = vm.NavigationPayload
                    },

                NotificationCategory.LabResult =>
                    new()
                    {
                        TargetPageType = typeof(Healthcare.Client.UI.Patient.LabResultsPage),
                        Parameter = vm.NavigationPayload
                    },

                NotificationCategory.Payment =>
                    new()
                    {
                        TargetPageType = typeof(Healthcare.Client.UI.Patient.PaymentCheckoutPage),
                        Parameter = vm.NavigationPayload
                    },

                NotificationCategory.Message =>
                    // Chat không phải Page mà là Component — Shell sẽ mở ChatControl
                    new()
                    {
                        TargetPageType = typeof(Healthcare.Client.UI.Patient.PatientHomePage),
                        Parameter = "open_chat"
                    },

                // Medication & System: chỉ đánh dấu đọc, không navigate
                _ => null
            };
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Shell gọi hàm này sau khi nhận được push notification từ hệ điều hành
        /// hoặc từ SignalR / Realtime để thêm thông báo mới vào đầu danh sách.
        /// </summary>
        public void PushNotification(NotificationViewModel vm)
        {
            _allItems.Insert(0, vm);
            ApplyFilter();
        }

        /// <summary>Số thông báo chưa đọc hiện tại — TopBar dùng để set badge number.</summary>
        public int UnreadCount => _allItems.Count(n => !n.IsRead);

        // ── Sample Data ───────────────────────────────────────────────────

        private static List<NotificationViewModel> GetSampleData() =>
        [
            new()
            {
                Id        = "1",
                Title     = "Lịch hẹn được xác nhận",
                Body      = "Lịch khám với BS. Nguyễn Văn A vào 10:00 AM ngày 20/05/2026 đã được xác nhận.",
                IsRead    = false,
                CreatedAt = DateTime.Now.AddMinutes(-5),
                Category  = NotificationCategory.Appointment,
                NavigationPayload = "APT-00123"
            },
            new()
            {
                Id        = "2",
                Title     = "Kết quả xét nghiệm đã có",
                Body      = "Kết quả xét nghiệm Sinh hóa máu toàn phần (LAB-10293) đã sẵn sàng để xem.",
                IsRead    = false,
                CreatedAt = DateTime.Now.AddHours(-1),
                Category  = NotificationCategory.LabResult,
                NavigationPayload = "LAB-10293"
            },
            new()
            {
                Id        = "3",
                Title     = "Nhắc uống thuốc",
                Body      = "Đã đến giờ uống thuốc Amoxicillin 500mg — 1 viên sau bữa sáng.",
                IsRead    = false,
                CreatedAt = DateTime.Now.AddHours(-2),
                Category  = NotificationCategory.Medication
            },
            new()
            {
                Id        = "4",
                Title     = "Thanh toán thành công",
                Body      = "Giao dịch viện phí #TXN-8821 trị giá 450,000₫ đã được ghi nhận.",
                IsRead    = true,
                CreatedAt = DateTime.Now.AddDays(-1),
                Category  = NotificationCategory.Payment,
                NavigationPayload = "TXN-8821"
            },
            new()
            {
                Id        = "5",
                Title     = "Tin nhắn mới từ BS. Trần Thị B",
                Body      = "Bác sĩ đã gửi cho bạn một tin nhắn. Nhấn để xem.",
                IsRead    = true,
                CreatedAt = DateTime.Now.AddDays(-2),
                Category  = NotificationCategory.Message
            }
        ];
    }
}
