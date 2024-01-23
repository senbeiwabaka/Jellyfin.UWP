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
            this.InitializeComponent();

            this.DataContext = Ioc.Default.GetRequiredService<SeasonViewModel>();

            this.Loaded += SeriesPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            seasonSeries = (SeasonSeries)e.Parameter;

            if (this.Frame.CanGoForward)
            {
                this.Frame.ForwardStack.Clear();
            }

            base.OnNavigatedTo(e);
        }

        private void SeriesItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(EpisodePage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        private async void SeriesPage_Loaded(object sender, RoutedEventArgs e)
        {
            var context = (SeasonViewModel)DataContext;

            await context.LoadMediaInformationAsync(seasonSeries);
        }

        private async void WholeSeriesPlay_Click(object sender, RoutedEventArgs e)
        {
            var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = await ((SeasonViewModel)DataContext).GetPlayIdAsync() };
            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }

        private async void EpisodePlay_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (UIMediaListItem)button.DataContext;

            item.IsSelected = true;

            var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = await ((SeasonViewModel)DataContext).GetPlayIdAsync() };

            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }
    }
}
