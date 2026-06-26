using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Microsoft.Web.WebView2.Core;

using Healthcare.Client.Helpers;
using Healthcare.Client.SupabaseIntegration;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Windows.Media.Capture;
using Windows.UI;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class VideoCallControl : UserControl, IDisposable
    {
        #region Events

        public event EventHandler? CallStarted;
        public event EventHandler? CallEnded;

        #endregion

        #region Daily

        // Thay bằng subdomain Daily.co của bạn
        private const string DailySubdomain = "healthcare-nt106";

        #endregion

        #region Session

        private string _appointmentId = "";
        private string _roomCode = "";
        private string _targetUserId = "";

        private string _currentUserId = "";
        private string _displayName = "User";

        #endregion

        #region State

        private bool _webViewReady;
        private bool _callActive;
        private bool _disposed;

        private bool _micEnabled = true;
        private bool _cameraEnabled = true;
        private bool _speakerEnabled = true;

        #endregion

        #region Timer

        private DispatcherTimer? _callTimer;
        private TimeSpan _duration = TimeSpan.Zero;

        #endregion

        public VideoCallControl()
        {
            InitializeComponent();

            Loaded += VideoCallControl_Loaded;
            Unloaded += VideoCallControl_Unloaded;
        }

        private void VideoCallControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void VideoCallControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Cleanup();
        }

        public async Task InitializeAsync(
            string appointmentId,
            string targetUserId,
            string roomCode)
        {
            try
            {
                _appointmentId = appointmentId;
                _targetUserId = targetUserId;

                _roomCode = string.IsNullOrWhiteSpace(roomCode)
                    ? appointmentId
                    : roomCode;

                _roomCode = _roomCode.Trim().ToLower();

                _currentUserId =
                    SessionStorage.CurrentUser?.Id ?? "";

                _displayName =
                    SessionStorage.CurrentUser?.FullName ??
                    "Người dùng";

                ResetUi();

                await CheckPermissionsAsync();

                await VideoWebView.EnsureCoreWebView2Async();

                _webViewReady = true;

                VideoWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;

                VideoWebView.CoreWebView2.PermissionRequested +=
                    CoreWebView2_PermissionRequested;

                VideoWebView.CoreWebView2.WebMessageReceived +=
                    CoreWebView2_WebMessageReceived;

                Debug.WriteLine("VideoCall initialized.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                await ShowError(
                    "Khởi tạo Video Call thất bại",
                    ex.Message);
            }
        }

        private void CoreWebView2_PermissionRequested(
            CoreWebView2 sender,
            CoreWebView2PermissionRequestedEventArgs args)
        {
            if (args.PermissionKind ==
                    CoreWebView2PermissionKind.Camera ||
                args.PermissionKind ==
                    CoreWebView2PermissionKind.Microphone)
            {
                args.State =
                    CoreWebView2PermissionState.Allow;
            }
        }
                public async Task StartCallAsync()
        {
            if (!_webViewReady)
            {
                await ShowError(
                    "Video Call",
                    "WebView2 chưa được khởi tạo.");

                return;
            }

            try
            {
                SetConnecting(true);

                string roomName = $"hc-{_roomCode}";

                string roomUrl =
                    $"https://{DailySubdomain}.daily.co/{roomName}";

                string userName =
                    Uri.EscapeDataString(_displayName);

                string html = $@"
<!DOCTYPE html>
<html>

<head>

<meta charset='UTF-8'/>

<meta
name='viewport'
content='width=device-width,initial-scale=1'/>

<style>

html,
body
{{
margin:0;
padding:0;
width:100%;
height:100%;
overflow:hidden;
background:#111827;
}}

#call
{{
width:100%;
height:100%;
}}

</style>

</head>

<body>

<div id='call'></div>

<script src='https://unpkg.com/@daily-co/daily-js'></script>

<script>

let frame = null;

async function start()
{{
    frame = Daily.createFrame(
        document.getElementById('call'),
        {{
            iframeStyle:
            {{
                width:'100%',
                height:'100%',
                border:'0'
            }},

            showLeaveButton:false,
            showFullscreenButton:false,
            showParticipantsBar:false,
            showLocalVideo:true
        }});

    frame
        .on('joined-meeting',()=>
        {{
            chrome.webview.postMessage('joined');
        }});

    frame
        .on('left-meeting',()=>
        {{
            chrome.webview.postMessage('left');
        }});

    frame
        .on('participant-joined',(e)=>
        {{
            chrome.webview.postMessage('participant');
        }});

    frame
        .on('participant-left',(e)=>
        {{
            chrome.webview.postMessage('participant-left');
        }});

    frame
        .on('error',(e)=>
        {{
            chrome.webview.postMessage(
                'error:' + JSON.stringify(e));
        }});

    await frame.join(
    {{
        url:'{roomUrl}',
        userName:decodeURIComponent('{userName}'),
        startAudioOff:false,
        startVideoOff:false
    }});
}}

window.toggleMic = function(enable)
{{
    if(frame)
        frame.setLocalAudio(enable);
}}

window.toggleCamera = function(enable)
{{
    if(frame)
        frame.setLocalVideo(enable);
}}

window.leaveMeeting = function()
{{
    if(frame)
        frame.leave();
}}

window.startShare = async function()
{{
    if(frame)
        await frame.startScreenShare();
}}

window.stopShare = async function()
{{
    if(frame)
        await frame.stopScreenShare();
}}

start();

</script>

</body>

</html>";

                VideoWebView.NavigateToString(html);

                WaitingPlaceholder.Visibility =
                    Visibility.Collapsed;

                VideoWebView.Visibility =
                    Visibility.Visible;
            }
            catch (Exception ex)
            {
                SetConnecting(false);

                Debug.WriteLine(ex);

                await ShowError(
                    "Không thể bắt đầu cuộc gọi",
                    ex.Message);
            }
        }
                private void CoreWebView2_WebMessageReceived(
            CoreWebView2 sender,
            CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string message = args.TryGetWebMessageAsString();

                Debug.WriteLine($"Daily Message: {message}");

                DispatcherQueue.TryEnqueue(() =>
                {
                    switch (message)
                    {
                        case "joined":

                            SetConnecting(false);
                            SetCallActive(true);

                            break;

                        case "left":

                            StopTimer();

                            _callActive = false;

                            VideoWebView.Visibility =
                                Visibility.Collapsed;

                            WaitingPlaceholder.Visibility =
                                Visibility.Visible;

                            TxtWaitingStatus.Text =
                                "Cuộc gọi đã kết thúc";

                            CallEnded?.Invoke(
                                this,
                                EventArgs.Empty);

                            break;

                        case "participant":

                            TxtWaitingStatus.Text =
                                "Đã có người tham gia";

                            break;

                        case "participant-left":

                            TxtWaitingStatus.Text =
                                "Đối phương đã rời cuộc gọi";

                            break;

                        default:

                            if (message.StartsWith("error:"))
                            {
                                Debug.WriteLine(message);

                                TxtWaitingStatus.Text =
                                    "Có lỗi xảy ra";
                            }

                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void SetCallActive(bool active)
        {
            _callActive = active;

            WaitingPlaceholder.Visibility =
                active
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            VideoWebView.Visibility =
                active
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            LiveBadge.Visibility =
                active
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            QualityBadge.Visibility =
                active
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (active)
            {
                StartTimer();

                TxtWaitingStatus.Text =
                    "Đang trong cuộc gọi";

                CallStarted?.Invoke(
                    this,
                    EventArgs.Empty);
            }
            else
            {
                StopTimer();

                CallEnded?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }

        private void SetConnecting(bool connecting)
        {
            ConnectingRing.IsActive = connecting;

            WaitingPlaceholder.Visibility =
                Visibility.Visible;

            TxtWaitingStatus.Text =
                connecting
                    ? "Đang kết nối..."
                    : "Đang chờ...";
        }

        private void ResetUi()
        {
            _callActive = false;

            _micEnabled = true;
            _cameraEnabled = true;
            _speakerEnabled = true;

            WaitingPlaceholder.Visibility =
                Visibility.Visible;

            VideoWebView.Visibility =
                Visibility.Collapsed;

            LiveBadge.Visibility =
                Visibility.Collapsed;

            QualityBadge.Visibility =
                Visibility.Collapsed;

            TxtWaitingStatus.Text =
                "Đang chờ kết nối...";

            TxtDuration.Text =
                "LIVE 00:00";

            TxtQuality.Text =
                "HD";

            ConnectingRing.IsActive = false;

            IconMic.Glyph = "\uE720";
            IconCamera.Glyph = "\uE714";
            IconSpeaker.Glyph = "\uE767";

            BtnMic.Background =
                new SolidColorBrush(HexToColor("#00000066"));

            BtnCamera.Background =
                new SolidColorBrush(HexToColor("#00000066"));

            BtnSpeaker.Background =
                new SolidColorBrush(HexToColor("#00000066"));
        }

        private void StartTimer()
        {
            StopTimer();

            _duration = TimeSpan.Zero;

            _callTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _callTimer.Tick += CallTimer_Tick;

            _callTimer.Start();
        }

        private void StopTimer()
        {
            if (_callTimer != null)
            {
                _callTimer.Tick -= CallTimer_Tick;
                _callTimer.Stop();
                _callTimer = null;
            }

            _duration = TimeSpan.Zero;
        }

        private void CallTimer_Tick(object? sender, object e)
        {
            _duration += TimeSpan.FromSeconds(1);

            TxtDuration.Text =
                $"LIVE {_duration:mm\\:ss}";
        }
                #region Button Events

        private async void BtnMic_Click(object sender, RoutedEventArgs e)
        {
            _micEnabled = !_micEnabled;

            IconMic.Glyph =
                _micEnabled
                    ? "\uE720"
                    : "\uE74F";

            BtnMic.Background =
                new SolidColorBrush(
                    _micEnabled
                        ? HexToColor("#00000066")
                        : HexToColor("#DC2626"));

            if (!_webViewReady || !_callActive)
                return;

            try
            {
                await VideoWebView.CoreWebView2.ExecuteScriptAsync(
                    $"window.toggleMic({(_micEnabled ? "true" : "false")});");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async void BtnCamera_Click(object sender, RoutedEventArgs e)
        {
            _cameraEnabled = !_cameraEnabled;

            IconCamera.Glyph =
                _cameraEnabled
                    ? "\uE714"
                    : "\uE8B9";

            BtnCamera.Background =
                new SolidColorBrush(
                    _cameraEnabled
                        ? HexToColor("#00000066")
                        : HexToColor("#DC2626"));

            if (!_webViewReady || !_callActive)
                return;

            try
            {
                await VideoWebView.CoreWebView2.ExecuteScriptAsync(
                    $"window.toggleCamera({(_cameraEnabled ? "true" : "false")});");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async void BtnSpeaker_Click(object sender, RoutedEventArgs e)
        {
            _speakerEnabled = !_speakerEnabled;

            IconSpeaker.Glyph =
                _speakerEnabled
                    ? "\uE767"
                    : "\uE74F";

            BtnSpeaker.Background =
                new SolidColorBrush(
                    _speakerEnabled
                        ? HexToColor("#00000066")
                        : HexToColor("#DC2626"));

            if (!_webViewReady)
                return;

            try
            {
                string script =
                    _speakerEnabled
                    ? @"
document.querySelectorAll('audio,video')
.forEach(x=>x.muted=false);"
                    : @"
document.querySelectorAll('audio,video')
.forEach(x=>x.muted=true);";

                await VideoWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async void BtnEndCall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentDialog dialog = new()
                {
                    Title = "Kết thúc cuộc gọi",

                    Content =
                        "Bạn có chắc chắn muốn kết thúc cuộc gọi?",

                    PrimaryButtonText = "Kết thúc",

                    CloseButtonText = "Huỷ",

                    DefaultButton = ContentDialogButton.Close,

                    XamlRoot = this.XamlRoot
                };

                if (await dialog.ShowAsync() !=
                    ContentDialogResult.Primary)
                    return;

                if (_webViewReady)
                {
                    try
                    {
                        await VideoWebView.CoreWebView2.ExecuteScriptAsync(
                            "window.leaveMeeting();");
                    }
                    catch
                    {
                    }
                }

                Cleanup();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void BtnMore_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyout flyout = new();

            MenuFlyoutItem shareScreen = new()
            {
                Text = "Chia sẻ màn hình"
            };

            MenuFlyoutItem stopShare = new()
            {
                Text = "Dừng chia sẻ"
            };

            MenuFlyoutItem reload = new()
            {
                Text = "Tải lại Video"
            };

            shareScreen.Click += async (_, __) =>
            {
                if (!_webViewReady)
                    return;

                try
                {
                    await VideoWebView.CoreWebView2.ExecuteScriptAsync(
                        "window.startShare();");
                }
                catch
                {
                }
            };

            stopShare.Click += async (_, __) =>
            {
                if (!_webViewReady)
                    return;

                try
                {
                    await VideoWebView.CoreWebView2.ExecuteScriptAsync(
                        "window.stopShare();");
                }
                catch
                {
                }
            };

            reload.Click += async (_, __) =>
            {
                if (_callActive)
                {
                    Cleanup();

                    await StartCallAsync();
                }
            };

            flyout.Items.Add(shareScreen);
            flyout.Items.Add(stopShare);
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(reload);

            flyout.ShowAt(BtnMore);
        }

        #endregion
                #region Cleanup

        public void Cleanup()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                StopTimer();

                if (_webViewReady)
                {
                    try
                    {
                        VideoWebView.CoreWebView2.WebMessageReceived -=
                            CoreWebView2_WebMessageReceived;

                        VideoWebView.CoreWebView2.PermissionRequested -=
                            CoreWebView2_PermissionRequested;

                        VideoWebView.CoreWebView2.ExecuteScriptAsync(
                            "if(window.leaveMeeting) window.leaveMeeting();");
                    }
                    catch
                    {
                    }

                    try
                    {
                        VideoWebView.NavigateToString(
@"<!DOCTYPE html>
<html>
<body style='background:#111827;'></body>
</html>");
                    }
                    catch
                    {
                    }
                }

                _callActive = false;
                _webViewReady = false;

                ResetUi();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void Dispose()
        {
            Cleanup();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Permission

        private async Task CheckPermissionsAsync()
        {
            try
            {
                MediaCaptureInitializationSettings settings = new()
                {
                    StreamingCaptureMode =
                        StreamingCaptureMode.AudioAndVideo
                };

                using MediaCapture capture = new();

                await capture.InitializeAsync(settings);
            }
            catch (UnauthorizedAccessException)
            {
                throw new Exception(
                    "Ứng dụng chưa được cấp quyền Camera hoặc Microphone.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        #endregion

        #region Dialog

        private async Task ShowError(
            string title,
            string message)
        {
            try
            {
                ContentDialog dialog = new()
                {
                    Title = title,

                    Content = message,

                    CloseButtonText = "Đóng",

                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch
            {
            }
        }

        #endregion

        #region Helper

        private static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", "");

            if (hex.Length == 8)
                hex = hex.Substring(2);

            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        #endregion
    }
}