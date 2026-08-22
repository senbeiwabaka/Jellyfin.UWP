namespace Jellyfin.Models;

public sealed class DetailsItemPlayRecord
{
    /// <summary>
    /// Gets or sets the item id of the media.
    /// </summary>
    public Guid MediaId { get; set; }

    /// <summary>
    /// Gets or sets the selected audio index for Jellyfin data.
    /// </summary>
    public int? SelectedAudioIndex { get; set; }

    /// <summary>
    /// Gets or sets the video (media source id) selected to be watched.
    /// </summary>
    public string? SelectedVideoId { get; set; }
}
