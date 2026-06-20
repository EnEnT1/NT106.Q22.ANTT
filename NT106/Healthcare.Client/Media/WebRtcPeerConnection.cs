using Microsoft.MixedReality.WebRTC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace Healthcare.Client.Communication
{
    public class WebRtcPeerConnection : IDisposable
    {
        private PeerConnection _peerConnection = new PeerConnection();

        private DeviceAudioTrackSource? _audioSource;
        private LocalAudioTrack? _localAudioTrack;
        private Transceiver? _audioTransceiver;

        private DeviceVideoTrackSource? _videoSource;
        private LocalVideoTrack? _localVideoTrack;
        private Transceiver? _videoTransceiver;

        private bool _isRemoteDescriptionSet = false;
        private readonly List<IceCandidate> _queuedIceCandidates = new List<IceCandidate>();

        public delegate void Argb32VideoFrameDelegate(Argb32VideoFrame frame);

        public event Argb32VideoFrameDelegate? OnLocalFrameReady;
        public event Argb32VideoFrameDelegate? OnRemoteFrameReady;
        public event Action<string, string>? OnSignalingGenerated;

        public async Task InitializeAsync()
        {
            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer>
                {
                    new IceServer
                    {
                        Urls = { "stun:stun.l.google.com:19302" }
                    }
                }
            };

            await _peerConnection.InitializeAsync(config);

            _peerConnection.IceCandidateReadytoSend += candidate =>
            {
                var json = JsonSerializer.Serialize(new IceCandidateData
                {
                    candidate = candidate.Content,
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMlineIndex
                });

                Debug.WriteLine("[WebRTC] Send ICE candidate");
                OnSignalingGenerated?.Invoke("ice-candidate", json);
            };

            _peerConnection.LocalSdpReadytoSend += message =>
            {
                Debug.WriteLine($"[WebRTC] Send SDP: {message.Type}");
                OnSignalingGenerated?.Invoke(message.Type.ToString().ToLower(), message.Content);
            };

            _peerConnection.VideoTrackAdded += track =>
            {
                Debug.WriteLine("[WebRTC] Remote video track added");

                track.Argb32VideoFrameReady += frame =>
                {
                    OnRemoteFrameReady?.Invoke(frame);
                };
            };

            await SetupLocalMediaAsync();

            Debug.WriteLine("[WebRTC] Initialized");
        }

        private async Task SetupLocalMediaAsync()
        {
            _audioSource = await DeviceAudioTrackSource.CreateAsync();

            _localAudioTrack = LocalAudioTrack.CreateFromSource(
                _audioSource,
                new LocalAudioTrackInitConfig
                {
                    trackName = "local_audio_track"
                });

            _audioTransceiver = _peerConnection.AddTransceiver(MediaKind.Audio);
            _audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
            _audioTransceiver.LocalAudioTrack = _localAudioTrack;

            _videoSource = await DeviceVideoTrackSource.CreateAsync();

            _videoSource.Argb32VideoFrameReady += frame =>
            {
                OnLocalFrameReady?.Invoke(frame);
            };

            _localVideoTrack = LocalVideoTrack.CreateFromSource(
                _videoSource,
                new LocalVideoTrackInitConfig
                {
                    trackName = "local_video_track"
                });

            _videoTransceiver = _peerConnection.AddTransceiver(MediaKind.Video);
            _videoTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
            _videoTransceiver.LocalVideoTrack = _localVideoTrack;

            Debug.WriteLine("[WebRTC] Local camera and microphone ready");
        }

        public void StartCall()
        {
            Debug.WriteLine("[WebRTC] Create offer");
            _peerConnection.CreateOffer();
        }

        public async Task HandleIncomingSignal(string type, string data)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(data))
            {
                return;
            }

            try
            {
                switch (type.ToLower())
                {
                    case "offer":
                        Debug.WriteLine("[WebRTC] Receive offer");

                        await _peerConnection.SetRemoteDescriptionAsync(new SdpMessage
                        {
                            Type = SdpMessageType.Offer,
                            Content = data
                        });

                        _isRemoteDescriptionSet = true;
                        FlushQueuedIceCandidates();

                        Debug.WriteLine("[WebRTC] Create answer");
                        _peerConnection.CreateAnswer();
                        break;

                    case "answer":
                        Debug.WriteLine("[WebRTC] Receive answer");

                        await _peerConnection.SetRemoteDescriptionAsync(new SdpMessage
                        {
                            Type = SdpMessageType.Answer,
                            Content = data
                        });

                        _isRemoteDescriptionSet = true;
                        FlushQueuedIceCandidates();
                        break;

                    case "ice-candidate":
                        Debug.WriteLine("[WebRTC] Receive ICE candidate");

                        var candidateData = JsonSerializer.Deserialize<IceCandidateData>(data);

                        if (candidateData == null || string.IsNullOrWhiteSpace(candidateData.candidate))
                        {
                            Debug.WriteLine("[WebRTC] Invalid ICE candidate");
                            return;
                        }

                        var candidate = new IceCandidate
                        {
                            Content = candidateData.candidate,
                            SdpMid = candidateData.sdpMid,
                            SdpMlineIndex = candidateData.sdpMLineIndex
                        };

                        lock (_queuedIceCandidates)
                        {
                            if (_isRemoteDescriptionSet)
                            {
                                _peerConnection.AddIceCandidate(candidate);
                                Debug.WriteLine("[WebRTC] ICE candidate added");
                            }
                            else
                            {
                                _queuedIceCandidates.Add(candidate);
                                Debug.WriteLine("[WebRTC] ICE candidate queued");
                            }
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebRTC] HandleIncomingSignal error: {ex.Message}");
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
                        Debug.WriteLine("[WebRTC] Queued ICE flushed");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[WebRTC] Flush ICE error: {ex.Message}");
                    }
                }

                _queuedIceCandidates.Clear();
            }
        }

        public void ToggleMic(bool isMuted)
        {
            if (_localAudioTrack != null)
            {
                _localAudioTrack.Enabled = !isMuted;
            }
        }

        public void ToggleCamera(bool isOff)
        {
            if (_localVideoTrack != null)
            {
                _localVideoTrack.Enabled = !isOff;
            }
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