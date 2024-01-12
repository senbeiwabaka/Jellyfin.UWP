using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        private readonly IMemoryCache memoryCache;

        private string searchText;

        public SearchPage()
        {
            this.InitializeComponent();

            this.DataContext = Ioc.Default.GetRequiredService<SearchViewModel>();
            this.memoryCache = Ioc.Default.GetRequiredService<IMemoryCache>();

            this.Loaded += SearchPage_Loaded;
        }

        private async void SearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            searchText = memoryCache.Get<string>("Searched-Text");

            if (!string.IsNullOrEmpty(searchText))
            {
                asbSearch.Text = searchText;

                await ((SearchViewModel)DataContext).LoadSearchAsync(searchText);
            }
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await ((SearchViewModel)DataContext).LoadSearchAsync(args.QueryText);

            searchText = args.QueryText;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var text = memoryCache.GetOrCreate<string>("Searched-Text", entry =>
            {
                entry.SetValue(searchText);

                return searchText;
            });

            if (!string.IsNullOrWhiteSpace(text))
            {
                memoryCache.Set<string>("Searched-Text", searchText);
            }

            ((Frame)Window.Current.Content).Navigate(typeof(DetailsPage), ((UIMediaListItem)e.ClickedItem).Id);
        }
    }
}
