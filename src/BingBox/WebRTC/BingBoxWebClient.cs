using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using BingBox.Utils;
using UnityEngine;

namespace BingBox.WebRTC
{
    public class BingBoxWebClient : MonoBehaviour
    {
        public static BingBoxWebClient? Instance { get; private set; }

        public BingBoxRtcManager RtcManager => _rtcManager;

        private ClientWebSocket? _ws;
        private CancellationTokenSource? _cts;
        private bool _isConnecting;
        private BingBoxRtcManager _rtcManager = null!;

        private static readonly string MODE_CLASSIC_JSON = "{\"type\": \"TOGGLE_QUEUE_MODE\", \"mode\": \"classic\"}";
        private static readonly string MODE_ROUNDROBIN_JSON = "{\"type\": \"TOGGLE_QUEUE_MODE\", \"mode\": \"roundrobin\"}";
        private static readonly string MODE_SHUFFLE_JSON = "{\"type\": \"TOGGLE_QUEUE_MODE\", \"mode\": \"shuffle\"}";

        private void Awake()
        {
            Instance = this;
            _rtcManager = gameObject.AddComponent<BingBoxRtcManager>();
            _rtcManager.Initialize(this);
        }

        private void Start()
        {
            Connect();
            RoomIdManager.OnRoomIdChanged += HandleRoomChange;
        }

        private void OnDestroy()
        {
            RoomIdManager.OnRoomIdChanged -= HandleRoomChange;
            _rtcManager?.Close();
            Disconnect();
        }

        public async void Connect()
        {
            if (_isConnecting || (_ws != null && _ws.State == WebSocketState.Open)) return;

            _isConnecting = true;
            _cts = new CancellationTokenSource();

            var url = Plugin.LiveUrl;

            if (!url.Contains("://"))
            {
                url = "wss://" + url;
            }
            else if (url.StartsWith("https")) url = url.Replace("https", "wss");
            else if (url.StartsWith("http")) url = url.Replace("http", "ws");

            if (url.EndsWith("/")) url = url.Substring(0, url.Length - 1);

            if (Plugin.DebugConfig.Value)
            {
                Plugin.Log.LogInfo($"[WebClient] Connecting to {url}...");
            }

            try
            {
                _ws = new ClientWebSocket();
                await _ws.ConnectAsync(new Uri(url), _cts.Token);

                Plugin.Log.LogInfo("[WebClient] Connected!");
                _isConnecting = false;

                SendJoinRoom();
                ReceiveLoop();

                _rtcManager.JoinStream();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[WebClient] Connection failed: {ex.Message}");
                _isConnecting = false;
            }
        }

        public void Disconnect()
        {
            if (_ws != null)
            {
                try
                {
                    _cts?.Cancel();
                    _ws.Dispose();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[WebClient] Error disconnecting: {ex.Message}");
                }
                finally
                {
                    _ws = null;
                }
            }
        }

        public void SendJoinRoom()
        {
            var roomId = RoomIdManager.CurrentRoomId;
            var userId = Plugin.UserId;
            var userName = Plugin.Username;

            var json = $"{{\"type\": \"JOIN_ROOM\", \"roomId\": \"{roomId}\", \"userId\": \"{userId}\", \"userName\": \"{userName}\"}}";

            SendJson(json);
        }

        private void HandleRoomChange(string newRoomId)
        {
            Plugin.Log.LogInfo($"[WebClient] Switching to room: {newRoomId}");
            SendJoinRoom();
            _rtcManager.Close();
            _rtcManager.JoinStream();
        }

        public void SendToggleQueueMode(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return;

            if (mode == "classic") SendJson(MODE_CLASSIC_JSON);
            else if (mode == "roundrobin") SendJson(MODE_ROUNDROBIN_JSON);
            else if (mode == "shuffle") SendJson(MODE_SHUFFLE_JSON);
        }

        public async void SendJson(string json)
        {
            if (_ws == null || _ws.State != WebSocketState.Open || _cts == null) return;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[WebClient] Send failed: {ex.Message}");
            }
        }

        private byte[] _receiveBuffer = new byte[32 * 1024];
        private int _receiveCount = 0;

        private async void ReceiveLoop()
        {
            var chunkBuffer = new byte[8192];
            try
            {
                while (_ws != null && _ws.State == WebSocketState.Open && _cts != null && !_cts.IsCancellationRequested)
                {
                    _receiveCount = 0;
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(chunkBuffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Plugin.Log.LogInfo("[WebClient] Server closed connection.");
                            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            return;
                        }

                        if (_receiveCount + result.Count > _receiveBuffer.Length)
                        {
                            int newSize = Math.Max(_receiveBuffer.Length * 2, _receiveCount + result.Count);
                            Array.Resize(ref _receiveBuffer, newSize);
                        }

                        Buffer.BlockCopy(chunkBuffer, 0, _receiveBuffer, _receiveCount, result.Count);
                        _receiveCount += result.Count;
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(_receiveBuffer, 0, _receiveCount);
                        await _rtcManager.HandleSignalingMessage(message);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                {
                    Plugin.Log.LogError($"[WebClient] Receive error: {ex.Message}");
                }
            }
        }
    }
}
