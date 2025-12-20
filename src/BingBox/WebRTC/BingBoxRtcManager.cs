using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using BingBox.Utils;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            if (Plugin.DebugConfig.Value)
            {
                Plugin.Log.LogInfo($"[RtcManager] Received Signal: {json}");
            }
            try
            {
                if (json.Contains("\"type\": \"OFFER\"") || json.Contains("\"type\":\"OFFER\""))
                {
                    await HandleOffer(json);
                }
                else if (json.Contains("\"type\": \"ICE_CANDIDATE\"") || json.Contains("\"type\":\"ICE_CANDIDATE\""))
                {
                    HandleIceCandidate(json);
                }
                else if (json.Contains("\"type\": \"UPDATE\"") || json.Contains("\"type\":\"UPDATE\""))
                {
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
                if (!string.IsNullOrEmpty(msg.QueueMode))
                {
                    if (CurrentQueueMode != msg.QueueMode)
                    {
                        Plugin.Log.LogInfo($"[RtcManager] Queue Mode Changed: {CurrentQueueMode} -> {msg.QueueMode}");
                        CurrentQueueMode = msg.QueueMode;
                        OnQueueModeUpdate?.Invoke(CurrentQueueMode);
                    }
                }

                if (msg.CurrentTrack != null && msg.IsPlaying)
                {
                    CurrentTrackInfo = msg.CurrentTrack;
                    CurrentTrackInfo.IsPaused = msg.IsPaused;
                    CurrentTrackInfo.StartedAt = msg.StartedAt ?? 0;
                    CurrentTrackInfo.TotalPausedDuration = msg.TotalPausedDuration ?? 0;

                    OnTrackUpdate?.Invoke(CurrentTrackInfo);
                }
                else
                {
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

        private void CreatePeerConnection(List<RTCIceServer>? iceServers = null)
        {
            var dtlsCert = CertificateUtils.GenerateSelfSignedRtcCertificate("BingBox");
            if (Plugin.DebugConfig.Value) Plugin.Log.LogInfo($"[RtcManager] Generated DTLS Certificate.");

            var config = new RTCConfiguration
            {
                iceServers = iceServers ?? new List<RTCIceServer>
                {
                    new RTCIceServer { urls = "stun:stun.l.google.com:19302" }
                },
                certificates2 = new List<RTCCertificate2>
                {
                    dtlsCert
                },
                bundlePolicy = RTCBundlePolicy.max_bundle,
                rtcpMuxPolicy = RTCRtcpMuxPolicy.require
            };

            if (Plugin.DebugConfig.Value)
                Plugin.Log.LogInfo("[RtcManager] Creating Peer Connection with MaxBundle & RequireMux.");
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
                    if (Plugin.DebugConfig.Value) Plugin.Log.LogInfo($"[RtcManager] Generated ICE Candidate: {candidate.candidate}");

                    string rawCand = candidate.candidate;
                    if (!rawCand.StartsWith("candidate:"))
                    {
                        rawCand = "candidate:" + rawCand;
                    }

                    string candJson = $"{{\"type\": \"ICE_CANDIDATE\", \"candidate\": {{ \"candidate\": \"{rawCand}\", \"sdpMid\": \"{candidate.sdpMid}\", \"sdpMLineIndex\": {candidate.sdpMLineIndex} }} }}";
                    _client.SendJson(candJson);
                }
            };

            _pc.oniceconnectionstatechange += (state) =>
            {
                if (Plugin.DebugConfig.Value)
                    Plugin.Log.LogInfo($"[RtcManager] ICE Connection State: {state}");
            };

            _pc.onicegatheringstatechange += (state) =>
            {
                if (Plugin.DebugConfig.Value)
                    Plugin.Log.LogInfo($"[RtcManager] ICE Gathering State: {state}");
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
                        Plugin.Log.LogInfo($"[RtcManager] Connection State: {state}");
                }
            };
        }

        private async Task HandleOffer(string json)
        {
            if (Plugin.DebugConfig.Value)
            {
                Plugin.Log.LogInfo("[RtcManager] Received OFFER via Manual Parsing.");
            }

            string remoteSdpStr = ExtractSdpFromOffer(json);

            if (!string.IsNullOrEmpty(remoteSdpStr))
            {
                if (_pc != null)
                {
                    Plugin.Log.LogInfo("[RtcManager] Disposing existing PeerConnection for new Offer...");
                    _pc.Close("Replacing PC for new offer");
                    _pc.Dispose();
                    _pc = null;
                }

                _pendingCandidates.Clear();

                var iceServers = ExtractIceServers(json);
                CreatePeerConnection(iceServers);

                var remoteDesc = new RTCSessionDescriptionInit
                {
                    type = RTCSdpType.offer,
                    sdp = remoteSdpStr
                };

                var result = _pc!.setRemoteDescription(remoteDesc);
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

                string cleanSdp = answer.sdp.Replace("\r", "").Replace("\n", "\\n");
                cleanSdp = cleanSdp.Replace("\"", "\\\"");

                string answerJson = $"{{\"type\": \"ANSWER\", \"sdp\": {{ \"type\": \"answer\", \"sdp\": \"{cleanSdp}\" }} }}";

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

        private void HandleIceCandidate(string json)
        {
            try
            {
                string candidateObj = ExtractJsonObject(json, "candidate");
                if (!string.IsNullOrEmpty(candidateObj))
                {
                    string candStr = ExtractJsonValue(candidateObj, "candidate");
                    string sdpMid = ExtractJsonValue(candidateObj, "sdpMid");

                    string sdpLineIndexStr = ExtractJsonValue(candidateObj, "sdpMLineIndex");
                    int sdpLineIndex = 0;
                    if (!string.IsNullOrEmpty(sdpLineIndexStr)) int.TryParse(sdpLineIndexStr, out sdpLineIndex);

                    if (!string.IsNullOrEmpty(candStr))
                    {
                        var init = new RTCIceCandidateInit
                        {
                            candidate = candStr,
                            sdpMid = sdpMid,
                            sdpMLineIndex = (ushort)sdpLineIndex
                        };

                        if (Plugin.DebugConfig.Value) Plugin.Log.LogInfo($"[RtcManager] Received ICE Candidate: {candStr}");

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

        private string ExtractJsonValue(string json, string key)
        {
            try
            {
                string keyPattern = $"\"{key}\":";
                int keyIdx = json.IndexOf(keyPattern);
                if (keyIdx == -1) return "";

                int valStart = keyIdx + keyPattern.Length;

                int startQuote = json.IndexOf("\"", valStart);

                int scan = valStart;
                while (scan < json.Length && (char.IsWhiteSpace(json[scan]))) scan++;

                if (scan >= json.Length) return "";

                if (json[scan] == '"')
                {
                    int endQuote = json.IndexOf("\"", scan + 1);
                    if (endQuote == -1) return "";
                    return json.Substring(scan + 1, endQuote - scan - 1);
                }
                else
                {
                    int end = scan;
                    while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
                    return json.Substring(scan, end - scan).Trim();
                }
            }
            catch { return ""; }
        }

        private string ExtractJsonObject(string json, string key)
        {
            try
            {
                string keyPattern = $"\"{key}\":";
                int keyIdx = json.IndexOf(keyPattern);
                if (keyIdx == -1) return "";

                int startBrace = json.IndexOf("{", keyIdx + keyPattern.Length);
                if (startBrace == -1) return "";

                int braceCount = 1;
                int i = startBrace + 1;
                while (i < json.Length && braceCount > 0)
                {
                    if (json[i] == '{') braceCount++;
                    else if (json[i] == '}') braceCount--;
                    i++;
                }

                if (braceCount == 0) return json.Substring(startBrace, i - startBrace + 1);
                return "";
            }
            catch { return ""; }
        }

        private string ExtractSdpFromOffer(string json)
        {
            string sdpObj = ExtractJsonObject(json, "sdp");
            if (string.IsNullOrEmpty(sdpObj)) return "";

            string keyPattern = "\"sdp\":";
            int keyIdx = sdpObj.IndexOf(keyPattern);
            if (keyIdx == -1) return "";

            int startQuote = sdpObj.IndexOf("\"", keyIdx + keyPattern.Length);
            if (startQuote == -1) return "";

            StringBuilder sb = new StringBuilder();
            bool escaped = false;
            for (int i = startQuote + 1; i < sdpObj.Length; i++)
            {
                char c = sdpObj[i];
                if (escaped)
                {
                    if (c == 'r') sb.Append('\r');
                    else if (c == 'n') sb.Append('\n');
                    else if (c == 't') sb.Append('\t');
                    else if (c == '"') sb.Append('"');
                    else if (c == '\\') sb.Append('\\');
                    else sb.Append(c);
                    escaped = false;
                }
                else
                {
                    if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        return sb.ToString();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            return sb.ToString();
        }

        private List<RTCIceServer>? ExtractIceServers(string json)
        {
            try
            {
                var jObj = JObject.Parse(json);
                var serversToken = jObj["iceServers"];
                if (serversToken != null && serversToken.Type == JTokenType.Array)
                {
                    var list = new List<RTCIceServer>();
                    foreach (var server in serversToken)
                    {
                        string? urls = server["urls"]?.ToString();
                        string? username = server["username"]?.ToString();
                        string? credential = server["credential"]?.ToString();

                        if (!string.IsNullOrEmpty(urls))
                        {
                            var rtcServer = new RTCIceServer { urls = urls };
                            if (!string.IsNullOrEmpty(username)) rtcServer.username = username;
                            if (!string.IsNullOrEmpty(credential)) rtcServer.credential = credential;
                            list.Add(rtcServer);
                        }
                    }
                    if (list.Count > 0)
                    {
                        if (Plugin.DebugConfig.Value) Plugin.Log.LogInfo($"[RtcManager] Parsed {list.Count} custom ICE servers.");
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Plugin.DebugConfig.Value) Plugin.Log.LogError($"[RtcManager] Failed to parse ICE servers: {ex}");
            }
            return null;
        }





    }
}
