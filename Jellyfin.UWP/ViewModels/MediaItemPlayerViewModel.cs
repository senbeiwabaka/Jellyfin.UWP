using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Windows.Media.Core;

namespace Jellyfin.UWP
{
    public sealed partial class MediaItemPlayerViewModel : ObservableObject
    {
        private readonly IMediaInfoClient mediaInfoClient;
        private readonly IMemoryCache memoryCache;
        private readonly IPlaystateClient playstateClient;
        private readonly ISessionClient sessionClient;
        private readonly ISubtitleClient subtitleClient;

        private readonly IReadOnlyDictionary<string, string> supportedAudioCodecs = new Dictionary<string, string>
        {
            { "aac", CodecSubtypes.AudioFormatAac },
            { "ac3", CodecSubtypes.AudioFormatDolbyAC3 },
            { "alac", CodecSubtypes.AudioFormatAlac },
            { "flac", CodecSubtypes.AudioFormatFlac },
            { "eac3", CodecSubtypes.AudioFormatAac },
            { "mp3", CodecSubtypes.AudioFormatMP3 },
        };

        private readonly IReadOnlyDictionary<string, string> supportedVideoCodecs = new Dictionary<string, string>
        {
            { "mp4v", CodecSubtypes.VideoFormatMP4V },
            { "h264", CodecSubtypes.VideoFormatH264 },
            { "hevc", CodecSubtypes.VideoFormatHevc },
            { "h263", CodecSubtypes.VideoFormatH263 },
        };

        private readonly ITvShowsClient tvShowsClient;

        private readonly IReadOnlyDictionary<string, string> unSupportedAudioCodecs = new Dictionary<string, string>
        {
            { "dts", CodecSubtypes.AudioFormatDts },
        };

        private readonly UserDto user;
        private readonly IUserLibraryClient userLibraryClient;
        private readonly IVideosClient videosClient;

        private DetailsItemPlayRecord detailsItemPlayRecord;

        [ObservableProperty]
        private bool isTranscoding;

        private BaseItemDto item;

        [ObservableProperty]
        private MediaPlayerPlayBackInfo mediaPlayerPlayBackInfo;

        private MediaSourceInfo mediaSourceInfo;
        private PlaybackInfoResponse playbackInfo;
        private string playbackSessionId = string.Empty;

        public MediaItemPlayerViewModel(
            IMemoryCache memoryCache,
            IVideosClient videosClient,
            IPlaystateClient playstateClient,
            IMediaInfoClient mediaInfoClient,
            ISubtitleClient subtitleClient,
            IUserLibraryClient userLibraryClient,
            ITvShowsClient tvShowsClient,
            ISessionClient sessionClient)
        {
            this.memoryCache = memoryCache;
            this.videosClient = videosClient;
            this.playstateClient = playstateClient;
            this.mediaInfoClient = mediaInfoClient;
            this.subtitleClient = subtitleClient;
            this.userLibraryClient = userLibraryClient;
            this.tvShowsClient = tvShowsClient;
            this.sessionClient = sessionClient;
            user = memoryCache.Get<UserDto>("user");
        }

        public async Task<BaseItemDtoQueryResult> GetNextSeasonEpisodes(Guid seriesId, Guid seasonId)
        {
            var seasons = await tvShowsClient.GetSeasonsAsync(
                    seriesId,
                    user.Id,
                    fields: new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.MediaSourceCount,
                    });
            var index = 0;
            foreach (var season in seasons.Items)
            {
                if (season.Id == seasonId)
                {
                    if (seasons.TotalRecordCount - 1 <= ++index)
                    {
                        return null;
                    }

                    return await tvShowsClient.GetEpisodesAsync(
                        seriesId: seriesId,
                        userId: user.Id,
                        seasonId: seasons.Items[index].SeasonId,
                        fields: new[]
                        {
                            ItemFields.ItemCounts,
                            ItemFields.PrimaryImageAspectRatio,
                            ItemFields.BasicSyncInfo,
                        });
                }

                ++index;
            }

            return null;
        }

