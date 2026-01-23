using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.Models.Filters;

//[GeneratedBindableCustomProperty(propertyNames: [nameof(IsSelected), nameof(Label)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
public partial class SortOrderModel
{
    public required string Label { get; init; } = default!;

    public required SortOrder Sort { get; init; }

    public bool IsSelected { get; set; }
}
