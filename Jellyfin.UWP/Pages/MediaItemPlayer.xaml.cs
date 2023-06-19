using CommunityToolkit.Mvvm.DependencyInjection;
using MetroLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaItemPlayer : Page
    {
        private readonly DispatcherTimer dispatcherTimer;
        private readonly ILogger Log;
        private readonly Dictionary<TimedTextSource, string> ttsMap = new();
        private Guid id;

        // Keep a map to correlate sources with their URIs for error handling

        public MediaItemPlayer()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<MediaItemPlayerViewModel>();

            this.Loaded += MediaItemPlayer_Loaded;
            this.Unloaded += MediaItemPlayer_Unloaded;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);

            dispatcherTimer.Start();

            Log = LogManagerFactory.DefaultLogManager.GetLogger<MediaItemPlayer>();
        }

        public void BackClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            id = (Guid)e.Parameter;

            base.OnNavigatedTo(e);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            dispatcherTimer.Stop();

            _mediaPlayerElement.MediaPlayer.Pause();

            await ((MediaItemPlayerViewModel)DataContext).SessionStopAsync(_mediaPlayerElement.MediaPlayer.PlaybackSession.Position.Ticks);

            base.OnNavigatingFrom(e);
        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            await ((MediaItemPlayerViewModel)DataContext).SessionProgressAsync(_mediaPlayerElement.MediaPlayer.PlaybackSession.Position.Ticks);
        }

        private async void MediaItemPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            var mediaSourceInfo = await ((MediaItemPlayerViewModel)DataContext).LoadMediaPlaybackInfoAsync(id);
            var mediaStreams = mediaSourceInfo.MediaStreams;

            Uri url;
            MediaSource source;

            if (mediaStreams.Any(x => x.Type == Sdk.MediaStreamType.Subtitle))
            {
                var firstSubtitle = mediaStreams.First(x => x.Type == Sdk.MediaStreamType.Subtitle);
                var subtitleUrl = ((MediaItemPlayerViewModel)DataContext).GetSubtitleUrl(
                    id,
                    firstSubtitle.Index,
                    string.Equals(firstSubtitle.Codec, "subrip", StringComparison.OrdinalIgnoreCase) ? "vtt" : firstSubtitle.Codec);

                var subtitleUri = new Uri(subtitleUrl);
                var timedTextSource = TimedTextSource.CreateFromUri(subtitleUri);

                timedTextSource.Resolved += Tts_Resolved;

                ttsMap[timedTextSource] = firstSubtitle.DisplayTitle;

                url = ((MediaItemPlayerViewModel)DataContext).GetVideoUrl(id, firstSubtitle.Index);

                source = MediaSource.CreateFromUri(url);

                source.ExternalTimedTextSources.Add(timedTextSource);
            }
            else
            {
                url = ((MediaItemPlayerViewModel)DataContext).GetVideoUrl(id);

                source = MediaSource.CreateFromUri(url);
            }

            var mediaPlayer = new MediaPlayer()
            {
                Source = source,
            };

            _mediaPlayerElement.SetMediaPlayer(mediaPlayer);

            mediaPlayer.Play();

            await ((MediaItemPlayerViewModel)DataContext).SessionPlayingAsync();
        }

        private void MediaItemPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            _mediaPlayerElement.MediaPlayer.Dispose();
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
