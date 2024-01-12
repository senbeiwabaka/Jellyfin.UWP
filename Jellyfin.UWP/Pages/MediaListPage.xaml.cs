﻿using CommunityToolkit.Mvvm.DependencyInjection;
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
            Frame.Navigate(typeof(DetailsPage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (e.NavigationMode == NavigationMode.Back)
            {
                NavigationCacheMode = NavigationCacheMode.Disabled;

                this.Loaded -= MediaListPage_Loaded;

                ResetPageCache();
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

            ApplicationView.GetForCurrentView().Title = ((MediaListViewModel)DataContext).GetTitle();
        }

        private void ResetPageCache()
        {
            int cacheSize = Frame.CacheSize;

            Frame.CacheSize = 0;
            Frame.CacheSize = cacheSize;
        }
    }
}
