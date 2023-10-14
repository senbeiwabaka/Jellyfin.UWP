﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Jellyfin.UWP
{
    public sealed partial class MediaItemPlayerViewModel : ObservableObject
    {
        private readonly IMediaInfoClient mediaInfoClient;
        private readonly IMemoryCache memoryCache;
        private readonly IPlaystateClient playstateClient;
        private readonly ISubtitleClient subtitleClient;
        private readonly IUserLibraryClient userLibraryClient;
        private readonly ITvShowsClient tvShowsClient;
        private readonly IVideosClient videosClient;

        private BaseItemDto item;
        private Guid itemId;
        private string playbackSessionId = string.Empty;

        public MediaItemPlayerViewModel(
            IMemoryCache memoryCache,
            IVideosClient videosClient,
            IPlaystateClient playstateClient,
            IMediaInfoClient mediaInfoClient,
            ISubtitleClient subtitleClient,
            IUserLibraryClient userLibraryClient,
            ITvShowsClient tvShowsClient)
        {
            this.memoryCache = memoryCache;
            this.videosClient = videosClient;
            this.playstateClient = playstateClient;
            this.mediaInfoClient = mediaInfoClient;
            this.subtitleClient = subtitleClient;
            this.userLibraryClient = userLibraryClient;
            this.tvShowsClient = tvShowsClient;
        }

        public string GetSubtitleUrl(int index, string routeFormat)
        {
            var routeId = itemId.ToString().Replace("-", string.Empty);

            return subtitleClient.GetSubtitleWithTicksUrl(itemId, routeId, index, 0, routeFormat);
        }

        public Uri GetVideoUrl()
        {
            var container = item.MediaSources[0].Container;
            var videoUrl = videosClient.GetVideoStreamByContainerUrl(
                itemId,
                container,
                @static: true);

            return new Uri(videoUrl);
        }

        public async Task<BaseItemDto> LoadMediaItemAsync(Guid id)
        {
            itemId = id;

            var user = memoryCache.Get<UserDto>("user");
            item = await userLibraryClient.GetItemAsync(user.Id, id);

            return item;
        }

        public async Task<MediaSourceInfo> LoadMediaPlaybackInfoAsync()
        {
            const string mp4VideoFormats = "h264,vp8,vp9";
            const string mkvVideoFormats = "h264,vc1,vp8,vp9,av1";
            const string audioFormarts = "aac,mp3,ac3";

            long? startTimeTicks = null;

            if (item.UserData.PlayedPercentage.HasValue && item.UserData.PlayedPercentage < 90)
            {
                startTimeTicks = item.UserData.PlaybackPositionTicks;
            }

            var user = memoryCache.Get<UserDto>("user");
            var playbackInfo = await mediaInfoClient.GetPostedPlaybackInfoAsync(
                itemId,
                body: new PlaybackInfoDto
                {
                    UserId = user.Id,
                    AutoOpenLiveStream = true,
                    EnableTranscoding = true,
                    AllowVideoStreamCopy = true,
                    AllowAudioStreamCopy = true,
                    MaxStreamingBitrate = 20_000_000,
                    MaxAudioChannels = 5,
                    StartTimeTicks = startTimeTicks,
                    EnableDirectStream = true,
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
                                        Condition = ProfileConditionType.EqualsAny,
                                        Property = ProfileConditionValue.IsAnamorphic,
                                        Value = "true",
                                    },
                                    new ProfileCondition
                                    {
                                        Condition = ProfileConditionType.EqualsAny,
                                        Property = ProfileConditionValue.VideoProfile,
                                        Value = "high|main|baseline|constrained baseline",
                                    },
                                    new ProfileCondition
                                    {
                                        Condition = ProfileConditionType.LessThanEqual,
                                        Property = ProfileConditionValue.VideoLevel,
                                        Value = "42",
                                    },
                                },
                                Type = CodecType.Video
                            },
                        },
                        DirectPlayProfiles = new[]
                        {
                            new DirectPlayProfile
                            {
                                Container = "mp4,m4v",
                                Type = DlnaProfileType.Video,
                                VideoCodec = mp4VideoFormats,
                                AudioCodec = audioFormarts,
                            },
                            new DirectPlayProfile
                            {
                                Container = "mkv",
                                Type = DlnaProfileType.Video,
                                VideoCodec = mp4VideoFormats,
                                AudioCodec = audioFormarts,
                            },
                            new DirectPlayProfile
                            {
                                Container = "m4a",
                                AudioCodec = "aac",
                                Type = DlnaProfileType.Audio,
                            },
                            new DirectPlayProfile
                            {
                                Container = "m4b",
                                AudioCodec = "aac",
                                Type = DlnaProfileType.Audio,
                            },
                            new DirectPlayProfile
                            {
                                Container = "mp3",
                                Type = DlnaProfileType.Audio,
                            },
                        },
                        TranscodingProfiles = new[]
                        {
                            new TranscodingProfile
                            {
                                Container = "ts",
                                Type = DlnaProfileType.Audio,
                                AudioCodec = "aac",
                                Context = EncodingContext.Streaming,
                                Protocol = "hls",
                                MaxAudioChannels = "2",
                                BreakOnNonKeyFrames = true,
                                MinSegments = 1,
                            },
                            new TranscodingProfile
                            {
                                Container = "aac",
                                Type = DlnaProfileType.Audio,
                                AudioCodec = "aac",
                                Context = EncodingContext.Streaming,
                                Protocol = "http",
                                MaxAudioChannels = "2",
                            },
                            new TranscodingProfile
                            {
                                Container = "mp3",
                                Type = DlnaProfileType.Audio,
                                AudioCodec = "mp3",
                                Context = EncodingContext.Streaming,
                                Protocol = "http",
                                MaxAudioChannels = "2",
                            },
                            new TranscodingProfile
                            {
                                Container = "ts",
                                Type = DlnaProfileType.Video,
                                VideoCodec = "h264",
                                Context = EncodingContext.Streaming,
                                MaxAudioChannels = "2",
                                AudioCodec = "aac,mp3",
                                BreakOnNonKeyFrames = true,
                                MinSegments = 1,
                                Protocol = "hls",
                            },
                            new TranscodingProfile
                            {
                                Container = "mp4",
                                Type = DlnaProfileType.Video,
                                VideoCodec = mp4VideoFormats,
                                Context = EncodingContext.Streaming,
                                MaxAudioChannels = "2",
                                CopyTimestamps = true,
                                AudioCodec = "aac,mp3",
                            },
                            new TranscodingProfile
                            {
                                Container = "mkv",
                                Type = DlnaProfileType.Video,
                                VideoCodec = mkvVideoFormats,
                                Context = EncodingContext.Streaming,
                                MaxAudioChannels = "2",
                                CopyTimestamps = true,
                                AudioCodec = "aac,mp3",
                            },
                        },
                        ResponseProfiles = new[]
                        {
                            new ResponseProfile
                            {
                                Container = "m4v",
                                MimeType = "video/mp4",
                                Type = DlnaProfileType.Video,
                            },
                        },
                    },
                });

            playbackSessionId = playbackInfo.PlaySessionId;

            return playbackInfo.MediaSources.Single();
        }

        public async Task SessionPlayingAsync(bool isTranscoding)
        {
            var session = memoryCache.Get<SessionInfo>("session");
            var playbackStartInfo = new PlaybackStartInfo
            {
                ItemId = itemId,
                SessionId = session.Id,
                PlayMethod = isTranscoding ? PlayMethod.Transcode : PlayMethod.DirectPlay,
                CanSeek = true,
                IsMuted = false,
                IsPaused = false,
                PlaySessionId = playbackSessionId,
            };

            await playstateClient.ReportPlaybackStartAsync(playbackStartInfo);
        }

        public async Task SessionProgressAsync(long position, bool isTranscoding, bool isPaused)
        {
            var session = memoryCache.Get<SessionInfo>("session");
            var playbackProgressInfo = new PlaybackProgressInfo
            {
                SessionId = session.Id,
                ItemId = itemId,
                PositionTicks = position,
                PlayMethod = isTranscoding ? PlayMethod.Transcode : PlayMethod.DirectPlay,
                CanSeek = true,
                IsMuted = false,
                IsPaused = isPaused,
                PlaySessionId = playbackSessionId,
            };

            await playstateClient.ReportPlaybackProgressAsync(playbackProgressInfo);
        }

        public async Task SessionStopAsync(long position)
        {
            var session = memoryCache.Get<SessionInfo>("session");

            await playstateClient.ReportPlaybackStoppedAsync(
                new PlaybackStopInfo
                {
                    PositionTicks = position,
                    ItemId = itemId,
                    SessionId = session.Id,
                    PlaySessionId = playbackSessionId,
                });
        }

        public async Task<BaseItemDtoQueryResult> GetSeriesAsync(Guid seriesId, Guid seasonId)
        {
            var user = memoryCache.Get<UserDto>("user");
            return await tvShowsClient.GetEpisodesAsync(
                    seriesId: seriesId,
                    userId: user.Id,
                    seasonId: seasonId,
                    fields: new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.BasicSyncInfo,
                    });
        }

        [RelayCommand]
        private async Task OpenLogsAsync()
        {
            var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("MetroLogs");

            await Launcher.LaunchFolderAsync(folder);
        }
    }
}
