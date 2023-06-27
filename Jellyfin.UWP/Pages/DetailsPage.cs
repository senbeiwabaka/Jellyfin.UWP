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
            this.InitializeComponent();

            this.DataContext = Ioc.Default.GetRequiredService<DetailsViewModel>();

            this.Loaded += DetailsPage_Loaded;
        }

        public void BackClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        public void HomeClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MainPage));
        }

        public async void PlayClick(object sender, RoutedEventArgs e)
        {
            var playId = await ((DetailsViewModel)DataContext).GetPlayId();
            var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

            if (((DetailsViewModel)DataContext).HasMultipleAudioStreams && (((DetailsViewModel)DataContext).IsMovie || ((DetailsViewModel)DataContext).IsEpisode))
            {
                var selectedAudio = ((DetailsViewModel)DataContext).SelectedAudioStream;

                detailsItemPlayRecord.SelectedAudioIndex = selectedAudio.Index;
                detailsItemPlayRecord.SelectedMediaStreamIndex = selectedAudio.MediaStreamIndex;
            }

            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            id = (Guid)e.Parameter;

            if (this.Frame.CanGoForward)
            {
                this.Frame.ForwardStack.Clear();
            }

            base.OnNavigatedTo(e);
        }

        private async void DetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var context = ((DetailsViewModel)DataContext);

            await context.LoadMediaInformationAsync(id);

            ApplicationView.GetForCurrentView().Title = context.MediaItem.Name;
        }

        private void NextUpButton_Click(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), ((DetailsViewModel)DataContext).SeriesNextUpId);
        }

        private async void SeasonPlay_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (UIMediaListItem)button.DataContext;

            item.IsSelected = true;

            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), await ((DetailsViewModel)DataContext).GetPlayId());
        }

        private void SeriesItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(SeriesPage), new SeasonSeries { SeasonId = ((UIMediaListItem)e.ClickedItem).Id, SeriesId = ((DetailsViewModel)DataContext).MediaItem.Id, });
        }

        private void SimiliarItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(DetailsPage), ((UIMediaListItem)e.ClickedItem).Id);
        }
    }
}
