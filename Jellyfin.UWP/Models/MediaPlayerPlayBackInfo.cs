namespace Jellyfin.UWP.Models
{
    public sealed class MediaPlayerPlayBackInfo
    {
        public string PlayMethod { get; internal set; }
        public string Protocol { get; internal set; }
        public string Stream { get; internal set; }
        public string Container { get; internal set; }
        public string Size { get; internal set; }
        public string Bitrate { get; internal set; }
        public string VideoCodec { get; internal set; }
        public string VideoBitrate { get; internal set; }
        public string VideoRangeType { get; internal set; }
        public string AudioCodec { get; internal set; }
        public string AudioBitrate { get; internal set; }
        public string AudioChannels { get; internal set; }
        public string AudioSampleRate { get; internal set; }
        public string VideoResolution { get; internal set; }
        public string PlayerDimensions { get; internal set; }
        public string TranscodingVideoCodec { get; internal set; }
        public string TranscodingAudioCodec { get; internal set; }
        public string TranscodingAudioChannels { get; internal set; }
        public string TranscodingBitrate { get; internal set; }
        public string TranscodingCompletion { get; internal set; }
        public string TranscodingFramerate { get; internal set; }
        public string TranscodingReason { get; internal set; }
    }
}
