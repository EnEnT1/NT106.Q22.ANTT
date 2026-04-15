using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System;
using System.Threading.Tasks;
using Windows.UI;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class VideoCallControl : UserControl
    {
        public event EventHandler? CallStarted;
        public event EventHandler? CallEnded;

        private string _appointmentId = string.Empty;
        private string _patientId = string.Empty;

        private DispatcherTimer? _callTimer;
        private TimeSpan _callDuration = TimeSpan.Zero;

        private bool _isCallActive = false;
        private bool _isMicOn = true;
        private bool _isCameraOn = true;
        private bool _isSpeakerOn = true;

        public VideoCallControl()
        {
            this.InitializeComponent();
            this.Unloaded += (_, _) => Cleanup();
        }

        public async Task InitializeAsync(string appointmentId, string patientId)
        {
            _appointmentId = appointmentId;
            _patientId = patientId;

            ResetUiToWaitingState();

            // TODO sau này:
            // - khởi tạo local camera stream
            // - subscribe realtime signal
            // - chờ offer/answer WebRTC

            await Task.CompletedTask;
        }

        public void Cleanup()
        {
            StopTimer();

            // TODO sau này:
            // - đóng WebRTC peer
            // - unsubscribe realtime signal
        }

        public async Task StartCallAsync()
        {
            if (_isCallActive) return;

            SetConnectingState(true);

            // TODO sau này:
            // - tạo offer WebRTC
            // - gửi offer qua bảng webrtc_signals
            // - nhận remote stream
            // - gắn remote video vào UI

            await Task.Delay(1000);

            SetConnectingState(false);
            SetCallActive(true);
        }

        private void ResetUiToWaitingState()
        {
            _isCallActive = false;
            _isMicOn = true;
            _isCameraOn = true;
            _isSpeakerOn = true;

            WaitingPlaceholder.Visibility = Visibility.Visible;
            LiveBadge.Visibility = Visibility.Collapsed;
            QualityBadge.Visibility = Visibility.Collapsed;

            TxtWaitingStatus.Text = "Đang chờ kết nối bệnh nhân...";
            TxtDuration.Text = "LIVE 00:00";
            TxtQuality.Text = "HD Quality";

            IconMic.Glyph = "\uE720";
            IconCamera.Glyph = "\uE714";
            IconSpeaker.Glyph = "\uE767";

            BtnMic.Background = new SolidColorBrush(HexToColor("#00000066"));
            BtnCamera.Background = new SolidColorBrush(HexToColor("#00000066"));
            BtnSpeaker.Background = new SolidColorBrush(HexToColor("#00000066"));

            PipContainer.Opacity = 1.0;
            ConnectingRing.IsActive = false;
        }

        private void SetConnectingState(bool connecting)
        {
            ConnectingRing.IsActive = connecting;
            TxtWaitingStatus.Text = connecting
                ? "Đang kết nối..."
                : "Đang chờ kết nối bệnh nhân...";
        }

        private void SetCallActive(bool active)
        {
            _isCallActive = active;

            WaitingPlaceholder.Visibility = active ? Visibility.Collapsed : Visibility.Visible;
            LiveBadge.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            QualityBadge.Visibility = active ? Visibility.Visible : Visibility.Collapsed;

            if (active)
            {
                StartTimer();
                CallStarted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                CallEnded?.Invoke(this, EventArgs.Empty);
            }
        }

        private void StartTimer()
        {
            StopTimer();

            _callDuration = TimeSpan.Zero;
            _callTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _callTimer.Tick += (_, _) =>
            {
                _callDuration = _callDuration.Add(TimeSpan.FromSeconds(1));
                TxtDuration.Text = $"LIVE {_callDuration:mm\\:ss}";
            };

            _callTimer.Start();
        }

        private void StopTimer()
        {
            _callTimer?.Stop();
            _callTimer = null;
        }

        private void BtnMic_Click(object sender, RoutedEventArgs e)
        {
            _isMicOn = !_isMicOn;

            IconMic.Glyph = _isMicOn ? "\uE720" : "\uE71A";
            BtnMic.Background = new SolidColorBrush(
                _isMicOn ? HexToColor("#00000066") : HexToColor("#DC2626"));

            // TODO sau này:
            // _webRtc.SetMicEnabled(_isMicOn);
        }

        private void BtnCamera_Click(object sender, RoutedEventArgs e)
        {
            _isCameraOn = !_isCameraOn;

            IconCamera.Glyph = _isCameraOn ? "\uE714" : "\uE8BA";
            BtnCamera.Background = new SolidColorBrush(
                _isCameraOn ? HexToColor("#00000066") : HexToColor("#DC2626"));

            PipContainer.Opacity = _isCameraOn ? 1.0 : 0.3;

            // TODO sau này:
            // _webRtc.SetCameraEnabled(_isCameraOn);
        }

        private void BtnSpeaker_Click(object sender, RoutedEventArgs e)
        {
            _isSpeakerOn = !_isSpeakerOn;

            IconSpeaker.Glyph = _isSpeakerOn ? "\uE767" : "\uE74F";
            BtnSpeaker.Background = new SolidColorBrush(
                _isSpeakerOn ? HexToColor("#00000066") : HexToColor("#DC2626"));

            // TODO sau này:
            // _webRtc.SetSpeakerEnabled(_isSpeakerOn);
        }

        private async void BtnEndCall_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Kết thúc cuộc gọi",
                Content = "Bạn có chắc muốn kết thúc cuộc gọi video?",
                PrimaryButtonText = "Kết thúc",
                CloseButtonText = "Huỷ",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                StopTimer();
                SetCallActive(false);
                ResetUiToWaitingState();

                // TODO sau này:
                // await _webRtc.CloseAsync();
            }
        }

        private void BtnMore_Click(object sender, RoutedEventArgs e)
        {
            var flyout = new MenuFlyout();

            flyout.Items.Add(new MenuFlyoutItem { Text = "Chia sẻ màn hình" });
            flyout.Items.Add(new MenuFlyoutItem { Text = "Cài đặt chất lượng" });

            flyout.ShowAt(BtnMore);
        }

        private static Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
    }
}