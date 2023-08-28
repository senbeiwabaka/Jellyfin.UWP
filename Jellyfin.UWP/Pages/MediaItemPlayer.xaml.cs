using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using MetroLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaItemPlayer : Page
    {
        private readonly DispatcherTimer dispatcherTimer;
        private readonly DisplayRequest displayRequest;
        private readonly ILogger Log;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly Dictionary<TimedTextSource, string> ttsMap = new();
        private readonly Stopwatch stopwatch = new Stopwatch();

        private DetailsItemPlayRecord detailsItemPlayRecord;
        private bool isTranscoding;

        public MediaItemPlayer()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<MediaItemPlayerViewModel>();
            sdkClientSettings = Ioc.Default.GetRequiredService<SdkClientSettings>();

            this.Loaded += MediaItemPlayer_Loaded;
            this.Unloaded += MediaItemPlayer_Unloaded;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);

            dispatcherTimer.Start();

            Log = LogManagerFactory.DefaultLogManager.GetLogger<MediaItemPlayer>();

            displayRequest = new DisplayRequest();
        }

        public void BackClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            detailsItemPlayRecord = (DetailsItemPlayRecord)e.Parameter;

            base.OnNavigatedTo(e);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            dispatcherTimer.Stop();

            _mediaPlayerElement.MediaPlayer.Pause();

            await ((MediaItemPlayerViewModel)DataContext).SessionStopAsync(_mediaPlayerElement.MediaPlayer.PlaybackSession.Position.Ticks);

            displayRequest.RequestRelease();

            base.OnNavigatingFrom(e);
        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            var mediaItemPlayerViewModel = ((MediaItemPlayerViewModel)DataContext);

            await mediaItemPlayerViewModel.SessionProgressAsync(_mediaPlayerElement.MediaPlayer.PlaybackSession.Position.Ticks);
        }

        private async void MediaItemPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            var mediaItemPlayerViewModel = ((MediaItemPlayerViewModel)DataContext);
            var item = await mediaItemPlayerViewModel.LoadMediaItemAsync(detailsItemPlayRecord.Id);
            var mediaSourceInfo = await mediaItemPlayerViewModel.LoadMediaPlaybackInfoAsync();
            var mediaStreams = mediaSourceInfo.MediaStreams;

            if (mediaStreams.Any(x => x.Type == Sdk.MediaStreamType.Subtitle))
            {
                var firstSubtitle = mediaStreams.First(x => x.Type == Sdk.MediaStreamType.Subtitle);
                var subtitleUrl = mediaItemPlayerViewModel.GetSubtitleUrl(
                    firstSubtitle.Index,
                    string.Equals(firstSubtitle.Codec, "subrip", StringComparison.OrdinalIgnoreCase) ? "vtt" : firstSubtitle.Codec);

                var subtitleUri = new Uri(subtitleUrl);
                var timedTextSource = TimedTextSource.CreateFromUri(subtitleUri);

                timedTextSource.Resolved += Tts_Resolved;

                ttsMap[timedTextSource] = firstSubtitle.DisplayTitle;
            }

            var codecQuery = new CodecQuery();
            var videoCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Encoder, ""))
                .Select(x => x).ToArray();
            var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Encoder, ""))
                .Select(x => x).ToArray();

            Uri mediaUri;

            var isSelectedAndDTS = detailsItemPlayRecord.SelectedMediaStreamIndex.HasValue &&
                mediaStreams.Single(x => x.Index == detailsItemPlayRecord.SelectedMediaStreamIndex.Value && x.Type == Sdk.MediaStreamType.Audio).Codec == "dts";
            var isFlacAudio = mediaStreams.Any(x => x.Type == Sdk.MediaStreamType.Audio && x.Codec == "flac");
            if (isSelectedAndDTS || isFlacAudio)
            {
                mediaUri = new Uri($"{sdkClientSettings.BaseUrl}{mediaSourceInfo.TranscodingUrl}");

                isTranscoding = true;
            }
            else
            {
                mediaUri = mediaItemPlayerViewModel.GetVideoUrl();
            }

            var source = MediaSource.CreateFromUri(mediaUri);

            foreach (var keyValuePair in ttsMap)
            {
                source.ExternalTimedTextSources.Add(keyValuePair.Key);
            }

            await source.OpenAsync();

            var mediaPlaybackItem = new MediaPlaybackItem(source);
            var mediaPlayer = new MediaPlayer
            {
                Source = mediaPlaybackItem,
                AudioCategory = MediaPlayerAudioCategory.Movie,
            };

            _mediaPlayerElement.SetMediaPlayer(mediaPlayer);

            if (item.UserData.PlayedPercentage > 0)
            {
                mediaPlayer.PlaybackSession.Position = new TimeSpan(item.UserData.PlaybackPositionTicks);
            }

            if (!isTranscoding && detailsItemPlayRecord.SelectedAudioIndex.HasValue)
            {
                mediaPlaybackItem.AudioTracks.SelectedIndex = detailsItemPlayRecord.SelectedAudioIndex.Value;
            }

            _mediaPlayerElement.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            _mediaPlayerElement.MediaPlayer.PlaybackSession.BufferingStarted += PlaybackSession_BufferingStarted; ;
            _mediaPlayerElement.MediaPlayer.PlaybackSession.BufferingEnded += PlaybackSession_BufferingEnded; ;
            _mediaPlayerElement.MediaPlayer.CurrentStateChanged += MediaPlayer_CurrentStateChanged;
            _mediaPlayerElement.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            mediaPlayer.Play();

            await mediaItemPlayerViewModel.SessionPlayingAsync();

            displayRequest.RequestActive();

            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerCursor = null;
        }

        private void PlaybackSession_BufferingEnded(MediaPlaybackSession sender, object args)
        {
            Log.Info(args?.ToString() ?? "No PlaybackSession_BufferingEnded args");
        }

        private void PlaybackSession_BufferingStarted(MediaPlaybackSession sender, object args)
        {
            Log.Info(args?.ToString() ?? "No PlaybackSession_BufferingStarted args");

            if (args is MediaPlaybackSessionBufferingStartedEventArgs)
            {
                var value = args as MediaPlaybackSessionBufferingStartedEventArgs;

                Log.Info("Is playback interrupted: {0}", value.IsPlaybackInterruption);
            }
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            Log.Info(args?.ToString() ?? "No MediaPlayer_MediaEnded args");

            ((Frame)Window.Current.Content).GoBack();
        }

        private void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            Log.Info(args?.ToString() ?? "No MediaPlayer_CurrentStateChanged args");
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

        private void MediaItemPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            _mediaPlayerElement.MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            _mediaPlayerElement.MediaPlayer.Dispose();

            stopwatch.Stop();

            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Log.Info(args.ErrorMessage);
        }

        private void Tts_Resolved(TimedTextSource sender, TimedTextSourceResolveResultEventArgs args)
        {
            // Handle errors
            if (args.Error != null)
            {
                var ignoreAwaitWarning = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //rootPage.NotifyUser("Error resolving track " + ttsUri + " due to error " + args.Error.ErrorCode, NotifyType.ErrorMessage);
                });
                return;
            }

            // Update label manually since the external SRT does not contain it
            args.Tracks[0].Label = ttsMap[sender];
        }

        private void _mediaPlayerElement_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape && _mediaPlayerElement.IsFullWindow)
            {
                _mediaPlayerElement.IsFullWindow = false;
            }

            if (e.Key == Windows.System.VirtualKey.Space)
            {
                if (_mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
                {
                    _mediaPlayerElement.MediaPlayer.Play();
                }

                if (_mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    _mediaPlayerElement.MediaPlayer.Pause();
                }
            }
        }
    }
}