        public async Task GetPlaybackInfo(double playerWidth, double playerHeight)
        {
            var session = (await sessionClient.GetSessionsAsync(deviceId: "Jellyfin.UWP")).FirstOrDefault();

            var videoMediaStream = mediaSourceInfo.MediaStreams.Single(x => x.Type == MediaStreamType.Video);
            MediaStream audioMediaStream;

            if (detailsItemPlayRecord.SelectedAudioMediaStreamIndex is null && user.Configuration.PlayDefaultAudioTrack)
            {
                audioMediaStream = mediaSourceInfo.MediaStreams.Single(x => x.IsDefault && x.Type == MediaStreamType.Audio);
            }
            else
            {
                audioMediaStream = mediaSourceInfo.MediaStreams.First(x => x.Type == MediaStreamType.Audio);
            }

            MediaPlayerPlayBackInfo = new MediaPlayerPlayBackInfo
            {
                PlayMethod = IsTranscoding ? "Transcoding" : "Direct Play",
                Protocol = "Https",
                Stream = IsTranscoding ? "HLS" : "Video",

                PlayerDimensions = $"{playerWidth:#}x{playerHeight:#}",
                VideoResolution = $"{videoMediaStream.Width}x{videoMediaStream.Height}",

                TranscodingVideoCodec = session?.TranscodingInfo.VideoCodec.ToUpper(),
                TranscodingAudioCodec = session?.TranscodingInfo.AudioCodec.ToUpper(),
                TranscodingAudioChannels = session?.TranscodingInfo.AudioChannels?.ToString(),
                TranscodingBitrate = session != null && session.TranscodingInfo.Bitrate.HasValue ? $"{session.TranscodingInfo.Bitrate.Value / 1000000m:#.#} Mbps" : "N/A",
                TranscodingCompletion = $"{session?.TranscodingInfo.CompletionPercentage?.ToString("#.#")}%",
                TranscodingFramerate = $"{session?.TranscodingInfo.Framerate} fps",
                TranscodingReason = string.Join(",", session?.TranscodingInfo.TranscodeReasons),

                Container = mediaSourceInfo.Container,
                Size = mediaSourceInfo.Size.HasValue ? $"{mediaSourceInfo.Size.Value / 1073741824m:#.#} GiB" : "N/A",
                Bitrate = mediaSourceInfo.Bitrate.HasValue ? $"{mediaSourceInfo.Bitrate.Value / 1000000m:#.#} Mbps" : "N/A",
                VideoCodec = $"{videoMediaStream.Codec.ToUpper()} {videoMediaStream.Profile}",
                VideoBitrate = videoMediaStream.BitRate.HasValue ? $"{videoMediaStream.BitRate.Value / 1000000m:#.#} Mbps" : "N/A",
                VideoRangeType = videoMediaStream.VideoRangeType,
                AudioCodec = $"{audioMediaStream.Codec.ToUpper()} {audioMediaStream.Profile}",
                AudioBitrate = audioMediaStream.BitRate.HasValue ? $"{audioMediaStream.BitRate.Value / 1000:#} kbps" : "N/A",
                AudioChannels = audioMediaStream.Channels.HasValue ? audioMediaStream.Channels.ToString() : "N/A",
                AudioSampleRate = audioMediaStream.SampleRate.HasValue ? $"{audioMediaStream.SampleRate} Hz" : "N/A",
            };
        }

        public async Task<BaseItemDtoQueryResult> GetSeriesAsync(Guid seriesId, Guid seasonId)
        {
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

        public string GetSubtitleUrl(int index, string routeFormat)
        {
            var routeId = item.Id.ToString().Replace("-", string.Empty);

            return subtitleClient.GetSubtitleWithTicksUrl(item.Id, routeId, index, 0, routeFormat);
        }

        public Uri GetVideoUrl(string videoId = null)
        {
            var container = item.MediaSources[0].Container;
            var videoUrl = videosClient.GetVideoStreamByContainerUrl(
                item.Id,
                container,
                @static: true,
                mediaSourceId: videoId);

            return new Uri(videoUrl);
        }

        public async Task<bool> IsTranscodingNeededBecauseOfAudio(DetailsItemPlayRecord detailsItemPlayRecord, IReadOnlyList<MediaStream> mediaStreams)
        {
            var codecQuery = new CodecQuery();
            var selectedAudioCodec = string.Empty;

            // Get the selected audio codec, if one was, or the default (first) codec.
            if (detailsItemPlayRecord.SelectedAudioMediaStreamIndex.HasValue)
            {
                selectedAudioCodec = mediaStreams.Single(x => x.Index == detailsItemPlayRecord.SelectedAudioMediaStreamIndex.Value && x.Type == MediaStreamType.Audio).Codec;
            }
            else
            {
                selectedAudioCodec = mediaStreams.First(x => x.Type == MediaStreamType.Audio).Codec;
            }

            var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Decoder, ""))
                .Select(x => x).ToArray();

