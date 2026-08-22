namespace Jellyfin.Models;

public sealed class UIMediaListItemSeries : UIMediaListItem
{
    public string? SeriesName { get; set; }

    public string Description { get; set; } = default!;
}
