using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP
{
    public sealed partial class MediaItemPlayerViewModel : ObservableObject
    {
        private readonly IMediaInfoClient mediaInfoClient;
        private readonly IMemoryCache memoryCache;
        private readonly IPlaystateClient playstateClient;
        private readonly ISubtitleClient subtitleClient;
        private readonly IUserLibraryClient userLibraryClient;
        private readonly IVideosClient videosClient;
        private Guid id;

        public MediaItemPlayerViewModel(
            IMemoryCache memoryCache,
            IVideosClient videosClient,
            IPlaystateClient playstateClient,
            IMediaInfoClient mediaInfoClient,
            ISubtitleClient subtitleClient,
            IUserLibraryClient userLibraryClient)
        {
            this.memoryCache = memoryCache;
            this.videosClient = videosClient;
            this.playstateClient = playstateClient;
            this.mediaInfoClient = mediaInfoClient;
            this.subtitleClient = subtitleClient;
            this.userLibraryClient = userLibraryClient;
        }

        public string GetSubtitleUrl(Guid id, int index, string routeFormat)
        {
            var routeId = id.ToString().Replace("-", string.Empty);

            return subtitleClient.GetSubtitleWithTicksUrl(id, routeId, index, 0, routeFormat);
        }

        public Uri GetVideoUrl(
            Guid id,
            bool needsEncoding,
            int? subtitleIndex = null,
            string container = "mp4",
            int? audioStreamIndex = null,
            string audioCodec = "aac,mp3",
            int? videoStreamIndex = null,
            string videoCodec = "h264,h264")
        {
            this.id = id;

            string videoUrl;

            if (needsEncoding)
            {
                videoUrl = videosClient.GetVideoStreamUrl(
                    id,
                    container: container,
                    @static: false,
                    subtitleStreamIndex: subtitleIndex,
                    audioStreamIndex: audioStreamIndex,
                    audioCodec: audioCodec,
                    videoStreamIndex: videoStreamIndex,
                    videoCodec: videoCodec,
                    videoBitRate: 139616000,
                    audioSampleRate: 384000,
                    maxFramerate: 23.976f,
                    requireAvc: false,
                    breakOnNonKeyFrames: true,
                    mediaSourceId: id.ToString().Replace("-", string.Empty),
                    transcodeReasons: "VideoCodecNotSupported, AudioCodecNotSupported");
            }
            else
            {
                videoUrl = videosClient.GetVideoStreamUrl(id, container: container, @static: true);
            }

            return new Uri(videoUrl);
        }

        public async Task<MediaSourceInfo> LoadMediaPlaybackInfoAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>("user");
            var playbackInfo = await mediaInfoClient.GetPostedPlaybackInfoAsync(
                id,
                body: new PlaybackInfoDto
                {
                    UserId = user.Id,
                    EnableDirectPlay = true,
                    AutoOpenLiveStream = true,
                    EnableTranscoding = true,
                    AllowVideoStreamCopy = true,
                    AllowAudioStreamCopy = true,
                    MaxStreamingBitrate = 140000000,
                    MediaSourceId = id.ToString().Replace("-", string.Empty),
                    DeviceProfile = new DeviceProfile
                    {
                        CodecProfiles = new[]
                        {
                            new CodecProfile
                            {
                                Codec = "aac",
                                Conditions = new []
                                {
                                    new ProfileCondition
                                    {
                                        Condition = ProfileConditionType.Equals,
                                        IsRequired = false,
                                        Property = ProfileConditionValue.IsSecondaryAudio,
                                        Value = "false",
                                    },
                                },
                                Type = CodecType.VideoAudio
                            },
                            new CodecProfile
                            {
                                Conditions = new []
                                {
                                    new ProfileCondition
                                    {
                                        Condition = ProfileConditionType.Equals,
                                        IsRequired = false,
                                        Property = ProfileConditionValue.IsSecondaryAudio,
                                        Value = "false",
                                    },
                                },
                                Type = CodecType.VideoAudio
                            },
                            new CodecProfile
                            {
                                Codec = "h264",
                                Conditions = new []
                                {
                                    new ProfileCondition
                                    {
                                        Condition = ProfileConditionType.Equals,
                                        IsRequired = false,
                                        Property = ProfileConditionValue.IsSecondaryAudio,
                                        Value = "false",
                                    },
                                },
                                Type = CodecType.VideoAudio
                            },
                        },
                        DirectPlayProfiles = new[]
                        {
                            new DirectPlayProfile
                            {
                                Container = "aac",
                                Type = DlnaProfileType.Audio
                            },
                        },
                    },
                });

            return playbackInfo.MediaSources.Single();
        }

        public async Task<BaseItemDto> LoadMediaItemAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>("user");
            var item = await userLibraryClient.GetItemAsync(user.Id, id);

            return item;
        }

        public async Task SessionPlayingAsync()
        {
            var session = memoryCache.Get<SessionInfo>("session");

            await playstateClient.ReportPlaybackStartAsync(
                new PlaybackStartInfo
                {
                    MediaSourceId = id.ToString(),
                    ItemId = id,
                    SessionId = session.Id,
                });
        }

        public async Task SessionProgressAsync(long position)
        {
            var session = memoryCache.Get<SessionInfo>("session");

            await playstateClient.ReportPlaybackProgressAsync(
                new PlaybackProgressInfo
                {
                    MediaSourceId = id.ToString(),
                    SessionId = session.Id,
                    ItemId = id,
                    PositionTicks = position,
                });
        }

        public async Task SessionStopAsync(long position)
        {
            var session = memoryCache.Get<SessionInfo>("session");

            await playstateClient.ReportPlaybackStoppedAsync(
                new PlaybackStopInfo
                {
                    PositionTicks = position,
                    ItemId = id,
                    SessionId = session.Id,
                    MediaSourceId = id.ToString(),
                });
        }
    }
}
