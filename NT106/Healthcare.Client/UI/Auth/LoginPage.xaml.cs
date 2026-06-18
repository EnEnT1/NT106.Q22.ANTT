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
// THÊM NAMESPACE CỦA SHELL VÀO ĐÂY
using Healthcare.Client.UI.Shell;
using Healthcare.Client.UI.Staff;

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

            // 1. Kiểm tra email đăng nhập trống
            if (string.IsNullOrEmpty(username))
            {
                await ShowDialogAsync("Thiếu thông tin", "Vui lòng nhập Email đăng nhập.");
                return;
            }

            // 2. Kiểm tra định dạng email
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(username, emailPattern))
            {
                await ShowDialogAsync("Email không hợp lệ", "Email đăng nhập không đúng định dạng. Vui lòng nhập đúng (Ví dụ: name@example.com).");
                return;
            }

            // 3. Kiểm tra mật khẩu trống
            if (string.IsNullOrEmpty(password))
            {
                await ShowDialogAsync("Thiếu thông tin", "Vui lòng nhập Mật khẩu.");
                return;
            }

            // 4. Kiểm tra độ dài mật khẩu (tối thiểu 6 ký tự)
            if (password.Length < 6)
            {
                await ShowDialogAsync("Mật khẩu không hợp lệ", "Mật khẩu đăng nhập phải có ít nhất 6 ký tự.");
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
                    string friendlyMessage = GetFriendlyErrorMessage(result.Message);
                    await ShowDialogAsync("Đăng nhập thất bại", friendlyMessage);
                    return;
                }

                // SỬA ĐIỀU HƯỚNG Ở ĐÂY: Trỏ toàn bộ tới SHELL thay vì HOMEPAGE
                if (result.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    // Lưu ý: Nếu bạn có AdminShell thì dùng AdminShell, nếu chưa có thì giữ nguyên AdminHomePage
                    Frame.Navigate(typeof(AdminHomePage), null,
                        new SlideNavigationTransitionInfo
                        {
                            Effect = SlideNavigationTransitionEffect.FromRight
                        });
                }
                else if (result.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
                {
                    // ✅ Đã sửa: Điều hướng tới DoctorShell
                    Frame.Navigate(typeof(DoctorShell), null,
                        new SlideNavigationTransitionInfo
                        {
                            Effect = SlideNavigationTransitionEffect.FromRight
                        });
                }
                else if (result.Role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                {
                    // ✅ Đã sửa: Điều hướng tới PatientShell
                    Frame.Navigate(typeof(PatientShell), null,
                        new SlideNavigationTransitionInfo
                        {
                            Effect = SlideNavigationTransitionEffect.FromRight
                        });
                }
                else if (result.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
                {
                    Frame.Navigate(typeof(StaffHomePage), null,
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
                string friendlyMessage = GetFriendlyErrorMessage(ex.Message);
                await ShowDialogAsync("Đăng nhập thất bại", friendlyMessage);
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
        // Helper: Chuyển đổi thông báo lỗi của Supabase sang Tiếng Việt dễ hiểu
        // ──────────────────────────────────────────────
        private string GetFriendlyErrorMessage(string rawMessage)
        {
            if (string.IsNullOrEmpty(rawMessage))
                return "Đã xảy ra lỗi không xác định. Vui lòng thử lại sau.";

            string lowerMsg = rawMessage.ToLower();

            if (lowerMsg.Contains("invalid login credentials") || lowerMsg.Contains("invalid_credentials") || lowerMsg.Contains("username or password"))
            {
                return "Email hoặc mật khẩu không chính xác. Vui lòng kiểm tra lại.";
            }
            if (lowerMsg.Contains("email not confirmed") || lowerMsg.Contains("confirm your email"))
            {
                return "Tài khoản chưa được xác nhận email. Vui lòng kiểm tra hộp thư điện tử của bạn để kích hoạt.";
            }
            if (lowerMsg.Contains("rate limit") || lowerMsg.Contains("too many requests"))
            {
                return "Bạn đã thử đăng nhập quá nhiều lần. Vui lòng thử lại sau ít phút.";
            }
            if (lowerMsg.Contains("invalid email"))
            {
                return "Địa chỉ email đăng nhập không hợp lệ.";
            }

            return rawMessage;
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