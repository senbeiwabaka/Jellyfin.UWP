using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Models;
using Jellyfin.Models.Filters;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage;

namespace Jellyfin.ViewModels;

public sealed partial class MediaItemPlayerViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, JellyfinSdkSettings settings, IMediaPlayerHelpers mediaPlayerHelpers, ILogger<MediaItemPlayerViewModel> logger) : ObservableObject
{
    private readonly Dictionary<TimedTextSource, string> ttsMap = [];

    private readonly ReadOnlyDictionary<string, string> unSupportedAudioCodecs = new(new Dictionary<string, string>
    {
        { "dts", CodecSubtypes.AudioFormatDts },
    });

    private readonly UserDto user = memoryCache.Get<UserDto>(JellyfinConstants.UserName)!;

    private PlaybackInfoResponse? playbackInfo;
    private string playbackSessionId = string.Empty;

    public delegate void EventHandler();

    public event EventHandler? DataIsLoaded;

    public event EventHandler? PlayingStarted;

    [ObservableProperty]
    public partial ObservableCollection<UIAudio> AudioList { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = default!;

    [ObservableProperty]
    public partial bool HasMultipleAudio { get; set; }

    [ObservableProperty]
    public partial bool IsAdaptiveStream { get; private set; }

    [ObservableProperty]
    public partial bool IsAudioOpen { get; set; }

    [ObservableProperty]
    public partial bool IsErrorPopupOpen { get; set; }

    [ObservableProperty]
    public partial bool IsNextItemOpen { get; set; }

    [ObservableProperty]
    public partial bool IsPlaybackOpen { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

    [ObservableProperty]
    public partial bool IsTranscoding { get; private set; }

    [ObservableProperty]
    public partial MediaPlayerPlayBackInfo MediaPlayerPlayBackInfo { get; set; }

    [ObservableProperty]
    public partial UIAudio SelectedAudio { get; set; }

    [ObservableProperty]
    public partial IMediaPlaybackSource Source { get; set; }

    public BaseItemDto Item { get; private set; }

    public MediaPlayerModel MediaPlayerModel { get; set; }

    public MediaSourceInfo MediaSourceInfo { get; private set; }

    public async Task SessionProgressAsync(long position, bool isPaused, CancellationToken cancellationToken = default)
    {
        var session = memoryCache.Get<SessionInfoDto>(JellyfinConstants.SessionName)!;
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

        if (mediaPlayerHelpers.SupportedVideoCodecs.Keys.Any(x => string.Equals(x, selectedVideoCodec, StringComparison.OrdinalIgnoreCase)))
        {
            var videoCodecId = mediaPlayerHelpers.SupportedVideoCodecs.Single(x => string.Equals(x.Key, selectedVideoCodec, StringComparison.OrdinalIgnoreCase)).Value;

            // Check to make sure the codec actually is there to use
            return !videoCodecsInstalled.Exists(x => x.Subtypes.Any(y => y.Equals(videoCodecId, StringComparison.InvariantCultureIgnoreCase)));
        }

        return true;
    }

    [RelayCommand]
    private void ClosePlaybackInfo() => IsPlaybackOpen = false;

    private Uri GetHLS()
    {
        var uniqueId = $"Jellyfin.UWP-{ApplicationData.Current.LocalSettings.Values["UniqueDeviceId"]}";
        var video = apiClient.Videos[Item.Id.Value].MasterM3u8
            .ToGetRequestInformation(options =>
            {
                options.QueryParameters.DeviceId = uniqueId;
                options.QueryParameters.MediaSourceId = MediaSourceInfo.Id;
                options.QueryParameters.VideoCodec = "h264,hevc";
                options.QueryParameters.AudioCodec = "aac";
                options.QueryParameters.AudioStreamIndex = MediaSourceInfo.MediaStreams.First(x => x.Type == MediaStream_Type.Audio).Index;
                options.QueryParameters.AudioBitRate = MediaSourceInfo.MediaStreams.First(x => x.Type == MediaStream_Type.Audio).BitRate;
                options.QueryParameters.PlaySessionId = playbackSessionId;
                options.QueryParameters.EnableAudioVbrEncoding = true;
                options.QueryParameters.SegmentContainer = MediaSourceInfo.Container;
                options.QueryParameters.BreakOnNonKeyFrames = true;
                options.QueryParameters.AllowVideoStreamCopy = user.Policy?.EnablePlaybackRemuxing;
                options.QueryParameters.AllowAudioStreamCopy = user.Policy?.EnablePlaybackRemuxing;
                options.QueryParameters.EnableAutoStreamCopy = true;
                options.QueryParameters.EnableAdaptiveBitrateStreaming = true;
                options.QueryParameters.Context = Sdk.Generated.Videos.Item.MasterM3u8.EncodingContext.Streaming;
                options.QueryParameters.EnableTrickplay = false;
                options.QueryParameters.AlwaysBurnInSubtitleWhenTranscoding = false;
            });

        return apiClient.BuildUri(video);
    }

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
        var sessions = await apiClient.Sessions.GetAsync(options => options.QueryParameters.DeviceId = JellyfinConstants.DeviceId, cancellationToken);
        var session = sessions?.FirstOrDefault();

        string? transcodingVideoCodec = null;
        string? transcodingAudioCodec = null;
        string? transcodingAudioChannels = null;
        string? transcodingBitrate = null;
        string? transcodingCompletion = null;
        string? transcodingFramerate = null;
        string? transcodingReason = null;

        if (session is not null && session.TranscodingInfo is not null)
        {
            transcodingVideoCodec = session.TranscodingInfo.VideoCodec?.ToUpper();
            transcodingAudioCodec = session.TranscodingInfo.AudioCodec?.ToUpper();
            transcodingAudioChannels = session.TranscodingInfo.AudioChannels?.ToString();
            transcodingBitrate = session.TranscodingInfo.Bitrate.HasValue ? $"{session.TranscodingInfo.Bitrate.Value / 1000000m:#.#} Mbps" : "N/A";
            transcodingCompletion = $"{session.TranscodingInfo.CompletionPercentage?.ToString("#.#")}%";
            transcodingFramerate = $"{session.TranscodingInfo.Framerate} fps";
            transcodingReason = string.Join(",", session.TranscodingInfo.TranscodeReasons ?? []);
        }

        var videoMediaStream = Item.MediaStreams.Single(x => x.Type == MediaStream_Type.Video);
        MediaStream audioMediaStream;

        if (HasMultipleAudio && AudioList.SingleOrDefault(x => x.IsSelected) is null && user.Configuration.PlayDefaultAudioTrack.Value && session is not null && session.TranscodingInfo is null)
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
            VideoCodec = $"{videoMediaStream.Codec?.ToUpper()} {videoMediaStream.Profile}",
            VideoBitrate = videoMediaStream.BitRate.HasValue ? $"{videoMediaStream.BitRate.Value / 1000000m:#.#} Mbps" : "N/A",
            VideoRangeType = videoMediaStream.VideoRangeType?.ToString(),
            AudioCodec = $"{audioMediaStream.Codec?.ToUpper()} {audioMediaStream.Profile}",
            AudioBitrate = audioMediaStream.BitRate.HasValue ? $"{audioMediaStream.BitRate.Value / 1000:#} kbps" : "N/A",
            AudioChannels = audioMediaStream.Channels?.ToString() ?? "N/A",
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
        //return subtitleClient.GetSubtitleWithTicksUrl(UserData.MediaId, routeId, index, 0, routeFormat);
    }

    private Uri GetVideoUrl()
    {
        var video = apiClient.Videos[Item.Id.Value]
            .StreamWithContainer(MediaSourceInfo.Container)
            .ToGetRequestInformation(options =>
            {
                options.QueryParameters.Static = true;
                options.QueryParameters.MediaSourceId = MediaSourceInfo.Id;
                options.QueryParameters.AllowVideoStreamCopy = user.Policy.EnablePlaybackRemuxing;
                options.QueryParameters.AllowAudioStreamCopy = user.Policy.EnablePlaybackRemuxing;
                options.QueryParameters.AudioStreamIndex = SelectedAudio?.Index;
                options.QueryParameters.StartTimeTicks = Item.UserData?.PlaybackPositionTicks;
            });

        return apiClient.BuildUri(video);
    }

    private async Task<bool> IsTranscodingNeededBecauseOfAudio(IReadOnlyList<MediaStream> mediaStreams, int? selectedAudioMediaStreamIndex)
    {
        var codecQuery = new CodecQuery();

        string selectedAudioCodec;

        // Get the selected audio codec, if one was, or the default (first) codec.
        if (selectedAudioMediaStreamIndex.HasValue)
        {
            selectedAudioCodec = mediaStreams.Single(x => x.Index == selectedAudioMediaStreamIndex.Value && x.Type == MediaStream_Type.Audio).Codec!;
        }
        else
        {
            selectedAudioCodec = mediaStreams.First(x => x.Type == MediaStream_Type.Audio).Codec!;
        }

        var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Decoder, ""))
            .Select(x => x).ToArray();

