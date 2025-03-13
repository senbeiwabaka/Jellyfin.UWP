using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.UWP.Models.Filters;

public sealed class FiltersModel
{
    public string DisplayName { get; init; } = default!;

    public ItemFilter Filter { get; init; }

    public bool IsSelected { get; set; }
}
