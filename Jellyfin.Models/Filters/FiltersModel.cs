using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.Models.Filters;

//[GeneratedBindableCustomProperty(propertyNames: [nameof(IsSelected), nameof(Name)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
public partial class FiltersModel
{
    public required string Name { get; init; } = default!;

    public required ItemFilter Filter { get; init; }

    public bool IsSelected { get; set; }
}