            // Check if the selected audio codec is a supported, by default, audio codec
            if (supportedAudioCodecs.ContainsKey(selectedAudioCodec))
            {
                var audioCodecId = supportedAudioCodecs[selectedAudioCodec];

                // Check to make sure the codec actually is there to use
                return !Array.Exists(audioCodecsInstalled, x => x.Subtypes.Any(y => y.Equals(audioCodecId, StringComparison.InvariantCultureIgnoreCase)));
            }

            // Check the "unsupported" as in not built in list
            if (unSupportedAudioCodecs.ContainsKey(selectedAudioCodec))
            {
                var audioCodecId = unSupportedAudioCodecs[selectedAudioCodec];

                // Check to make sure the codec actually is there to use
                return !Array.Exists(audioCodecsInstalled, x => x.Subtypes.Any(y => y.Equals(audioCodecId, StringComparison.InvariantCultureIgnoreCase)));
            }

            return true;
        }

        public async Task<bool> IsTranscodingNeededBecauseOfVideo(IReadOnlyList<MediaStream> mediaStreams)
        {
            // I have not seen where 10-bit will work at all so we automatically need to use the transcoded version of those
            if (mediaStreams.Any(x => x.Type == MediaStreamType.Video && x.BitDepth == 10))
            {
                return true;
            }

            var codecQuery = new CodecQuery();
            var selectedVideoCodec = mediaStreams.First(x => x.Type == MediaStreamType.Video).Codec;
            var videoCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Decoder, ""))
                .Select(x => x).ToArray();

            if (supportedVideoCodecs.ContainsKey(selectedVideoCodec))
            {
                var videoCodecId = supportedVideoCodecs[selectedVideoCodec];

                // Check to make sure the codec actually is there to use
                return !Array.Exists(videoCodecsInstalled, x => x.Subtypes.Any(y => y.Equals(videoCodecId, StringComparison.InvariantCultureIgnoreCase)));
            }

            return true;
        }

        public async Task<BaseItemDto> LoadMediaItemAsync(DetailsItemPlayRecord detailsItemPlayRecord)
        {
            this.detailsItemPlayRecord = detailsItemPlayRecord;

            item = await userLibraryClient.GetItemAsync(user.Id, detailsItemPlayRecord.Id);

            return item;
        }

        public async Task<MediaSourceInfo> LoadMediaPlaybackInfoAsync(string videoId = null)
        {
            long? startTimeTicks = null;

            if (item.UserData.PlayedPercentage.HasValue && item.UserData.PlayedPercentage < 90)
            {
                startTimeTicks = item.UserData.PlaybackPositionTicks;
            }

            playbackInfo = await mediaInfoClient.GetPostedPlaybackInfoAsync(
                item.Id,
                body: GetPlaybackInfoBody(user.Id, startTimeTicks));

            playbackSessionId = playbackInfo.PlaySessionId;

            if (string.IsNullOrWhiteSpace(videoId))
            {
                mediaSourceInfo = playbackInfo.MediaSources.Single();

                return mediaSourceInfo;
            }

            mediaSourceInfo = playbackInfo.MediaSources.Single(x => string.Equals(x.Id, videoId, StringComparison.CurrentCultureIgnoreCase));

            return mediaSourceInfo;
        }

        public async Task SessionPlayingAsync()
        {
            var session = memoryCache.Get<SessionInfo>("session");
            var playbackStartInfo = new PlaybackStartInfo
            {
                ItemId = item.Id,
                SessionId = session.Id,
                PlayMethod = IsTranscoding ? PlayMethod.Transcode : PlayMethod.DirectPlay,
                CanSeek = true,
                IsMuted = false,
                IsPaused = false,
                PlaySessionId = playbackSessionId,
            };

            await playstateClient.ReportPlaybackStartAsync(playbackStartInfo);
        }

        public async Task SessionProgressAsync(long position, bool isPaused)
        {
            var session = memoryCache.Get<SessionInfo>("session");
            var playbackProgressInfo = new PlaybackProgressInfo
            {
                SessionId = session.Id,
                ItemId = item.Id,
                PositionTicks = position,
                PlayMethod = IsTranscoding ? PlayMethod.Transcode : PlayMethod.DirectPlay,
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
                    ItemId = item.Id,
                    SessionId = session.Id,
                    PlaySessionId = playbackSessionId,
                });
        }

        private static PlaybackInfoDto GetPlaybackInfoBody(Guid userId, long? startTimeTicks)
        {
            const string mp4VideoFormats = "h264,vp8,vp9";
            const string mkvVideoFormats = "h264,vc1,vp8,vp9,av1";
            const string audioFormarts = "aac,mp3,ac3";

            return new PlaybackInfoDto
            {
                UserId = userId,
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
            };
        }
    }
}
