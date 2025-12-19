using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BingBox.Network;
using BingBox.WebRTC;
using BingBox.Utils;

namespace BingBox.UI
{
    public class BingBoxPlayerUIController : MonoBehaviour
    {
        public TextMeshProUGUI? TitleText;
        public TextMeshProUGUI? ArtistText;
        public TextMeshProUGUI? RequesterText;

        public Image? AlbumArtImage;
        public Button? PlayPauseButton;
        public Button? PrevButton;
        public Button? NextButton;
        public Image? PlayPauseImage;
        public Button? LogoButton;

        public TextMeshProUGUI? CurrentTimeText;
        public TextMeshProUGUI? TotalTimeText;
        public RectTransform? ProgressFillRect;
        public TMP_InputField? RequestInput;
        public float MaxProgressWidth = 302f;

        private long _currentTrackDuration = 0;
        private long _currentTrackStart = 0;
        private long _totalPausedDuration = 0;
        private bool _hasTrack = false;

        private BingBoxTrackInfo _pendingTrackInfo = new BingBoxTrackInfo();
        private bool _trackDataChanged = false;

        private string? _lastThumbnailUrl;
        private Coroutine? _downloadCoroutine;

        private Sprite? _playSprite;
        private Sprite? _pauseSprite;
        private bool _isPaused;

        private void Start()
        {
            SetupText(TitleText);
            SetupText(ArtistText);
            SetupText(RequesterText);

            _playSprite = UIUtils.LoadSprite("Images.play.png");
            _pauseSprite = UIUtils.LoadSprite("Images.pause.png");

            if (PlayPauseButton != null)
            {
                PlayPauseButton.onClick.AddListener(OnPlayPauseClicked);
            }

            if (PrevButton != null) PrevButton.onClick.AddListener(OnPrevClicked);
            if (NextButton != null) NextButton.onClick.AddListener(OnNextClicked);

            if (RequestInput != null)
            {
                RequestInput.onSubmit.AddListener(OnRequestSubmit);
            }

            if (BingBoxWebClient.Instance != null && BingBoxWebClient.Instance.RtcManager != null)
            {
                BingBoxWebClient.Instance.RtcManager.OnTrackUpdate += OnTrackReceived;
            }

            if (LogoButton != null)
            {
                LogoButton.onClick.AddListener(OnLogoClicked);
            }
        }

        private bool _wasVisible = false;

        private float _lastDebugTime = 0f;

        private void Update()
        {
            if (Time.unscaledTime - _lastDebugTime > 3.0f)
            {
                DebugVisibility();
                _lastDebugTime = Time.unscaledTime;
            }

            bool isVisible = IsVisible();
            if (isVisible != _wasVisible)
            {
                if (isVisible) Plugin.Log.LogInfo("PAUSE MENU APPEARED");
                else Plugin.Log.LogInfo("PAUSE MENU DISAPPEARED");
                _wasVisible = isVisible;
            }

            if (!isVisible) return;

            if (_trackDataChanged)
            {
                UpdateTrackUI();
                _trackDataChanged = false;
            }

            if (!_hasTrack || _isPaused || _currentTrackStart <= 0) return;

            long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long effectiveStart = _currentTrackStart + _totalPausedDuration;
            long elapsedMs = now - effectiveStart;
            if (elapsedMs < 0) elapsedMs = 0;

            float elapsedSec = elapsedMs / 1000f;
            float totalSec = _currentTrackDuration;

            UpdateProgressDisplay(elapsedSec, totalSec);
        }

        private void DebugVisibility()
        {
            var t = transform;
            Plugin.Log.LogInfo($"--- VISIBILITY REPORT ({gameObject.name}) ---");
            while (t != null)
            {
                string info = $"{t.name}: Active={t.gameObject.activeSelf}, Scale={t.localScale}";
                var cg = t.GetComponent<CanvasGroup>();
                if (cg != null) info += $", CG.Alpha={cg.alpha}";
                var c = t.GetComponent<Canvas>();
                if (c != null) info += $", Canvas.Enabled={c.enabled}";

                Plugin.Log.LogInfo(info);
                t = t.parent;
            }
            Plugin.Log.LogInfo("-------------------------------------------");
        }

        private bool IsVisible()
        {
            if (!gameObject.activeInHierarchy) return false;

            var group = GetComponentInParent<CanvasGroup>();
            if (group != null && group.alpha == 0) return false;

            return true;
        }

        private void OnDestroy()
        {
            if (BingBoxWebClient.Instance != null && BingBoxWebClient.Instance.RtcManager != null)
            {
                BingBoxWebClient.Instance.RtcManager.OnTrackUpdate -= OnTrackReceived;
            }

            if (_downloadCoroutine != null)
            {
                StopCoroutine(_downloadCoroutine);
                _downloadCoroutine = null;
            }
        }

