using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels.Latest;
using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages.Latest
{
    public sealed partial class ShowsPage : Page
    {
        private readonly IMediaHelpers mediaHelpers;

        private Guid id;

        public ShowsPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<ShowsViewModel>();

            mediaHelpers = Ioc.Default.GetRequiredService<IMediaHelpers>();

            this.Loaded += LatestShowsPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            id = (Guid)e.Parameter;

            if (Frame.CanGoForward)
            {
                Frame.ForwardStack.Clear();
            }

            base.OnNavigatedTo(e);
        }

        private async void LatestShowsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ((ShowsViewModel)DataContext).LoadInitialAsync(id);

            ((ShowsViewModel)DataContext).HasEnoughDataForContinueScrolling = PageHelpers.IsThereEnoughDataForScrolling(lv_Continue);
            ((ShowsViewModel)DataContext).HasEnoughDataForLatestScrolling = PageHelpers.IsThereEnoughDataForScrolling(lv_Latest);
            ((ShowsViewModel)DataContext).HasEnoughDataForNextUpScrolling = PageHelpers.IsThereEnoughDataForScrolling(lv_NextUp);
        }

        private void MediaClickItemList(object sender, ItemClickEventArgs e)
        {
            var mediaItem = (UIMediaListItem)e.ClickedItem;

            if (mediaItem.Type == BaseItemDto_Type.Episode)
            {
                Frame.Navigate(typeof(EpisodePage), mediaItem.Id);
            }
            else
            {
                Frame.Navigate(typeof(DetailsPage), mediaItem.Id);
            }
        }

        private async void MediaPlayButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (UIMediaListItem)button.DataContext;

            if (item.Type == BaseItemDto_Type.AggregateFolder)
            {
                var playId = await mediaHelpers.GetPlayIdAsync(item);
                var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

                Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
            }

            if (item.Type == BaseItemDto_Type.Episode || item.Type == BaseItemDto_Type.Movie)
            {
                var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = item.Id, };

                Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
            }
        }

        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var listView = button.FindParent<StackPanel>().FindParent<Grid>().FindParent<StackPanel>().FindChild<ListView>();
            var itemsPanelChildren = listView.ItemsPanelRoot.Children;
            var maxItemWidth = itemsPanelChildren.Max(x => x.ActualSize.X);
            var scrollViewer = listView.FindVisualChild<ScrollViewer>();

            if (scrollViewer != null)
            {
                var viewportWidth = scrollViewer.ViewportWidth;
                var index = -1;

                foreach (var item in itemsPanelChildren)
                {
                    var itemToVisualBounds = item.TransformToVisual(scrollViewer).TransformBounds(new Rect(0, 0, item.ActualSize.X, item.ActualSize.Y));

                    if (itemToVisualBounds.Left > 0)
                    {
                        break;
                    }

                    index = listView.IndexFromContainer(item);
                }

                var maxViewable = (int)(viewportWidth / maxItemWidth);

                var scrollToIndex = index - maxViewable;

                listView.UpdateLayout();

                if (scrollToIndex <= 0)
                {
                    listView.ScrollIntoView(listView.Items[0]);

                    button.IsEnabled = false;
                }
                else
                {
                    listView.ScrollIntoView(listView.Items[scrollToIndex]);
                }

                listView.UpdateLayout();

                var nextButton = (Button)button.FindParent<StackPanel>().Children[1];

                nextButton.IsEnabled = true;
            }
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var listView = button.FindParent<StackPanel>().FindParent<Grid>().FindParent<StackPanel>().FindChild<ListView>();
            var itemsPanelChildren = listView.ItemsPanelRoot.Children;
            var maxItemWidth = itemsPanelChildren.Max(x => x.ActualSize.X);
            var scrollViewer = listView.FindVisualChild<ScrollViewer>();

            if (scrollViewer != null)
            {
                var viewportWidth = scrollViewer.ViewportWidth;
                var lastIndex = -1;

                foreach (var item in itemsPanelChildren)
                {
                    var itemToVisualBounds = item.TransformToVisual(scrollViewer).TransformBounds(new Rect(0, 0, item.ActualSize.X, item.ActualSize.Y));

                    if (itemToVisualBounds.Left > viewportWidth)
                    {
                        break;
                    }

                    ++lastIndex;
                }

                var maxViewable = (int)(viewportWidth / maxItemWidth);

                var scrollToIndex = lastIndex + maxViewable;

                listView.UpdateLayout();

                if (scrollToIndex >= listView.Items.Count)
                {
                    listView.ScrollIntoView(listView.Items[listView.Items.Count - 1]);

                    button.IsEnabled = false;
                }
                else
                {
                    listView.ScrollIntoView(listView.Items[scrollToIndex]);
                }

                listView.UpdateLayout();

                var previousButton = (Button)button.FindParent<StackPanel>().Children[0];

                previousButton.IsEnabled = true;
            }
        }

        private async void SeriesLink_Click(object sender, RoutedEventArgs e)
        {
            var mediaItem = (UIMediaListItemSeries)((HyperlinkButton)sender).DataContext;
            var seriesId = await mediaHelpers.GetSeriesIdFromEpisodeIdAsync(mediaItem.Id);

            Frame.Navigate(typeof(DetailsPage), seriesId);
        }
    }
}
