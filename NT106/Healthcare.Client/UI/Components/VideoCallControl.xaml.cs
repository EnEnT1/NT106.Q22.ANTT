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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
        public event EventHandler<ChatMessageEventArgs>? ChatMessageReceived;

        #endregion

        #region Daily

        // Subdomain Daily.co của bạn
        private const string DailySubdomain = "healthcare-nt106";

        // Lấy từ: dashboard.daily.co → Developers → API Keys
        private const string DailyApiKey = "ad487288c21d60c2528ed756b6f2bad1d634e48fe0231ed9897ad361e1db6f69";

        private static readonly HttpClient _dailyHttpClient = new()
        {
            BaseAddress = new Uri("https://api.daily.co/v1/")
        };

        private string _activeRoomUrl = "";
        private string _activeUserName = "";

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

                var settings = VideoWebView.CoreWebView2.Settings;
                settings.IsWebMessageEnabled = true;
                settings.IsScriptEnabled = true;
                settings.AreDefaultScriptDialogsEnabled = true;

                // Map virtual host so the page has a real origin (not null).
                // Daily.co's iframe needs a real origin to request camera/mic permission.
                try
                {
                    VideoWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "videocall.local",
                        System.IO.Path.GetTempPath(),
                        CoreWebView2HostResourceAccessKind.Allow);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[VideoCall] VirtualHost mapping failed (non-fatal): {ex.Message}");
                }

                VideoWebView.CoreWebView2.PermissionRequested +=
                    CoreWebView2_PermissionRequested;

                VideoWebView.CoreWebView2.WebMessageReceived +=
                    CoreWebView2_WebMessageReceived;

                // Khi Daily.co page load xong → hiện UI call và kích hoạt timer
                VideoWebView.CoreWebView2.NavigationCompleted +=
                    CoreWebView2_NavigationCompleted;

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
            // Grant all media permissions so Daily.co iframe can access camera and mic.
            // This covers both the top-level page and cross-origin Daily.co iframes.
            switch (args.PermissionKind)
            {
                case CoreWebView2PermissionKind.Camera:
                case CoreWebView2PermissionKind.Microphone:
                case CoreWebView2PermissionKind.Geolocation:
                    args.State = CoreWebView2PermissionState.Allow;
                    break;
            }

            Debug.WriteLine($"[VideoCall] Permission requested: {args.PermissionKind} → {args.State}");
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

                // Tự động tạo room nếu chưa tồn tại
                await EnsureRoomExistsAsync(roomName);

                string roomUrl = $"https://{DailySubdomain}.daily.co/{roomName}";
                string userName = Uri.EscapeDataString(_displayName);

                _activeRoomUrl = roomUrl;
                _activeUserName = userName;

                // Tạo file HTML trong temp folder
                string tempHtmlPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "index.html");
                string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <style>
        html, body, #call {{
            width: 100%;
            height: 100%;
            margin: 0;
            padding: 0;
            overflow: hidden;
            background-color: #111827;
        }}
    </style>
</head>
<body>
    <div id='call'></div>
    <script src='https://unpkg.com/@daily-co/daily-js'></script>
    <script>
        let callFrame = null;
        
        function startCall(roomUrl, userName) {{
            callFrame = Daily.createFrame(document.getElementById('call'), {{
                iframeStyle: {{
                    width: '100%',
                    height: '100%',
                    border: '0'
                }},
                showLeaveButton: false,
                showFullscreenButton: false,
                showChat: false, // Hide Daily prebuilt chat UI to use our custom ChatControl
            }});
            
            callFrame.on('joined-meeting', () => {{
                chrome.webview.postMessage(JSON.stringify({{ type: 'joined' }}));
            }});
            
            callFrame.on('left-meeting', () => {{
                chrome.webview.postMessage(JSON.stringify({{ type: 'left' }}));
            }});
            
            callFrame.on('participant-joined', () => {{
                chrome.webview.postMessage(JSON.stringify({{ type: 'participant-joined' }}));
            }});
            
            callFrame.on('participant-left', () => {{
                chrome.webview.postMessage(JSON.stringify({{ type: 'participant-left' }}));
            }});
            
            callFrame.on('chat-message', (e) => {{
                chrome.webview.postMessage(JSON.stringify({{
                    type: 'chat-message',
                    text: e.message.text,
                    senderId: e.message.senderId,
                    senderName: e.message.senderName || 'Đối phương',
                    local: e.message.local
                }}));
            }});
            
            callFrame.on('error', (e) => {{
                chrome.webview.postMessage(JSON.stringify({{ type: 'error', error: e.errorMsg }}));
            }});
            
            callFrame.join({{
                url: roomUrl,
                userName: userName
            }});
        }}
        
        window.sendChatMessage = function(text) {{
            if (callFrame) {{
                callFrame.sendChatMessage(text);
            }}
        }};
        
        window.toggleMic = function(enable) {{
            if (callFrame) callFrame.setLocalAudio(enable);
        }};
        
        window.toggleCamera = function(enable) {{
            if (callFrame) callFrame.setLocalVideo(enable);
        }};
        
        window.leaveMeeting = function() {{
            if (callFrame) callFrame.leave();
        }};
    </script>
