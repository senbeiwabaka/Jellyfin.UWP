using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using MetroLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

            Uri mediaUri;

            if (detailsItemPlayRecord.SelectedMediaStreamIndex.HasValue &&
                mediaStreams.Single(x => x.Index == detailsItemPlayRecord.SelectedMediaStreamIndex.Value && x.Type == Sdk.MediaStreamType.Audio).Codec == "dts")
            {
                mediaUri = new Uri($"{sdkClientSettings.BaseUrl}{mediaSourceInfo.TranscodingUrl}");

                isTranscoding = true;
            }
            else
            {
                mediaUri = mediaItemPlayerViewModel.GetVideoUrl(detailsItemPlayRecord.SelectedMediaStreamIndex);
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

            if (!isTranscoding)
            {
                if (detailsItemPlayRecord.SelectedAudioIndex.HasValue)
                {
                    mediaPlaybackItem.AudioTracks.SelectedIndex = detailsItemPlayRecord.SelectedAudioIndex.Value;
                }
            }

            mediaPlayer.Play();

            await mediaItemPlayerViewModel.SessionPlayingAsync();

            displayRequest.RequestActive();

            _mediaPlayerElement.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        }

        private void MediaItemPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            _mediaPlayerElement.MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            _mediaPlayerElement.MediaPlayer.Dispose();
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
    }
}
