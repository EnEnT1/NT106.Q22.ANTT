using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Healthcare.Client.Communication;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Foundation;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.MixedReality.WebRTC;
using WinRT;

namespace Healthcare.Client.UI.Components
{
    public sealed partial class DeviceTestCard : UserControl
    {
        private WebRtcPeerConnection? _testRtc;
        private SoftwareBitmapSource _testVideoSource = new SoftwareBitmapSource();
        private AudioGraph? _audioGraph;
        private AudioDeviceInputNode? _deviceInputNode;
        private AudioFrameOutputNode? _frameOutputNode;
        private volatile bool _isTesting = false;
        private volatile bool _isRenderingFrame = false;
        private DateTime _lastVolumeUpdateTime = DateTime.MinValue;

        public DeviceTestCard()
        {
            this.InitializeComponent();
            this.Unloaded += (s, e) => StopDeviceTesting();
        }

        private async void BtnRunDeviceTest_Click(object sender, RoutedEventArgs e)
        {
            if (_isTesting)
            {
                StopDeviceTesting();
                VideoPreviewImage.Source = null;
                MicVolumeProgress.Value = 0;
                VolumeValueText.Text = "0%";
                DeviceTestLivePanel.Visibility = Visibility.Collapsed;
                DeviceTestStatusPanel.Visibility = Visibility.Collapsed;

                BtnTestText.Text = "Bat dau kiem tra thiet bi";
                BtnTestIcon.Glyph = "\uE722";
                BtnRunDeviceTest.Background = new SolidColorBrush(ParseColor("#EFF4FD"));
                BtnRunDeviceTest.Foreground = new SolidColorBrush(ParseColor("#2D6CDF"));
                return;
            }

            _isTesting = true;
            _frameCount = 0;

            // Re-create source so frames can be rendered
            _testVideoSource = new SoftwareBitmapSource();
            VideoPreviewImage.Source = _testVideoSource;

            DeviceTestStatusPanel.Visibility = Visibility.Visible;
            DeviceTestLivePanel.Visibility = Visibility.Visible;
            BtnRunDeviceTest.IsEnabled = false;
            BtnTestText.Text = "Dang ket noi...";
            BtnTestIcon.Glyph = "\uF12B";

            // Reset indicators
            CamStatusIndicator.Background = new SolidColorBrush(ParseColor("#F1F5F9"));
            CamStatusIcon.Glyph = "\uE722";
            CamStatusIcon.Foreground = new SolidColorBrush(ParseColor("#64748B"));
            CamStatusText.Text = "Camera: Dang ket noi...";

            MicStatusIndicator.Background = new SolidColorBrush(ParseColor("#F1F5F9"));
            MicStatusIcon.Glyph = "\uE720";
            MicStatusIcon.Foreground = new SolidColorBrush(ParseColor("#64748B"));
            MicStatusText.Text = "Micro: Dang ket noi...";

            MicVolumeProgress.Value = 0;
            VolumeValueText.Text = "0%";

            await Task.Delay(500);

            // 1. Test Camera
            bool camSuccess = false;
            string camError = string.Empty;
            try
            {
                _testRtc = new WebRtcPeerConnection();
                _testRtc.OnLocalFrameReady += OnTestLocalFrameReceived;
                await _testRtc.InitializeAsync();
                camSuccess = true;
            }
            catch (Exception ex)
            {
                camError = ex.Message;
                Debug.WriteLine($"[DeviceTestCard] Camera error: {ex.Message}");
                if (_testRtc != null)
                {
                    try { _testRtc.Dispose(); } catch { }
                    _testRtc = null;
                }
            }

            if (camSuccess)
            {
                CamStatusIndicator.Background = new SolidColorBrush(ParseColor("#ECFDF5"));
                CamStatusIcon.Glyph = "\uE8FB";
                CamStatusIcon.Foreground = new SolidColorBrush(ParseColor("#10B981"));
                CamStatusText.Text = "Camera: Ket noi tot (San sang)";
            }
            else
            {
                CamStatusIndicator.Background = new SolidColorBrush(ParseColor("#FEF2F2"));
                CamStatusIcon.Glyph = "\uEB90";
                CamStatusIcon.Foreground = new SolidColorBrush(ParseColor("#EF4444"));
                CamStatusText.Text = $"Camera: Khong ket noi duoc";
                VideoPreviewImage.Source = null;
            }

            // 2. Test Microphone
            bool micSuccess = false;
            string micError = string.Empty;
            try
            {
                var graphSettings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Communications);
                var createResult = await AudioGraph.CreateAsync(graphSettings);
                if (createResult.Status != AudioGraphCreationStatus.Success)
                    throw new Exception("Khong the tao AudioGraph: " + createResult.Status);

                _audioGraph = createResult.Graph;
                var inputResult = await _audioGraph.CreateDeviceInputNodeAsync(Windows.Media.Capture.MediaCategory.Communications);
                if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
                    throw new Exception("Khong the ket noi Micro: " + inputResult.Status);

                _deviceInputNode = inputResult.DeviceInputNode;
                _frameOutputNode = _audioGraph.CreateFrameOutputNode();
                _deviceInputNode.AddOutgoingConnection(_frameOutputNode);
                _audioGraph.QuantumStarted += AudioGraph_QuantumStarted;
                _audioGraph.Start();
                micSuccess = true;
            }
            catch (Exception ex)
            {
                micError = ex.Message;
                Debug.WriteLine($"[DeviceTestCard] Mic error: {ex.Message}");
                if (_audioGraph != null)
                {
                    try { _audioGraph.Dispose(); } catch { }
                    _audioGraph = null;
                }
                _deviceInputNode = null;
                _frameOutputNode = null;
            }

