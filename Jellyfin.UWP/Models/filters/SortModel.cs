using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.UWP.Models.Filters;

internal sealed class SortModel
{
    public required string Name { get; init; } = default!;

    public required ItemSortBy Sort { get; init; }

    public bool IsSelected { get; set; }
}
