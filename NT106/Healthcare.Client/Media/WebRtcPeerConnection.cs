using Microsoft.MixedReality.WebRTC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Healthcare.Client.Communication
{
    /// <summary>
    /// Xử lý lõi WebRTC: Quản lý thiết bị, kết nối P2P và truyền nhận tín hiệu.
    /// Đã cập nhật tương thích 100% với Microsoft.MixedReality.WebRTC phiên bản 2.x
    /// </summary>
    public class WebRtcPeerConnection : IDisposable
    {
        private PeerConnection _peerConnection;

        //  Api  yêu cầu quản lý Source và Track tách biệt ---
        private DeviceAudioTrackSource _audioSource;
        private LocalAudioTrack _localAudioTrack;
        private Transceiver _audioTransceiver;

        private DeviceVideoTrackSource _videoSource;
        private LocalVideoTrack _localVideoTrack;
        private Transceiver _videoTransceiver;
        private bool _isRemoteDescriptionSet = false;
        private readonly List<IceCandidate> _queuedIceCandidates = new List<IceCandidate>();
        public delegate void Argb32VideoFrameDelegate(Argb32VideoFrame frame);

        public event Argb32VideoFrameDelegate OnLocalFrameReady;
        public event Argb32VideoFrameDelegate OnRemoteFrameReady;

        public event Action<string, string> OnSignalingGenerated;

        public WebRtcPeerConnection()
        {
            _peerConnection = new PeerConnection();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var config = new PeerConnectionConfiguration
                {
                    IceServers = new List<IceServer> {
                        new IceServer { Urls = { "stun:stun.l.google.com:19302" } }
                    }
                };

                await _peerConnection.InitializeAsync(config);

                _peerConnection.IceCandidateReadytoSend += (IceCandidate candidate) =>
                {
                    string json = $"{{\"candidate\":\"{candidate.Content}\",\"sdpMid\":\"{candidate.SdpMid}\",\"sdpMLineIndex\":{candidate.SdpMlineIndex}}}";
                    OnSignalingGenerated?.Invoke("ice-candidate", json);
                };

                _peerConnection.LocalSdpReadytoSend += (SdpMessage message) =>
                {
                    OnSignalingGenerated?.Invoke(message.Type.ToString().ToLower(), message.Content);
                };

                _peerConnection.VideoTrackAdded += (RemoteVideoTrack track) =>
                {
                    // Đổi sang Argb32 để xử lý ảnh màu trực tiếp
                    track.Argb32VideoFrameReady += (Argb32VideoFrame frame) =>
                    {
                        OnRemoteFrameReady?.Invoke(frame);
                    };
                };

                await SetupLocalMedia();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khởi tạo WebRTC: {ex.Message}");
            }
        }
        private async Task SetupLocalMedia()
        {
            _audioSource = await DeviceAudioTrackSource.CreateAsync();
            var audioConfig = new LocalAudioTrackInitConfig { trackName = "local_audio_track" };
            _localAudioTrack = LocalAudioTrack.CreateFromSource(_audioSource, audioConfig);
            _audioTransceiver = _peerConnection.AddTransceiver(MediaKind.Audio);
            _audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
            _audioTransceiver.LocalAudioTrack = _localAudioTrack;

            _videoSource = await DeviceVideoTrackSource.CreateAsync();
            var videoConfig = new LocalVideoTrackInitConfig { trackName = "local_video_track" };
            _localVideoTrack = LocalVideoTrack.CreateFromSource(_videoSource, videoConfig);

            // Đổi sang Argb32 cho local preview
            _localVideoTrack.Argb32VideoFrameReady += (Argb32VideoFrame frame) =>
            {
                OnLocalFrameReady?.Invoke(frame);
            };

            _videoTransceiver = _peerConnection.AddTransceiver(MediaKind.Video);
            _videoTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
            _videoTransceiver.LocalVideoTrack = _localVideoTrack;
        }

        public void StartCall()
        {
            _peerConnection.CreateOffer();
        }

        public async Task HandleIncomingSignal(string type, string data)
        {
            switch (type.ToLower())
            {
                case "offer":
                    await _peerConnection.SetRemoteDescriptionAsync(new SdpMessage { Type = SdpMessageType.Offer, Content = data });
                    _isRemoteDescriptionSet = true;
                    FlushQueuedIceCandidates();
                    _peerConnection.CreateAnswer();
                    break;

                case "answer":
                    await _peerConnection.SetRemoteDescriptionAsync(new SdpMessage { Type = SdpMessageType.Answer, Content = data });
                    _isRemoteDescriptionSet = true;
                    FlushQueuedIceCandidates();
                    break;

                case "ice-candidate":
                    try
                    {
                        var candidateInit = System.Text.Json.JsonSerializer.Deserialize<IceCandidateData>(data);
                        if (candidateInit != null)
                        {
                            var candidate = new IceCandidate
                            {
                                Content = candidateInit.candidate,
                                SdpMid = candidateInit.sdpMid,
                                SdpMlineIndex = candidateInit.sdpMLineIndex
                            };

                            lock (_queuedIceCandidates)
                            {
                                if (_isRemoteDescriptionSet)
                                {
                                    _peerConnection.AddIceCandidate(candidate);
                                    Debug.WriteLine($"[WebRTC] Added ICE candidate: {candidate.SdpMid}");
                                }
                                else
                                {
                                    _queuedIceCandidates.Add(candidate);
                                    Debug.WriteLine($"[WebRTC] Queued ICE candidate because remote description is not set yet.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Lỗi phân tích hoặc thêm ICE Candidate: {ex.Message}");
                    }
                    break;
            }
        }

        private void FlushQueuedIceCandidates()
        {
            lock (_queuedIceCandidates)
            {
                foreach (var candidate in _queuedIceCandidates)
                {
                    try
                    {
                        _peerConnection.AddIceCandidate(candidate);
                        Debug.WriteLine($"[WebRTC] Flushed queued ICE candidate: {candidate.SdpMid}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[WebRTC] Error adding flushed ICE candidate: {ex.Message}");
                    }
                }
                _queuedIceCandidates.Clear();
            }
        }

        public void ToggleMic(bool isMuted)
        {
            if (_localAudioTrack != null) _localAudioTrack.Enabled = !isMuted;
        }

        public void ToggleCamera(bool isOff)
        {
            if (_localVideoTrack != null) _localVideoTrack.Enabled = !isOff;
        }

        public void Dispose()
        {
            _isRemoteDescriptionSet = false;
            lock (_queuedIceCandidates)
            {
                _queuedIceCandidates.Clear();
            }

            _localVideoTrack?.Dispose();
            _videoSource?.Dispose();

            _localAudioTrack?.Dispose();
            _audioSource?.Dispose();

            _peerConnection?.Close();
            _peerConnection?.Dispose();
        }
    }

    public class IceCandidateData
    {
        public string candidate { get; set; } = string.Empty;
        public string sdpMid { get; set; } = string.Empty;
        public int sdpMLineIndex { get; set; }
    }
}