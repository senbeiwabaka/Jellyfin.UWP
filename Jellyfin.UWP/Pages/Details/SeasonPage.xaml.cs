using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages
{
    public sealed partial class SeasonPage : Page
    {
        private SeasonSeries seasonSeries;

        public SeasonPage()
        {
            InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<SeasonViewModel>();

            Loaded += SeasonPage_Loaded;
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
            var item = (UIMediaListItemSeries)((Button)sender).DataContext;
            var context = (SeasonViewModel)DataContext;
            var items = context.SeriesMetadata;
            var index = items.IndexOf(item);

            await context.EpisodePlayStateAsync(item);

            var updateItem = await context.GetLatestOnSeriesItemAsync(item.Id);

            items[index] = updateItem;
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

        private async void SeasonPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (DebugHelpers.IsDebugRelease)
            {
                tbDebugPageBlock.Visibility = Visibility.Visible;
            }

            var context = (SeasonViewModel)DataContext;

            await context.LoadMediaInformationAsync(seasonSeries);

            ApplicationView.GetForCurrentView().Title = $"{context.MediaItem.SeriesName} -- {context.MediaItem.Name}";
        }

        private async void WholeSeriesPlay_Click(object sender, RoutedEventArgs e)
        {
            var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = await ((SeasonViewModel)DataContext).GetPlayIdAsync() };

            Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }

        private async void btn_EpisodeMarkFavoriteState_Click(object sender, RoutedEventArgs e)
        {
            var item = (UIMediaListItemSeries)((Button)sender).DataContext;
            var context = (SeasonViewModel)DataContext;
            var items = context.SeriesMetadata;
            var index = items.IndexOf(item);

            await context.EpisodeFavoriteStateAsync(item);

            var updateItem = await context.GetLatestOnSeriesItemAsync(item.Id);

            items[index] = updateItem;
        }
    }
}
