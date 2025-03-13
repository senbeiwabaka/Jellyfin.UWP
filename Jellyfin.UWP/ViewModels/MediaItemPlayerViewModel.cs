using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Models.filters;
using MetroLog;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Jellyfin.UWP.ViewModels;

internal sealed partial class MediaItemPlayerViewModel : ObservableObject
{
    private readonly JellyfinApiClient apiClient;
    private readonly ILogger Log = LoggerFactory.GetLogger(nameof(MediaItemPlayerViewModel));
    private readonly IMemoryCache memoryCache;

    private readonly ReadOnlyDictionary<string, string> supportedAudioCodecs = new(new Dictionary<string, string>
    {
        { "aac", CodecSubtypes.AudioFormatAac },
        { "ac3", CodecSubtypes.AudioFormatDolbyAC3 },
        { "alac", CodecSubtypes.AudioFormatAlac },
        { "flac", CodecSubtypes.AudioFormatFlac },
        { "eac3", CodecSubtypes.AudioFormatAac },
        { "mp3", CodecSubtypes.AudioFormatMP3 },
    });

    private readonly ReadOnlyDictionary<string, string> supportedVideoCodecs = new(new Dictionary<string, string>
    {
        { "mp4v", CodecSubtypes.VideoFormatMP4V },
        { "h264", CodecSubtypes.VideoFormatH264 },
        { "hevc", CodecSubtypes.VideoFormatHevc },
        { "h263", CodecSubtypes.VideoFormatH263 },
    });

    private readonly Dictionary<TimedTextSource, string> ttsMap = [];

    private readonly ReadOnlyDictionary<string, string> unSupportedAudioCodecs = new(new Dictionary<string, string>
    {
        { "dts", CodecSubtypes.AudioFormatDts },
    });

    private readonly UserDto user;

    private DetailsItemPlayRecord? detailsItemPlayRecord;

    private PlaybackInfoResponse? playbackInfo;

    private string playbackSessionId = string.Empty;

