using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace Jellyfin.UWP
{
    public sealed partial class MediaItemPlayerViewModel : ObservableObject
    {
        private readonly JellyfinApiClient apiClient;
        private readonly IMemoryCache memoryCache;

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

        private readonly IReadOnlyDictionary<string, string> unSupportedAudioCodecs = new Dictionary<string, string>
        {
            { "dts", CodecSubtypes.AudioFormatDts },
        };

        private readonly UserDto user;

        private DetailsItemPlayRecord detailsItemPlayRecord;

        [ObservableProperty]
        private bool isTranscoding;

        private BaseItemDto item;

        [ObservableProperty]
        private MediaPlayerPlayBackInfo mediaPlayerPlayBackInfo;

        private MediaSourceInfo mediaSourceInfo;
        private PlaybackInfoResponse playbackInfo;
        private string playbackSessionId = string.Empty;

        public MediaItemPlayerViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
            user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
        }

        public async Task<BaseItemDtoQueryResult> GetNextSeasonEpisodes(Guid seriesId, Guid seasonId)
        {
            var seasons = await apiClient.Shows[seriesId].Seasons
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.Fields = new[] { ItemFields.ItemCounts, ItemFields.MediaSourceCount, };
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

                    return await apiClient.Shows[seriesId].Episodes
                        .GetAsync(options =>
                        {
                            options.QueryParameters.UserId = user.Id;
                            options.QueryParameters.SeasonId = seasons.Items[index].SeasonId;
                            options.QueryParameters.Fields = new[] { ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio, };
                        });
                }

                ++index;
            }

            return null;
        }

        public async Task GetPlaybackInfo(double playerWidth, double playerHeight)
        {
            var session = (await apiClient.Sessions.GetAsync(options => options.QueryParameters.DeviceId = JellyfinConstants.DeviceId))
                .FirstOrDefault();

            string transcodingVideoCodec = null;
            string transcodingAudioCodec = null;
            string transcodingAudioChannels = null;
            string transcodingBitrate = null;
            string transcodingCompletion = null;
            string transcodingFramerate = null;
            string transcodingReason = null;

            if (session is not null && session.TranscodingInfo is not null)
            {
                transcodingVideoCodec = session.TranscodingInfo.VideoCodec.ToUpper();
                transcodingAudioCodec = session.TranscodingInfo.AudioCodec.ToUpper();
                transcodingAudioChannels = session.TranscodingInfo.AudioChannels?.ToString();
                transcodingBitrate = session.TranscodingInfo.Bitrate.HasValue ? $"{session.TranscodingInfo.Bitrate.Value / 1000000m:#.#} Mbps" : "N/A";
                transcodingCompletion = $"{session.TranscodingInfo.CompletionPercentage?.ToString("#.#")}%";
                transcodingFramerate = $"{session.TranscodingInfo.Framerate} fps";
                transcodingReason = string.Join(",", session.TranscodingInfo?.TranscodeReasons);
            }

            var videoMediaStream = mediaSourceInfo.MediaStreams.Single(x => x.Type == MediaStream_Type.Video);
            MediaStream audioMediaStream;

            if (detailsItemPlayRecord.SelectedAudioMediaStreamIndex is null && user.Configuration.PlayDefaultAudioTrack.Value && session is not null && session.TranscodingInfo is null)
            {
                audioMediaStream = mediaSourceInfo.MediaStreams.Single(x => x.IsDefault.Value && x.Type == MediaStream_Type.Audio);
            }
            else
            {
                audioMediaStream = mediaSourceInfo.MediaStreams.First(x => x.Type == MediaStream_Type.Audio);
            }

            MediaPlayerPlayBackInfo = new MediaPlayerPlayBackInfo
            {
                PlayMethod = IsTranscoding ? "Transcoding" : "Direct Play",
                Protocol = "Https",
                Stream = IsTranscoding ? "HLS" : "Video",

                PlayerDimensions = $"{playerWidth:#}x{playerHeight:#}",
                VideoResolution = $"{videoMediaStream.Width}x{videoMediaStream.Height}",

                TranscodingVideoCodec = transcodingVideoCodec,
                TranscodingAudioCodec = transcodingAudioCodec,
                TranscodingAudioChannels = transcodingAudioChannels,
                TranscodingBitrate = transcodingBitrate,
                TranscodingCompletion = transcodingCompletion,
                TranscodingFramerate = transcodingFramerate,
                TranscodingReason = transcodingReason,

                Container = mediaSourceInfo.Container,
                Size = mediaSourceInfo.Size.HasValue ? $"{mediaSourceInfo.Size.Value / 1073741824m:#.#} GiB" : "N/A",
                Bitrate = mediaSourceInfo.Bitrate.HasValue ? $"{mediaSourceInfo.Bitrate.Value / 1000000m:#.#} Mbps" : "N/A",
                VideoCodec = $"{videoMediaStream.Codec.ToUpper()} {videoMediaStream.Profile}",
                VideoBitrate = videoMediaStream.BitRate.HasValue ? $"{videoMediaStream.BitRate.Value / 1000000m:#.#} Mbps" : "N/A",
                VideoRangeType = videoMediaStream.VideoRangeType?.ToString(),
                AudioCodec = $"{audioMediaStream.Codec.ToUpper()} {audioMediaStream.Profile}",
                AudioBitrate = audioMediaStream.BitRate.HasValue ? $"{audioMediaStream.BitRate.Value / 1000:#} kbps" : "N/A",
                AudioChannels = audioMediaStream.Channels.HasValue ? audioMediaStream.Channels.ToString() : "N/A",
                AudioSampleRate = audioMediaStream.SampleRate.HasValue ? $"{audioMediaStream.SampleRate} Hz" : "N/A",
            };
        }

        public async Task<BaseItemDtoQueryResult> GetSeriesAsync(Guid seriesId, Guid seasonId)
        {
            return await apiClient.Shows[seriesId].Episodes
                         .GetAsync(options =>
                         {
                             options.QueryParameters.UserId = user.Id;
                             options.QueryParameters.SeasonId = seasonId;
                             options.QueryParameters.Fields = new[] { ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio, };
                         });
        }

        public Uri GetSubtitleUrl(int index, string routeFormat)
        {
            var routeId = item.Id.ToString().Replace("-", string.Empty);
            var subtitleRequest = apiClient.Videos[item.Id.Value][routeId].Subtitles[index][0]
                .StreamWithRouteFormat(routeFormat)
                .ToGetRequestInformation();

            return apiClient.BuildUri(subtitleRequest);
            //return subtitleClient.GetSubtitleWithTicksUrl(item.Id, routeId, index, 0, routeFormat);
        }

        public Uri GetVideoUrl(string videoId = null)
        {
            var container = item.MediaSources[0].Container;
            var video = apiClient.Videos[item.Id.Value]
                .StreamWithContainer(container)
                .ToGetRequestInformation(options =>
                {
                    options.QueryParameters.Static = true;
                    options.QueryParameters.MediaSourceId = videoId;
                });
            //var videoUrl = videosClient.GetVideoStreamByContainerUrl(
            //    item.Id,
            //    container,
            //    @static: true,
            //    mediaSourceId: videoId);

            //return new Uri(videoUrl);

            return apiClient.BuildUri(video);
        }

        public async Task<bool> IsTranscodingNeededBecauseOfAudio(DetailsItemPlayRecord detailsItemPlayRecord, IReadOnlyList<MediaStream> mediaStreams)
        {
            var codecQuery = new CodecQuery();
            var selectedAudioCodec = string.Empty;

            // Get the selected audio codec, if one was, or the default (first) codec.
            if (detailsItemPlayRecord.SelectedAudioMediaStreamIndex.HasValue)
            {
                selectedAudioCodec = mediaStreams.Single(x => x.Index == detailsItemPlayRecord.SelectedAudioMediaStreamIndex.Value && x.Type == MediaStream_Type.Audio).Codec;
            }
            else
            {
                selectedAudioCodec = mediaStreams.First(x => x.Type == MediaStream_Type.Audio).Codec;
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
            // If it is H264 10 bit then we transcode it
            if (mediaStreams.Any(x =>
                x.Type == MediaStream_Type.Video &&
                    x.BitDepth == 10 &&
                    string.Equals("H264", x.Codec, StringComparison.CurrentCultureIgnoreCase)))
            {
                return true;
            }

            var codecQuery = new CodecQuery();
            var selectedVideoCodec = mediaStreams.First(x => x.Type == MediaStream_Type.Video).Codec;
            var videoCodecsInstalled = new List<CodecInfo>();

            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                var h265 = (await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Decoder, "H265"))
                    .Select(x => x).ToArray();
                var h264 = (await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Decoder, "H264"))
                    .Select(x => x).ToArray();
                var hevc = (await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Decoder, "HEVC"))
                    .Select(x => x).ToArray();

                videoCodecsInstalled.AddRange(h265);
                videoCodecsInstalled.AddRange(h264);
                videoCodecsInstalled.AddRange(hevc);
            }
            else
            {
                var codecs = (await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Decoder, string.Empty))
                    .Select(x => x).ToArray();

                videoCodecsInstalled.AddRange(codecs);
            }

            if (supportedVideoCodecs.Keys.Any(x => string.Equals(x, selectedVideoCodec, StringComparison.OrdinalIgnoreCase)))
            {
                var videoCodecId = supportedVideoCodecs.Single(x => string.Equals(x.Key, selectedVideoCodec, StringComparison.OrdinalIgnoreCase)).Value;

                // Check to make sure the codec actually is there to use
                return !videoCodecsInstalled.Exists(x => x.Subtypes.Any(y => y.Equals(videoCodecId, StringComparison.InvariantCultureIgnoreCase)));
            }

            return true;
        }

        public async Task<BaseItemDto> LoadMediaItemAsync(DetailsItemPlayRecord detailsItemPlayRecord)
        {
            this.detailsItemPlayRecord = detailsItemPlayRecord;

            item = await apiClient.Items[detailsItemPlayRecord.Id]
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                });

            return item;
        }

        public async Task<MediaSourceInfo> LoadMediaPlaybackInfoAsync(string videoId = null)
        {
            var startTimeTicks = 0L;

            if (item.UserData.PlayedPercentage.HasValue && item.UserData.PlayedPercentage < 90 && item.UserData.PlaybackPositionTicks.HasValue)
            {
                startTimeTicks = item.UserData.PlaybackPositionTicks.Value;
            }

            var playbackBody = GetPlaybackInfoBody(user, startTimeTicks);
            playbackInfo = await apiClient.Items[item.Id.Value].PlaybackInfo
                .PostAsync(playbackBody);

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
            var session = memoryCache.Get<SessionInfo>(JellyfinConstants.SessionName);
            var playbackStartInfo = new PlaybackStartInfo
            {
                ItemId = item.Id,
                SessionId = session.Id,
                PlayMethod = IsTranscoding ? PlaybackStartInfo_PlayMethod.Transcode : PlaybackStartInfo_PlayMethod.DirectPlay,
                CanSeek = true,
                IsMuted = false,
                IsPaused = false,
                PlaySessionId = playbackSessionId,
            };

            await apiClient.Sessions.Playing.PostAsync(playbackStartInfo);
        }

        public async Task SessionProgressAsync(long position, bool isPaused)
        {
            var session = memoryCache.Get<SessionInfo>(JellyfinConstants.SessionName);
            var playbackProgressInfo = new PlaybackProgressInfo
            {
                SessionId = session.Id,
                ItemId = item.Id,
                PositionTicks = position,
                PlayMethod = IsTranscoding ? PlaybackProgressInfo_PlayMethod.Transcode : PlaybackProgressInfo_PlayMethod.DirectPlay,
                CanSeek = true,
                IsMuted = false,
                IsPaused = isPaused,
                PlaySessionId = playbackSessionId,
            };

            await apiClient.Sessions.Playing.Progress.PostAsync(playbackProgressInfo);
        }

        public async Task SessionStopAsync(long position)
        {
            var session = memoryCache.Get<SessionInfo>(JellyfinConstants.SessionName);

            await apiClient.Sessions.Playing.Stopped
                .PostAsync(new PlaybackStopInfo
                {
                    PositionTicks = position,
                    ItemId = item.Id,
                    SessionId = session.Id,
                    PlaySessionId = playbackSessionId,
                });
        }

        private static PlaybackInfoDto GetPlaybackInfoBody(UserDto user, long startTimeTicks)
        {
            const string mp4VideoFormats = "h264,vp8,vp9";
            const string mkvVideoFormats = "h264,vc1,vp8,vp9,av1";
            const string audioFormarts = "aac,mp3,ac3";

            return new PlaybackInfoDto
            {
                UserId = user.Id.Value,
                AutoOpenLiveStream = true,
                EnableTranscoding = user.Policy.EnableVideoPlaybackTranscoding,
                AllowVideoStreamCopy = true,
                AllowAudioStreamCopy = true,
                MaxStreamingBitrate = user.Policy.RemoteClientBitrateLimit,
                MaxAudioChannels = 5,
                StartTimeTicks = startTimeTicks,
                EnableDirectStream = true,

                DeviceProfile = new DeviceProfile
                {
                    CodecProfiles = new List<CodecProfile>
                        {
                            new() {
                                Codec = "aac",
                                Conditions = new List<ProfileCondition>
                                {
                                    new() {
                                        Condition = ProfileCondition_Condition.Equals,
                                        Property = ProfileCondition_Property.IsSecondaryAudio,
                                        Value = "false",
                                    },
                                },
                                Type = CodecProfile_Type.VideoAudio
                            },
                            new() {
                                Codec = "h264",
                                Conditions = new List<ProfileCondition>
                                {
                                    new() {
                                        Condition = ProfileCondition_Condition.NotEquals,
                                        Property = ProfileCondition_Property.IsAnamorphic,
                                        Value = "true",
                                        IsRequired = false,
                                    },
                                    new() {
                                        Condition = ProfileCondition_Condition.EqualsAny,
                                        Property = ProfileCondition_Property.VideoProfile,
                                        Value = "high|main|baseline|constrained baseline",
                                        IsRequired = false,
                                    },
                                    new() {
                                        Condition = ProfileCondition_Condition.EqualsAny,
                                        Property = ProfileCondition_Property.VideoRangeType,
                                        Value = "SDR",
                                        IsRequired = false,
                                    },
                                    new() {
                                        Condition = ProfileCondition_Condition.LessThanEqual,
                                        Property = ProfileCondition_Property.VideoLevel,
                                        Value = "52",
                                        IsRequired = false,
                                    },
                                    new() {
                                        Condition = ProfileCondition_Condition.NotEquals,
                                        Property = ProfileCondition_Property.IsInterlaced,
                                        Value = "true",
                                        IsRequired = false,
                                    },
                                },
                                Type = CodecProfile_Type.Video
                            },
                        },
                    DirectPlayProfiles = new List<DirectPlayProfile>
                        {
                            new() {
                                Container = "mp4,m4v",
                                Type = DirectPlayProfile_Type.Video,
                                VideoCodec = mp4VideoFormats,
                                AudioCodec = audioFormarts,
                            },
                            new() {
                                Container = "mkv",
                                Type = DirectPlayProfile_Type.Video,
                                VideoCodec = mp4VideoFormats,
                                AudioCodec = audioFormarts,
                            },
                            new() {
                                Container = "m4a",
                                AudioCodec = "aac",
                                Type = DirectPlayProfile_Type.Audio,
                            },
                            new() {
                                Container = "m4b",
                                AudioCodec = "aac",
                                Type = DirectPlayProfile_Type.Audio,
                            },
                            new() {
                                Container = "mp3",
                                Type = DirectPlayProfile_Type.Audio,
                            },
                        },
                    TranscodingProfiles = new List<TranscodingProfile>
                        {
                            new() {
                                Container = "ts",
                                Type = TranscodingProfile_Type.Audio,
                                AudioCodec = "aac",
                                Context = TranscodingProfile_Context.Streaming,
                                Protocol = TranscodingProfile_Protocol.Hls,
                                MaxAudioChannels = "2",
                                BreakOnNonKeyFrames = true,
                                MinSegments = 1,
                            },
                            new() {
                                Container = "aac",
                                Type = TranscodingProfile_Type.Audio,
                                AudioCodec = "aac",
                                Context = TranscodingProfile_Context.Streaming,
                                Protocol = TranscodingProfile_Protocol.Http,
                                MaxAudioChannels = "2",
                            },
                            new() {
                                Container = "mp3",
                                Type = TranscodingProfile_Type.Audio,
                                AudioCodec = "mp3",
                                Context = TranscodingProfile_Context.Streaming,
                                Protocol = TranscodingProfile_Protocol.Http,
                                MaxAudioChannels = "2",
                            },
                            new() {
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
                            new() {
                                Container = "mp4",
                                Type = TranscodingProfile_Type.Video,
                                VideoCodec = mp4VideoFormats,
                                Context = TranscodingProfile_Context.Streaming,
                                MaxAudioChannels = "2",
                                CopyTimestamps = true,
                                AudioCodec = "aac,mp3",
                            },
                            new() {
                                Container = "mkv",
                                Type = TranscodingProfile_Type.Video,
                                VideoCodec = mkvVideoFormats,
                                Context = TranscodingProfile_Context.Streaming,
                                MaxAudioChannels = "2",
                                CopyTimestamps = true,
                                AudioCodec = "aac,mp3",
                            },
                        },
                },
            };
        }
    }
}
