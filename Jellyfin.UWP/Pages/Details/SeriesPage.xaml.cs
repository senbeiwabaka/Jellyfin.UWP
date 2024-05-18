using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels.Details;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SeriesPage : Page
    {
        private Guid id;

        public SeriesPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<SeriesDetailViewModel>();

            Loaded += SeriesPage_Loaded;
        }

        public async void PlayClick(object sender, RoutedEventArgs e)
        {
            var context = (SeriesDetailViewModel)DataContext;
            var playId = await context.GetPlayIdAsync();
            var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

            if (context.HasMultipleAudioStreams && (context.IsMovie || context.IsEpisode))
            {
                var selected = context.SelectedAudioStream;

                detailsItemPlayRecord.SelectedAudioIndex = selected.Index;
                detailsItemPlayRecord.SelectedAudioMediaStreamIndex = selected.MediaStreamIndex;
            }

            if (context.HasMultipleVideoStreams && (context.IsMovie || context.IsEpisode))
            {
                var selected = context.SelectedVideoStream;

                detailsItemPlayRecord.SelectedVideoId = selected.VideoId;
            }

            Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            id = (Guid)e.Parameter;

            if (Frame.CanGoForward)
            {
                Frame.ForwardStack.Clear();
            }

            base.OnNavigatedTo(e);
        }

        private void NextUpButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(EpisodePage), ((SeriesDetailViewModel)DataContext).NextUpItem?.Id);
        }

        private void SeriesItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(SeasonPage), new SeasonSeries { SeasonId = ((UIMediaListItem)e.ClickedItem).Id, SeriesId = ((SeriesDetailViewModel)DataContext).MediaItem.Id.Value, });
        }

        private async void SeriesPage_Loaded(object sender, RoutedEventArgs e)
        {
            var context = (SeriesDetailViewModel)DataContext;

            await context.LoadMediaInformationAsync(id);

            ApplicationView.GetForCurrentView().Title = context.MediaItem.Name;
        }

        private void SimiliarItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(SeriesPage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        private async void ViewedFavoriteButtonControl_ButtonClick(object sender, RoutedEventArgs e)
        {
            await ((SeriesDetailViewModel)DataContext).LoadMediaInformationAsync(id);
        }
    }
}
