using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BingBox.Network
{
    [Serializable]
    public class BingBoxTrackInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; } = "";

        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("artist")]
        public string Artist { get; set; } = "";

        [JsonProperty("cleanTitle")]
        public string CleanTitle { get; set; } = "";

        [JsonProperty("addedBy")]
        public string AddedBy { get; set; } = "";

        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; } = "";

        [JsonProperty("isPaused")]
        public bool IsPaused { get; set; } = false;

        [JsonProperty("startedAt")]
        public long StartedAt { get; set; } = 0;

        [JsonProperty("durationSec")]
        public long DurationSec { get; set; } = 0;

        [JsonProperty("totalPausedDuration")]
        public long TotalPausedDuration { get; set; } = 0;

        [JsonProperty("addedAt")]
        public long AddedAt { get; set; } = 0;

        [JsonIgnore]
        public bool IsVideo => !string.IsNullOrEmpty(CleanTitle);

        [JsonIgnore]
        public string DisplayTitle => !string.IsNullOrEmpty(CleanTitle) ? CleanTitle : Title;

        [JsonIgnore]
        public string DisplayArtist => Artist;
    }

    [Serializable]
    public class SignalingMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("sdp")]
        public SdpDetail? Sdp { get; set; }

        [JsonProperty("candidate")]
        public IceCandidateDetail? Candidate { get; set; }
    }

    [Serializable]
    public class SdpDetail
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("sdp")]
        public string Sdp { get; set; } = "";
    }

    [Serializable]
    public class IceCandidateDetail
    {
        [JsonProperty("candidate")]
        public string Candidate { get; set; } = "";

        [JsonProperty("sdpMid")]
        public string SdpMid { get; set; } = "";

        [JsonProperty("sdpMLineIndex")]
        public int SdpMLineIndex { get; set; }
    }

    [Serializable]
    public class SignalUpdateMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("queueMode")]
        public string QueueMode { get; set; } = "";

        [JsonProperty("isPlaying")]
        public bool IsPlaying { get; set; }

        [JsonProperty("isPaused")]
        public bool IsPaused { get; set; }

        [JsonProperty("startedAt")]
        public long? StartedAt { get; set; }

        [JsonProperty("totalPausedDuration")]
        public long? TotalPausedDuration { get; set; }

        [JsonProperty("currentTrack")]
        public BingBoxTrackInfo? CurrentTrack { get; set; }

        [JsonProperty("queue")]
        public List<BingBoxTrackInfo>? Queue { get; set; }
    }
}