    public MediaItemPlayerViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient)
    {
        this.memoryCache = memoryCache;
        this.apiClient = apiClient;
        user = memoryCache.Get<UserDto>(JellyfinConstants.UserName)!;
    }

    [ObservableProperty]
    public partial bool IsNextItemOpen { get; set; }

    [ObservableProperty]
    public partial bool IsPlaybackOpen { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

    [ObservableProperty]
    public partial bool IsTranscoding { get; set; }

    [ObservableProperty]
    public partial MediaPlayerPlayBackInfo MediaPlayerPlayBackInfo { get; set; }

    [ObservableProperty]
    public partial IMediaPlaybackSource Source { get; set; }

    internal BaseItemDto Item { get; private set; }

    internal MediaPlayerModel MediaPlayerModel { get; set; }

    internal MediaSourceInfo MediaSourceInfo { get; private set; }

    public async Task SessionProgressAsync(long position, bool isPaused, CancellationToken cancellationToken = default)
    {
        var session = memoryCache.Get<SessionInfoDto>(JellyfinConstants.SessionName);
        var playbackProgressInfo = new PlaybackProgressInfo
        {
            SessionId = session.Id,
            ItemId = Item.Id,
            PositionTicks = position,
            PlayMethod = IsTranscoding ? PlaybackProgressInfo_PlayMethod.Transcode : PlaybackProgressInfo_PlayMethod.DirectPlay,
            CanSeek = true,
            IsMuted = false,
            IsPaused = isPaused,
            PlaySessionId = playbackSessionId,
        };

        await apiClient.Sessions.Playing.Progress.PostAsync(playbackProgressInfo, cancellationToken: cancellationToken);
    }

    public async Task SessionStopAsync(long position, CancellationToken cancellationToken = default)
    {
        var session = memoryCache.Get<SessionInfoDto>(JellyfinConstants.SessionName);

        await apiClient.Sessions.Playing.Stopped
            .PostAsync(new PlaybackStopInfo
            {
                PositionTicks = position,
                ItemId = Item.Id,
                SessionId = session.Id,
                PlaySessionId = playbackSessionId,
            }, cancellationToken: cancellationToken);
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
                        new()
                        {
                            Codec = "aac",
                            Conditions = new List<ProfileCondition>
                        {
                            new()
                            {
                                Condition = ProfileCondition_Condition.Equals,
                                Property = ProfileCondition_Property.IsSecondaryAudio,
                                Value = "false",
                            },
                        },
                            Type = CodecProfile_Type.VideoAudio
                        },
                        new()
                            {
                                Codec = "h264",
                                Conditions = new List<ProfileCondition>
                                {
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
                                },
                                Type = CodecProfile_Type.Video
                            },
                    },
                DirectPlayProfiles = new List<DirectPlayProfile>
                    {
                        new()
                        {
                            Container = "mp4,m4v",
                            Type = DirectPlayProfile_Type.Video,
                            VideoCodec = mp4VideoFormats,
                            AudioCodec = audioFormarts,
                        },
                        new()
                        {
                            Container = "mkv",
                            Type = DirectPlayProfile_Type.Video,
                            VideoCodec = mp4VideoFormats,
                            AudioCodec = audioFormarts,
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
                    },
                TranscodingProfiles = new List<TranscodingProfile>
                    {
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
                    },
            },
        };
    }

    [RelayCommand]
    private void ClosePlaybackInfo() => IsPlaybackOpen = false;

    private async Task<BaseItemDtoQueryResult?> GetNextSeasonEpisodes(Guid seriesId, Guid seasonId, CancellationToken cancellationToken = default)
    {
        var seasons = await apiClient.Shows[seriesId].Seasons
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.Fields = [ItemFields.ItemCounts, ItemFields.MediaSourceCount,];
            }, cancellationToken);
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
                        options.QueryParameters.Fields = [ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio,];
                    }, cancellationToken);
            }

            ++index;
        }

        return null;
    }

    private async Task GetPlaybackInfo(double playerWidth, double playerHeight, CancellationToken cancellationToken = default)
    {
        var session = (await apiClient.Sessions.GetAsync(options => options.QueryParameters.DeviceId = JellyfinConstants.DeviceId, cancellationToken))
            .FirstOrDefault();

        string? transcodingVideoCodec = null;
        string? transcodingAudioCodec = null;
        string? transcodingAudioChannels = null;
        string? transcodingBitrate = null;
        string? transcodingCompletion = null;
        string? transcodingFramerate = null;
        string? transcodingReason = null;

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

        var videoMediaStream = Item.MediaStreams.Single(x => x.Type == MediaStream_Type.Video);
        MediaStream audioMediaStream;

        if (detailsItemPlayRecord.SelectedAudioMediaStreamIndex is null && user.Configuration.PlayDefaultAudioTrack.Value && session is not null && session.TranscodingInfo is null)
        {
            var stream = Item.MediaStreams.SingleOrDefault(x => x.IsDefault.Value && x.Type == MediaStream_Type.Audio);

            audioMediaStream = stream ?? Item.MediaStreams.First(x => x.Type == MediaStream_Type.Audio);
        }
        else
        {
            audioMediaStream = Item.MediaStreams.First(x => x.Type == MediaStream_Type.Audio);
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

            Container = Item.MediaSources[0].Container,
            Size = Item.MediaSources[0].Size.HasValue ? $"{Item.MediaSources[0].Size.Value / 1073741824m:#.#} GiB" : "N/A",
            Bitrate = Item.MediaSources[0].Bitrate.HasValue ? $"{Item.MediaSources[0].Bitrate.Value / 1000000m:#.#} Mbps" : "N/A",
            VideoCodec = $"{videoMediaStream.Codec.ToUpper()} {videoMediaStream.Profile}",
            VideoBitrate = videoMediaStream.BitRate.HasValue ? $"{videoMediaStream.BitRate.Value / 1000000m:#.#} Mbps" : "N/A",
            VideoRangeType = videoMediaStream.VideoRangeType?.ToString(),
            AudioCodec = $"{audioMediaStream.Codec.ToUpper()} {audioMediaStream.Profile}",
            AudioBitrate = audioMediaStream.BitRate.HasValue ? $"{audioMediaStream.BitRate.Value / 1000:#} kbps" : "N/A",
            AudioChannels = audioMediaStream.Channels.HasValue ? audioMediaStream.Channels.ToString() : "N/A",
            AudioSampleRate = audioMediaStream.SampleRate.HasValue ? $"{audioMediaStream.SampleRate} Hz" : "N/A",
        };
    }

    private Task<BaseItemDtoQueryResult?> GetSeriesAsync(Guid seriesId, Guid seasonId, CancellationToken cancellationToken = default)
    {
        return apiClient.Shows[seriesId].Episodes
                   .GetAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                        options.QueryParameters.SeasonId = seasonId;
                        options.QueryParameters.Fields = [ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio,];
                    }, cancellationToken);
    }

    private Uri GetSubtitleUrl(int index, string routeFormat)
    {
        var routeId = Item.Id.ToString().Replace("-", string.Empty);
        var subtitleRequest = apiClient.Videos[Item.Id.Value][routeId].Subtitles[index][0]
            .StreamWithRouteFormat(routeFormat)
            .ToGetRequestInformation();

        return apiClient.BuildUri(subtitleRequest);
        //return subtitleClient.GetSubtitleWithTicksUrl(Item.Id, routeId, index, 0, routeFormat);
    }

    private Uri GetVideoUrl(string? videoId = default)
    {
        var container = Item.MediaSources[0].Container;
        var video = apiClient.Videos[Item.Id.Value]
            .StreamWithContainer(container)
            .ToGetRequestInformation(options =>
            {
                options.QueryParameters.Static = true;
                options.QueryParameters.MediaSourceId = videoId;
            });
        //var videoUrl = videosClient.GetVideoStreamByContainerUrl(
        //    Item.Id,
        //    container,
        //    @static: true,
        //    mediaSourceId: videoId);

        //return new Uri(videoUrl);

        return apiClient.BuildUri(video);
    }

    private async Task<bool> IsTranscodingNeededBecauseOfAudio(IReadOnlyList<MediaStream> mediaStreams)
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

    private async Task<bool> IsTranscodingNeededBecauseOfVideo(IReadOnlyList<MediaStream> mediaStreams)
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

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task LoadMediaItemAsync(DetailsItemPlayRecord detailsItemPlayRecord, CancellationToken cancellationToken = default)
    {
        this.detailsItemPlayRecord = detailsItemPlayRecord;

        Item = await apiClient.Items[detailsItemPlayRecord.Id]
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
            }, cancellationToken)
            .ConfigureAwait(true);

        await LoadMediaPlaybackInfoAsync(detailsItemPlayRecord.SelectedVideoId, cancellationToken)
            .ConfigureAwait(true);

        var needsToTranscodeAudio = await IsTranscodingNeededBecauseOfAudio(MediaSourceInfo.MediaStreams)
            .ConfigureAwait(true);
        var needsToTranscodeVideo = await IsTranscodingNeededBecauseOfVideo(MediaSourceInfo.MediaStreams)
            .ConfigureAwait(true);

        // If a sketchy codec is selected and the decoder does not exist or the file is 10-bit then we will use a transcoded version.
        if (needsToTranscodeAudio || needsToTranscodeVideo)
        {
            IsTranscoding = true;

            Log.Debug("Transcoding because of audio: {0} ;; video: {1}", needsToTranscodeAudio, needsToTranscodeVideo);
        }

        var source = LoadSource();

        var mediaPlaybackItem = new MediaPlaybackItem(source);

        var props = mediaPlaybackItem.GetDisplayProperties();
        props.Type = Windows.Media.MediaPlaybackType.Video;

        foreach (var genre in Item.Genres)
        {
            props.VideoProperties.Genres.Add(genre);
        }

        props.VideoProperties.Title = Item.Name;

        mediaPlaybackItem.ApplyDisplayProperties(props);

        if (!IsTranscoding && detailsItemPlayRecord.SelectedAudioIndex.HasValue)
        {
            mediaPlaybackItem.AudioTracks.SelectedIndex = detailsItemPlayRecord.SelectedAudioIndex.Value;
        }

        Source = mediaPlaybackItem;

        await SessionPlayingAsync(cancellationToken).ConfigureAwait(true);

        WeakReferenceMessenger.Default.Send(new WeakRefMessage("Weak Reference Messenger"));
    }

    private async Task LoadMediaPlaybackInfoAsync(string? videoId = default, CancellationToken cancellationToken = default)
    {
        var startTimeTicks = 0L;

        if (Item.UserData.PlayedPercentage.HasValue && Item.UserData.PlayedPercentage < 90 && Item.UserData.PlaybackPositionTicks.HasValue)
        {
            startTimeTicks = Item.UserData.PlaybackPositionTicks.Value;
        }

        var playbackBody = GetPlaybackInfoBody(user, startTimeTicks);
        playbackInfo = await apiClient.Items[Item.Id.Value].PlaybackInfo
            .PostAsync(playbackBody, cancellationToken: cancellationToken)
            .ConfigureAwait(true);

        playbackSessionId = playbackInfo.PlaySessionId;

        if (string.IsNullOrWhiteSpace(videoId))
        {
            MediaSourceInfo = playbackInfo.MediaSources.Single();
        }
        else
        {
            MediaSourceInfo = playbackInfo.MediaSources.Single(x => string.Equals(x.Id, videoId, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    private MediaSource LoadSource()
    {
        MediaSource source;

        if (IsTranscoding)
        {
            var mediaUri = new Uri($"{memoryCache.Get<string>(JellyfinConstants.HostUrlName)}{MediaSourceInfo.TranscodingUrl}");
            //var result = await AdaptiveMediaSource.CreateFromUriAsync(mediaUri);

            //source = MediaSource.CreateFromAdaptiveMediaSource(result.MediaSource);
            source = MediaSource.CreateFromUri(mediaUri);
        }
        else
        {
            var mediaUri = GetVideoUrl(detailsItemPlayRecord.SelectedVideoId);
            source = MediaSource.CreateFromUri(mediaUri);
        }

        var mediaStreams = MediaSourceInfo.MediaStreams;

        if (mediaStreams.Exists(x => x.Type == MediaStream_Type.Subtitle) && !string.Equals("mkv", Item.MediaSources[0].Container, StringComparison.InvariantCultureIgnoreCase))
        {
            var firstSubtitle = mediaStreams.First(x => x.Type == MediaStream_Type.Subtitle);
            var subtitleUrl = GetSubtitleUrl(
                firstSubtitle.Index.Value,
                string.Equals(firstSubtitle.Codec, "subrip", StringComparison.OrdinalIgnoreCase) ? "vtt" : firstSubtitle.Codec);

            var timedTextSource = TimedTextSource.CreateFromUri(subtitleUrl);

            timedTextSource.Resolved += (TimedTextSource sender, TimedTextSourceResolveResultEventArgs args) =>
            {
                // Handle errors
                if (args.Error != null)
                {
                    Log.Error($"Subtitle error: {args.Error.ErrorCode}", args.Error.ExtendedError);

                    return;
                }

                // Update label manually since the external SRT does not contain it
                args.Tracks[0].Label = ttsMap[sender];
            };

            ttsMap[timedTextSource] = firstSubtitle.DisplayTitle;

            foreach (var keyValuePair in ttsMap)
            {
                source.ExternalTimedTextSources.Add(keyValuePair.Key);
            }
        }

        //try
        //{
        //    await source.OpenAsync();
        //}
        //catch (Exception ex)
        //{
        //    Log.Error("failed to open source", ex);
        //}

        return source;
    }

    [RelayCommand]
    private void NoNextEpisode() => IsNextItemOpen = false;

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task OpenPlaybackInfo(CancellationToken cancellationToken)
    {
        IsPlaybackOpen = true;

        IsSettingsOpen = false;

        await GetPlaybackInfo(MediaPlayerModel.Width, MediaPlayerModel.Height, cancellationToken);
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task PlayNextEpisode(CancellationToken cancellationToken)
    {
        var episodes = await GetSeriesAsync(Item.SeriesId.Value, Item.SeasonId.Value, cancellationToken);

        if (episodes is not null)
        {
            var nextIndex = Item.IndexNumber + 1;

            if (episodes.Items.Any(x => x.IndexNumber == nextIndex))
            {
                detailsItemPlayRecord.Id = episodes.Items.Single(x => x.IndexNumber.Value == nextIndex).Id.Value;
            }
            else
            {
                var nextSeasonEpisodes = await GetNextSeasonEpisodes(Item.SeriesId.Value, Item.SeasonId.Value, cancellationToken);

                if (nextSeasonEpisodes is null || nextSeasonEpisodes.TotalRecordCount == 0 || nextSeasonEpisodes.Items is null)
                {
                    return;
                }

                detailsItemPlayRecord.Id = nextSeasonEpisodes.Items[0].Id!.Value;
            }

            await LoadMediaItemAsync(detailsItemPlayRecord, cancellationToken);

            IsNextItemOpen = false;

            await SessionPlayingAsync(cancellationToken);
        }
    }

    private async Task SessionPlayingAsync(CancellationToken cancellationToken = default)
    {
        var session = memoryCache.Get<SessionInfoDto>(JellyfinConstants.SessionName);
        var playbackStartInfo = new PlaybackStartInfo
        {
            ItemId = Item.Id,
            SessionId = session.Id,
            PlayMethod = IsTranscoding ? PlaybackStartInfo_PlayMethod.Transcode : PlaybackStartInfo_PlayMethod.DirectPlay,
            CanSeek = true,
            IsMuted = false,
            IsPaused = false,
            PlaySessionId = playbackSessionId,
        };

        await apiClient.Sessions.Playing.PostAsync(playbackStartInfo, cancellationToken: cancellationToken);
    }
}
