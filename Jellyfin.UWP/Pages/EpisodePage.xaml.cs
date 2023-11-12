using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels;
using System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EpisodePage : Page
    {
        private Guid id;

        public EpisodePage()
        {
            this.InitializeComponent();

            this.DataContext = Ioc.Default.GetRequiredService<EpisodeViewModel>();

            this.Loaded += EpisodePage_Loaded;
        }

        public void BackClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        public void HomeClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MainPage));
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

        private async void EpisodePage_Loaded(object sender, RoutedEventArgs e)
        {
            var context = ((EpisodeViewModel)DataContext);

            await context.LoadMediaInformationAsync(id);

            ApplicationView.GetForCurrentView().Title = context.MediaItem.Name;
        }

        private void MediaClickItemList(object sender, ItemClickEventArgs e)
        {
            var mediaItem = ((UIMediaListItem)e.ClickedItem);

            ((Frame)Window.Current.Content).Navigate(typeof(EpisodePage), mediaItem.Id);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            var context = ((EpisodeViewModel)DataContext);
            var playId = context.GetPlayIdAsync();
            var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

            if (context.HasMultipleAudioStreams)
            {
                var selectedAudio = context.SelectedAudioStream;

                detailsItemPlayRecord.SelectedAudioIndex = selectedAudio.Index;
                detailsItemPlayRecord.SelectedMediaStreamIndex = selectedAudio.MediaStreamIndex;
            }

            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }

        private void SeriesName_Click(object sender, RoutedEventArgs e)
        {
            var context = ((EpisodeViewModel)DataContext);

            ((Frame)Window.Current.Content).Navigate(typeof(DetailsPage), context.MediaItem.SeriesId);
        }
    }
}
