namespace Jellyfin.UWP.Models
{
    public sealed class MediaPlayerPlayBackInfo
    {
        public string PlayMethod { get; set; }
        public string Protocol { get; set; }
        public string Stream { get; set; }
        public string Container { get; set; }
        public string Size { get; set; }
        public string Bitrate { get; set; }
        public string VideoCodec { get; set; }
        public string VideoBitrate { get; set; }
        public string VideoRangeType { get; set; }
        public string AudioCodec { get; set; }
        public string AudioBitrate { get; set; }
        public string AudioChannels { get; set; }
        public string AudioSampleRate { get; set; }
        public string VideoResolution { get; set; }
        public string PlayerDimensions { get; set; }
        public string TranscodingVideoCodec { get; set; }
        public string TranscodingAudioCodec { get; set; }
        public string TranscodingAudioChannels { get; set; }
        public string TranscodingBitrate { get; set; }
        public string TranscodingCompletion { get; set; }
        public string TranscodingFramerate { get; set; }
        public string TranscodingReason { get; set; }
    }
}
