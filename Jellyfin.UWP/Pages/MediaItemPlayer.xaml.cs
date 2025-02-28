using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels;
using MetroLog;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages;

internal sealed partial class MediaItemPlayer : Page
{
    private readonly DispatcherTimer dispatcherTimer;
    private readonly DisplayRequest displayRequest;
    private readonly ILogger Log;
    private readonly Stopwatch stopwatch = new();
    private readonly Dictionary<TimedTextSource, string> ttsMap = new();
    private readonly IMemoryCache memoryCache;

    private DetailsItemPlayRecord detailsItemPlayRecord;

    private BaseItemDto item;

    public MediaItemPlayer()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<MediaItemPlayerViewModel>();
        memoryCache = Ioc.Default.GetRequiredService<IMemoryCache>();

        Loaded += MediaItemPlayer_Loaded;
        Unloaded += MediaItemPlayer_Unloaded;

        dispatcherTimer = new DispatcherTimer();
        dispatcherTimer.Tick += DispatcherTimer_Tick;
        dispatcherTimer.Interval = new TimeSpan(0, 0, 5);

        Log = LogManagerFactory.DefaultLogManager.GetLogger<MediaItemPlayer>();

        displayRequest = new DisplayRequest();
    }

    internal MediaItemPlayerViewModel ViewModel => (MediaItemPlayerViewModel)DataContext;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        detailsItemPlayRecord = (DetailsItemPlayRecord)e.Parameter;

        base.OnNavigatedTo(e);
    }

    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        dispatcherTimer.Stop();

        _mediaPlayerElement.MediaPlayer.Pause();

        await ViewModel.SessionStopAsync(_mediaPlayerElement.MediaPlayer.PlaybackSession.Position.Ticks);

        try
        {
            displayRequest.RequestRelease();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message, ex);
        }

        base.OnNavigatingFrom(e);
    }

    private async void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
    {
        if (Window.Current.CoreWindow.PointerCursor == null)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        stopwatch.Restart();
        await Task.Delay(1500);

        if (stopwatch == null)
        {
            return;
        }

        if (stopwatch.ElapsedMilliseconds >= 1500)
        {
            Window.Current.CoreWindow.PointerCursor = null;
        }
    }

    private async void DispatcherTimer_Tick(object? sender, object e)
    {
        await ViewModel.SessionProgressAsync(
                  _mediaPlayerElement.MediaPlayer.PlaybackSession.Position.Ticks,
                  _mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused);

        if (_mediaPlayerElement.MediaPlayer.PlaybackSession.Position.TotalSeconds + 30 >= _mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds
            && item.Type == BaseItemDto_Type.Episode
            && !NextEpisodePopup.IsOpen)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                      CoreDispatcherPriority.Normal,
                      () =>
                      {
                          NextEpisodePopup.IsOpen = true;
                      });
        }
    }

    private async Task<MediaSource> LoadSourceAsync()
    {
        var mediaSourceInfo = await ViewModel.LoadMediaPlaybackInfoAsync(detailsItemPlayRecord.SelectedVideoId);
        var mediaStreams = mediaSourceInfo.MediaStreams;

        var needsToTranscodeAudio = await ViewModel.IsTranscodingNeededBecauseOfAudio(detailsItemPlayRecord, mediaStreams);
        var needsToTranscodeVideo = await ViewModel.IsTranscodingNeededBecauseOfVideo(mediaStreams);

        // If a sketchy codec is selected and the decoder does not exist or the file is 10-bit then we will use a transcoded version.
        if (needsToTranscodeAudio || needsToTranscodeVideo)
        {
            ViewModel.IsTranscoding = true;

            Log.Debug("Transcoding because of audio: {0} ;; video: {1}", needsToTranscodeAudio, needsToTranscodeVideo);
        }

        MediaSource source;

        if (ViewModel.IsTranscoding)
        {
            var mediaUri = new Uri($"{memoryCache.Get<string>(JellyfinConstants.HostUrlName)}{mediaSourceInfo.TranscodingUrl}");
            var result = await AdaptiveMediaSource.CreateFromUriAsync(mediaUri);

            source = MediaSource.CreateFromAdaptiveMediaSource(result.MediaSource);
        }
        else
        {
            var mediaUri = ViewModel.GetVideoUrl(detailsItemPlayRecord.SelectedVideoId);
            source = MediaSource.CreateFromUri(mediaUri);
        }

        if (mediaStreams.Exists(x => x.Type == MediaStream_Type.Subtitle) && !string.Equals("mkv", item.MediaSources[0].Container, StringComparison.InvariantCultureIgnoreCase))
        {
            var firstSubtitle = mediaStreams.First(x => x.Type == MediaStream_Type.Subtitle);
            var subtitleUrl = ViewModel.GetSubtitleUrl(
                firstSubtitle.Index.Value,
                string.Equals(firstSubtitle.Codec, "subrip", StringComparison.OrdinalIgnoreCase) ? "vtt" : firstSubtitle.Codec);

            var timedTextSource = TimedTextSource.CreateFromUri(subtitleUrl);

            timedTextSource.Resolved += Tts_Resolved;

            ttsMap[timedTextSource] = firstSubtitle.DisplayTitle;

            foreach (var keyValuePair in ttsMap)
            {
                source.ExternalTimedTextSources.Add(keyValuePair.Key);
            }
        }

        try
        {
            await source.OpenAsync();
        }
        catch (Exception ex)
        {
            Log.Error("failed to open source", ex);
        }

        return source;
    }

    private async void MediaItemPlayer_Loaded(object sender, RoutedEventArgs e)
    {
        if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
        {
            _mediaPlayerElement.IsFullWindow = true;
        }

        var mediaControlsCommandBar = mediaControls.FindVisualChild<CommandBar>();

        var settingsAppBarButton = new AppBarButton
        {
            Icon = new SymbolIcon(Symbol.Setting),
            Label = "Settings"
        };
        settingsAppBarButton.Click += (_, _) => SettingsPopup.IsOpen = true;

        mediaControlsCommandBar.PrimaryCommands.Add(settingsAppBarButton);

        item = await ViewModel.LoadMediaItemAsync(detailsItemPlayRecord);

        var source = await LoadSourceAsync();
        var mediaPlaybackItem = new MediaPlaybackItem(source);

        mediaPlaybackItem.TimedMetadataTracksChanged += MediaPlaybackItem_TimedMetadataTracksChanged;

        var props = mediaPlaybackItem.GetDisplayProperties();
        props.Type = Windows.Media.MediaPlaybackType.Video;

        foreach (var genre in item.Genres)
        {
            props.VideoProperties.Genres.Add(genre);
        }

        props.VideoProperties.Title = item.Name;

        mediaPlaybackItem.ApplyDisplayProperties(props);

        var mediaPlayer = new MediaPlayer
        {
            Source = mediaPlaybackItem,
            AudioCategory = MediaPlayerAudioCategory.Media,
            RealTimePlayback = true,
        };

        _mediaPlayerElement.SetMediaPlayer(mediaPlayer);

        if (item.UserData.PlayedPercentage > 0 && item.UserData.PlaybackPositionTicks.HasValue)
        {
            mediaPlayer.PlaybackSession.Position = new TimeSpan(item.UserData.PlaybackPositionTicks.Value);
        }

        if (!ViewModel.IsTranscoding && detailsItemPlayRecord.SelectedAudioIndex.HasValue)
        {
            mediaPlaybackItem.AudioTracks.SelectedIndex = detailsItemPlayRecord.SelectedAudioIndex.Value;
        }

        _mediaPlayerElement.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        _mediaPlayerElement.MediaPlayer.PlaybackSession.BufferingStarted += PlaybackSession_BufferingStarted;
        _mediaPlayerElement.MediaPlayer.PlaybackSession.BufferingEnded += PlaybackSession_BufferingEnded;
        _mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        _mediaPlayerElement.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

        mediaPlayer.Play();

        await ViewModel.SessionPlayingAsync();

        dispatcherTimer.Start();

        displayRequest.RequestActive();

        Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
        Window.Current.CoreWindow.PointerCursor = null;
        Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

        if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Xbox")
        {
            ApplicationView.GetForCurrentView().Title = item.Name;
        }
    }

    private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
    {
        if (args.VirtualKey == Windows.System.VirtualKey.Escape && _mediaPlayerElement.IsFullWindow)
        {
            _mediaPlayerElement.IsFullWindow = false;
        }

        var mediaPlayerSession = _mediaPlayerElement.MediaPlayer.PlaybackSession;

        if (args.VirtualKey == Windows.System.VirtualKey.Right || args.VirtualKey == Windows.System.VirtualKey.GamepadRightShoulder)
        {
            var newTime = TimeSpan.FromSeconds(30);
            mediaPlayerSession.Position += newTime;
        }

        if (args.VirtualKey == Windows.System.VirtualKey.Left || args.VirtualKey == Windows.System.VirtualKey.GamepadLeftShoulder)
        {
            var newTime = TimeSpan.FromSeconds(10);
            mediaPlayerSession.Position += newTime;
        }

        if (args.VirtualKey == Windows.System.VirtualKey.GamepadY)
        {
            _mediaPlayerElement.IsFullWindow = !_mediaPlayerElement.IsFullWindow;
        }
    }

    private void MediaItemPlayer_Unloaded(object sender, RoutedEventArgs e)
    {
        _mediaPlayerElement.MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
        _mediaPlayerElement.MediaPlayer.Dispose();

        stopwatch.Stop();

        Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;

        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
    }

    private void MediaPlaybackItem_TimedMetadataTracksChanged(MediaPlaybackItem sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
    {
        Log.Debug("Timed metadata track has changed");
    }

    private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        Log.Debug("Media has ended playback");

        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                  CoreDispatcherPriority.Normal,
                      () =>
                      {
                          dispatcherTimer.Stop();
                      });

        await ViewModel.SessionStopAsync(sender.PlaybackSession.Position.Ticks);

        if (item.Type == BaseItemDto_Type.Episode && !NextEpisodePopup.IsOpen)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                      CoreDispatcherPriority.Normal,
                      () =>
                      {
                          NextEpisodePopup.IsOpen = true;
                      });
        }
        else
        {
            Frame.GoBack();
        }
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Log.Error($"Error: {args.Error} with Message: {args.ErrorMessage}", args.ExtendedErrorCode);
    }

    private void PlaybackSession_BufferingEnded(MediaPlaybackSession sender, object args)
    {
        Log.Debug("Buffering has ended");
    }

    private void PlaybackSession_BufferingStarted(MediaPlaybackSession sender, object args)
    {
        Log.Debug("Buffering has started");

        if (args is MediaPlaybackSessionBufferingStartedEventArgs)
        {
            var value = args as MediaPlaybackSessionBufferingStartedEventArgs;

            Log.Info("Buffering: Is playback interrupted: {0}", value.IsPlaybackInterruption);
        }
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        Log.Debug("Playback state has changed");
    }

    private void Tts_Resolved(TimedTextSource sender, TimedTextSourceResolveResultEventArgs args)
    {
        // Handle errors
        if (args.Error != null)
        {
            Log.Error($"Subtitle error: {args.Error.ErrorCode}", args.Error.ExtendedError);

            var ignoreAwaitWarning = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //rootPage.NotifyUser("Error resolving track " + ttsUri + " due to error " + args.Error.ErrorCode, NotifyType.ErrorMessage);
            });
            return;
        }

        // Update label manually since the external SRT does not contain it
        args.Tracks[0].Label = ttsMap[sender];
    }

    private async void YesButton_Click(object sender, RoutedEventArgs e)
    {
        var episodes = await ViewModel.GetSeriesAsync(item.SeriesId.Value, item.SeasonId.Value);

        if (episodes is not null)
        {
            var nextIndex = item.IndexNumber + 1;

            if (episodes.Items.Any(x => x.IndexNumber == nextIndex))
            {
                detailsItemPlayRecord.Id = episodes.Items.Single(x => x.IndexNumber.Value == nextIndex).Id.Value;
            }
            else
            {
                var nextSeasonEpisodes = await ViewModel.GetNextSeasonEpisodes(item.SeriesId.Value, item.SeasonId.Value);

                if (nextSeasonEpisodes is null || nextSeasonEpisodes.TotalRecordCount == 0)
                {
                    return;
                }

                detailsItemPlayRecord.Id = nextSeasonEpisodes.Items[0].Id.Value;
            }

            item = await ViewModel.LoadMediaItemAsync(detailsItemPlayRecord);

            var source = await LoadSourceAsync();

            var mediaPlaybackItem = new MediaPlaybackItem(source);

            _mediaPlayerElement.Source = mediaPlaybackItem;

            await ViewModel.SessionPlayingAsync();

            dispatcherTimer.Start();

            ApplicationView.GetForCurrentView().Title = item.Name;

            NextEpisodePopup.IsOpen = false;
        }
    }

    private async void btn_PlaybackInfo_Click(object sender, RoutedEventArgs e)
    {
        PlaybackInfoPopup.IsOpen = true;

        SettingsPopup.IsOpen = false;

        await ViewModel.GetPlaybackInfo(_mediaPlayerElement.ActualWidth, _mediaPlayerElement.ActualHeight);
    }

    private void btn_PlaybackInfoPopupClose_Click(object sender, RoutedEventArgs e)
    {
        PlaybackInfoPopup.IsOpen = false;
    }
}
