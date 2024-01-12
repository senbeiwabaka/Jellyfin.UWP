namespace Jellyfin.UWP.Models
{
    public sealed class UIUserData
    {
        public int? UnplayedItemCount { get; internal set; }

        public bool HasBeenWatched { get; internal set; }

        public bool IsFavorite { get; internal set; }
    }
}
