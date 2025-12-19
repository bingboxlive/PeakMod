namespace BingBox.Network
{
    public class BingBoxTrackInfo
    {
        public string Title = "";
        public string Id = "";
        public string Artist = "";
        public string CleanTitle = "";
        public string AddedBy = "";
        public string Thumbnail = "";
        public bool IsPaused = false;
        public long StartedAt = 0;
        public long DurationSec = 0;
        public long TotalPausedDuration = 0;
        public long AddedAt = 0;

        public bool IsVideo => !string.IsNullOrEmpty(CleanTitle);

        public string DisplayTitle => !string.IsNullOrEmpty(CleanTitle) ? CleanTitle : Title;

        public string DisplayArtist => Artist;
    }
}
