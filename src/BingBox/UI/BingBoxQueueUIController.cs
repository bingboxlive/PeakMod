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

        private List<BingBoxTrackInfo> _pendingQueue = new List<BingBoxTrackInfo>();
        private bool _dataChanged = false;
        private float _lastUpdateTime = 0f;
        private const float UpdateInterval = 0.2f; // Throttle to 5fps max for queue rebuilds

        private void Start()
        {
            if (BingBoxWebClient.Instance != null && BingBoxWebClient.Instance.RtcManager != null)
            {
                BingBoxWebClient.Instance.RtcManager.OnQueueUpdate += OnQueueReceived;
            }
            RoomIdManager.OnRoomIdChanged += HandleRoomChange;
        }

        private void OnDestroy()
        {
            if (BingBoxWebClient.Instance != null && BingBoxWebClient.Instance.RtcManager != null)
            {
                BingBoxWebClient.Instance.RtcManager.OnQueueUpdate -= OnQueueReceived;
            }
            RoomIdManager.OnRoomIdChanged -= HandleRoomChange;
        }

        public void Init(RectTransform content, object? font)
        {
            ContentContainer = content;
            FontAsset = font;
        }

        private void HandleRoomChange(string newRoomId)
        {
            // Clear queue immediately on room switch
            lock (_pendingQueue)
            {
                _pendingQueue.Clear();
                _dataChanged = true;
            }
        }

        private void OnQueueReceived(List<BingBoxTrackInfo> newQueue)
        {
            lock (_pendingQueue)
            {
                _pendingQueue = newQueue;
                _dataChanged = true;
            }
        }

        private void Update()
        {
            if (_dataChanged && Time.unscaledTime - _lastUpdateTime > UpdateInterval)
            {
                RebuildQueueUI();
                _dataChanged = false;
                _lastUpdateTime = Time.unscaledTime;
            }
        }

        private void RebuildQueueUI()
        {
            if (ContentContainer == null) return;

            List<BingBoxTrackInfo> currentQueueSnapshot;
            lock (_pendingQueue)
            {
                currentQueueSnapshot = new List<BingBoxTrackInfo>(_pendingQueue);
            }

            var existingItems = new List<QueueItem>(ContentContainer.GetComponentsInChildren<QueueItem>());

            int spacerCount = 0;
            foreach (Transform child in ContentContainer)
            {
                if (child.name == "TopSpacer") spacerCount++;
            }

            for (int i = 0; i < currentQueueSnapshot.Count; i++)
            {
                var info = currentQueueSnapshot[i];
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
