using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using BingBox.Network;
using BingBox.WebRTC;
using BingBox.Utils;

namespace BingBox.UI
{
    public class BingBoxQueueUIController : MonoBehaviour
    {
        public RectTransform? ContentContainer;
        public object? FontAsset;

        private List<BingBoxTrackInfo> _currentQueue = new List<BingBoxTrackInfo>();

        private void Start()
        {
            if (BingBoxWebClient.Instance != null && BingBoxWebClient.Instance.RtcManager != null)
            {
                BingBoxWebClient.Instance.RtcManager.OnQueueUpdate += UpdateQueue;
            }
        }

        private void OnDestroy()
        {
            if (BingBoxWebClient.Instance != null && BingBoxWebClient.Instance.RtcManager != null)
            {
                BingBoxWebClient.Instance.RtcManager.OnQueueUpdate -= UpdateQueue;
            }
        }

        public void Init(RectTransform content, object? font)
        {
            ContentContainer = content;
            FontAsset = font;
        }

        private void UpdateQueue(List<BingBoxTrackInfo> newQueue)
        {
            if (ContentContainer == null) return;

            _currentQueue = newQueue;

            var existingItems = new List<QueueItem>(ContentContainer.GetComponentsInChildren<QueueItem>());

            int spacerCount = 0;
            foreach (Transform child in ContentContainer)
            {
                if (child.name == "TopSpacer") spacerCount++;
            }

            for (int i = 0; i < newQueue.Count; i++)
            {
                var info = newQueue[i];
                var match = existingItems.Find(x => x.Id == info.Id);

                QueueItem itemComp;

                if (match != null)
                {
                    existingItems.Remove(match);
                    itemComp = match;
                }
                else
                {
                    itemComp = QueueUI.CreateQueueItem(ContentContainer, FontAsset);
                }

                itemComp.transform.SetSiblingIndex(i + spacerCount);
                itemComp.Configure(info, RemoveTrack);
            }

            foreach (var leftover in existingItems)
            {
                Destroy(leftover.gameObject);
            }
        }

        private void RemoveTrack(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            Plugin.Log.LogInfo($"[QueueController] Removing track {id}");
            if (BingBoxWebClient.Instance != null)
            {
                BingBoxWebClient.Instance.SendJson($"{{\"type\": \"REMOVE_TRACK\", \"trackId\": \"{id}\"}}");
            }
        }
    }
}
