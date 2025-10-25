using Jellyfin.Sdk.Generated.Models;
using WinRT;

namespace Jellyfin.UWP.Models.Filters;

[GeneratedBindableCustomProperty(propertyNames: [nameof(IsSelected), nameof(Label)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
internal partial class SortOrderModel
{
    public required string Label { get; init; } = default!;

    public required SortOrder Sort { get; init; }

    public bool IsSelected { get; set; }
}
