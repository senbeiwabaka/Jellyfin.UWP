using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels.Latest;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages.Latest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MoviesPage : Page
    {
        private Guid id;

        public MoviesPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<MoviesViewModel>();

            this.Loaded += LatestMoviesPage_Loaded;
        }

        private async void LatestMoviesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ((MoviesViewModel)DataContext).LoadInitialAsync(id);

            ((MoviesViewModel)DataContext).HasEnoughDataForContinueScrolling = PageHelpers.IsThereEnoughDataForScrolling(lv_Continue);
            ((MoviesViewModel)DataContext).HasEnoughDataForLatestScrolling = PageHelpers.IsThereEnoughDataForScrolling(lv_Latest);
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

        private void MediaClickItemList(object sender, ItemClickEventArgs e)
        {
            var mediaItem = (UIMediaListItem)e.ClickedItem;

            if (mediaItem.Type == BaseItemKind.Episode)
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

            if (item.Type == BaseItemKind.AggregateFolder)
            {
                var playId = await MediaHelpers.GetPlayIdAsync(item);
                var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

                Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
            }

            if (item.Type == BaseItemKind.Episode || item.Type == BaseItemKind.Movie)
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
    }
}
