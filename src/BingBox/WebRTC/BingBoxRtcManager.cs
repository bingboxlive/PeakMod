using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using BingBox.Utils;
using UnityEngine;
using Newtonsoft.Json;
using BingBox.Network;

namespace BingBox.WebRTC
{
    public class BingBoxRtcManager : MonoBehaviour
    {
        private RTCPeerConnection? _pc;
        private BingBoxWebClient _client = null!;
        private readonly List<RTCIceCandidateInit> _pendingCandidates = new List<RTCIceCandidateInit>();
        public BingBoxAudioSink AudioSink { get; } = new BingBoxAudioSink();

        public void Initialize(BingBoxWebClient client)
        {
            _client = client;
        }

        public void JoinStream()
        {
            if (_client == null)
            {
                Plugin.Log.LogError("[RtcManager] WebClient not assigned.");
                return;
            }
            // Use an anonymous object for simpler sending if desired, or just raw string
            _client.SendJson("{\"type\": \"JOIN_STREAM\"}");
        }

        public event Action<BingBoxTrackInfo>? OnTrackUpdate;
        public event Action<List<BingBoxTrackInfo>>? OnQueueUpdate;
        public event Action<string>? OnQueueModeUpdate;

        public BingBoxTrackInfo CurrentTrackInfo { get; private set; } = new BingBoxTrackInfo();
        public List<BingBoxTrackInfo> CurrentQueue { get; private set; } = new List<BingBoxTrackInfo>();
        public string CurrentQueueMode { get; private set; } = "classic";

