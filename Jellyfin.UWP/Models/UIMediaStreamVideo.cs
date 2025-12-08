using WinRT;

namespace Jellyfin.UWP.Models;

[GeneratedBindableCustomProperty(propertyNames: [nameof(Title)], indexerPropertyTypes: [typeof(string)])]
public partial class UIMediaStreamVideo
{
    public string Title { get; init; } = default!;

    public string MediaSourceId { get; init; } = default!;

    public int MediaSourceListIndex { get; init; }
}
