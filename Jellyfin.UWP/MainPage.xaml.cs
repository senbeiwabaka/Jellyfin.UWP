using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Pages;
using Jellyfin.UWP.Pages.Latest;
using Jellyfin.UWP.ViewModels.MainPage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Toolkit.Uwp.UI;
using System.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<MainViewModel>();

            var memoryCache = Ioc.Default.GetRequiredService<IMemoryCache>();

            memoryCache.Remove("Searched-Text");

            this.Loaded += MainPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (Frame.CanGoForward)
            {
                Frame.ForwardStack.Clear();
            }

            if (Frame.CanGoBack)
            {
                Frame.BackStack.Clear();
            }

            base.OnNavigatedTo(e);
        }

        private void btn_Favorites_Click(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).HasEnoughDataToScrollMoviesFavorites = PageHelpers.IsThereEnoughDataForScrolling(lv_FavoriteMovies);
            ((MainViewModel)DataContext).HasEnoughDataToScrollShowsFavorites = PageHelpers.IsThereEnoughDataForScrolling(lv_FavoriteShows);
            ((MainViewModel)DataContext).HasEnoughDataToScrollEpisodesFavorites = PageHelpers.IsThereEnoughDataForScrolling(lv_FavoriteEpisodes);
            ((MainViewModel)DataContext).HasEnoughDataToScrollPeopleFavorites = PageHelpers.IsThereEnoughDataForScrolling(lv_FavoritePeople);
        }

        private void btn_Home_Click(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).HasEnoughDataToScrollContinueWatching = PageHelpers.IsThereEnoughDataForScrolling(lv_Resume);
            ((MainViewModel)DataContext).HasEnoughDataToScrollNextUp = PageHelpers.IsThereEnoughDataForScrolling(lv_NextUp);
        }

        private void ClickItemList(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(MediaListPage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            localSettings.Values.Remove("accessToken");
            localSettings.Values.Remove("session");

            Frame.Navigate(typeof(LoginPage));
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationView.GetForCurrentView().Title = string.Empty;

            await ((MainViewModel)DataContext).LoadInitialAsync();

            SetupLatest();

            ((MainViewModel)DataContext).HasEnoughDataToScrollContinueWatching = PageHelpers.IsThereEnoughDataForScrolling(lv_Resume);
            ((MainViewModel)DataContext).HasEnoughDataToScrollNextUp = PageHelpers.IsThereEnoughDataForScrolling(lv_NextUp);
        }

        private void MediaClickItemList(object sender, ItemClickEventArgs e)
        {
            var mediaItem = (UIMediaListItem)e.ClickedItem;

            if (mediaItem.Type == BaseItemKind.Episode)
            {
                Frame.Navigate(typeof(EpisodePage), mediaItem.Id);
            }
            else if (mediaItem.Type == BaseItemKind.Movie)
            {
                Frame.Navigate(typeof(DetailsPage), mediaItem.Id);
            }
            else
            {
                Frame.Navigate(typeof(SeriesPage), mediaItem.Id);
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

            ScrollViewer scrollViewer = listView.FindVisualChild<ScrollViewer>();
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

        private void SearchClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SearchPage));
        }

        private async void SeriesLink_Click(object sender, RoutedEventArgs e)
        {
            var mediaItem = (UIMediaListItemSeries)((HyperlinkButton)sender).DataContext;
            var seriesId = await MediaHelpers.GetSeriesIdFromEpisodeIdAsync(mediaItem.Id);

            Frame.Navigate(typeof(DetailsPage), seriesId);
        }

        private void SetupLatest()
        {
            lv_Latest.Children.Clear();
            lv_Latest.UpdateLayout();

            foreach (var item in ((MainViewModel)DataContext).MediaListGrouped)
            {
                if (!item.Any())
                {
                    continue;
                }

                var stackPanel = new StackPanel
                {
                    Name = item.Key.Name,
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"Latest {item.Key.Name}",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 40.0d,
                });

                var greaterThanFontIcon = new FontIcon
                {
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Glyph = "\xE76C",
                };

                var viewAllLatestButton = new Button
                {
                    Name = $"button_{item.Key.Name}",
                    Content = greaterThanFontIcon,
                    Background = new SolidColorBrush(Colors.Black),
                };

                if (string.Equals(item.Key.CollectionType, CollectionTypeOptions.Movies.ToString(), System.StringComparison.CurrentCultureIgnoreCase))
                {
                    viewAllLatestButton.Click += (obj, e) =>
                    {
                        Frame.Navigate(typeof(MoviesPage), item.Key.Id);
                    };
                }

                if (string.Equals(item.Key.CollectionType, CollectionTypeOptions.TvShows.ToString(), System.StringComparison.CurrentCultureIgnoreCase))
                {
                    viewAllLatestButton.Click += (obj, e) =>
                    {
                        Frame.Navigate(typeof(ShowsPage), item.Key.Id);
                    };
                }

                stackPanel.Children.Add(viewAllLatestButton);

                lv_Latest.Children.Add(stackPanel);

                var listView = new ListView
                {
                    ItemsSource = item,
                    ItemsPanel = PageHelpers.GetItemsPanelTemplate(),
                    IsItemClickEnabled = true,
                    Name = $"listview_{item.Key.Name}",
                };

                if (string.Equals(CollectionTypeOptions.TvShows.ToString(), item[0].CollectionType, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    listView.ItemTemplate = (DataTemplate)Resources["UISeriesMediaListItemDataTemplate"];
                }
                else
                {
                    listView.ItemTemplate = (DataTemplate)Resources["UIMoviesMediaListItemDataTemplate"];
                }

                listView.ItemClick += MediaClickItemList;

                lv_Latest.Children.Add(listView);

                listView.UpdateLayout();

                var listViewScrollViewer = listView.FindVisualChild<ScrollViewer>();

                listViewScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                listViewScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                listViewScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                listViewScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
            }
        }

        private async void ViewedFavoriteButtonControl_ButtonClick(object sender, RoutedEventArgs e)
        {
            var context = (MainViewModel)DataContext;

            if (context.IsHomeSelected)
            {
                await context.HomeLoadAsync();

                context.HasEnoughDataToScrollContinueWatching = PageHelpers.IsThereEnoughDataForScrolling(lv_Resume);
                context.HasEnoughDataToScrollNextUp = PageHelpers.IsThereEnoughDataForScrolling(lv_NextUp);

                SetupLatest();
            }
            else
            {
                await context.FavoriteLoadAsync();

                context.HasEnoughDataToScrollMoviesFavorites = PageHelpers.IsThereEnoughDataForScrolling(lv_FavoriteMovies);
                context.HasEnoughDataToScrollShowsFavorites = PageHelpers.IsThereEnoughDataForScrolling(lv_FavoriteShows);
                context.HasEnoughDataToScrollEpisodesFavorites = PageHelpers.IsThereEnoughDataForScrolling(lv_FavoriteEpisodes);
                context.HasEnoughDataToScrollPeopleFavorites = PageHelpers.IsThereEnoughDataForScrolling(lv_FavoritePeople);
            }
        }
    }
}
