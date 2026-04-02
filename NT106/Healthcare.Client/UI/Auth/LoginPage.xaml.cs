using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;
using Healthcare.Client.Helpers;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.UI.Patient;
using Healthcare.Client.UI.Doctor;
using Healthcare.Client.UI.Admin;

namespace Healthcare.Client.UI.Auth
{
    public sealed partial class LoginPage : Page
    {
        private bool _isPasswordRevealed = false;

        public LoginPage()
        {
            this.InitializeComponent();
        }

        // ──────────────────────────────────────────────
        // Toggle hiện/ẩn mật khẩu
        // ──────────────────────────────────────────────
        private void RevealBtn_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordRevealed = !_isPasswordRevealed;

            if (_isPasswordRevealed)
            {
                PasswordBox.PasswordRevealMode = PasswordRevealMode.Visible;
                RevealIcon.Glyph = "\uED1A"; // Eye-off icon
            }
            else
            {
                PasswordBox.PasswordRevealMode = PasswordRevealMode.Hidden;
                RevealIcon.Glyph = "\uE7B3"; // Eye icon
            }
        }

        // ──────────────────────────────────────────────
        // Đăng nhập
        // ──────────────────────────────────────────────
        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await ShowDialogAsync("Thông tin chưa đầy đủ", "Vui lòng nhập tên đăng nhập và mật khẩu.");
                return;
            }

            var originalButtonContent = LoginBtn.Content;
            LoginBtn.IsEnabled = false;
            LoginBtn.Content = "Đang xác thực...";

            try
            {
                var result = await SupabaseAuthService.SignInAsync(username, password);

                if (!result.Success)
                {
                    await ShowDialogAsync("Đăng nhập thất bại", result.Message);
                    return;
                }
                if (result.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    // Trỏ đúng đến namespace của AdminHomePage
                    Frame.Navigate(typeof(Healthcare.Client.UI.Admin.AdminHomePage), null,
                        new SlideNavigationTransitionInfo
                        {
                            Effect = SlideNavigationTransitionEffect.FromRight
                        });
                }
                else if (result.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
                {
                    Frame.Navigate(typeof(DoctorHomePage), null,
                        new SlideNavigationTransitionInfo
                        {
                            Effect = SlideNavigationTransitionEffect.FromRight
                        });
                }
                else if (result.Role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                {
                    Frame.Navigate(typeof(PatientHomePage), null,
                        new SlideNavigationTransitionInfo
                        {
                            Effect = SlideNavigationTransitionEffect.FromRight
                        });
                }
                else
                {
                    await ShowDialogAsync("Lỗi phân quyền", "Tài khoản của bạn chưa được phân quyền hệ thống hợp lệ.");
                }
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Đăng nhập thất bại", ex.Message);
            }
            finally
            {
                LoginBtn.Content = originalButtonContent;
                LoginBtn.IsEnabled = true;
            }
        }

        // ──────────────────────────────────────────────
        // Quên mật khẩu
        // ──────────────────────────────────────────────
        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(
                typeof(ForgotPasswordPage),
                null,
                new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }
            );
        }

        // ──────────────────────────────────────────────
        // Yêu cầu cấp quyền truy cập
        // ──────────────────────────────────────────────
        private void RequestAccess_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(
                typeof(RegisterPage),
                null,
                new SlideNavigationTransitionInfo
                {
                    Effect = SlideNavigationTransitionEffect.FromRight
                });
        }


        // ──────────────────────────────────────────────
        // Helper: Hiển thị dialog thông báo
        // ──────────────────────────────────────────────
        private async Task ShowDialogAsync(string title, string message)
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
    }
}
