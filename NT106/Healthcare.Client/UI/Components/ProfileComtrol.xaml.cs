using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.SupabaseIntegration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class ProfileControl : UserControl
    {
        private User? _currentUser;

        public ProfileControl()
        {
            this.InitializeComponent();
            this.Loaded += ProfileControl_Loaded;
        }

        private async void ProfileControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCurrentUserProfileAsync();
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadCurrentUserProfileAsync();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveCurrentUserProfileAsync();
        }

        private async Task LoadCurrentUserProfileAsync()
        {
            SetButtonsEnabled(false);
            ShowStatus("Đang tải thông tin cá nhân...", "#334155", "#F8FAFC", "#E2E8F0");

            try
            {
                var sessionUser = SessionStorage.CurrentUser;

                if (sessionUser == null || string.IsNullOrWhiteSpace(sessionUser.Id))
                {
                    ClearForm();
                    ShowStatus("Không tìm thấy người dùng đang đăng nhập.", "#B91C1C", "#FEF2F2", "#FECACA");
                    return;
                }

                var client = SupabaseManager.Instance.Client;

                var response = await client
                    .From<User>()
                    .Get();

                _currentUser = response.Models
                    .FirstOrDefault(u => u.Id == sessionUser.Id);

                if (_currentUser == null)
                {
                    _currentUser = sessionUser;
                }

                BindUserToForm(_currentUser);
                ApplyRoleVisibility(_currentUser.Role);

                ShowStatus("Đã tải thông tin từ database.", "#15803D", "#ECFDF5", "#A7F3D0");
            }
            catch (Exception ex)
            {
                ShowStatus("Không tải được thông tin cá nhân: " + ex.Message, "#B91C1C", "#FEF2F2", "#FECACA");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void BindUserToForm(User user)
        {
            FullNameBox.Text = string.IsNullOrWhiteSpace(user.FullName)
                ? string.Empty
                : user.FullName;

            EmailBox.Text = string.IsNullOrWhiteSpace(user.Email)
                ? "Chưa cập nhật"
                : user.Email;

            PhoneBox.Text = string.IsNullOrWhiteSpace(user.Phone)
                ? string.Empty
                : user.Phone;

            RoleBox.Text = string.IsNullOrWhiteSpace(user.Role)
                ? "Không xác định"
                : user.Role;
        }

        private async Task SaveCurrentUserProfileAsync()
        {
            SetButtonsEnabled(false);
            ShowStatus("Đang lưu thay đổi...", "#334155", "#F8FAFC", "#E2E8F0");

            try
            {
                var sessionUser = SessionStorage.CurrentUser;

                if (sessionUser == null || string.IsNullOrWhiteSpace(sessionUser.Id))
                {
                    ShowStatus("Không tìm thấy người dùng đang đăng nhập.", "#B91C1C", "#FEF2F2", "#FECACA");
                    return;
                }

                string fullName = FullNameBox.Text.Trim();
                string phone = PhoneBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    ShowStatus("Họ tên không được để trống.", "#B91C1C", "#FEF2F2", "#FECACA");
                    return;
                }

                var client = SupabaseManager.Instance.Client;

                var response = await client
                    .From<User>()
                    .Get();

                var dbUser = response.Models
                    .FirstOrDefault(u => u.Id == sessionUser.Id);

                if (dbUser == null)
                {
                    ShowStatus("Không tìm thấy tài khoản trong database.", "#B91C1C", "#FEF2F2", "#FECACA");
                    return;
                }

                // Chỉ sửa dữ liệu được phép sửa.
                // Không sửa Email, Role, Password, CreatedAt...
                dbUser.FullName = fullName;
                dbUser.Phone = phone;

                await dbUser.Update<User>();

                SessionStorage.CurrentUser = dbUser;
                _currentUser = dbUser;

                BindUserToForm(dbUser);
                ApplyRoleVisibility(dbUser.Role);

                ShowStatus("Đã lưu thông tin cá nhân vào database.", "#15803D", "#ECFDF5", "#A7F3D0");
            }
            catch (Exception ex)
            {
                ShowStatus("Không lưu được thông tin cá nhân: " + ex.Message, "#B91C1C", "#FEF2F2", "#FECACA");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void ClearForm()
        {
            FullNameBox.Text = string.Empty;
            EmailBox.Text = string.Empty;
            PhoneBox.Text = string.Empty;
            RoleBox.Text = string.Empty;

            PatientPanel.Visibility = Visibility.Collapsed;
            DoctorPanel.Visibility = Visibility.Collapsed;
            AdminPanel.Visibility = Visibility.Collapsed;
        }

        private void ApplyRoleVisibility(string? role)
        {
            PatientPanel.Visibility = Visibility.Collapsed;
            DoctorPanel.Visibility = Visibility.Collapsed;
            AdminPanel.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(role))
                return;

            if (role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
            {
                PatientPanel.Visibility = Visibility.Visible;
            }
            else if (role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            {
                DoctorPanel.Visibility = Visibility.Visible;
            }
            else if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                AdminPanel.Visibility = Visibility.Visible;
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            SaveButton.IsEnabled = enabled;
            ReloadButton.IsEnabled = enabled;
        }

        private void ShowStatus(string message, string foregroundHex, string backgroundHex, string borderHex)
        {
            StatusMessageBorder.Visibility = Visibility.Visible;
            StatusMessageText.Text = message;

            StatusMessageText.Foreground = new SolidColorBrush(ParseColor(foregroundHex));
            StatusMessageBorder.Background = new SolidColorBrush(ParseColor(backgroundHex));
            StatusMessageBorder.BorderBrush = new SolidColorBrush(ParseColor(borderHex));
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
}