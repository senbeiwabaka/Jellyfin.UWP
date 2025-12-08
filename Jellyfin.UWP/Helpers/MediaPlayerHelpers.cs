using Jellyfin.Sdk.Generated.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Media.Core;

namespace Jellyfin.UWP.Helpers;

internal static class MediaPlayerHelpers
{
    internal static PlaybackInfoDto GetPlaybackInfoBody(UserDto user, long startTimeTicks, string? mediaSourceId, int? selectedAudioMediaStreamIndex)
    {
        const string mp4VideoFormats = "h264,hevc,vp8,vp9";
        const string mkvVideoFormats = "h264,hevc,vc1,vp8,vp9";
        const string audioFormats = "aac,mp3,ac3";

        return new PlaybackInfoDto
        {
            UserId = user.Id.Value,
            AutoOpenLiveStream = true,
            EnableTranscoding = user.Policy.EnableVideoPlaybackTranscoding,
            EnableDirectPlay = true,
            AllowVideoStreamCopy = user.Policy.EnablePlaybackRemuxing,
            AllowAudioStreamCopy = user.Policy.EnablePlaybackRemuxing,
            MaxStreamingBitrate = 3 * 1_000_000,
            MaxAudioChannels = 6,
            StartTimeTicks = startTimeTicks,
            EnableDirectStream = true,
            AudioStreamIndex = selectedAudioMediaStreamIndex,
            MediaSourceId = mediaSourceId,
            

            DeviceProfile = new DeviceProfile
            {
                CodecProfiles =
                    [
                        new()
                    {
                        Codec = "aac",
                        Conditions =
                        [
                            new()
                            {
                                Condition = ProfileCondition_Condition.Equals,
                                Property = ProfileCondition_Property.IsSecondaryAudio,
                                Value = "false",
                            },
                        ],
                        Type = CodecProfile_Type.VideoAudio
                    },
                    new()
                        {
                            Codec = "h264",
                            Conditions =
                            [
                                new()
                                {
                                    Condition = ProfileCondition_Condition.NotEquals,
                                    Property = ProfileCondition_Property.IsAnamorphic,
                                    Value = "true",
                                    IsRequired = false,
                                },
                                new()
                                {
                                    Condition = ProfileCondition_Condition.EqualsAny,
                                    Property = ProfileCondition_Property.VideoProfile,
                                    Value = "high|main|baseline|constrained baseline",
                                    IsRequired = false,
                                },
                                new()
                                {
                                    Condition = ProfileCondition_Condition.EqualsAny,
                                    Property = ProfileCondition_Property.VideoRangeType,
                                    Value = "SDR",
                                    IsRequired = false,
                                },
                                new()
                                {
                                    Condition = ProfileCondition_Condition.LessThanEqual,
                                    Property = ProfileCondition_Property.VideoLevel,
                                    Value = "52",
                                    IsRequired = false,
                                },
                                new()
                                {
                                    Condition = ProfileCondition_Condition.NotEquals,
                                    Property = ProfileCondition_Property.IsInterlaced,
                                    Value = "true",
                                    IsRequired = false,
                                },
                            ],
                            Type = CodecProfile_Type.Video
                        },
                ],
                DirectPlayProfiles =
                    [
                        new()
                    {
                        Container = "mp4,m4v",
                        Type = DirectPlayProfile_Type.Video,
                        VideoCodec = mp4VideoFormats,
                        AudioCodec = audioFormats,
                    },
                    new()
                    {
                        Container = "mkv",
                        Type = DirectPlayProfile_Type.Video,
                        VideoCodec = mp4VideoFormats,
                        AudioCodec = audioFormats,
                    },
                    new()
                    {
                        Container = "m4a",
                        AudioCodec = "aac",
                        Type = DirectPlayProfile_Type.Audio,
                    },
                    new()
                    {
                        Container = "m4b",
                        AudioCodec = "aac",
                        Type = DirectPlayProfile_Type.Audio,
                    },
                    new()
                        {
                            Container = "mp3",
                            Type = DirectPlayProfile_Type.Audio,
                        },
                ],
                TranscodingProfiles =
                    [
                        new()
                    {
                        Container = "ts",
                        Type = TranscodingProfile_Type.Audio,
                        AudioCodec = "aac",
                        Context = TranscodingProfile_Context.Streaming,
                        Protocol = TranscodingProfile_Protocol.Hls,
                        MaxAudioChannels = "2",
                        BreakOnNonKeyFrames = true,
                        MinSegments = 1,
                    },
                    new()
                    {
                        Container = "aac",
                        Type = TranscodingProfile_Type.Audio,
                        AudioCodec = "aac",
                        Context = TranscodingProfile_Context.Streaming,
                        Protocol = TranscodingProfile_Protocol.Http,
                        MaxAudioChannels = "2",
                    },
                    new()
                    {
                        Container = "mp3",
                        Type = TranscodingProfile_Type.Audio,
                        AudioCodec = "mp3",
                        Context = TranscodingProfile_Context.Streaming,
                        Protocol = TranscodingProfile_Protocol.Http,
                        MaxAudioChannels = "2",
                    },
                    new()
                    {
                        Container = "ts",
                        Type = TranscodingProfile_Type.Video,
                        VideoCodec = "h264",
                        Context = TranscodingProfile_Context.Streaming,
                        MaxAudioChannels = "2",
                        AudioCodec = "aac,mp3",
                        BreakOnNonKeyFrames = true,
                        MinSegments = 1,
                        Protocol = TranscodingProfile_Protocol.Hls,
                    },
                    new()
                    {
                        Container = "mp4",
                        Type = TranscodingProfile_Type.Video,
                        VideoCodec = mp4VideoFormats,
                        Context = TranscodingProfile_Context.Streaming,
                        MaxAudioChannels = "2",
                        CopyTimestamps = true,
                        AudioCodec = "aac,mp3",
                    },
                    new()
                        {
                            Container = "mkv",
                            Type = TranscodingProfile_Type.Video,
                            VideoCodec = mkvVideoFormats,
                            Context = TranscodingProfile_Context.Streaming,
                            MaxAudioChannels = "2",
                            CopyTimestamps = true,
                            AudioCodec = "aac,mp3",
                        },
                ],
            },
        };
    }

    internal static readonly ReadOnlyDictionary<string, string> SupportedAudioCodecs = new(new Dictionary<string, string>
    {
        { "aac", CodecSubtypes.AudioFormatAac },
        { "ac3", CodecSubtypes.AudioFormatDolbyAC3 },
        { "alac", CodecSubtypes.AudioFormatAlac },
        { "flac", CodecSubtypes.AudioFormatFlac },
        { "eac3", CodecSubtypes.AudioFormatAac },
        { "mp3", CodecSubtypes.AudioFormatMP3 },
    });

    internal static readonly ReadOnlyDictionary<string, string> SupportedVideoCodecs = new(new Dictionary<string, string>
    {
        { "mp4v", CodecSubtypes.VideoFormatMP4V },
        { "h264", CodecSubtypes.VideoFormatH264 },
        { "hevc", CodecSubtypes.VideoFormatHevc },
        { "h263", CodecSubtypes.VideoFormatH263 },
        { "av1", "{31305641-0000-0010-8000-00AA00389B71}" },
    });
}
