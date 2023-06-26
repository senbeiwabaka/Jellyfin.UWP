﻿using CommunityToolkit.Mvvm.DependencyInjection;
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
        private readonly ILogger Log;
        private readonly Dictionary<TimedTextSource, string> ttsMap = new();
        private readonly SdkClientSettings sdkClientSettings;
        private DetailsItemPlayRecord detailsItemPlayRecord;

        private readonly DisplayRequest displayRequest;

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
            await ((MediaItemPlayerViewModel)DataContext).SessionProgressAsync(_mediaPlayerElement.MediaPlayer.PlaybackSession.Position.Ticks);
        }

        private async void MediaItemPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            var item = await ((MediaItemPlayerViewModel)DataContext).LoadMediaItemAsync(detailsItemPlayRecord.Id);
            var mediaSourceInfo = await ((MediaItemPlayerViewModel)DataContext).LoadMediaPlaybackInfoAsync();
            var mediaStreams = mediaSourceInfo.MediaStreams;

            if (mediaStreams.Any(x => x.Type == Sdk.MediaStreamType.Subtitle))
            {
                var firstSubtitle = mediaStreams.First(x => x.Type == Sdk.MediaStreamType.Subtitle);
                var subtitleUrl = ((MediaItemPlayerViewModel)DataContext).GetSubtitleUrl(
                    firstSubtitle.Index,
                    string.Equals(firstSubtitle.Codec, "subrip", StringComparison.OrdinalIgnoreCase) ? "vtt" : firstSubtitle.Codec);

                var subtitleUri = new Uri(subtitleUrl);
                var timedTextSource = TimedTextSource.CreateFromUri(subtitleUri);

                timedTextSource.Resolved += Tts_Resolved;

                ttsMap[timedTextSource] = firstSubtitle.DisplayTitle;
            }

            var isTranscoding = false;

            Uri mediaUri;

            if (detailsItemPlayRecord.SelectedAudioIndex.HasValue &&
                mediaStreams.Single(x => x.Index == detailsItemPlayRecord.SelectedAudioIndex.Value && x.Type == Sdk.MediaStreamType.Audio).Codec == "dts")
            {
                mediaUri = new Uri($"{sdkClientSettings.BaseUrl}{mediaSourceInfo.TranscodingUrl}");

                isTranscoding = true;
            }
            else
            {
                mediaUri = ((MediaItemPlayerViewModel)DataContext).GetVideoUrl();
            }

            var source = MediaSource.CreateFromUri(mediaUri);

            foreach (var keyValuePair in ttsMap)
            {
                source.ExternalTimedTextSources.Add(keyValuePair.Key);
            }

            var mediaPlayer = new MediaPlayer
            {
                Source = source,
                AudioCategory = MediaPlayerAudioCategory.Movie,
            };

            _mediaPlayerElement.SetMediaPlayer(mediaPlayer);

            if (!isTranscoding && item.UserData.PlayedPercentage > 0)
            {
                mediaPlayer.PlaybackSession.Position = new TimeSpan(item.UserData.PlaybackPositionTicks);
            }

            mediaPlayer.Play();

            await ((MediaItemPlayerViewModel)DataContext).SessionPlayingAsync();

            displayRequest.RequestActive();

            _mediaPlayerElement.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Log.Info(args.ErrorMessage);
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
