using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using MetroLog;
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
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaItemPlayer : Page
    {
        /*
         * These aren't actually sketchy but you can't just get a DTS decoder from the store.
         * You need to get DTS Sound Unbound from the store and then purchase the DTS decoder from there.
         * Screw that. It isn't super expensive as of this time but I don't want to pay for it.
         * Especially after all of the time it took to figure out how/where to get the freaking thing.
         * After some digging, didn't find anything for FLAC for Media Foundation which is what Media Player uses.
         * Media Player used here is the same one that Windows Media Player (modern) uses.
        */

        private readonly string[] sketchyAudioTypes = new string[2]
        {
            "DTS",
            "FLAC",
        };

        private readonly DispatcherTimer dispatcherTimer;
        private readonly DisplayRequest displayRequest;
        private readonly ILogger Log;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly Stopwatch stopwatch = new();
        private readonly Dictionary<TimedTextSource, string> ttsMap = new();

        private MediaItemPlayerViewModel context;
        private DetailsItemPlayRecord detailsItemPlayRecord;
        private bool isTranscoding;
        private BaseItemDto item;

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

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            await context.SessionProgressAsync(
                _mediaPlayerElement.MediaPlayer.PlaybackSession.Position.Ticks,
                isTranscoding,
                _mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused);
        }

        private async Task<MediaSource> LoadSourceAsync()
        {
            var mediaSourceInfo = await context.LoadMediaPlaybackInfoAsync();
            var mediaStreams = mediaSourceInfo.MediaStreams;

            if (mediaStreams.Any(x => x.Type == MediaStreamType.Subtitle))
            {
                var firstSubtitle = mediaStreams.First(x => x.Type == MediaStreamType.Subtitle);
                var subtitleUrl = context.GetSubtitleUrl(
                    firstSubtitle.Index,
                    string.Equals(firstSubtitle.Codec, "subrip", StringComparison.OrdinalIgnoreCase) ? "vtt" : firstSubtitle.Codec);

                var subtitleUri = new Uri(subtitleUrl);
                var timedTextSource = TimedTextSource.CreateFromUri(subtitleUri);

                timedTextSource.Resolved += Tts_Resolved;

                ttsMap[timedTextSource] = firstSubtitle.DisplayTitle;
            }

            Uri mediaUri;

            var selectedCodec = string.Empty;

            // Get the selected codec, if one was, or the default (first) codec.
            if (detailsItemPlayRecord.SelectedMediaStreamIndex.HasValue)
            {
                selectedCodec = mediaStreams.Single(x => x.Index == detailsItemPlayRecord.SelectedMediaStreamIndex.Value && x.Type == MediaStreamType.Audio).Codec;
            }
            else
            {
                selectedCodec = mediaStreams.First(x => x.Type == MediaStreamType.Audio).Codec;
            }

            // Checks if the selected codec is one of the sketchy ones. We need to know this so we can see if there is a decoder for it installed.
            var isSketchyCodec = Array.Exists(sketchyAudioTypes, x => x.Contains(selectedCodec, StringComparison.InvariantCultureIgnoreCase));
            var doesDecoderExistForSketchyCodec = false;

            if (isSketchyCodec)
            {
                var codecQuery = new CodecQuery();
                var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Decoder, ""))
                    .Select(x => x).ToArray();

                foreach (var sketchyCodec in sketchyAudioTypes)
                {
                    doesDecoderExistForSketchyCodec = Array.Exists(audioCodecsInstalled, x => x.DisplayName.Contains("dts", StringComparison.InvariantCultureIgnoreCase));

                    // If the codec is found then don't search the rest as that probably will set it to false.
                    if (doesDecoderExistForSketchyCodec)
                    {
                        break;
                    }
                }
            }

            var is10Bit = mediaStreams.Any(x => x.Type == MediaStreamType.Video && x.BitDepth == 10);

            // If a sketchy codec is selected and the decoder does not exist or the file is 10-bit then we will use a transcoded version.
            if ((isSketchyCodec && !doesDecoderExistForSketchyCodec) || is10Bit)
            {
                mediaUri = new Uri($"{sdkClientSettings.BaseUrl}{mediaSourceInfo.TranscodingUrl}");

                isTranscoding = true;
            }
            else
            {
                mediaUri = context.GetVideoUrl();
            }

            MediaSource source;

            if (isTranscoding)
            {
                var result = await AdaptiveMediaSource.CreateFromUriAsync(mediaUri);

                source = MediaSource.CreateFromAdaptiveMediaSource(result.MediaSource);
            }
            else
            {
                source = MediaSource.CreateFromUri(mediaUri);
            }

            foreach (var keyValuePair in ttsMap)
            {
                source.ExternalTimedTextSources.Add(keyValuePair.Key);
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
            context = ((MediaItemPlayerViewModel)DataContext);
            item = await context.LoadMediaItemAsync(detailsItemPlayRecord.Id);

            var source = await LoadSourceAsync();
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
            _mediaPlayerElement.MediaPlayer.PlaybackSession.BufferingStarted += PlaybackSession_BufferingStarted;
            _mediaPlayerElement.MediaPlayer.PlaybackSession.BufferingEnded += PlaybackSession_BufferingEnded;
            _mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            _mediaPlayerElement.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            mediaPlayer.Play();

            await context.SessionPlayingAsync(isTranscoding);

            dispatcherTimer.Start();

            displayRequest.RequestActive();

            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            Window.Current.CoreWindow.PointerCursor = null;

            ApplicationView.GetForCurrentView().Title = item.Name;
        }

        private void MediaItemPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            _mediaPlayerElement.MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            _mediaPlayerElement.MediaPlayer.Dispose();

            stopwatch.Stop();

            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            Log.Info(args?.ToString() ?? "No MediaPlayer_MediaEnded args");

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                      CoreDispatcherPriority.Normal,
                          () =>
                          {
                              dispatcherTimer.Stop();
                          });

            await context.SessionStopAsync(sender.PlaybackSession.Position.Ticks);

            if (item.Type == BaseItemKind.Episode)
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
                ((Frame)Window.Current.Content).GoBack();
            }
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Log.Error($"Error: {args.Error} with Message: {args.ErrorMessage}", args.ExtendedErrorCode);
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

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            Log.Info(args?.ToString() ?? "No PlaybackSession_PlaybackStateChanged args");
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
            var episodes = await context.GetSeriesAsync(item.SeriesId.Value, item.SeasonId.Value);

            if (episodes is not null)
            {
                var nextIndex = item.IndexNumber + 1;

                if (episodes.Items.Any(x => x.IndexNumber == nextIndex))
                {
                    detailsItemPlayRecord.Id = episodes.Items.Single(x => x.IndexNumber == nextIndex).Id;

                    item = await context.LoadMediaItemAsync(detailsItemPlayRecord.Id);
                }
                else
                {
                    // TODO: GET THE NEXT SEASON
                }

                var source = await LoadSourceAsync();
                var mediaPlaybackItem = new MediaPlaybackItem(source);

                _mediaPlayerElement.Source = mediaPlaybackItem;

                await context.SessionPlayingAsync(isTranscoding);

                dispatcherTimer.Start();

                ApplicationView.GetForCurrentView().Title = item.Name;

                NextEpisodePopup.IsOpen = false;
            }
        }
    }
}
