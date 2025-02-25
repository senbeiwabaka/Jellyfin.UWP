﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Pages.Details;
using Jellyfin.UWP.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages;

public sealed partial class SearchPage : Page
{
    private IMemoryCache memoryCache;

    private string searchText;

    public SearchPage()
    {
        this.InitializeComponent();

        this.Loaded += SearchPage_Loaded;
    }

    internal Type PageType { get; } = typeof(SearchPage);
    internal SearchViewModel ViewModel => (SearchViewModel)DataContext;

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (e.NavigationMode == NavigationMode.Back || (e.NavigationMode == NavigationMode.New && string.Equals(e.SourcePageType.Name, "MainPage", StringComparison.CurrentCultureIgnoreCase)))
        {
            NavigationCacheMode = NavigationCacheMode.Disabled;

            this.Loaded -= SearchPage_Loaded;

            PageHelpers.ResetPageCache();
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.NavigationMode == NavigationMode.New)
        {
            DataContext = Ioc.Default.GetRequiredService<SearchViewModel>();
            memoryCache = Ioc.Default.GetRequiredService<IMemoryCache>();

            this.Loaded += SearchPage_Loaded;
        }
    }

    private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        await ViewModel.LoadSearchAsync(args.QueryText);

        searchText = args.QueryText;
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var text = memoryCache.GetOrCreate<string>("Searched-Text", entry =>
        {
            entry.SetValue(searchText);

            return searchText;
        });

        if (!string.IsNullOrWhiteSpace(text))
        {
            memoryCache.Set<string>("Searched-Text", searchText);
        }

        Frame.Navigate(typeof(DetailsPage), ((UIMediaListItem)e.ClickedItem).Id);
    }

    private async void SearchPage_Loaded(object sender, RoutedEventArgs e)
    {
        searchText = memoryCache.Get<string>("Searched-Text");

        asbSearch.Text = string.Empty;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            asbSearch.Text = searchText;

            await ViewModel.LoadSearchAsync(searchText);
        }

        ApplicationView.GetForCurrentView().Title = "Search";
    }

    private async void ViewedFavoriteButtonControl_ButtonClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadSearchAsync(asbSearch.Text);
    }
}
