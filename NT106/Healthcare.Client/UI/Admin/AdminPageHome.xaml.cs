using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.Models.Core;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.APIClient;
using Healthcare.Client.UI.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Healthcare.Client.UI.Admin
{
    public sealed partial class AdminHomePage : Page
    {
        private string _currentTab = "Dashboard";
        private readonly AdminApiClient _adminApiClient = new AdminApiClient();

        // Cấu trúc dữ liệu hiển thị (Display Models) để map tên từ ID
        public class UserDisplayModel : User 
        { 
            public string Specialty { get; set; } 
            public string DateOfBirth { get; set; }
            public string ExtraInfo => !string.IsNullOrEmpty(Specialty) ? Specialty : DateOfBirth;
        }
        public class AppointmentDisplay { public string Id { get; set; } public string PatientName { get; set; } public string DoctorName { get; set; } public DateTime AppointmentDate { get; set; } public string Status { get; set; } public string RoomCode { get; set; } }
        public class TransactionDisplay { public string Id { get; set; } public string PatientName { get; set; } public decimal Amount { get; set; } public string PaymentMethod { get; set; } public string Status { get; set; } public DateTime? PaidAt { get; set; } }

        public AdminHomePage()
        {
            this.InitializeComponent();
            LoadDataAsync();
        }

        private void UpdateTableHeaders()
        {
            // Thiết lập template và tiêu đề cột dựa trên tab hiện tại
            switch (_currentTab)
            {
                case "Patient":
                case "Doctor":
                case "Staff":
                    AdminDataGrid.ItemTemplate = (DataTemplate)Resources["UserTemplate"];
                    ColHeader0.Text = "MÃ QUẢN LÝ";
                    ColHeader1.Text = "HỌ TÊN / EMAIL";
                    ColHeader2.Text = (_currentTab == "Doctor" ? "CHUYÊN KHOA" : (_currentTab == "Staff" ? "SĐT" : "SĐT / NĂM SINH"));
                    ColHeader3.Text = "NGÀY TẠO";
                    ActionButtonsPanel.Visibility = Visibility.Visible;
                    break;

                case "Appointment":
                    AdminDataGrid.ItemTemplate = (DataTemplate)Resources["AppointmentTemplate"];
                    ColHeader0.Text = "MÃ LỊCH HẸN";
                    ColHeader1.Text = "BỆNH NHÂN / BÁC SĨ";
                    ColHeader2.Text = "NGÀY HẸN / TRẠNG THÁI";
                    ColHeader3.Text = "MÃ PHÒNG";
                    ActionButtonsPanel.Visibility = Visibility.Collapsed;
                    break;
                case "Transaction":
                    AdminDataGrid.ItemTemplate = (DataTemplate)Resources["TransactionTemplate"];
                    ColHeader0.Text = "MÃ GIAO DỊCH";
                    ColHeader1.Text = "NGƯỜI THANH TOÁN / PT";
                    ColHeader2.Text = "SỐ TIỀN / TRẠNG THÁI";
                    ColHeader3.Text = "THỜI GIAN";
                    ActionButtonsPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private async void LoadDataAsync()
        {
            try
            {
                AdminDataGrid.ItemsSource = null;
                UpdateTableHeaders();

                switch (_currentTab)
                {
                    case "Dashboard":
                        await LoadDashboardDataAsync();
                        break;
                    case "Patient":
                        await LoadUsersByRoleAsync("Patient");
                        break;
                    case "Doctor":
                        await LoadUsersByRoleAsync("Doctor");
                        break;
                    case "Staff":
                        await LoadUsersByRoleAsync("Staff");
                        break;
                    case "Appointment":
                        await LoadAppointmentsAsync();
                        break;
                    case "Transaction":
                        await LoadTransactionsAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Lỗi hệ thống", $"Không thể tải dữ liệu: {ex.Message}");
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            var client = SupabaseManager.Instance.Client;
            
            // 1. Doanh thu tháng này (Tổng amount của các Transaction có PaidAt trong tháng hiện tại)
            var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var transactionsResponse = await client.From<Transaction>()
                .Where(t => t.Status == "Completed" || t.Status == "Paid")
                .Get();
            
            decimal totalRevenue = 0;
            if (transactionsResponse.Models != null)
            {
                totalRevenue = transactionsResponse.Models
                    .Where(t => t.PaidAt.HasValue && t.PaidAt.Value >= currentMonthStart)
                    .Sum(t => t.Amount);
            }
            TxtMonthlyRevenue.Text = totalRevenue.ToString("N0") + " đ";

            // 2. Lượt khám hôm nay
            var today = DateTime.Today;
            var appointmentsResponse = await client.From<Appointment>()
                .Where(a => a.AppointmentDate >= today && a.AppointmentDate < today.AddDays(1))
                .Get();
                
            int todayAppts = appointmentsResponse.Models?.Count ?? 0;
            TxtTodayAppointments.Text = todayAppts.ToString();

            // 3. Bệnh nhân mới trong tháng
            var usersResponse = await client.From<User>()
                .Where(u => u.Role == "Patient")
                .Get();
                
            int newPatients = 0;
            if (usersResponse.Models != null)
            {
                newPatients = usersResponse.Models
                    .Count(u => u.CreatedAt >= currentMonthStart);
            }
            TxtNewPatients.Text = newPatients.ToString();
        }


        private async Task LoadUsersByRoleAsync(string role)
        {
            var usersResponse = await SupabaseManager.Instance.Client
                .From<User>()
                .Where(u => u.Role == role)
                .Get();
                
            var users = usersResponse.Models;
            var displayList = new List<UserDisplayModel>();

            if (role == "Doctor")
            {
                var profiles = await SupabaseDbService.GetAllAsync<DoctorProfile>();
                displayList = users.Select(u => new UserDisplayModel
                {
                    Id = u.Id, FullName = u.FullName, Email = u.Email, Phone = u.Phone, CreatedAt = u.CreatedAt,
                    Specialty = profiles.FirstOrDefault(p => p.DoctorId == u.Id)?.Specialty
                }).ToList();
            }
            else if (role == "Staff")
            {
                displayList = users.Select(u => new UserDisplayModel
                {
                    Id = u.Id, FullName = u.FullName, Email = u.Email, Phone = u.Phone, CreatedAt = u.CreatedAt
                }).ToList();
            }
            else
            {
                var profiles = await SupabaseDbService.GetAllAsync<PatientProfile>();
                displayList = users.Select(u => new UserDisplayModel
                {
                    Id = u.Id, FullName = u.FullName, Email = u.Email, Phone = u.Phone, CreatedAt = u.CreatedAt,
                    DateOfBirth = profiles.FirstOrDefault(p => p.PatientId == u.Id)?.DateOfBirth
                }).ToList();
            }

            AdminDataGrid.ItemsSource = displayList;
        }

        private async Task LoadAppointmentsAsync()
        {
            var appointments = await SupabaseDbService.GetAllAsync<Appointment>();
            var allUsers = await SupabaseDbService.GetAllAsync<User>();
            
            var displayList = appointments.Select(a => new AppointmentDisplay
            {
                Id = a.Id,
                PatientName = allUsers.FirstOrDefault(u => u.Id == a.PatientId)?.FullName ?? "N/A",
                DoctorName = allUsers.FirstOrDefault(u => u.Id == a.DoctorId)?.FullName ?? "Bác sĩ ẩn",
                AppointmentDate = a.AppointmentDate,
                Status = a.Status,
                RoomCode = a.RoomCode
            }).ToList();

            AdminDataGrid.ItemsSource = displayList;
        }

        private async Task LoadTransactionsAsync()
        {
            var transactions = await SupabaseDbService.GetAllAsync<Transaction>();
            var allUsers = await SupabaseDbService.GetAllAsync<User>();

            var displayList = transactions.Select(t => new TransactionDisplay
            {
                Id = t.Id,
                PatientName = allUsers.FirstOrDefault(u => u.Id == t.PatientId)?.FullName ?? "N/A",
                Amount = t.Amount,
                PaymentMethod = t.PaymentMethod,
                Status = t.Status,
                PaidAt = t.PaidAt
            }).ToList();

            AdminDataGrid.ItemsSource = displayList;
        }

        private void AdminNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer?.Tag != null)
            {
                _currentTab = args.InvokedItemContainer.Tag.ToString();
                
                if (_currentTab == "Dashboard")
                {
                    DashboardPanel.Visibility = Visibility.Visible;
                    ListViewPanel.Visibility = Visibility.Collapsed;
                    HeaderTitle.Text = "Dashboard";
                }
                else
                {
                    DashboardPanel.Visibility = Visibility.Collapsed;
                    ListViewPanel.Visibility = Visibility.Visible;
                    HeaderTitle.Text = args.InvokedItem.ToString();
                }
                
                LoadDataAsync();
            }
        }

        private async void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog logoutDialog = new ContentDialog
            {
                Title = "Xác nhận đăng xuất",
                Content = "Bạn có chắc chắn muốn thoát khỏi phiên làm việc này không?",
                PrimaryButtonText = "Đăng xuất",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await logoutDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Frame.Navigate(typeof(LoginPage));
            }
        }

        private async void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            await ShowUserEditorDialog(null);
        }

        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AdminDataGrid.SelectedItem is User selectedUser)
            {
                await ShowUserEditorDialog(selectedUser);
            }
            else
            {
                await ShowDialogAsync("Thông báo", "Vui lòng chọn một người dùng để chỉnh sửa.");
            }
        }


        private async Task ShowUserEditorDialog(User existingUser)
        {
            bool isEdit = existingUser != null;
            string roleLabel = _currentTab == "Doctor" ? "Bác sĩ" : (_currentTab == "Staff" ? "Nhân viên" : "Bệnh nhân");
            string roleValue = _currentTab == "Doctor" ? "Doctor" : (_currentTab == "Staff" ? "Staff" : "Patient");

            // Tạo giao diện Form
            TextBox nameBox = new TextBox { Header = "Họ và Tên", Text = existingUser?.FullName ?? "", PlaceholderText = "Ví dụ: Nguyễn Văn A", Margin = new Thickness(0, 0, 0, 12) };
            TextBox emailBox = new TextBox { Header = "Địa chỉ Email", Text = existingUser?.Email ?? "", PlaceholderText = "email@example.com", Margin = new Thickness(0, 0, 0, 12), IsEnabled = !isEdit };
            TextBox phoneBox = new TextBox { Header = "Số điện thoại", Text = existingUser?.Phone ?? "", PlaceholderText = "09xxx", Margin = new Thickness(0, 0, 0, 12) };
            
            // Các field bổ sung dựa trên Role
            TextBox specialtyBox = new TextBox { Header = "Chuyên khoa", PlaceholderText = "Ví dụ: Nội khoa", Margin = new Thickness(0, 0, 0, 12) };
            TextBox dobBox = new TextBox { Header = "Ngày tháng năm sinh", PlaceholderText = "dd/mm/yyyy", Margin = new Thickness(0, 0, 0, 12) };
            PasswordBox passBox = new PasswordBox { Header = "Mật khẩu", PlaceholderText = isEdit ? "(Để trống nếu không đổi)" : "Tối thiểu 6 ký tự", Margin = new Thickness(0, 0, 0, 12) };

            StackPanel panel = new StackPanel { Padding = new Thickness(0, 8, 16, 0) };
            panel.Children.Add(nameBox);
            panel.Children.Add(emailBox);
            panel.Children.Add(phoneBox);

            if (_currentTab == "Doctor")
            {
                panel.Children.Add(specialtyBox);
                if (isEdit)
                {
                    var profile = (await SupabaseManager.Instance.Client.From<DoctorProfile>().Where(p => p.DoctorId == existingUser.Id).Get()).Models.FirstOrDefault();
                    if (profile != null) specialtyBox.Text = profile.Specialty ?? "";
                }
            }
            else if (_currentTab == "Patient")
            {
                panel.Children.Add(dobBox);
                if (isEdit)
                {
                    var profile = (await SupabaseManager.Instance.Client.From<PatientProfile>().Where(p => p.PatientId == existingUser.Id).Get()).Models.FirstOrDefault();
                    if (profile != null)
                    {
                        dobBox.Text = profile.DateOfBirth ?? "";
                    }
                }
            }

            if (!isEdit) panel.Children.Add(passBox);
            
            // Wrap trong ScrollViewer để tránh mất field khi màn hình nhỏ hoặc form dài
            ScrollViewer scrollViewer = new ScrollViewer 
            { 
                Content = panel, 
                MaxHeight = 450, 
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto 
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = isEdit ? $"Chỉnh sửa {roleLabel}" : $"Thêm {roleLabel} mới",
                Content = scrollViewer,
                PrimaryButtonText = "Xác nhận",
                CloseButtonText = "Hủy bỏ",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                // Validation (chuẩn hóa theo yêu cầu)
                if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(emailBox.Text))
                {
                    await ShowDialogAsync("Lỗi", "Họ tên và Email không được để trống.");
                    return;
                }

                if (!IsValidEmail(emailBox.Text))
                {
                    await ShowDialogAsync("Lỗi", "Định dạng Email không hợp lệ (ví dụ: abc@gmail.com).");
                    return;
                }

                if (string.IsNullOrEmpty(phoneBox.Text) || phoneBox.Text.Length < 10 || !phoneBox.Text.All(char.IsDigit))
                {
                    await ShowDialogAsync("Lỗi", "Số điện thoại phải là chữ số và tối thiểu 10 ký tự.");
                    return;
                }

                try
                {
                    if (isEdit)
                    {
                        // 1. Cập nhật User chính
                        existingUser.FullName = nameBox.Text;
                        existingUser.Phone = phoneBox.Text;
                        await SupabaseDbService.UpdateAsync(existingUser);

                        // 2. Cập nhật Profile tương ứng
                        if (_currentTab == "Doctor")
                        {
                            var docProfile = new DoctorProfile { DoctorId = existingUser.Id, Specialty = specialtyBox.Text };
                            await SupabaseDbService.UpdateAsync(docProfile);
                        }
                        else if (_currentTab == "Patient")
                        {
                            var patProfile = new PatientProfile { PatientId = existingUser.Id, DateOfBirth = dobBox.Text };
                            await SupabaseDbService.UpdateAsync(patProfile);
                        }

                        await ShowDialogAsync("Thành công", $"Đã cập nhật thông tin {roleLabel} thành công.");
                    }
                    else
                    {
                        // Tạo mới (Sử dụng API Server để đảm bảo bảo mật và đồng bộ Auth)
                        bool success = await _adminApiClient.CreateUserViaServerAsync(emailBox.Text, passBox.Password, nameBox.Text, roleValue);
                        if (success)
                        {
                            // Nếu là Patient, cập nhật thêm address ngay sau khi tạo (tùy Server API có hỗ trợ sẵn không)
                            // Ở đây ta cứ thông báo thành công cho đơn giản theo API hiện tại
                            await ShowDialogAsync("Thành công", $"Đã tạo mới tài khoản {roleLabel}.");
                        }
                        else
                        {
                            await ShowDialogAsync("Thất bại", "Không thể tạo tài khoản. Email có thể đã tồn tại.");
                        }
                    }
                    LoadDataAsync();
                }
                catch (Exception ex)
                {
                    await ShowDialogAsync("Lỗi", ex.Message);
                }
            }
        }

        private bool IsValidEmail(string email)
        {
            try { return System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"); }
            catch { return false; }
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AdminDataGrid.SelectedItem == null)
            {
                await ShowDialogAsync("Thông báo", "Vui lòng chọn một dòng để xóa.");
                return;
            }
            await ExecuteDelete(AdminDataGrid.SelectedItem);
        }

        private async void AdminDataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (AdminDataGrid.SelectedItem is User user && (_currentTab == "Patient" || _currentTab == "Doctor"))
                await ShowUserEditorDialog(user);
        }

        private async Task ExecuteDelete(object item)
        {
            if (item is not User user)
            {
                await ShowDialogAsync("Thông báo", "Hệ thống chỉ hỗ trợ xóa tài khoản người dùng trực tiếp.");
                return;
            }

            ContentDialog dialog = new ContentDialog
            {
                Title = "Xác nhận xóa tài khoản",
                Content = $"Bạn có chắc chắn muốn xóa vĩnh viễn '{user.FullName}'?",
                PrimaryButtonText = "Xóa ngay",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try
                {
                    if (await _adminApiClient.DeleteUserViaServerAsync(user.Id))
                    {
                        LoadDataAsync();
                    }
                }
                catch (Exception ex) { await ShowDialogAsync("Lỗi", ex.Message); }
            }
        }

        private async Task ShowDialogAsync(string title, string message)
        {
            ContentDialog dialog = new ContentDialog { Title = title, Content = message, CloseButtonText = "Đóng", XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
        }
    }
}