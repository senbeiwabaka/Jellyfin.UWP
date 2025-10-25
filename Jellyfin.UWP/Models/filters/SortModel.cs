using Jellyfin.Sdk.Generated.Models;
using WinRT;

namespace Jellyfin.UWP.Models.Filters;

[GeneratedBindableCustomProperty(propertyNames: [nameof(IsSelected), nameof(Name)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
internal partial class SortModel
{
    public required string Name { get; init; } = default!;

    public required ItemSortBy Sort { get; init; }

    public bool IsSelected { get; set; }
}
