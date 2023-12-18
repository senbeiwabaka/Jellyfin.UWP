﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Pages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Toolkit.Uwp.UI;
using System.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
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

        private void ClickItemList(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(MediaListPage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        private async void EpisodeItemPlayClick_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (UIMediaListItem)button.DataContext;
            var playId = await MediaHelpers.GetPlayIdAsync(item);
            var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

            Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }

        private ItemsPanelTemplate GetItemsPanelTemplate()
        {
            string xaml = @"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                            <StackPanel Background=""Transparent"" Orientation=""Horizontal"" />
                    </ItemsPanelTemplate>";
            return XamlReader.LoadWithInitialTemplateValidation(xaml) as ItemsPanelTemplate;
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
            ((MainViewModel)DataContext).LoadInitial();
            await ((MainViewModel)DataContext).LoadMediaListAsync();
            await ((MainViewModel)DataContext).LoadResumeItemsAsync();
            await ((MainViewModel)DataContext).LoadNextUpAsync();
            await ((MainViewModel)DataContext).LoadLatestAsync();

            foreach (var item in ((MainViewModel)DataContext).MediaListGrouped)
            {
                if (!item.Any())
                {
                    continue;
                }

                latest.Children.Add(
                    new TextBlock
                    {
                        Text = $"Latest {item.Key} >",
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 40.0d,
                    });

                var listView = new ListView
                {
                    ItemsSource = item,
                    ItemsPanel = GetItemsPanelTemplate(),
                    IsItemClickEnabled = true,
                    Name = item.Key,
                };

                if (string.Equals(CollectionTypeOptions.TvShows.ToString(), item[0].CollectionType, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    listView.ItemTemplate = (DataTemplate)Resources["UIShowsMediaListItemDataTemplate"];
                }
                else
                {
                    listView.ItemTemplate = (DataTemplate)Resources["UIMediaListItemDataTemplate"];
                }

                listView.ItemClick += MediaClickItemList;

                latest.Children.Add(listView);

                listView.UpdateLayout();

                var listViewScrollViewer = listView.FindVisualChild<ScrollViewer>();

                listViewScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                listViewScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                listViewScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                listViewScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
            }
        }

        private void MediaClickItemList(object sender, ItemClickEventArgs e)
        {
            var mediaItem = ((UIMediaListItem)e.ClickedItem);

            if (mediaItem.Type == BaseItemKind.Episode)
            {
                Frame.Navigate(typeof(EpisodePage), mediaItem.Id);
            }
            else
            {
                Frame.Navigate(typeof(DetailsPage), mediaItem.Id);
            }
        }

        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var listView = button.FindParent<StackPanel>().FindParent<Grid>().FindParent<StackPanel>().FindChild<ListView>();
            var itemsPanelChildren = listView.ItemsPanelRoot.Children;
            var maxItemWidth = itemsPanelChildren.Max(x => x.ActualSize.X);

            ScrollViewer scrollViewer = listView.FindVisualChild<ScrollViewer>();
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

                if (scrollToIndex < 0)
                {
                    listView.ScrollIntoView(listView.Items[0]);

                    button.IsEnabled = false;
                }
                else
                {
                    listView.ScrollIntoView(listView.Items[scrollToIndex]);
                }

                listView.UpdateLayout();

                var nextButton = (Button)button.FindParent<StackPanel>().Children.Last();

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
                    listView.ScrollIntoView(listView.Items.Count - 1);

                    button.IsEnabled = false;
                }
                else
                {
                    listView.ScrollIntoView(listView.Items[scrollToIndex]);
                }

                listView.UpdateLayout();

                var previousButton = (Button)button.FindParent<StackPanel>().Children.First();

                previousButton.IsEnabled = true;
            }
        }

        private void SearchClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SearchPage));
        }
    }
}
