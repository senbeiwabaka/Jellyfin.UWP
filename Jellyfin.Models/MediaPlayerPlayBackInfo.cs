namespace Jellyfin.Models;

public sealed class MediaPlayerPlayBackInfo
{
    public string PlayMethod { get; set; } = default!;
    public string Protocol { get; set; } = default!;
    public string Stream { get; set; } = default!;
    public string Container { get; set; } = default!;
    public string Size { get; set; } = default!;
    public string Bitrate { get; set; } = default!;
    public string VideoCodec { get; set; } = default!;
    public string VideoBitrate { get; set; } = default!;
    public string? VideoRangeType { get; set; }
    public string AudioCodec { get; set; } = default!;
    public string AudioBitrate { get; set; } = default!;
    public string AudioChannels { get; set; } = default!;
    public string AudioSampleRate { get; set; } = default!;
    public string VideoResolution { get; set; } = default!;
    public string PlayerDimensions { get; set; } = default!;
    public string? TranscodingVideoCodec { get; set; }
    public string? TranscodingAudioCodec { get; set; }
    public string? TranscodingAudioChannels { get; set; }
    public string? TranscodingBitrate { get; set; }
    public string? TranscodingCompletion { get; set; }
    public string? TranscodingFramerate { get; set; }
    public string? TranscodingReason { get; set; }
}
