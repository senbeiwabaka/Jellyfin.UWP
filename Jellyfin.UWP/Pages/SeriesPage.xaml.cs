using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SeriesPage : Page
    {
        private SeasonSeries seasonSeries;

        public SeriesPage()
        {
            this.InitializeComponent();

            this.DataContext = Ioc.Default.GetRequiredService<SeriesViewModel>();

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

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MainPage));
        }

        private async void SeriesPage_Loaded(object sender, RoutedEventArgs e)
        {
            var context = ((SeriesViewModel)DataContext);

            await context.LoadMediaInformationAsync(seasonSeries);
        }

        private void WholeSeriesPlay_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SeriesItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), ((UIMediaListItem)e.ClickedItem).Id);
        }
    }
}