</body>
</html>";

                await System.IO.File.WriteAllTextAsync(tempHtmlPath, html, Encoding.UTF8);

                // Navigate WebView2
                VideoWebView.CoreWebView2.Navigate("http://videocall.local/index.html");

                WaitingPlaceholder.Visibility = Visibility.Collapsed;
                VideoWebView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                SetConnecting(false);
                Debug.WriteLine(ex);
                await ShowError("Không thể bắt đầu cuộc gọi", ex.Message);
            }
        }

        private void CoreWebView2_WebMessageReceived(
            CoreWebView2 sender,
            CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string rawJson = args.TryGetWebMessageAsString();
                Debug.WriteLine($"[VideoCall] WebMessage: {rawJson}");

                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp)) return;

                string type = typeProp.GetString() ?? "";

                DispatcherQueue.TryEnqueue(() =>
                {
                    switch (type)
                    {
                        case "joined":
                            SetConnecting(false);
                            SetCallActive(true);
                            break;

                        case "left":
                            StopTimer();
                            _callActive = false;
                            VideoWebView.Visibility = Visibility.Collapsed;
                            WaitingPlaceholder.Visibility = Visibility.Visible;
                            TxtWaitingStatus.Text = "Cuộc gọi đã kết thúc";
                            CallEnded?.Invoke(this, EventArgs.Empty);
                            break;

                        case "participant-joined":
                            TxtWaitingStatus.Text = "Đã có người tham gia";
                            break;

                        case "participant-left":
                            TxtWaitingStatus.Text = "Đối phương đã rời cuộc gọi";
                            break;

                        case "chat-message":
                            if (root.TryGetProperty("text", out var textProp))
                            {
                                string text = textProp.GetString() ?? "";
                                string senderName = root.TryGetProperty("senderName", out var senderProp) ? (senderProp.GetString() ?? "") : "Đối phương";
                                bool isLocal = root.TryGetProperty("local", out var localProp) && localProp.GetBoolean();

                                ChatMessageReceived?.Invoke(this, new ChatMessageEventArgs
                                {
                                    Text = text,
                                    SenderName = senderName,
                                    IsLocal = isLocal
                                });
                            }
                            break;

                        case "error":
                            string error = root.TryGetProperty("error", out var errProp) ? (errProp.GetString() ?? "") : "Có lỗi xảy ra";
                            Debug.WriteLine($"[VideoCall] Daily.co Error: {error}");
                            TxtWaitingStatus.Text = $"Lỗi: {error}";
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void CoreWebView2_NavigationCompleted(
            CoreWebView2 sender,
            CoreWebView2NavigationCompletedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                if (args.IsSuccess)
                {
                    SetConnecting(false);
                    // Bắt đầu cuộc gọi bằng cách gọi hàm JS
                    await VideoWebView.CoreWebView2.ExecuteScriptAsync($"startCall('{_activeRoomUrl}', '{Uri.UnescapeDataString(_activeUserName)}');");
                    Debug.WriteLine("[VideoCall] WebView2 wrapper loaded successfully.");
                }
                else
                {
                    Debug.WriteLine($"[VideoCall] Navigation failed: {args.WebErrorStatus}");
                    TxtWaitingStatus.Text = "Không thể tải trang cuộc gọi";
                }
            });
        }

        public async Task SendChatMessageAsync(string text)
        {
            if (!_webViewReady || !_callActive) return;
            try
            {
                string safeText = Uri.EscapeDataString(text);
                await VideoWebView.CoreWebView2.ExecuteScriptAsync($"if(window.sendChatMessage) window.sendChatMessage(decodeURIComponent('{safeText}'));");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VideoCall] SendChatMessageAsync error: {ex.Message}");
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

        /// <summary>
        /// Gọi Daily.co REST API để tạo room nếu chưa tồn tại.
        /// Nếu room đã tồn tại, API trả về thông tin room bình thường (không lỗi).
        /// </summary>
        private static async Task EnsureRoomExistsAsync(string roomName)
        {
            try
            {
                if (string.IsNullOrEmpty(DailyApiKey) ||
                    DailyApiKey == "PASTE_YOUR_DAILY_API_KEY_HERE")
                {
                    Debug.WriteLine("[VideoCall] Daily API key chưa được cấu hình — bỏ qua tạo room tự động.");
                    return;
                }

                // Kiểm tra room đã tồn tại chưa (GET /v1/rooms/{name})
                using var getReq = new HttpRequestMessage(
                    HttpMethod.Get, $"rooms/{roomName}");
                getReq.Headers.Add("Authorization", $"Bearer {DailyApiKey}");

                var getResp = await _dailyHttpClient.SendAsync(getReq);

                if (getResp.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[VideoCall] Room '{roomName}' đã tồn tại.");
                    return;
                }

                // Room chưa tồn tại → tạo mới (POST /v1/rooms)
                var body = JsonSerializer.Serialize(new
                {
                    name       = roomName,
                    privacy    = "public",
                    properties = new
                    {
                        // Phòng tự huỷ sau 4 giờ
                        exp = DateTimeOffset.UtcNow.AddHours(4).ToUnixTimeSeconds()
                    }
                });

                using var createReq = new HttpRequestMessage(HttpMethod.Post, "rooms")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                createReq.Headers.Add("Authorization", $"Bearer {DailyApiKey}");

                var createResp = await _dailyHttpClient.SendAsync(createReq);
                var respText   = await createResp.Content.ReadAsStringAsync();

                if (createResp.IsSuccessStatusCode)
                    Debug.WriteLine($"[VideoCall] Room '{roomName}' tạo thành công.");
                else
                    Debug.WriteLine($"[VideoCall] Tạo room thất bại: {respText}");
            }
            catch (Exception ex)
            {
                // Không throw — nếu API lỗi, thử join trực tiếp
                Debug.WriteLine($"[VideoCall] EnsureRoomExistsAsync error (non-fatal): {ex.Message}");
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

    public class ChatMessageEventArgs : EventArgs
    {
        public string Text { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public bool IsLocal { get; set; }
    }
}