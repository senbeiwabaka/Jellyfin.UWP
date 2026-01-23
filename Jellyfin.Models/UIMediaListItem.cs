using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.Models;

public class UIMediaListItem : UIItem
{
    public string? Name { get; set; }

    public string? Url { get; set; }

    public bool IsFolder { get; set; }

    public BaseItemDto_CollectionType? CollectionType { get; set; }

    public string? Year { get; set; }

    public int? IndexNumber { get; set; }
}
