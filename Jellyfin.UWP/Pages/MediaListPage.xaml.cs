using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Models.Filters;
using Jellyfin.UWP.ViewModels;
using System;
using System.Linq;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaListPage : Page
    {
        private Guid id;

        public MediaListPage()
        {
            this.InitializeComponent();
        }

        public void ClickItemList(object sender, ItemClickEventArgs e)
        {
            var mediaItem = (UIMediaListItem)e.ClickedItem;

            if (mediaItem.Type == BaseItemDto_Type.Episode)
            {
                Frame.Navigate(typeof(EpisodePage), mediaItem.Id);
            }
            else if (mediaItem.Type == BaseItemDto_Type.Movie)
            {
                Frame.Navigate(typeof(DetailsPage), mediaItem.Id);
            }
            else
            {
                Frame.Navigate(typeof(SeriesPage), mediaItem.Id);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (e.NavigationMode == NavigationMode.Back || (e.NavigationMode == NavigationMode.New && string.Equals(e.SourcePageType.Name, "MainPage", StringComparison.CurrentCultureIgnoreCase)))
            {
                NavigationCacheMode = NavigationCacheMode.Disabled;

                this.Loaded -= MediaListPage_Loaded;

                PageHelpers.ResetPageCache();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                DataContext = Ioc.Default.GetRequiredService<MediaListViewModel>();

                this.Loaded += MediaListPage_Loaded;
            }

            id = (Guid)e.Parameter;
        }

        private async void btn_Favorite_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (UIMediaListItem)button.DataContext;
            var context = (MediaListViewModel)DataContext;
            var items = context.MediaList;
            var index = items.IndexOf(item);

            await context.IsFavoriteStateAsync(item.UserData.IsFavorite, item.Id);

            var updateItem = await context.GetLatestOnItemAsync(item.Id);

            items[index] = updateItem;
        }

        private async void btn_Viewed_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (UIMediaListItem)button.DataContext;
            var context = (MediaListViewModel)DataContext;
            var items = context.MediaList;
            var index = items.IndexOf(item);

            await context.PlayedStateAsync(item.UserData.HasBeenWatched, item.Id);

            var updateItem = await context.GetLatestOnItemAsync(item.Id);

            items[index] = updateItem;
        }

        private async void FiltersButton_Click(object sender, RoutedEventArgs e)
        {
            await ((MediaListViewModel)DataContext).LoadFiltersAsync();

            Filters.IsOpen = true;
        }

        private void FiltersFiltering_ItemClick(object sender, ItemClickEventArgs e)
        {
            Filters.IsOpen = false;
        }

        private async void FiltersFiltering_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((MediaListViewModel)DataContext).FilterReset();

            var listView = (ListView)sender;
            var currentListViewSelectedItems = listView.SelectedItems.Cast<FiltersModel>();
            var genresSelectedItems = GenreFiltering.SelectedItems.Cast<GenreFiltersModel>();

            await ((MediaListViewModel)DataContext).LoadMediaAsync(
                genresSelectedItems.Any() ? genresSelectedItems.Select(x => x.Id).Cast<Guid?>().ToArray() : null,
                currentListViewSelectedItems.Any() ? currentListViewSelectedItems.Select(x => x.Filter).ToArray() : null);
        }

        private void GenreFiltering_ItemClick(object sender, ItemClickEventArgs e)
        {
            Filters.IsOpen = false;
        }

        private async void GenreFiltering_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((MediaListViewModel)DataContext).FilterReset();

            var listView = (ListView)sender;
            var currentListViewSelectedItems = listView.SelectedItems.Cast<GenreFiltersModel>();
            var itemFiltersSelectedItems = FiltersFiltering.SelectedItems.Cast<FiltersModel>();

            await ((MediaListViewModel)DataContext).LoadMediaAsync(
                currentListViewSelectedItems.Any() ? currentListViewSelectedItems.Select(x => x.Id).Cast<Guid?>().ToArray() : null,
                itemFiltersSelectedItems.Any() ? itemFiltersSelectedItems.Select(x => x.Filter).ToArray() : null);
        }

        private async void MediaListPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ((MediaListViewModel)DataContext).InitialLoadAsync(id);

            ApplicationView.GetForCurrentView().Title = ((MediaListViewModel)DataContext).GetTitle();
        }
    }
}
