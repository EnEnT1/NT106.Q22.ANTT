using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;
using Healthcare.Client.SupabaseIntegration;

namespace Healthcare.Client.UI.Auth
{
    public sealed partial class RegisterPage : Page
    {
        private bool _isPasswordRevealed = false;
        private bool _isConfirmRevealed = false;

        public RegisterPage()
        {
            this.InitializeComponent();
        }

        // ──────────────────────────────────────────────
        // Toggle hiện/ẩn mật khẩu
        // ──────────────────────────────────────────────
        private void RevealPassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordRevealed = !_isPasswordRevealed;
            PasswordBox.PasswordRevealMode = _isPasswordRevealed
                ? PasswordRevealMode.Visible
                : PasswordRevealMode.Hidden;
            RevealPasswordIcon.Glyph = _isPasswordRevealed ? "\uED1A" : "\uE7B3";
        }

        private void RevealConfirm_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmRevealed = !_isConfirmRevealed;
            ConfirmPasswordBox.PasswordRevealMode = _isConfirmRevealed
                ? PasswordRevealMode.Visible
                : PasswordRevealMode.Hidden;
            RevealConfirmIcon.Glyph = _isConfirmRevealed ? "\uED1A" : "\uE7B3";
        }

        // ──────────────────────────────────────────────
        // Đăng ký
        // ──────────────────────────────────────────────
        private async void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            // 1. Kiểm tra họ và tên
            if (string.IsNullOrEmpty(fullName))
            {
                await ShowDialogAsync("Thiếu thông tin", "Vui lòng nhập Họ và tên.");
                return;
            }

            // 2. Kiểm tra email trống
            if (string.IsNullOrEmpty(email))
            {
                await ShowDialogAsync("Thiếu thông tin", "Vui lòng nhập địa chỉ Email.");
                return;
            }

            // 3. Kiểm tra định dạng email
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, emailPattern))
            {
                await ShowDialogAsync("Email không hợp lệ", "Định dạng Email không đúng. Vui lòng nhập theo mẫu (Ví dụ: name@example.com).");
                return;
            }

            // 4. Kiểm tra số điện thoại trống
            if (string.IsNullOrEmpty(phone))
            {
                await ShowDialogAsync("Thiếu thông tin", "Vui lòng nhập Số điện thoại.");
                return;
            }

            // 5. Kiểm tra định dạng số điện thoại
            string phonePattern = @"^(0\d{9})$"; // 10 chữ số bắt đầu bằng 0
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, phonePattern))
            {
                await ShowDialogAsync("Số điện thoại không hợp lệ", "Số điện thoại phải bao gồm đúng 10 chữ số và bắt đầu bằng số 0 (Ví dụ: 0912345678).");
                return;
            }

            // 6. Kiểm tra mật khẩu trống
            if (string.IsNullOrEmpty(password))
            {
                await ShowDialogAsync("Thiếu thông tin", "Vui lòng nhập Mật khẩu.");
                return;
            }

            // 7. Kiểm tra ràng buộc độ dài mật khẩu (ít nhất 6 ký tự)
            if (password.Length < 6)
            {
                await ShowDialogAsync("Mật khẩu không hợp lệ", "Mật khẩu phải có độ dài tối thiểu là 6 ký tự để đảm bảo an toàn.");
                return;
            }

            // 8. Kiểm tra mật khẩu xác nhận
            if (password != confirm)
            {
                await ShowDialogAsync("Mật khẩu không khớp", "Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập. Vui lòng kiểm tra lại.");
                return;
            }

            // 9. Kiểm tra đồng ý điều khoản
            if (TermsCheckBox.IsChecked != true)
            {
                await ShowDialogAsync("Chưa đồng ý điều khoản", "Vui lòng đồng ý với điều khoản sử dụng và chính sách bảo mật trước khi đăng ký.");
                return;
            }

            RegisterBtn.IsEnabled = false;

            try
            {
                var result = await SupabaseAuthService.SignUpAsync(fullName, email, phone, password);

                await ShowDialogAsync("Đăng ký thành công", result.Message);

                if (Frame.CanGoBack)
                    Frame.GoBack();
                else
                    Frame.Navigate(typeof(LoginPage), null,
                        new SlideNavigationTransitionInfo
                        {
                            Effect = SlideNavigationTransitionEffect.FromLeft
                        });
            }
            catch (Exception ex)
            {
                string friendlyMessage = GetFriendlyErrorMessage(ex.Message);
                await ShowDialogAsync("Đăng ký thất bại", friendlyMessage);
            }
            finally
            {
                RegisterBtn.IsEnabled = true;
            }
        }

        // ──────────────────────────────────────────────
        // Quay về trang Login
        // ──────────────────────────────────────────────
        private void GoToLogin_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
            else
                Frame.Navigate(typeof(LoginPage),
                    null,
                    new SlideNavigationTransitionInfo
                    {
                        Effect = SlideNavigationTransitionEffect.FromLeft
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

            if (lowerMsg.Contains("already registered") || lowerMsg.Contains("already exists") || lowerMsg.Contains("user_already_exists"))
            {
                return "Email này đã được sử dụng để đăng ký tài khoản khác trên hệ thống. Vui lòng chọn email khác hoặc tiến hành đăng nhập.";
            }
            if (lowerMsg.Contains("password should be at least"))
            {
                return "Mật khẩu của bạn quá ngắn. Độ dài tối thiểu phải từ 6 ký tự.";
            }
            if (lowerMsg.Contains("invalid email") || lowerMsg.Contains("signup requires a valid email"))
            {
                return "Địa chỉ email không được hệ thống chấp nhận hoặc không tồn tại. Vui lòng kiểm tra lại.";
            }
            if (lowerMsg.Contains("rate limit") || lowerMsg.Contains("too many requests"))
            {
                return "Bạn đã thực hiện quá nhiều yêu cầu đăng ký liên tiếp. Vui lòng đợi vài phút rồi thử lại.";
            }

            // Nếu không khớp các lỗi trên, loại bỏ tiền tố chung nếu có và trả về
            string cleaned = rawMessage.Replace("Không thể đăng ký tài khoản. ", "").Trim();
            return string.IsNullOrEmpty(cleaned) ? rawMessage : cleaned;
        }

        // ──────────────────────────────────────────────
        // Helper: Dialog thông báo
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
