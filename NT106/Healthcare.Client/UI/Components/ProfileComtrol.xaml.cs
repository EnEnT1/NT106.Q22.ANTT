using Healthcare.Client.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class ProfileControl : UserControl
    {
        public ProfileControl()
        {
            this.InitializeComponent();
            this.Loaded += ProfileControl_Loaded;
        }

        private void ProfileControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCurrentUserProfile();
        }

        private void LoadCurrentUserProfile()
        {
            var user = SessionStorage.CurrentUser;

            if (user == null)
            {
                FullNameText.Text = "Chưa đăng nhập";
                EmailText.Text = "Không có dữ liệu";
                PhoneText.Text = "Không có dữ liệu";
                RoleText.Text = "Không xác định";

                PatientPanel.Visibility = Visibility.Collapsed;
                DoctorPanel.Visibility = Visibility.Collapsed;
                AdminPanel.Visibility = Visibility.Collapsed;

                return;
            }

            FullNameText.Text = string.IsNullOrWhiteSpace(user.FullName)
                ? "Chưa cập nhật"
                : user.FullName;

            EmailText.Text = string.IsNullOrWhiteSpace(user.Email)
                ? "Chưa cập nhật"
                : user.Email;

            PhoneText.Text = string.IsNullOrWhiteSpace(user.Phone)
                ? "Chưa cập nhật"
                : user.Phone;

            RoleText.Text = string.IsNullOrWhiteSpace(user.Role)
                ? "Không xác định"
                : user.Role;

            ApplyRoleVisibility(user.Role);
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
    }
}