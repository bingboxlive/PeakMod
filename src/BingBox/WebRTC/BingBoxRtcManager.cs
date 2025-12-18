using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using BingBox.Utils;
using UnityEngine;

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
            _client.SendJson("{\"type\": \"JOIN_STREAM\"}");
        }

        public async Task HandleSignalingMessage(string json)
        {
            try
            {
                if (json.Contains("\"type\":\"OFFER\"") || json.Contains("\"type\": \"OFFER\""))
                {
                    await HandleOffer(json);
                }
                else if (json.Contains("\"type\":\"ICE_CANDIDATE\"") || json.Contains("\"type\": \"ICE_CANDIDATE\""))
                {
                    HandleIceCandidate(json);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[RtcManager] Error handling signaling: {ex}");
            }
        }

        private async Task HandleOffer(string json)
        {
            Plugin.Log.LogInfo($"[RtcManager] Received OFFER raw JSON: {json}");
            Plugin.Log.LogInfo("[RtcManager] Creating PeerConnection...");

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
                Plugin.Log.LogInfo("[RtcManager] Audio formats negotiated.");
            };

            _pc.OnRtpPacketReceived += (remoteEP, type, packet) =>
            {
                if (type == SDPMediaTypesEnum.audio && packet.Header.PayloadType == 111) // 111 is typical Opus dynamic PT
                {
                    AudioSink.GotAudioRtp(remoteEP, packet.Header.SyncSource, packet.Header.SequenceNumber, packet.Header.Timestamp, packet.Header.PayloadType, packet.Header.MarkerBit == 1, packet.Payload);
                }
            };

            _pc.onicecandidate += (candidate) =>
            {
                if (candidate != null)
                {
                    var candJson = $"{{\"type\": \"ICE_CANDIDATE\", \"candidate\": {{\"candidate\": \"{candidate.candidate}\", \"sdpMid\": \"{candidate.sdpMid}\", \"sdpMLineIndex\": {candidate.sdpMLineIndex}}}}}";
                    _client.SendJson(candJson);
                }
            };

            _pc.onconnectionstatechange += (state) =>
            {
                Plugin.Log.LogInfo($"[RtcManager] Connection State: {state}");
            };

            string? sdpStr = ExtractSdpFromOffer(json);

            if (!string.IsNullOrEmpty(sdpStr))
            {
                Plugin.Log.LogInfo("[RtcManager] Manually parsed SDP successfully.");
                var remoteDesc = new RTCSessionDescriptionInit
                {
                    type = RTCSdpType.offer,
                    sdp = sdpStr
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

                var answerJson = $"{{\"type\": \"ANSWER\", \"sdp\": {{\"type\": \"answer\", \"sdp\": \"{answer.sdp.Replace("\r\n", "\\r\\n")}\"}} }}";
                _client.SendJson(answerJson);
                Plugin.Log.LogInfo("[RtcManager] Sent ANSWER.");
            }
            else
            {
                Plugin.Log.LogError($"[RtcManager] Failed to manual parse OFFER SDP. JSON length: {json.Length}");
            }
        }

        private void HandleIceCandidate(string json)
        {
            try
            {
                string candidate = ExtractJsonValue(json, "candidate");
                string sdpMid = ExtractJsonValue(json, "sdpMid");
                string sdpMLineIndexStr = ExtractJsonValue(json, "sdpMLineIndex", false);

                if (!string.IsNullOrEmpty(candidate))
                {
                    var init = new RTCIceCandidateInit
                    {
                        candidate = candidate,
                        sdpMid = sdpMid,
                        sdpMLineIndex = ushort.TryParse(sdpMLineIndexStr, out var i) ? i : (ushort)0
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

        private string ExtractSdpFromOffer(string json)
        {
            const string sdpKey = "\"sdp\":";
            int sdpKeyIndex = json.IndexOf(sdpKey);
            if (sdpKeyIndex == -1) return string.Empty;

            int firstIndex = json.IndexOf("\"sdp\":\"");
            if (firstIndex == -1) return string.Empty;

            int valueStart = firstIndex + 7;
            if (valueStart >= json.Length) return string.Empty;

            int endQuote = json.IndexOf('"', valueStart);
            if (endQuote == -1) return string.Empty;

            string sdpStr = json.Substring(valueStart, endQuote - valueStart);
            return sdpStr.Replace("\\r\\n", "\r\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private string ExtractJsonValue(string json, string key, bool expectsString = true)
        {
            string search = $"\"{key}\":";
            int idx = json.IndexOf(search);
            if (idx == -1) return "";

            int start = idx + search.Length;
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

            if (start >= json.Length) return "";

            char endChar = ',';
            if (expectsString && json[start] == '"')
            {
                start++;
                endChar = '"';
            }

            int end = json.IndexOf(endChar, start);
            if (end == -1)
            {
                end = json.IndexOf('}', start);
            }

            if (end != -1)
            {
                return json.Substring(start, end - start);
            }
            return "";
        }

        public void Close()
        {
            _pc?.Close("Application closing");
            _pc = null;
        }
    }
}
