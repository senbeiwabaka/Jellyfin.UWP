using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.Models.Filters;

//[GeneratedBindableCustomProperty(propertyNames: [nameof(IsSelected), nameof(Name)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
public partial class SortModel
{
    public required string Name { get; init; } = default!;

    public required ItemSortBy Sort { get; init; }

    public bool IsSelected { get; set; }
}
