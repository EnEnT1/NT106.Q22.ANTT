using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;

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

            // Validate
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(password))
            {
                await ShowDialogAsync("Thiếu thông tin", "Vui lòng điền đầy đủ tất cả các trường.");
                return;
            }

            if (password != confirm)
            {
                await ShowDialogAsync("Mật khẩu không khớp", "Mật khẩu xác nhận không trùng với mật khẩu đã nhập.");
                return;
            }

            if (TermsCheckBox.IsChecked != true)
            {
                await ShowDialogAsync("Chưa đồng ý điều khoản", "Vui lòng đồng ý với điều khoản sử dụng trước khi đăng ký.");
                return;
            }

            RegisterBtn.IsEnabled = false;

            try
            {
                // TODO: Gọi SupabaseAuthService.SignUpAsync(email, password)
                // TODO: Lưu thêm thông tin fullName, phone vào bảng PatientProfile

                await ShowDialogAsync("Đăng ký thành công", "Tài khoản đã được tạo. Vui lòng kiểm tra email để xác nhận.");

                // Quay lại trang đăng nhập
                Frame.GoBack();
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Đăng ký thất bại", ex.Message);
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
