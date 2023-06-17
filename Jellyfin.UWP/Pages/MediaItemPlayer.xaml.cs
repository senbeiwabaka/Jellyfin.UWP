using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using Windows.Media.Core;
using Windows.Media.Playback;
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

        private Guid id;

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
            var url = ((MediaItemPlayerViewModel)DataContext).GetVideoUrl(id);

            var mediaPlayer = new MediaPlayer
            {
                Source = MediaSource.CreateFromUri(url)
            };

            _mediaPlayerElement.SetMediaPlayer(mediaPlayer);

            mediaPlayer.Play();

            await ((MediaItemPlayerViewModel)DataContext).SessionPlayingAsync();
        }

        private void MediaItemPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            _mediaPlayerElement.MediaPlayer.Dispose();
        }
    }
}
