using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Pages;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

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

        private void ClickItemList(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MediaListPage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        private void MediaClickItemList(object sender, ItemClickEventArgs e)
        {
            var mediaItem = ((UIMediaListItem)e.ClickedItem);

            if (mediaItem.Type == Sdk.BaseItemKind.Episode)
            {
                ((Frame)Window.Current.Content).Navigate(typeof(EpisodePage), mediaItem.Id);
            }
            else
            {
                ((Frame)Window.Current.Content).Navigate(typeof(DetailsPage), mediaItem.Id);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            localSettings.Values.Remove("accessToken");
            localSettings.Values.Remove("session");

            ((Frame)Window.Current.Content).Navigate(typeof(LoginPage));
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
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
                    ItemsSource = item.ToList(),
                    ItemsPanel = GetItemsPanelTemplate(),
                    ItemTemplate = (DataTemplate)Resources["UiMediaListItemDataTemplate"],
                    IsItemClickEnabled = true,
                };

                listView.ItemClick += MediaClickItemList;

                latest.Children.Add(listView);

                listView.UpdateLayout();

                var listViewScrollViewer = listView.FindVisualChild<ScrollViewer>();

                listViewScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                listViewScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                listViewScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
                listViewScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
            }
        }

        private void SearchClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(SearchPage));
        }

        private ItemsPanelTemplate GetItemsPanelTemplate()
        {
            string xaml = @"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                            <StackPanel Background=""Transparent"" Orientation=""Horizontal"" />
                    </ItemsPanelTemplate>";
            return XamlReader.LoadWithInitialTemplateValidation(xaml) as ItemsPanelTemplate;
        }
    }
}
