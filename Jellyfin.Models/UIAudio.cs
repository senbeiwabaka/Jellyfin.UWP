namespace Jellyfin.Models;

//[GeneratedBindableCustomProperty(propertyNames: [nameof(IsSelected), nameof(Title)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
public partial class UIAudio
{
    public required string Title { get; init; } = default!;

    public bool IsSelected { get; set; }

    public int? Index { get; init; }
}