            if (micSuccess)
            {
                MicStatusIndicator.Background = new SolidColorBrush(ParseColor("#ECFDF5"));
                MicStatusIcon.Glyph = "\uE8FB";
                MicStatusIcon.Foreground = new SolidColorBrush(ParseColor("#10B981"));
                MicStatusText.Text = "Micro: Ket noi tot (San sang)";
            }
            else
            {
                MicStatusIndicator.Background = new SolidColorBrush(ParseColor("#FEF2F2"));
                MicStatusIcon.Glyph = "\uEB90";
                MicStatusIcon.Foreground = new SolidColorBrush(ParseColor("#EF4444"));
                MicStatusText.Text = $"Micro: Khong ket noi duoc";
            }

            BtnRunDeviceTest.IsEnabled = true;
            BtnTestText.Text = "Dung kiem tra";
            BtnTestIcon.Glyph = "\uE71A";
            BtnRunDeviceTest.Background = new SolidColorBrush(ParseColor("#FEF2F2"));
            BtnRunDeviceTest.Foreground = new SolidColorBrush(ParseColor("#EF4444"));
        }

        public void StopDeviceTesting()
        {
            _isTesting = false;

            if (_testRtc != null)
            {
                try
                {
                    _testRtc.OnLocalFrameReady -= OnTestLocalFrameReceived;
                    _testRtc.Dispose();
                }
                catch { }
                _testRtc = null;
            }

            if (_audioGraph != null)
            {
                try { _audioGraph.Stop(); _audioGraph.Dispose(); } catch { }
                _audioGraph = null;
            }

            _deviceInputNode = null;
            _frameOutputNode = null;
        }

        private volatile int _frameCount = 0;

        private void OnTestLocalFrameReceived(Argb32VideoFrame frame)
        {
            _frameCount++;
            if (_frameCount % 15 == 0)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (_isTesting)
                    {
                        CamStatusText.Text = $"Camera: Kết nối tốt (Đang nhận: {_frameCount} frames)";
                    }
                });
            }

            // Drop frames if previous frame is still rendering to avoid queue buildup
            if (_isRenderingFrame)
            {
                return;
            }

            var bitmap = ProcessFrameSync(frame);
            if (bitmap != null)
            {
                _isRenderingFrame = true;
                DispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        if (_isTesting)
                            await _testVideoSource.SetBitmapAsync(bitmap);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[DeviceTestCard] SetBitmapAsync error: {ex.Message}");
                    }
                    finally 
                    { 
                        bitmap.Dispose(); 
                        _isRenderingFrame = false;
                    }
                });
            }
        }

        private SoftwareBitmap? ProcessFrameSync(Argb32VideoFrame frame)
        {
            if (!_isTesting || frame.data == IntPtr.Zero || frame.width == 0 || frame.height == 0) return null;
            try
            {
                var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, (int)frame.width, (int)frame.height, BitmapAlphaMode.Premultiplied);
                using (var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
                using (var reference = buffer.CreateReference())
                {
                    unsafe
                    {
                        byte* dataPtr; uint capacity;
                        if (TryGetBuffer(reference, out dataPtr, out capacity))
                        {
                            long requiredSize = (long)frame.width * frame.height * 4;
                            if (capacity >= requiredSize)
                                System.Buffer.MemoryCopy((void*)frame.data, (void*)dataPtr, capacity, requiredSize);
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
                Debug.WriteLine($"[DeviceTestCard] ProcessFrameSync error: {ex.Message}");
                DispatcherQueue.TryEnqueue(() =>
                {
                    CamStatusText.Text = $"Camera Proc Exception: {ex.Message}";
                });
                return null;
            }
        }

        private void AudioGraph_QuantumStarted(AudioGraph sender, object args)
        {
            if (!_isTesting || _frameOutputNode == null) return;

            // Throttle volume update to at most once per 60 milliseconds to avoid clogging dispatcher
            if ((DateTime.Now - _lastVolumeUpdateTime).TotalMilliseconds < 60)
            {
                return;
            }
            _lastVolumeUpdateTime = DateTime.Now;

            try
            {
                using (Windows.Media.AudioFrame frame = _frameOutputNode.GetFrame())
                using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    unsafe
                    {
                        byte* dataIn; uint capacity;
                        if (TryGetBuffer(reference, out dataIn, out capacity))
                        {
                            if (capacity == 0) return;

                            float* floatData = (float*)dataIn;
                            uint sampleCount = capacity / sizeof(float);
                            float sum = 0;
                            for (uint i = 0; i < sampleCount; i++)
                            {
                                float s = floatData[i];
                                sum += s * s;
                            }
                            float rms = (float)Math.Sqrt(sum / sampleCount);
                            float volumePercent = Math.Min(100f, rms * 500f);

                            DispatcherQueue.TryEnqueue(() =>
                            {
                                if (_isTesting)
                                {
                                    MicVolumeProgress.Value = volumePercent;
                                    VolumeValueText.Text = $"{(int)volumePercent}%";
                                }
                            });
                        }
                        else
                        {
                            throw new Exception("COM interface IMemoryBufferByteAccess not supported on this reference.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    MicStatusText.Text = $"Mic Proc Exception: {ex.Message}";
                });
            }
        }

        private static Windows.UI.Color ParseColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
                return Windows.UI.Color.FromArgb(255,
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
            return Windows.UI.Color.FromArgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16));
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
