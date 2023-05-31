using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        public SearchPage()
        {
            this.InitializeComponent();

            this.DataContext = Ioc.Default.GetRequiredService<SearchViewModel>();
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await ((SearchViewModel)DataContext).LoadSearchAsync(args.QueryText);
        }

        private void BackClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPage), ((UIMediaListItem)e.ClickedItem).Id);
        }
    }
}