        public async Task HandleSignalingMessage(string json)
        {
            try
            {
                // First pass: Deserialize as a generic wrapper to check type.
                // We use partial deserialization logic or just check the type field first.
                // For efficiency/simplicity with Newtonsoft, we can deserialize to a common wrapper
                // that has optional fields, or dynamic.
                // Given our Models, SignalingMessage covers internal signaling.
                // SignalUpdateMessage covers 'update'.
                // Since they are distinct structures, we can do a quick check on "type" or try/catch.

                // Fast path: check type string roughly, or just deserialize to a JObject if we wanted.
                // But efficient path: Deserialize to a structural subset.

                // Let's assume standard structure: { type: "..." }
                // We can use a lightweight struct or JObject or dynamic.

                // Simplest: Check type via Newtonsoft JObject (robust)
                // var jobj = Newtonsoft.Json.Linq.JObject.Parse(json);
                // var type = jobj["type"]?.ToString();

                // Or use our SignalingMessage which has "Type"
                var msg = JsonConvert.DeserializeObject<SignalingMessage>(json);
                if (msg == null) return;

                if (string.Equals(msg.Type, "OFFER", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleOffer(msg);
                }
                else if (string.Equals(msg.Type, "ICE_CANDIDATE", StringComparison.OrdinalIgnoreCase))
                {
                    HandleIceCandidate(msg);
                }
                else if (string.Equals(msg.Type, "UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    // SignalingMessage might not have the update fields, so re-deserialize as update
                    var updateMsg = JsonConvert.DeserializeObject<SignalUpdateMessage>(json);
                    if (updateMsg != null)
                    {
                        HandleUpdate(updateMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Plugin.DebugConfig.Value)
                {
                    Plugin.Log.LogError($"[RtcManager] Error handling signaling: {ex}");
                }
            }
        }

        private void HandleUpdate(SignalUpdateMessage msg)
        {
            try
            {
                // check mode
                if (!string.IsNullOrEmpty(msg.QueueMode))
                {
                    if (CurrentQueueMode != msg.QueueMode)
                    {
                        Plugin.Log.LogInfo($"[RtcManager] Queue Mode Changed: {CurrentQueueMode} -> {msg.QueueMode}");
                        CurrentQueueMode = msg.QueueMode;
                        OnQueueModeUpdate?.Invoke(CurrentQueueMode);
                    }
                }

                if (msg.CurrentTrack != null)
                {
                    CurrentTrackInfo = msg.CurrentTrack;
                    OnTrackUpdate?.Invoke(CurrentTrackInfo);
                }
                else
                {
                    // Null track implies nothing playing
                    CurrentTrackInfo = new BingBoxTrackInfo();
                    OnTrackUpdate?.Invoke(CurrentTrackInfo);
                }

                if (msg.Queue != null)
                {
                    CurrentQueue = msg.Queue;
                    OnQueueUpdate?.Invoke(CurrentQueue);
                }
            }
            catch (Exception ex)
            {
                if (Plugin.DebugConfig.Value)
                {
                    Plugin.Log.LogError($"[RtcManager] Error handling UPDATE model: {ex}");
                }
            }
        }

        private async Task HandleOffer(SignalingMessage msg)
        {
            if (Plugin.DebugConfig.Value)
            {
                Plugin.Log.LogInfo("[RtcManager] Received OFFER via Newtonsoft.");
            }

            var config = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer>
                {
                    new RTCIceServer { urls = "stun:stun.l.google.com:19302" }
                }
            };

            _pc = new RTCPeerConnection(config);

            var opusFormat = new SDPAudioVideoMediaFormat(SDPMediaTypesEnum.audio, 111, "opus", 48000, 2, "minptime=10;useinbandfec=1");
            var audioTrack = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPAudioVideoMediaFormat> { opusFormat });
            _pc.addTrack(audioTrack);

            _pc.OnAudioFormatsNegotiated += (formats) =>
            {
                if (Plugin.DebugConfig.Value)
                {
                    Plugin.Log.LogInfo("[RtcManager] Audio formats negotiated.");
                }
            };

            _pc.OnRtpPacketReceived += (remoteEP, type, packet) =>
            {
                if (type == SDPMediaTypesEnum.audio && packet.Header.PayloadType == 111)
                {
                    AudioSink.GotAudioRtp(remoteEP, packet.Header.SyncSource, packet.Header.SequenceNumber, packet.Header.Timestamp, packet.Header.PayloadType, packet.Header.MarkerBit == 1, packet.Payload);
                }
            };

            _pc.onicecandidate += (candidate) =>
            {
                if (candidate != null)
                {
                    // Construct ICE candidate JSON
                    // We can use an anonymous object for serialization
                    var candPayload = new
                    {
                        type = "ICE_CANDIDATE",
                        candidate = new
                        {
                            candidate = candidate.candidate,
                            sdpMid = candidate.sdpMid,
                            sdpMLineIndex = candidate.sdpMLineIndex
                        }
                    };
                    string candJson = JsonConvert.SerializeObject(candPayload);
                    _client.SendJson(candJson);
                }
            };

            _pc.onconnectionstatechange += (state) =>
            {
                if (state.ToString().ToLower() == "connected")
                {
                    Plugin.Log.LogInfo("[RtcManager] Connected!");
                }
                else
                {
                    if (Plugin.DebugConfig.Value)
                    {
                        Plugin.Log.LogInfo($"[RtcManager] Connection State: {state}");
                    }
                }
            };

            // msg.Sdp is the object inside "sdp" key. 
            // In some signaling, "sdp" might be the string directly or an object. 
            // Our model defines SdpDetail. Let's verify usage.
            // If the incoming JSON is { type: "OFFER", sdp: "..." } vs { type: "OFFER", sdp: { type: "offer", sdp: "..." } }
            // The existing code manually parsed 'sdp' key.
            // Based on models, we expect nested object.

            string remoteSdpStr = "";
            if (msg.Sdp != null)
            {
                remoteSdpStr = msg.Sdp.Sdp;
            }

            if (!string.IsNullOrEmpty(remoteSdpStr))
            {
                if (Plugin.DebugConfig.Value)
                {
                    Plugin.Log.LogInfo("[RtcManager] Parsed SDP successfully.");
                }

                var remoteDesc = new RTCSessionDescriptionInit
                {
                    type = RTCSdpType.offer,
                    sdp = remoteSdpStr
                };

                var result = _pc.setRemoteDescription(remoteDesc);
                if (result != SetDescriptionResultEnum.OK)
                {
                    Plugin.Log.LogError($"[RtcManager] Failed to set remote description: {result}");
                    return;
                }

                foreach (var cand in _pendingCandidates)
                {
                    _pc.addIceCandidate(cand);
                }
                _pendingCandidates.Clear();

                var answer = _pc.createAnswer(null);
                await _pc.setLocalDescription(answer);

                // Send Answer
                var answerPayload = new
                {
                    type = "ANSWER",
                    sdp = new
                    {
                        type = "answer",
                        sdp = answer.sdp
                    }
                };

                string answerJson = JsonConvert.SerializeObject(answerPayload);
                _client.SendJson(answerJson);

                if (Plugin.DebugConfig.Value)
                {
                    Plugin.Log.LogInfo("[RtcManager] Sent ANSWER.");
                }
            }
            else
            {
                Plugin.Log.LogError($"[RtcManager] Failed to parse OFFER SDP.");
            }
        }

        private void HandleIceCandidate(SignalingMessage msg)
        {
            try
            {
                if (msg.Candidate != null)
                {
                    var init = new RTCIceCandidateInit
                    {
                        candidate = msg.Candidate.Candidate,
                        sdpMid = msg.Candidate.SdpMid,
                        sdpMLineIndex = (ushort)msg.Candidate.SdpMLineIndex
                    };

                    if (_pc != null && _pc.remoteDescription != null)
                    {
                        _pc.addIceCandidate(init);
                    }
                    else
                    {
                        _pendingCandidates.Add(init);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[RtcManager] Candidate parse error: {ex}");
            }
        }

        public void Close()
        {
            _pc?.Close("Application closing");
            _pc = null;
        }
    }
}
