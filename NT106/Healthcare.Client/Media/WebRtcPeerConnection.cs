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
            _peerConnection.LocalSdpReadytoSend += (SdpMessage message) =>
            {
                OnSignalingGenerated?.Invoke(message.Type.ToString().ToLower(), message.Content);
            };

            _peerConnection.CreateOffer();
        }

        public async Task HandleIncomingSignal(string type, string data)
        {
            switch (type.ToLower())
            {
                case "offer":
                    await _peerConnection.SetRemoteDescriptionAsync(new SdpMessage { Type = SdpMessageType.Offer, Content = data });
                    _peerConnection.LocalSdpReadytoSend += (SdpMessage answer) =>
                    {
                        OnSignalingGenerated?.Invoke("answer", answer.Content);
                    };
                    _peerConnection.CreateAnswer();
                    break;

                case "answer":
                    await _peerConnection.SetRemoteDescriptionAsync(new SdpMessage { Type = SdpMessageType.Answer, Content = data });
                    break;

                case "ice-candidate":
                    // Parse candidate JSON và truyền vào _peerConnection.AddIceCandidate(...)
                    break;
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
            _localVideoTrack?.Dispose();
            _videoSource?.Dispose();

            _localAudioTrack?.Dispose();
            _audioSource?.Dispose();

            _peerConnection?.Close();
            _peerConnection?.Dispose();
        }
    }
}