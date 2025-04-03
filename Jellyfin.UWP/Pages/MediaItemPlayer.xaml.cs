using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.MessagingModels;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Models.filters;
using Jellyfin.UWP.ViewModels;
using MetroLog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages;

internal sealed partial class MediaItemPlayer : Page, IRecipient<MediaPlayerItemUserDataChanged>
{
    private readonly DispatcherTimer dispatcherTimer;
    private readonly DisplayRequest displayRequest;
    private readonly ILogger Log;
    private readonly Stopwatch stopwatch = new();

    private DetailsItemPlayRecord detailsItemPlayRecord;

    public MediaItemPlayer()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<MediaItemPlayerViewModel>();

        Loaded += MediaItemPlayer_Loaded;
        Unloaded += MediaItemPlayer_Unloaded;

        dispatcherTimer = new DispatcherTimer();
        dispatcherTimer.Tick += DispatcherTimer_Tick;
        dispatcherTimer.Interval = new TimeSpan(0, 0, 5);

        Log = LogManagerFactory.DefaultLogManager.GetLogger<MediaItemPlayer>();

        displayRequest = new DisplayRequest();
        detailsItemPlayRecord = new DetailsItemPlayRecord();
    }

    internal MediaItemPlayerViewModel ViewModel => (MediaItemPlayerViewModel)DataContext;

    public void Receive(MediaPlayerItemUserDataChanged message)
    {
        if (message.Value.PlayedPercentage > 0 && message.Value.PlaybackPositionTicks.HasValue)
        {
            _mediaPlayerElement.MediaPlayer.PlaybackSession.Position = new TimeSpan(message.Value.PlaybackPositionTicks.Value);
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
        WeakReferenceMessenger.Default.Register<MediaPlayerItemUserDataChanged>(this);

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
        WeakReferenceMessenger.Default.Unregister<MediaPlayerItemUserDataChanged>(this);

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
                displayRequest.RequestActive();
            }
            else // PlaybackState is Buffering, None, Opening, or Paused.
            {
                displayRequest.RequestRelease();
            }
        }
    }

    private void btnErrorClose_Click(object sender, RoutedEventArgs e) => Frame.GoBack();
}
