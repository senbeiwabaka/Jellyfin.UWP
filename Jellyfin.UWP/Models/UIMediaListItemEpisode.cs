namespace Jellyfin.UWP.Models
{
    public sealed class UIMediaListItemEpisode : UIMediaListItem
    {
        public string SeriesName { get; internal set; }

        public string Description { get; internal set; }

        public int UnPlayedCount { get; internal set; }
    }
}
