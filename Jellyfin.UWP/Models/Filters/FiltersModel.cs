using Jellyfin.Sdk.Generated.Models;
using WinRT;

namespace Jellyfin.UWP.Models.Filters;

[GeneratedBindableCustomProperty(propertyNames: [nameof(IsSelected), nameof(Name)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
internal partial class FiltersModel
{
    public required string Name { get; init; } = default!;

    public required ItemFilter Filter { get; init; }

    public bool IsSelected { get; set; }
}