        private void OnTrackReceived(BingBoxTrackInfo info)
        {
            lock (_pendingTrackInfo)
            {
                _pendingTrackInfo = info;
                _trackDataChanged = true;
            }
        }

        private void UpdateTrackUI()
        {
            BingBoxTrackInfo info;
            lock (_pendingTrackInfo)
            {
                info = _pendingTrackInfo;
            }

            Plugin.Log.LogInfo($"[UIController] UpdateUI: Title='{info.Title}' Clean='{info.CleanTitle}' Artist='{info.Artist}' DisplayArtist='{info.DisplayArtist}' Thumb='{info.Thumbnail}' Paused={info.IsPaused}");

            _isPaused = info.IsPaused;
            if (PlayPauseImage != null)
            {
                PlayPauseImage.sprite = _isPaused ? _playSprite : _pauseSprite;
            }

            if (TitleText != null)
            {
                TitleText.text = string.IsNullOrEmpty(info.DisplayTitle) ? "Nothing Playing" : info.DisplayTitle;
            }

            if (ArtistText != null)
            {
                if (string.IsNullOrEmpty(info.DisplayArtist))
                {
                    ArtistText.text = "";
                }
                else
                {
                    ArtistText.text = info.DisplayArtist;
                }
            }

            if (RequesterText != null)
            {
                if (string.IsNullOrEmpty(info.Title))
                {
                    RequesterText.text = "Queue is empty.";
                }
                else if (!string.IsNullOrEmpty(info.AddedBy))
                {
                    RequesterText.text = $"Requested by: {info.AddedBy}";
                }
                else
                {
                    RequesterText.text = "Requested by: Unknown";
                }
            }

            UpdateAlbumArt(info.Thumbnail);

            _currentTrackDuration = info.DurationSec;
            _currentTrackStart = info.StartedAt;
            _totalPausedDuration = info.TotalPausedDuration;
            _hasTrack = !string.IsNullOrEmpty(info.Title);

            if (_hasTrack && info.StartedAt > 0)
            {
                long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long effectiveStart = _currentTrackStart + _totalPausedDuration;
                long elapsedMs = now - effectiveStart;
                if (elapsedMs < 0) elapsedMs = 0;
                UpdateProgressDisplay(elapsedMs / 1000f, _currentTrackDuration);
            }
            else
            {
                UpdateProgressDisplay(0, _currentTrackDuration);
                if (_hasTrack)
                {
                    if (CurrentTimeText != null) CurrentTimeText.text = "0:00";
                }
            }
        }

        private void UpdateProgressDisplay(float current, float total)
        {
            if (current > total && total > 0) current = total;

            if (CurrentTimeText != null) CurrentTimeText.text = FormatTime(current);
            if (TotalTimeText != null) TotalTimeText.text = FormatTime(total);

            if (ProgressFillRect != null)
            {
                float pct = (total > 0) ? (current / total) : 0f;
                if (pct > 1f) pct = 1f;
                float width = MaxProgressWidth * pct;
                ProgressFillRect.sizeDelta = new Vector2(width, 0);
            }
        }

        private string FormatTime(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0) seconds = 0;
            int m = Mathf.FloorToInt(seconds / 60);
            int s = Mathf.FloorToInt(seconds % 60);
            return $"{m}:{s:00}";
        }

        private void OnPlayPauseClicked()
        {
            if (BingBoxWebClient.Instance == null) return;

            string type = _isPaused ? "RESUME" : "PAUSE";
            BingBoxWebClient.Instance.SendJson($"{{\"type\": \"{type}\"}}");
        }

        private void OnLogoClicked()
        {
            Application.OpenURL("https://bingbox.live");
        }

        private void OnPrevClicked()
        {
            if (BingBoxWebClient.Instance == null) return;
            BingBoxWebClient.Instance.SendJson("{\"type\": \"PREVIOUS\"}");
        }

        private void OnNextClicked()
        {
            if (BingBoxWebClient.Instance == null) return;
            BingBoxWebClient.Instance.SendJson("{\"type\": \"SKIP\"}");
        }

