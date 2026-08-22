namespace Jellyfin.Models;

//[GeneratedBindableCustomProperty(propertyNames: [nameof(IsDefault), nameof(Title)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
public partial class UIMediaStream
{
    public string Title { get; init; } = default!;

    public bool IsDefault { get; set; }

    public int MediaStreamIndex { get; init; }
}
