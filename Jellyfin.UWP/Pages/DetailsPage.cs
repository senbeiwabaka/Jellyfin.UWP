using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels;
using System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DetailsPage : Page
    {
        private Guid id;

        public DetailsPage()
        {
            InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<DetailsViewModel>();

            Loaded += DetailsPage_Loaded;
        }

        public async void PlayClick(object sender, RoutedEventArgs e)
        {
            var context = ((DetailsViewModel)DataContext);
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
                var selected = context.SelectedAudioStream;

                detailsItemPlayRecord.SelectedVideoIndex = selected.Index;
                detailsItemPlayRecord.SelectedVideoMediaStreamIndex = selected.MediaStreamIndex;
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

        private async void DetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var context = ((DetailsViewModel)DataContext);

            await context.LoadMediaInformationAsync(id);

            ApplicationView.GetForCurrentView().Title = context.MediaItem.Name;
        }

        private async void SeasonPlay_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (UIMediaListItem)button.DataContext;

            item.IsSelected = true;

            Frame.Navigate(typeof(MediaItemPlayer), await ((DetailsViewModel)DataContext).GetPlayIdAsync());
        }

        private void SeriesItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(SeasonPage), new SeasonSeries { SeasonId = ((UIMediaListItem)e.ClickedItem).Id, SeriesId = ((DetailsViewModel)DataContext).MediaItem.Id, });
        }

        private void SimiliarItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(DetailsPage), ((UIMediaListItem)e.ClickedItem).Id);
        }
    }
}
