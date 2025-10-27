using WinRT;

namespace Jellyfin.UWP.Models;

[GeneratedBindableCustomProperty(propertyNames: [nameof(IsSelected), nameof(Title)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
public partial class UIMediaStream
{
    public string Title { get; init; } = default!;

    public int MediaSourceIndex { get; init; }

    public bool IsSelected { get; set; }

    public int MediaStreamIndex { get; init; }

    public int MediaListIndex { get; init; }
}
