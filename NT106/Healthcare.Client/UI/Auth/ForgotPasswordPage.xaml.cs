using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;

namespace Healthcare.Client.UI.Auth
{
    public sealed partial class ForgotPasswordPage : Page
    {
        private int _currentStep = 1;
        private string _phoneNumber = "";
        private bool _isNewPasswordRevealed = false;
        private bool _isConfirmPasswordRevealed = false;

        // Countdown timer cho nút Gửi lại OTP
        private DispatcherTimer _countdownTimer;
        private int _remainingSeconds = 119; // 01:59

        public ForgotPasswordPage()
        {
            this.InitializeComponent();
            InitCountdownTimer();
        }

        // ──────────────────────────────────────────────
        // Chuyển step + cập nhật dot indicator
        // ──────────────────────────────────────────────
        private void GoToStep(int step)
        {
            Step1Panel.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

            Dot1.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                step >= 1 ? Microsoft.UI.ColorHelper.FromArgb(255, 0, 89, 187)
                          : Microsoft.UI.ColorHelper.FromArgb(255, 193, 198, 215));
            Dot2.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                step >= 2 ? Microsoft.UI.ColorHelper.FromArgb(255, 0, 89, 187)
                          : Microsoft.UI.ColorHelper.FromArgb(255, 193, 198, 215));
            Dot3.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                step >= 3 ? Microsoft.UI.ColorHelper.FromArgb(255, 0, 89, 187)
                          : Microsoft.UI.ColorHelper.FromArgb(255, 193, 198, 215));

            _currentStep = step;
        }

        // ──────────────────────────────────────────────
        // STEP 1: Gửi mã OTP
        // ──────────────────────────────────────────────
        private async void SendOtp_Click(object sender, RoutedEventArgs e)
        {
            _phoneNumber = PhoneBox.Text.Trim();

            if (string.IsNullOrEmpty(_phoneNumber))
            {
                await ShowDialogAsync("Thiếu thông tin", "Vui lòng nhập số điện thoại.");
                return;
            }

            try
            {
                // TODO: Gọi API gửi OTP về số điện thoại
                // await SupabaseAuthService.SendOtpAsync(_phoneNumber);

                // Hiển thị số điện thoại bị che ở step 2
                string masked = _phoneNumber.Length >= 6
                    ? _phoneNumber[..3] + "***" + _phoneNumber[^4..]
                    : _phoneNumber;
                MaskedPhoneText.Text = masked;

                // Bắt đầu đếm ngược
                StartCountdown();

                GoToStep(2);
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Gửi OTP thất bại", ex.Message);
            }
        }

        // ──────────────────────────────────────────────
        // STEP 2: Xác thực OTP
        // ──────────────────────────────────────────────
        private async void VerifyOtp_Click(object sender, RoutedEventArgs e)
        {
            string otp = Otp1.Text + Otp2.Text + Otp3.Text +
                         Otp4.Text + Otp5.Text + Otp6.Text;

            if (otp.Length < 6)
            {
                await ShowDialogAsync("Mã OTP chưa đầy đủ", "Vui lòng nhập đủ 6 chữ số.");
                return;
            }

            try
            {
                // TODO: Gọi API xác thực OTP
                // await SupabaseAuthService.VerifyOtpAsync(_phoneNumber, otp);

                _countdownTimer?.Stop();
                GoToStep(3);
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Xác thực thất bại", ex.Message);
            }
        }

        // Auto-focus ô tiếp theo khi nhập OTP
        private void OtpBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var boxes = new[] { Otp1, Otp2, Otp3, Otp4, Otp5, Otp6 };
            var current = sender as TextBox;
            int idx = Array.IndexOf(boxes, current);

            if (!string.IsNullOrEmpty(current?.Text) && idx < boxes.Length - 1)
                boxes[idx + 1].Focus(FocusState.Programmatic);
        }

        // ──────────────────────────────────────────────
        // STEP 2: Gửi lại OTP
        // ──────────────────────────────────────────────
        private void ResendOtp_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Gọi lại API gửi OTP
            ClearOtpBoxes();
            StartCountdown();
        }

        private void ClearOtpBoxes()
        {
            Otp1.Text = Otp2.Text = Otp3.Text = "";
            Otp4.Text = Otp5.Text = Otp6.Text = "";
            Otp1.Focus(FocusState.Programmatic);
        }

        // ──────────────────────────────────────────────
        // STEP 3: Cập nhật mật khẩu mới
        // ──────────────────────────────────────────────
        private async void UpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            string newPass = NewPasswordBox.Password;
            string confirmPass = ConfirmNewPasswordBox.Password;

            if (string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
            {
                await ShowDialogAsync("Thiếu thông tin", "Vui lòng nhập đầy đủ mật khẩu mới.");
                return;
            }

            if (newPass.Length < 8)
            {
                await ShowDialogAsync("Mật khẩu quá ngắn", "Mật khẩu phải có ít nhất 8 ký tự.");
                return;
            }

            if (newPass != confirmPass)
            {
                await ShowDialogAsync("Không khớp", "Mật khẩu xác nhận không trùng với mật khẩu mới.");
                return;
            }

            try
            {
                // TODO: Gọi API đặt lại mật khẩu
                // await SupabaseAuthService.UpdatePasswordAsync(newPass);

                await ShowDialogAsync("Thành công", "Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại.");
                BackToLogin_Click(sender, e);
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Cập nhật thất bại", ex.Message);
            }
        }

        // ──────────────────────────────────────────────
        // Reveal password Step 3
        // ──────────────────────────────────────────────
        private void RevealNew_Click(object sender, RoutedEventArgs e)
        {
            _isNewPasswordRevealed = !_isNewPasswordRevealed;
            NewPasswordBox.PasswordRevealMode = _isNewPasswordRevealed
                ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden;
            RevealNewIcon.Glyph = _isNewPasswordRevealed ? "\uED1A" : "\uE7B3";
        }

        private void RevealConfirm_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordRevealed = !_isConfirmPasswordRevealed;
            ConfirmNewPasswordBox.PasswordRevealMode = _isConfirmPasswordRevealed
                ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden;
            RevealConfirmIcon.Glyph = _isConfirmPasswordRevealed ? "\uED1A" : "\uE7B3";
        }

        // ──────────────────────────────────────────────
        // Quay lại Login
        // ──────────────────────────────────────────────
        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            _countdownTimer?.Stop();
            if (Frame.CanGoBack)
                Frame.GoBack();
            else
                Frame.Navigate(typeof(LoginPage), null,
                    new SlideNavigationTransitionInfo
                    {
                        Effect = SlideNavigationTransitionEffect.FromLeft
                    });
        }

        // ──────────────────────────────────────────────
        // Countdown timer cho Gửi lại OTP
        // ──────────────────────────────────────────────
        private void InitCountdownTimer()
        {
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;
        }

        private void StartCountdown()
        {
            _remainingSeconds = 119;
            ResendBtn.IsEnabled = false;
            UpdateCountdownText();
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object? sender, object e)
        {
            _remainingSeconds--;
            UpdateCountdownText();

            if (_remainingSeconds <= 0)
            {
                _countdownTimer.Stop();
                CountdownText.Text = "";
                ResendBtn.IsEnabled = true;
            }
        }

        private void UpdateCountdownText()
        {
            int minutes = _remainingSeconds / 60;
            int seconds = _remainingSeconds % 60;
            CountdownText.Text = $"Gửi lại mã sau {minutes:D2}:{seconds:D2}";
        }

        // ──────────────────────────────────────────────
        // Helper: Dialog
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
