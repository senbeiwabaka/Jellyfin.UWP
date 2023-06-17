﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

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

            this.Loaded += MainPage_Loaded;
        }

        private void ClickItemList(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MediaListPage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        private void ContinueWatchingClickItemList(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(DetailsPage), ((UIMediaListItem)e.ClickedItem).Id);
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ((MainViewModel)DataContext).LoadMediaListAsync();
            await ((MainViewModel)DataContext).LoadResumeItemsAsync();
            await ((MainViewModel)DataContext).LoadNextUpAsync();
        }

        private void SearchClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(SearchPage));
        }
    }
}