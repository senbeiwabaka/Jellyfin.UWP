using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Models.Filters;
using System;
using System.Linq;
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
    public sealed partial class MediaListPage : Page
    {
        private Guid id;

        public MediaListPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<MediaListViewModel>();

            this.Loaded += MediaListPage_Loaded;
        }

        public void BackClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        public void ClickItemList(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            id = (Guid)e.Parameter;

            base.OnNavigatedTo(e);
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
                genresSelectedItems.Any() ? genresSelectedItems.Select(x => x.Id) : null,
                currentListViewSelectedItems.Any() ? currentListViewSelectedItems.Select(x => x.Filter) : null);
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
                currentListViewSelectedItems.Any() ? currentListViewSelectedItems.Select(x => x.Id) : null,
                itemFiltersSelectedItems.Any() ? itemFiltersSelectedItems.Select(x => x.Filter) : null);
        }

        private async void MediaListPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ((MediaListViewModel)DataContext).InitialLoadAsync(id);

            ApplicationView.GetForCurrentView().Title = id.ToString();
        }
    }
}