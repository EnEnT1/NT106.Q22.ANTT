// ============================================================
//  VideoCallControl.xaml.cs
//  Healthcare.Client — UI/Components/VideoCallControl
// ============================================================

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
        // ─────────────────────────────────────────────────────────
        //  Events
        // ─────────────────────────────────────────────────────────
        public event EventHandler? CallStarted;
        public event EventHandler? CallEnded;

        // ─────────────────────────────────────────────────────────
        //  State
        // ─────────────────────────────────────────────────────────
        private string _appointmentId = string.Empty;
        private string _patientId = string.Empty;

        private DispatcherTimer? _callTimer;
        private TimeSpan _callDuration = TimeSpan.Zero;

        private bool _isCallActive = false;
        private bool _isMicOn = true;
        private bool _isCameraOn = true;
        private bool _isSpeakerOn = true;

        // ─────────────────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────────────────
        public VideoCallControl()
        {
            this.InitializeComponent();
            this.Unloaded += (_, _) => Cleanup();
        }

        // ─────────────────────────────────────────────────────────
        //  PUBLIC API
        // ─────────────────────────────────────────────────────────

        public async Task InitializeAsync(string appointmentId, string patientId)
        {
            _appointmentId = appointmentId;
            _patientId = patientId;

            // TODO — WebRTC: Khởi tạo local camera stream
            // var localStream = await WebRtcPeerConnection.GetLocalStreamAsync();
            // LocalVideo.Source = localStream;

            // TODO — Realtime: Subscribe signal channel để nhận offer từ bệnh nhân
            // await SubscribeToSignalsAsync();

            await Task.CompletedTask;
        }

        public void Cleanup()
        {
            StopTimer();
            // TODO — WebRTC: _webRtc?.Close();
            // TODO — Realtime: _realtime?.Unsubscribe(_signalChannel);
        }

        public async Task StartCallAsync()
        {
            if (_isCallActive) return;

            SetConnectingState(true);

            // ══════════════════════════════════════════════════════
            // TODO — WebRTC:
            //   var sdpOffer = await _webRtc.CreateOfferAsync();
            //   await _db.AddAsync(new WebrtcSignal { ... });
            //   _webRtc.RemoteStreamReceived += stream => {
            //       DispatcherQueue.TryEnqueue(() => {
            //           RemoteVideo.Source = stream;
            //           WaitingPlaceholder.Visibility = Visibility.Collapsed;
            //       });
            //   };
            // ══════════════════════════════════════════════════════

            // Mock delay
            await Task.Delay(1000);

            SetConnectingState(false);
            SetCallActive(true);
        }

        // ─────────────────────────────────────────────────────────
        //  CALL STATE
        // ─────────────────────────────────────────────────────────

        private void SetConnectingState(bool connecting)
        {
            ConnectingRing.IsActive = connecting;
            TxtWaitingStatus.Text = connecting
                ? "Đang kết nối..." : "Đang chờ kết nối bệnh nhân...";
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

        // ─────────────────────────────────────────────────────────
        //  TIMER
        // ─────────────────────────────────────────────────────────

        private void StartTimer()
        {
            _callDuration = TimeSpan.Zero;
            _callTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
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

        // ─────────────────────────────────────────────────────────
        //  BUTTON HANDLERS
        // ─────────────────────────────────────────────────────────

        private void BtnMic_Click(object sender, RoutedEventArgs e)
        {
            _isMicOn = !_isMicOn;
            IconMic.Glyph = _isMicOn ? "\uE720" : "\uE71A";
            BtnMic.Background = new SolidColorBrush(
                _isMicOn ? HexToColor("#00000066") : HexToColor("#DC2626"));

            // TODO — WebRTC: _webRtc.SetMicEnabled(_isMicOn);
        }

        private void BtnCamera_Click(object sender, RoutedEventArgs e)
        {
            _isCameraOn = !_isCameraOn;
            IconCamera.Glyph = _isCameraOn ? "\uE714" : "\uE8BA";
            BtnCamera.Background = new SolidColorBrush(
                _isCameraOn ? HexToColor("#00000066") : HexToColor("#DC2626"));
            PipContainer.Opacity = _isCameraOn ? 1.0 : 0.3;

            // TODO — WebRTC: _webRtc.SetCameraEnabled(_isCameraOn);
        }

        private void BtnSpeaker_Click(object sender, RoutedEventArgs e)
        {
            _isSpeakerOn = !_isSpeakerOn;
            IconSpeaker.Glyph = _isSpeakerOn ? "\uE767" : "\uE74F";
            BtnSpeaker.Background = new SolidColorBrush(
                _isSpeakerOn ? HexToColor("#00000066") : HexToColor("#DC2626"));

            // TODO — WebRTC: _webRtc.SetSpeakerEnabled(_isSpeakerOn);
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
                TxtDuration.Text = "LIVE 00:00";
                // TODO — WebRTC: await _webRtc.CloseAsync();
            }
        }

        private void BtnMore_Click(object sender, RoutedEventArgs e)
        {
            var flyout = new MenuFlyout();
            flyout.Items.Add(new MenuFlyoutItem { Text = "Chia sẻ màn hình" });
            flyout.Items.Add(new MenuFlyoutItem { Text = "Cài đặt chất lượng" });
            flyout.ShowAt(BtnMore);
        }

        // ─────────────────────────────────────────────────────────
        //  UTILITY
        // ─────────────────────────────────────────────────────────

        private static Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
    }
}
