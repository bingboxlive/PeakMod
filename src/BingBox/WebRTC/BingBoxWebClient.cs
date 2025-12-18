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
        private ClientWebSocket? _ws;
        private CancellationTokenSource? _cts;
        private bool _isConnecting;
        private BingBoxRtcManager _rtcManager = null!;

        private void Awake()
        {
            _rtcManager = gameObject.AddComponent<BingBoxRtcManager>();
            _rtcManager.Initialize(this);
        }

        private void Start()
        {
            Connect();
        }

        private void OnDestroy()
        {
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

            Plugin.Log.LogInfo($"[WebClient] Connecting to {url}...");

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

        private void SendJoinRoom()
        {
            var roomId = RoomIdManager.CurrentRoomId;
            var userId = Plugin.UserId;
            var userName = Plugin.Username;

            var json = $"{{\"type\": \"JOIN_ROOM\", \"roomId\": \"{roomId}\", \"userId\": \"{userId}\", \"userName\": \"{userName}\"}}";

            SendJson(json);
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

        private async void ReceiveLoop()
        {
            var buffer = new byte[8192];
            try
            {
                while (_ws != null && _ws.State == WebSocketState.Open && _cts != null && !_cts.IsCancellationRequested)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Plugin.Log.LogInfo("[WebClient] Server closed connection.");
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await _rtcManager.HandleSignalingMessage(message);
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
