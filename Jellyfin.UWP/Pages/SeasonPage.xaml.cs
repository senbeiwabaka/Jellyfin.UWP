using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SeasonPage : Page
    {
        private SeasonSeries seasonSeries;

        public SeasonPage()
        {
            InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<SeasonViewModel>();

            Loaded += SeriesPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            seasonSeries = (SeasonSeries)e.Parameter;

            if (Frame.CanGoForward)
            {
                Frame.ForwardStack.Clear();
            }

            base.OnNavigatedTo(e);
        }

        private async void btn_EpisodeMarkPlayState_Click(object sender, RoutedEventArgs e)
        {
            var item = (UIItem)((Button)sender).DataContext;

            await ((SeasonViewModel)DataContext).EpisodePlayStateAsync(item);
        }

        private async void EpisodePlay_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (UIMediaListItem)button.DataContext;

            item.IsSelected = true;

            var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = await ((SeasonViewModel)DataContext).GetPlayIdAsync() };

            Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }

        private void SeriesItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(EpisodePage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        private async void SeriesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ((SeasonViewModel)DataContext).LoadMediaInformationAsync(seasonSeries);
        }

        private async void WholeSeriesPlay_Click(object sender, RoutedEventArgs e)
        {
            var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = await ((SeasonViewModel)DataContext).GetPlayIdAsync() };

            Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }
    }
}
