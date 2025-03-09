using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Models.filters;
using Jellyfin.UWP.ViewModels;
using MetroLog;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages;

internal sealed partial class MediaItemPlayer : Page, IRecipient<WeakRefMessage>
{
    private readonly DispatcherTimer dispatcherTimer;
    private readonly ILogger Log;
    private readonly IMemoryCache memoryCache;
    private readonly Stopwatch stopwatch = new();

    private DetailsItemPlayRecord detailsItemPlayRecord;
    private DisplayRequest? displayRequest;

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

        detailsItemPlayRecord = new DetailsItemPlayRecord();
    }

    internal MediaItemPlayerViewModel ViewModel => (MediaItemPlayerViewModel)DataContext;

    public void Receive(WeakRefMessage message)
    {
        if (ViewModel.Item.UserData.PlayedPercentage > 0 && ViewModel.Item.UserData.PlaybackPositionTicks.HasValue)
        {
            _mediaPlayerElement.MediaPlayer.PlaybackSession.Position = new TimeSpan(ViewModel.Item.UserData.PlaybackPositionTicks.Value);
        }

        dispatcherTimer.Start();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        detailsItemPlayRecord = (DetailsItemPlayRecord)e.Parameter;

        _mediaPlayerElement.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        _mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        _mediaPlayerElement.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        _mediaPlayerElement.SizeChanged += _mediaPlayerElement_SizeChanged;

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        dispatcherTimer.Stop();

        _mediaPlayerElement.MediaPlayer.Pause();

        ViewModel.SessionStopAsync(_mediaPlayerElement.MediaPlayer.PlaybackSession.Position.Ticks);

        _mediaPlayerElement.MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
        _mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
        _mediaPlayerElement.MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;

        try
        {
            displayRequest?.RequestRelease();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message, ex);
        }

        base.OnNavigatingFrom(e);
    }

    private void _mediaPlayerElement_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.MediaPlayerModel = new MediaPlayerModel
        {
            Width = e.NewSize.Width,
            Height = e.NewSize.Height,
        };
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

    private async void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
    {
        if (Window.Current.CoreWindow.PointerCursor == null)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        stopwatch.Restart();

        await Task.Delay(1500);

        if (stopwatch is null)
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
            && ViewModel.Item.Type == BaseItemDto_Type.Episode
            && !ViewModel.IsNextItemOpen)
        {
            ViewModel.IsNextItemOpen = true;
        }
    }

    private void MediaItemPlayer_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<WeakRefMessage>(this);

        if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
        {
            _mediaPlayerElement.IsFullWindow = true;
        }

        var mediaControlsCommandBar = mediaControls.FindVisualChild<CommandBar>()!;

        var settingsAppBarButton = new AppBarButton
        {
            Icon = new SymbolIcon(Symbol.Setting),
            Label = "Settings"
        };
        settingsAppBarButton.Click += (_, _) => ViewModel.IsSettingsOpen = true;

        mediaControlsCommandBar.PrimaryCommands.Add(settingsAppBarButton);

        Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
        Window.Current.CoreWindow.PointerCursor = null;
        Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
    }

    private void MediaItemPlayer_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<WeakRefMessage>(this);

        stopwatch.Stop();

        Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
        Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        Log.Debug("Media has ended playback");

        //await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
        //          CoreDispatcherPriority.Normal,
        //              () =>
        //              {
        //                  dispatcherTimer.Stop();
        //              });

        dispatcherTimer.Stop();

        ViewModel.SessionStopAsync(sender.PlaybackSession.Position.Ticks);

        if (ViewModel.Item.Type == BaseItemDto_Type.Episode && !ViewModel.IsNextItemOpen)
        {
            ViewModel.IsNextItemOpen = true;
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

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        Log.Debug("Playback state has changed");

        MediaPlaybackSession playbackSession = sender as MediaPlaybackSession;
        if (playbackSession != null && playbackSession.NaturalVideoHeight != 0)
        {
            if (playbackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                if (displayRequest == null)
                {
                    // This call creates an instance of the DisplayRequest object
                    displayRequest = new DisplayRequest();
                    displayRequest.RequestActive();
                }
            }
            else // PlaybackState is Buffering, None, Opening, or Paused.
            {
                if (displayRequest != null)
                {
                    // Deactivate the display request and set the var to null.
                    displayRequest.RequestRelease();
                    displayRequest = null;
                }
            }
        }
    }

    private async void YesButton_Click(object sender, RoutedEventArgs e)
    {
        var episodes = await ViewModel.GetSeriesAsync(ViewModel.Item.SeriesId.Value, ViewModel.Item.SeasonId.Value);

        if (episodes is not null)
        {
            var nextIndex = ViewModel.Item.IndexNumber + 1;

            if (episodes.Items.Any(x => x.IndexNumber == nextIndex))
            {
                detailsItemPlayRecord.Id = episodes.Items.Single(x => x.IndexNumber.Value == nextIndex).Id.Value;
            }
            else
            {
                var nextSeasonEpisodes = await ViewModel.GetNextSeasonEpisodes(ViewModel.Item.SeriesId.Value, ViewModel.Item.SeasonId.Value);

                if (nextSeasonEpisodes is null || nextSeasonEpisodes.TotalRecordCount == 0 || nextSeasonEpisodes.Items is null)
                {
                    return;
                }

                detailsItemPlayRecord.Id = nextSeasonEpisodes.Items[0].Id!.Value;
            }

            //await ViewModel.LoadMediaItemAsync(detailsItemPlayRecord);

            //var source = LoadSourceAsync();

            //var mediaPlaybackItem = new MediaPlaybackItem(source);

            //_mediaPlayerElement.Source = mediaPlaybackItem;

            //await ViewModel.SessionPlayingAsync();

            //dispatcherTimer.Start();

            //NextEpisodePopup.IsOpen = false;
        }
    }
}
