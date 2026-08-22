namespace Jellyfin.Models;

public sealed class UIUserData
{
    public int UnplayedItemCount { get; set; }

    public bool HasBeenWatched { get; set; }

    public bool IsFavorite { get; set; }
}
