namespace Jellyfin.UWP.Models;

public sealed class UIMediaListItemSeries : UIMediaListItem
{
    public string SeriesName { get; internal set; } = default!;

    public string Description { get; internal set; } = default!;
}
