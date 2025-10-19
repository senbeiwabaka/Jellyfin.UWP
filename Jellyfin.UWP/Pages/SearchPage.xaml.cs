using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Pages.Details;
using Jellyfin.UWP.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages;

public sealed partial class SearchPage : Page
{
    private IMemoryCache memoryCache;
    private string? searchText = string.Empty;

    public SearchPage()
    {
        InitializeComponent();
    }

    internal Type PageType { get; } = typeof(SearchPage);

    internal SearchViewModel ViewModel => (SearchViewModel)DataContext;

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (e.NavigationMode == NavigationMode.Back || (e.NavigationMode == NavigationMode.New && string.Equals(e.SourcePageType.Name, "MainPage", StringComparison.CurrentCultureIgnoreCase)))
        {
            NavigationCacheMode = NavigationCacheMode.Disabled;

            Loaded -= SearchPage_Loaded;

            PageHelpers.ResetPageCache();
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        DataContext = DataContext ?? Ioc.Default.GetRequiredService<SearchViewModel>();
        memoryCache = memoryCache ?? Ioc.Default.GetRequiredService<IMemoryCache>();

        searchText = memoryCache.Get<string>("Searched-Text");

        Loaded += SearchPage_Loaded;
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var text = memoryCache.GetOrCreate<string>("Searched-Text", entry =>
        {
            entry.SetValue(asbSearch.Text);

            return asbSearch.Text;
        });

        if (!string.IsNullOrWhiteSpace(text))
        {
            memoryCache.Set<string>("Searched-Text", text);
        }

        Frame.Navigate(typeof(DetailsPage), ((UIMediaListItem)e.ClickedItem).Id);
    }

    private void SearchPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            asbSearch.Text = searchText;
        }

        ApplicationView.GetForCurrentView().Title = "Search";
    }

    private void ViewedFavoriteButtonControl_ButtonClick(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadSearchCommand.Execute(asbSearch.Text);
    }

    private void StackPanel_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var panel = (StackPanel)sender;

        var image = panel.FindChild<Image>()!;

        Canvas.SetZIndex(image, -10);

        var child = panel.Children.Last(x => x.GetType() == typeof(Canvas));

        child.Visibility = Visibility.Visible;
    }

    private void StackPanel_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var panel = (StackPanel)sender;

        var image = panel.FindChild<Image>()!;

        Canvas.SetZIndex(image, 5);

        var child = panel.Children.Last(x => x.GetType() == typeof(Canvas));

        child.Visibility = Visibility.Collapsed;
    }
}