        private void SetupText(TextMeshProUGUI? text)
        {
            if (text != null)
            {
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private void UpdateAlbumArt(string url)
        {
            if (AlbumArtImage == null)
            {
                Plugin.Log.LogWarning("[UIController] AlbumArtImage is null!");
                return;
            }

            if (string.IsNullOrEmpty(url)) url = "";

            if (url.Contains("ytimg.com") && url.Contains("?"))
            {
                int qIdx = url.IndexOf('?');
                if (qIdx != -1)
                {
                    url = url.Substring(0, qIdx);
                }
            }

            if (_lastThumbnailUrl == url) return;
            _lastThumbnailUrl = url;

            Plugin.Log.LogInfo($"[UIController] Updating Album Art. URL: '{url}'");

            if (_downloadCoroutine != null)
            {
                StopCoroutine(_downloadCoroutine);
                _downloadCoroutine = null;
            }

            if (string.IsNullOrEmpty(url))
            {
                ResetAlbumArt();
            }
            else
            {
                _downloadCoroutine = StartCoroutine(DownloadImage(url));
            }
        }

        private void ResetAlbumArt()
        {
            Plugin.Log.LogInfo("[UIController] Resetting Album Art to default.");
            var sprite = UIUtils.LoadSprite("Images.bing-bong.png");
            if (AlbumArtImage != null && sprite != null)
            {
                AlbumArtImage.sprite = sprite;
                AlbumArtImage.color = Color.white;
            }
            else if (AlbumArtImage != null)
            {
                AlbumArtImage.color = Color.black;
            }
        }

        private System.Collections.IEnumerator DownloadImage(string url)
        {
            Plugin.Log.LogInfo($"[UIController] Starting download coroutine for: {url}");
            using (var uwr = new UnityEngine.Networking.UnityWebRequest(url, UnityEngine.Networking.UnityWebRequest.kHttpVerbGET))
            {
                uwr.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                uwr.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                yield return uwr.SendWebRequest();

                if (uwr.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Plugin.Log.LogError($"[UIController] Failed to download art: {uwr.error} Code: {uwr.responseCode}");
                    ResetAlbumArt();
                }
                else
                {
                    byte[] data = uwr.downloadHandler.data;
                    if (data == null || data.Length == 0)
                    {
                        Plugin.Log.LogError("[UIController] Downloaded data is empty.");
                        ResetAlbumArt();
                    }
                    else
                    {
                        var texture = new Texture2D(2, 2);
                        if (ImageConversion.LoadImage(texture, data))
                        {
                            Plugin.Log.LogInfo($"[UIController] Download success. Texture size: {texture.width}x{texture.height}");

                            int side = Mathf.Min(texture.width, texture.height);
                            int xOffset = (texture.width - side) / 2;
                            int yOffset = (texture.height - side) / 2;
                            var cropRect = new Rect(xOffset, yOffset, side, side);

                            var sprite = Sprite.Create(texture, cropRect, new Vector2(0.5f, 0.5f));
                            if (AlbumArtImage != null)
                            {
                                AlbumArtImage.sprite = sprite;
                                AlbumArtImage.color = Color.white;
                            }
                        }
                        else
                        {
                            string textPreview = System.Text.Encoding.UTF8.GetString(data, 0, System.Math.Min(data.Length, 100));
                            Plugin.Log.LogError($"[UIController] Failed to decode image. First 100 chars: {textPreview}");
                            ResetAlbumArt();
                        }
                    }
                }
            }
            _downloadCoroutine = null;
        }
        private void OnRequestSubmit(string url)
        {
            Plugin.Log.LogInfo($"[UIController] OnRequestSubmit called with: '{url}'");
            if (string.IsNullOrEmpty(url)) return;
            if (RequestInput != null) RequestInput.text = "";

            StartCoroutine(SendTrackRequest(url));
        }

        private System.Collections.IEnumerator SendTrackRequest(string url)
        {
            Plugin.Log.LogInfo($"[UIController] Starting SendTrackRequest for: {url}");
            var host = Plugin.LiveUrl;
            if (host.StartsWith("wss://")) host = host.Replace("wss://", "https://");
            else if (host.StartsWith("ws://")) host = host.Replace("ws://", "http://");

            if (!host.StartsWith("http")) host = "https://" + host;

            var apiUrl = $"{host}/api/queue";
            Plugin.Log.LogInfo($"[UIController] API URL: {apiUrl}");

            var roomId = RoomIdManager.CurrentRoomId;
            var userId = Plugin.UserId;
            var userName = Plugin.Username;

            var json = $"{{\"url\": \"{JsonEscape(url)}\", \"roomId\": \"{roomId}\", \"userId\": \"{userId}\", \"userName\": \"{JsonEscape(userName)}\"}}";
            Plugin.Log.LogInfo($"[UIController] JSON Payload: {json}");

            using (var uwr = new UnityEngine.Networking.UnityWebRequest(apiUrl, "POST"))
            {
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
                uwr.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(jsonToSend);
                uwr.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                uwr.SetRequestHeader("Content-Type", "application/json");

                yield return uwr.SendWebRequest();

                if (uwr.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Plugin.Log.LogError($"[UIController] Failed to add track: {uwr.error} - Code: {uwr.responseCode} - Text: {uwr.downloadHandler.text}");
                }
                else
                {
                    Plugin.Log.LogInfo($"[UIController] Track added successfully. Response: {uwr.downloadHandler.text}");
                }
            }
        }

        private string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