        // Check if the selected audio codec is a supported, by default, audio codec
        if (mediaPlayerHelpers.SupportedAudioCodecs.TryGetValue(selectedAudioCodec, out var supportedAudioCodecId))
        {
            // Check to make sure the codec actually is there to use
            return !Array.Exists(audioCodecsInstalled, x => x.Subtypes.Any(y => y.Equals(supportedAudioCodecId, StringComparison.InvariantCultureIgnoreCase)));
        }

        // Check the "unsupported" as in not built in list
        if (unSupportedAudioCodecs.TryGetValue(selectedAudioCodec, out var unSupportedAudioCodecId))
        {
            // Check to make sure the codec actually is there to use
            return !Array.Exists(audioCodecsInstalled, x => x.Subtypes.Any(y => y.Equals(unSupportedAudioCodecId, StringComparison.InvariantCultureIgnoreCase)));
        }

        return true;
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task LoadMediaItemAsync(DetailsItemPlayRecord detailsItemPlayRecord, CancellationToken cancellationToken = default)
    {
        var item = await apiClient.Items[detailsItemPlayRecord.MediaId]
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
            }, cancellationToken);

        if (item is null)
        {
            IsErrorPopupOpen = true;
            ErrorMessage = $"Media item not found.";

            return;
        }

        Item = item;

        await LoadMediaPlaybackInfoAsync(detailsItemPlayRecord, cancellationToken);

        HasMultipleAudio = MediaSourceInfo!.MediaStreams!.Count(x => x.Type == MediaStream_Type.Audio) > 1;

        if (HasMultipleAudio)
        {
            var audioStreams = MediaSourceInfo!.MediaStreams!.Where(x => x.Type == MediaStream_Type.Audio).ToList();
            AudioList = new ObservableCollection<UIAudio>(audioStreams.Select(x => new UIAudio { Title = x.DisplayTitle ?? "No Audio Title", Index = x.Index, }));

            if (detailsItemPlayRecord.SelectedAudioIndex.HasValue)
            {
                AudioList[audioStreams.IndexOf(audioStreams.Single(x => x.Index == detailsItemPlayRecord.SelectedAudioIndex.Value))].IsSelected = true;
            }
            else
            {
                AudioList[0].IsSelected = true;
            }

            SelectedAudio = AudioList.Single(x => x.IsSelected);
        }
        else
        {
            await SetupMediaPlayer(cancellationToken);
        }

        DataIsLoaded?.Invoke();
    }

    private async Task LoadMediaPlaybackInfoAsync(DetailsItemPlayRecord detailsItemPlayRecord, CancellationToken cancellationToken)
    {
        var startTimeTicks = 0L;

        if (Item.UserData.PlayedPercentage.HasValue && Item.UserData.PlayedPercentage < 90 && Item.UserData.PlaybackPositionTicks.HasValue)
        {
            startTimeTicks = Item.UserData.PlaybackPositionTicks.Value;
        }

        var playbackBody = mediaPlayerHelpers.GetPlaybackInfoBody(user, startTimeTicks, detailsItemPlayRecord.SelectedVideoId, detailsItemPlayRecord.SelectedAudioIndex);
        playbackInfo = await apiClient.Items[Item.Id.Value].PlaybackInfo
            .PostAsync(playbackBody, cancellationToken: cancellationToken)
            .ConfigureAwait(true);

        playbackSessionId = playbackInfo.PlaySessionId;

        if (string.IsNullOrWhiteSpace(detailsItemPlayRecord?.SelectedVideoId))
        {
            MediaSourceInfo = playbackInfo.MediaSources![0];
        }
        else
        {
            MediaSourceInfo = playbackInfo.MediaSources!.Single(x => string.Equals(x.Id, detailsItemPlayRecord.SelectedVideoId, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    private async Task<MediaSource> LoadSource()
    {
        MediaSource source;

        if (IsTranscoding)
        {
            var mediaUri = new Uri($"{memoryCache.Get<string>(JellyfinConstants.HostUrlName)}{MediaSourceInfo.TranscodingUrl}");
            source = MediaSource.CreateFromUri(mediaUri);
        }
        else if (IsAdaptiveStream)
        {
            var mediaUri = GetHLS();
            mediaUri = new Uri($"{mediaUri.AbsoluteUri}&api_key={settings.AccessToken}");

            var adapativeUri = await AdaptiveMediaSource.CreateFromUriAsync(mediaUri);

            if (adapativeUri.Status != AdaptiveMediaSourceCreationStatus.Success)
            {
                var issue = await adapativeUri.HttpResponseMessage.Content.ReadAsStringAsync();
                logger.LogError(issue);
            }

            source = MediaSource.CreateFromAdaptiveMediaSource(adapativeUri.MediaSource);
        }
        else
        {
            var mediaUri = GetVideoUrl();
            source = MediaSource.CreateFromUri(mediaUri);
        }

        var mediaStreams = MediaSourceInfo.MediaStreams;

        if (mediaStreams is not null && mediaStreams.Exists(x => x.Type == MediaStream_Type.Subtitle))
        {
            var firstSubtitle = mediaStreams.First(x => x.Type == MediaStream_Type.Subtitle);
            var subtitleUrl = GetSubtitleUrl(
                firstSubtitle.Index ?? 0,
                string.Equals(firstSubtitle.Codec, "subrip", StringComparison.OrdinalIgnoreCase) ? "vtt" : firstSubtitle.Codec ?? string.Empty);

            var timedTextSource = TimedTextSource.CreateFromUri(subtitleUrl);

            timedTextSource.Resolved += (TimedTextSource sender, TimedTextSourceResolveResultEventArgs args) =>
            {
                // Handle errors
                if (args.Error != null)
                {
                    logger.LogError(args.Error.ExtendedError, "Subtitle error: {ErrorCode}", args.Error.ErrorCode);

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

        if (!IsTranscoding)
        {
            try
            {
                await source.OpenAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load media");
            }
        }

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
        IsNextItemOpen = false;

        var episodes = await GetSeriesAsync(Item.SeriesId.Value, Item.SeasonId.Value, cancellationToken);

        var detailsItemPlayRecord = new DetailsItemPlayRecord();

        if (episodes is not null)
        {
            var nextIndex = Item.IndexNumber + 1;

            if (episodes.Items.Any(x => x.IndexNumber == nextIndex))
            {
                detailsItemPlayRecord.MediaId = episodes.Items.Single(x => x.IndexNumber.Value == nextIndex).Id.Value;
            }
            else
            {
                var nextSeasonEpisodes = await GetNextSeasonEpisodes(Item.SeriesId.Value, Item.SeasonId.Value, cancellationToken);

                if (nextSeasonEpisodes is null || nextSeasonEpisodes.TotalRecordCount == 0 || nextSeasonEpisodes.Items is null)
                {
                    return;
                }

                detailsItemPlayRecord.MediaId = nextSeasonEpisodes.Items[0].Id!.Value;
            }

            await LoadMediaItemAsync(detailsItemPlayRecord, cancellationToken);

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

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task SetupMediaPlayer(CancellationToken cancellationToken)
    {
        IsAudioOpen = false;

        var needsToTranscodeAudio = await IsTranscodingNeededBecauseOfAudio(MediaSourceInfo.MediaStreams, SelectedAudio?.Index);
        var needsToTranscodeVideo = await IsTranscodingNeededBecauseOfVideo(MediaSourceInfo.MediaStreams);

        // If a sketchy codec is selected and the decoder does not exist or the file is 10-bit then we will use a transcoded version.
        if (needsToTranscodeAudio || needsToTranscodeVideo || (MediaSourceInfo.Bitrate / 1_000_000) > 8)
        {
            IsTranscoding = true;

            logger.LogDebug("Transcoding because of audio: {0} ;; video: {1} for media: {2}", needsToTranscodeAudio, needsToTranscodeVideo, Item.Name);
        }

        if (IsTranscoding && !((user.Policy?.EnableAudioPlaybackTranscoding ?? false) || (user.Policy.EnableVideoPlaybackTranscoding ?? false)))
        {
            IsErrorPopupOpen = true;
            ErrorMessage = $"Transcoding is needed for {Item.Name} but policy does not allow this account to transcode. Please contact your administrator.";

            return;
        }

        // TODO: When jellyfin gets better adaptive streaming, re-enable this to handle
        //if (!IsTranscoding)
        //{
        //    IsAdaptiveStream = (MediaSourceInfo.Bitrate / 1_000_000) > 3;

        //    Log.Debug("Is adaptive media: {0} ;; for media: {1}", IsAdaptiveStream, UserData.Label);
        //}

        var source = await LoadSource();

        var mediaPlaybackItem = new MediaPlaybackItem(source);

        var props = mediaPlaybackItem.GetDisplayProperties();
        props.Type = Windows.Media.MediaPlaybackType.Video;

        foreach (var genre in Item.Genres ?? [])
        {
            props.VideoProperties.Genres.Add(genre);
        }

        props.VideoProperties.Title = Item.Name;

        mediaPlaybackItem.ApplyDisplayProperties(props);

        // TODO: FIX
        if (!IsTranscoding && HasMultipleAudio)
        {
            var index = AudioList.IndexOf(AudioList.Single(x => x.Index == SelectedAudio.Index));
            mediaPlaybackItem.AudioTracks.SelectedIndex = index;
        }

        Source = mediaPlaybackItem;

        await SessionPlayingAsync(cancellationToken);

        PlayingStarted?.Invoke();
    }
}
