using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Healthcare.Client.Communication;
using Healthcare.Client.Models.Communication;
using Healthcare.Client.SupabaseIntegration;
using Healthcare.Client.Helpers;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using Postgrest;
using Microsoft.MixedReality.WebRTC;
using Windows.Media.Capture;
using WinRT;
using Windows.Foundation;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class VideoCallControl : UserControl
    {
        public event EventHandler? CallStarted;
        public event EventHandler? CallEnded;

        private string _appointmentId = string.Empty;
        private string _roomCode = string.Empty;
        private string _targetId = string.Empty;
        private string _currentUserId = string.Empty;

        private WebRtcPeerConnection? _rtc;
        private RealtimeChannel? _signalChannel;

        private SoftwareBitmapSource _localSource = new SoftwareBitmapSource();
        private SoftwareBitmapSource _remoteSource = new SoftwareBitmapSource();

        private DispatcherTimer? _callTimer;
        private TimeSpan _callDuration = TimeSpan.Zero;

        private bool _isCallActive = false;
        private bool _isMicOn = true;
        private bool _isCameraOn = true;
        private bool _isSpeakerOn = true;
        private bool _isDisposed = false;
        private volatile bool _isLocalRendering = false;
        private volatile bool _isRemoteRendering = false;

        public VideoCallControl()
        {
            try
            {
                this.InitializeComponent();
                LocalVideo.Source = _localSource;
                RemoteVideo.Source = _remoteSource;
                this.Unloaded += OnUnloaded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VideoCallControl] Constructor Error: {ex.Message}");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Cleanup();

        public async Task InitializeAsync(string appointmentId, string targetId, string roomCode)
        {
            try
            {
                _appointmentId = appointmentId;
                _targetId = targetId;
                _roomCode = roomCode;
                _currentUserId = SessionStorage.CurrentUser?.Id ?? string.Empty;
                _isDisposed = false;

                ResetUiToWaitingState();

                // Yêu cầu quyền truy cập Camera và Micro trước khi bắt đầu kết nối
                await RequestCameraAndMicrophonePermissionsAsync();

                // Dọn dẹp cũ nến có
                if (_rtc != null) { _rtc.Dispose(); }

                _rtc = new WebRtcPeerConnection();
                _rtc.OnLocalFrameReady += OnLocalFrameReceived;
                _rtc.OnRemoteFrameReady += OnRemoteFrameReceived;
                _rtc.OnSignalingGenerated += OnLocalSignalingGenerated;

                await _rtc.InitializeAsync();
                await SubscribeToSignalsAsync();
                Debug.WriteLine("[VideoCallControl] Initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VideoCallControl] InitializeAsync Error: {ex.Message}");
                await ShowDetailedErrorAsync("Lỗi khởi tạo cuộc gọi", ex.Message);
            }
        }

        private async Task SubscribeToSignalsAsync()
        {
            try 
            {
                var client = SupabaseManager.Instance.Client;
                _signalChannel = client.Realtime.Channel("webrtc_" + _roomCode, "public", "webrtc_signals");

                _signalChannel.AddPostgresChangeHandler(
                    PostgresChangesOptions.ListenType.Inserts,
                    (sender, change) =>
                    {
                        if (_isDisposed) return;
                        var model = change.Model<WebrtcSignal>();
                        if (model != null && model.RoomCode == _roomCode && model.SenderId != _currentUserId)
                        {
                            DispatcherQueue.TryEnqueue(async () => {
                                if (_rtc != null)
                                {
                                    await _rtc.HandleIncomingSignal(model.SignalType, model.Payload);
                                    if (model.SignalType.ToLower() == "offer" && !_isCallActive) SetCallActive(true);
                                }
                            });
                        }
                    });

                await _signalChannel.Subscribe();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VideoCallControl] Subscribe Error: {ex.Message}");
            }
        }

        private async void OnLocalSignalingGenerated(string type, string data)
        {
            if (_isDisposed) return;
            try
            {
                var signal = new WebrtcSignal { RoomCode = _roomCode, SenderId = _currentUserId, SignalType = type, Payload = data, CreatedAt = DateTime.UtcNow };
                await SupabaseManager.Instance.Client.From<WebrtcSignal>().Insert(signal);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VideoCallControl] Signal Send Error: {ex.Message}");
            }
        }

        private void OnLocalFrameReceived(Argb32VideoFrame frame)
        {
            if (_isDisposed || _isLocalRendering) return;
            var bitmap = ProcessFrameSync(frame);
            if (bitmap != null)
            {
                _isLocalRendering = true;
                DispatcherQueue.TryEnqueue(async () => {
                    try { if (!_isDisposed) await _localSource.SetBitmapAsync(bitmap); } catch { }
                    finally 
                    { 
                        bitmap.Dispose(); 
                        _isLocalRendering = false;
                    }
                });
            }
        }

        private void OnRemoteFrameReceived(Argb32VideoFrame frame)
        {
            if (_isDisposed || _isRemoteRendering) return;
            var bitmap = ProcessFrameSync(frame);
            if (bitmap != null)
            {
                _isRemoteRendering = true;
                DispatcherQueue.TryEnqueue(async () => {
                    try 
                    { 
                        if (_isDisposed) return;
                        if (!_isCallActive) SetCallActive(true);
                        await _remoteSource.SetBitmapAsync(bitmap); 
                    } 
                    catch { }
                    finally 
                    { 
                        bitmap.Dispose(); 
                        _isRemoteRendering = false;
                    }
                });
            }
        }

        private SoftwareBitmap? ProcessFrameSync(Argb32VideoFrame frame)
        {
            if (_isDisposed || frame.data == IntPtr.Zero || frame.width == 0 || frame.height == 0) return null;

            try
            {
                // Sử dụng định dạng mặc định cho WinUI 3 (Bgra8 Premultiplied)
                var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, (int)frame.width, (int)frame.height, BitmapAlphaMode.Premultiplied);
                
                using (var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
                using (var reference = buffer.CreateReference())
                {
                    unsafe {
                        byte* dataPtr; uint capacity;
                        if (TryGetBuffer(reference, out dataPtr, out capacity))
                        {
                            long requiredSize = (long)frame.width * frame.height * 4;
                            if (capacity >= requiredSize)
                            {
                                System.Buffer.MemoryCopy((void*)frame.data, (void*)dataPtr, capacity, requiredSize);
                            }
                            else
                            {
                                Debug.WriteLine($"[VideoCallControl] Buffer capacity too small: {capacity} < {requiredSize}");
                                softwareBitmap.Dispose();
                                return null;
                            }
                        }
                        else
                        {
                            throw new Exception("COM interface IMemoryBufferByteAccess not supported on this reference.");
                        }
                    }
                }
                return softwareBitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VideoCallControl] ProcessFrameSync Exception: {ex.Message}");
                return null;
            }
        }

        public void Cleanup()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                StopTimer();
                _signalChannel?.Unsubscribe();
                _signalChannel = null;
                
                _rtc?.Dispose();
                _rtc = null;

                Debug.WriteLine("[VideoCallControl] Cleanup completed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VideoCallControl] Cleanup Error: {ex.Message}");
            }
        }

        public async Task StartCallAsync()
        {
            if (_rtc == null)
            {
                Debug.WriteLine("[VideoCallControl] Cannot start call because RTC is null.");
                return;
            }

            try
            {
                SetConnectingState(true);

                Debug.WriteLine("[VideoCallControl] Starting call...");
                _rtc.StartCall();

                TxtWaitingStatus.Text = "Đang gọi bệnh nhân...";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VideoCallControl] StartCallAsync Error: {ex.Message}");
                await ShowDetailedErrorAsync("Lỗi bắt đầu cuộc gọi", ex.Message);
            }
        }

        private void ResetUiToWaitingState()
        {
            _isCallActive = false; _isMicOn = true; _isCameraOn = true; _isSpeakerOn = true;
            WaitingPlaceholder.Visibility = Visibility.Visible;
            LiveBadge.Visibility = Visibility.Collapsed;
            QualityBadge.Visibility = Visibility.Collapsed;
            TxtWaitingStatus.Text = "Đang chờ kết nối...";
            TxtDuration.Text = "LIVE 00:00";
            TxtQuality.Text = "HD Quality";
            IconMic.Glyph = "\uE720"; IconCamera.Glyph = "\uE714"; IconSpeaker.Glyph = "\uE767";
            BtnMic.Background = new SolidColorBrush(HexToColor("#00000066"));
            BtnCamera.Background = new SolidColorBrush(HexToColor("#00000066"));
            BtnSpeaker.Background = new SolidColorBrush(HexToColor("#00000066"));
            PipContainer.Opacity = 1.0; ConnectingRing.IsActive = false;
        }

        private void SetConnectingState(bool connecting)
        {
            ConnectingRing.IsActive = connecting;
            TxtWaitingStatus.Text = connecting ? "Đang kết nối..." : "Đang chờ kết nối...";
        }

        private void SetCallActive(bool active)
        {
            _isCallActive = active;
            WaitingPlaceholder.Visibility = active ? Visibility.Collapsed : Visibility.Visible;
            LiveBadge.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            QualityBadge.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            if (active) { StartTimer(); CallStarted?.Invoke(this, EventArgs.Empty); }
            else { CallEnded?.Invoke(this, EventArgs.Empty); }
        }

        private void StartTimer()
        {
            StopTimer(); _callDuration = TimeSpan.Zero;
            _callTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _callTimer.Tick += (_, _) => {
                _callDuration = _callDuration.Add(TimeSpan.FromSeconds(1));
                TxtDuration.Text = $"LIVE {_callDuration:mm\\:ss}";
            };
            _callTimer.Start();
        }

        private void StopTimer() { _callTimer?.Stop(); _callTimer = null; }

        private void BtnMic_Click(object sender, RoutedEventArgs e)
        {
            _isMicOn = !_isMicOn;
            IconMic.Glyph = _isMicOn ? "\uE720" : "\uE71A";
            BtnMic.Background = new SolidColorBrush(_isMicOn ? HexToColor("#00000066") : HexToColor("#DC2626"));
            _rtc?.ToggleMic(!_isMicOn);
        }

        private void BtnCamera_Click(object sender, RoutedEventArgs e)
        {
            _isCameraOn = !_isCameraOn;
            IconCamera.Glyph = _isCameraOn ? "\uE714" : "\uE8BA";
            BtnCamera.Background = new SolidColorBrush(_isCameraOn ? HexToColor("#00000066") : HexToColor("#DC2626"));
            PipContainer.Opacity = _isCameraOn ? 1.0 : 0.3;
            _rtc?.ToggleCamera(!_isCameraOn);
        }

        private void BtnSpeaker_Click(object sender, RoutedEventArgs e)
        {
            _isSpeakerOn = !_isSpeakerOn;
            IconSpeaker.Glyph = _isSpeakerOn ? "\uE767" : "\uE74F";
            BtnSpeaker.Background = new SolidColorBrush(_isSpeakerOn ? HexToColor("#00000066") : HexToColor("#DC2626"));
        }

        private async void BtnEndCall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ContentDialog { Title = "Kết thúc cuộc gọi", Content = "Bạn có chắc muốn kết thúc cuộc gọi video?", PrimaryButtonText = "Kết thúc", CloseButtonText = "Huỷ", DefaultButton = ContentDialogButton.Close, XamlRoot = this.XamlRoot };
                if (await dialog.ShowAsync() == ContentDialogResult.Primary) { Cleanup(); SetCallActive(false); ResetUiToWaitingState(); }
            }
            catch { }
        }

        private void BtnMore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var flyout = new MenuFlyout();
                flyout.Items.Add(new MenuFlyoutItem { Text = "Chia sẻ màn hình" });
                flyout.Items.Add(new MenuFlyoutItem { Text = "Cài đặt chất lượng" });
                flyout.ShowAt(BtnMore);
            }
            catch { }
        }

        private async Task RequestCameraAndMicrophonePermissionsAsync()
        {
            try
            {
                var settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo
                };
                using (var mediaCapture = new MediaCapture())
                {
                    await mediaCapture.InitializeAsync(settings);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception("Ứng dụng chưa được cấp quyền truy cập Camera hoặc Micro. Vui lòng bật quyền truy cập trong Settings > Privacy của Windows.", ex);
            }
            catch (Exception ex)
            {
                // Bỏ qua lỗi nếu không có phần cứng thực tế (chỉ kiểm tra quyền truy cập)
                Debug.WriteLine($"[Permission Request] Thiết bị không sẵn sàng: {ex.Message}");
            }
        }

        private async Task ShowDetailedErrorAsync(string title, string message)
        {
            try
            {
                var d = new ContentDialog { Title = title, Content = $"Chi tiết: {message}\n\nVui lòng kiểm tra quyền truy cập Camera/Micro và kết nối mạng.", CloseButtonText = "Đóng", XamlRoot = this.XamlRoot };
                await d.ShowAsync();
            }
            catch { }
        }

        private static Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 8) hex = hex.Substring(2); 
            return Color.FromArgb(255, Convert.ToByte(hex.Substring(0, 2), 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
        }

        private static unsafe bool TryGetBuffer(IMemoryBufferReference reference, out byte* buffer, out uint capacity)
        {
            buffer = null;
            capacity = 0;
            try
            {
                IntPtr pUnk = ((WinRT.IWinRTObject)reference).NativeObject.ThisPtr;
                if (pUnk == IntPtr.Zero) return false;

                Guid iid = new Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D");
                int hr = Marshal.QueryInterface(pUnk, ref iid, out IntPtr pByteAccess);
                if (hr == 0 && pByteAccess != IntPtr.Zero)
                {
                    try
                    {
                        IntPtr* vtable = *(IntPtr**)pByteAccess;
                        IntPtr getBufferPtr = vtable[3];
                        delegate* unmanaged[Stdcall]<IntPtr, byte**, uint*, int> getBuffer = 
                            (delegate* unmanaged[Stdcall]<IntPtr, byte**, uint*, int>)getBufferPtr;
                        
                        byte* pBuffer = null;
                        uint size = 0;
                        int result = getBuffer(pByteAccess, &pBuffer, &size);
                        if (result == 0)
                        {
                            buffer = pBuffer;
                            capacity = size;
                            return true;
                        }
                    }
                    finally
                    {
                        Marshal.Release(pByteAccess);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TryGetBuffer] Error: {ex.Message}");
            }
            return false;
        }
    }
}