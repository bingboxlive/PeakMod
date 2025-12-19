using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using BingBox.Network;

namespace BingBox.UI
{
    public class QueueItem : MonoBehaviour
    {
        public string Id { get; private set; } = "";

        public TextMeshProUGUI? TitleText;
        public TextMeshProUGUI? ArtistText;
        public TextMeshProUGUI? RequesterText;
        public TextMeshProUGUI? DetailsText;
        public Image? AlbumArtImage;
        public Button? RemoveButton;

        private string _currentThumbnailUrl = "";
        private Coroutine? _loadCoroutine;

        public void Configure(BingBoxTrackInfo info, System.Action<string> onRemove)
        {
            Id = info.Id;

            if (TitleText != null) TitleText.text = info.DisplayTitle;

            if (ArtistText != null)
            {
                if (string.IsNullOrEmpty(info.DisplayArtist))
                {
                    ArtistText.gameObject.SetActive(false);
                }
                else
                {
                    ArtistText.gameObject.SetActive(true);
                    ArtistText.text = info.DisplayArtist;
                }
            }

            if (RequesterText != null) RequesterText.text = $"Req: {info.AddedBy}";

            if (DetailsText != null)
            {
                string durationStr = FormatDuration(info.DurationSec);
                string timeStr = "Unknown";
                if (info.AddedAt > 0)
                {
                    try
                    {
                        var dto = System.DateTimeOffset.FromUnixTimeMilliseconds(info.AddedAt).ToLocalTime();
                        timeStr = dto.ToString("HH:mm");
                    }
                    catch { }
                }
                DetailsText.text = $"{durationStr} | Requested at: {timeStr}";
            }

            if (RemoveButton != null)
            {
                RemoveButton.onClick.RemoveAllListeners();
                RemoveButton.onClick.AddListener(() =>
                {
                    Plugin.Log.LogInfo($"[QueueItem] Remove clicked for ID: {Id}");
                    onRemove(Id);
                });
            }

            string thumbUrl = info.Thumbnail;
            if (!string.IsNullOrEmpty(thumbUrl) && thumbUrl.Contains("ytimg.com") && thumbUrl.Contains("?"))
            {
                int qIdx = thumbUrl.IndexOf('?');
                if (qIdx != -1) thumbUrl = thumbUrl.Substring(0, qIdx);
            }

            if (_currentThumbnailUrl != thumbUrl)
            {
                _currentThumbnailUrl = thumbUrl;
                if (!string.IsNullOrEmpty(_currentThumbnailUrl))
                {
                    Plugin.Log.LogInfo($"[QueueItem] Loading art for {Id}: {_currentThumbnailUrl}");
                    if (_loadCoroutine != null) StopCoroutine(_loadCoroutine);
                    _loadCoroutine = StartCoroutine(LoadThumbnail(_currentThumbnailUrl));
                }
                else
                {
                    Plugin.Log.LogInfo($"[QueueItem] No thumbnail for {Id}");
                }
            }
        }

        private string FormatDuration(long seconds)
        {
            if (seconds < 0) seconds = 0;
            long m = seconds / 60;
            long s = seconds % 60;
            return $"{m}:{s:00}";
        }

        private IEnumerator LoadThumbnail(string url)
        {
            if (AlbumArtImage == null)
            {
                Plugin.Log.LogError($"[QueueItem] AlbumArtImage is NULL for {Id}");
                yield break;
            }

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(uwr);
                    if (tex != null && AlbumArtImage != null)
                    {
                        Plugin.Log.LogInfo($"[QueueItem] Applied art for {Id}");

                        int side = Mathf.Min(tex.width, tex.height);
                        int xOffset = (tex.width - side) / 2;
                        int yOffset = (tex.height - side) / 2;
                        var cropRect = new Rect(xOffset, yOffset, side, side);

                        var sprite = Sprite.Create(tex, cropRect, new Vector2(0.5f, 0.5f));
                        AlbumArtImage.sprite = sprite;
                        AlbumArtImage.color = Color.white;
                    }
                }
                else
                {
                    Plugin.Log.LogError($"[QueueItem] Failed to load art for {Id}: {uwr.error}");
                }
            }
        }
    }
}
