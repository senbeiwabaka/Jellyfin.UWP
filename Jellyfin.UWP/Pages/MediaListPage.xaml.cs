using System;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Jellyfin.Models;
using Jellyfin.Models.Filters;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Pages.Details;
using Jellyfin.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages;

internal sealed partial class MediaListPage : Page
{
    private Guid id;

    public MediaListPage()
    {
        InitializeComponent();
    }

    internal MediaListViewModel ViewModel => (MediaListViewModel)DataContext;

    public void ClickItemList(object sender, ItemClickEventArgs e)
    {
        var mediaItem = (UIMediaListItem)e.ClickedItem;

        if (mediaItem.Type == BaseItemDto_Type.Episode)
        {
            Frame.Navigate(typeof(EpisodePage), mediaItem.Id);
        }
        else if (mediaItem.Type == BaseItemDto_Type.Movie)
        {
            Frame.Navigate(typeof(DetailsPage), mediaItem.Id);
        }
        else
        {
            Frame.Navigate(typeof(SeriesPage), mediaItem.Id);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (e.NavigationMode == NavigationMode.Back || (e.NavigationMode == NavigationMode.New && string.Equals(e.SourcePageType.Name, "MainPage", StringComparison.CurrentCultureIgnoreCase)))
        {
            NavigationCacheMode = NavigationCacheMode.Disabled;

            Loaded -= MediaListPage_Loaded;

            PageHelpers.ResetPageCache();
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.NavigationMode == NavigationMode.New)
        {
            DataContext = Ioc.Default.GetRequiredService<MediaListViewModel>();

            Loaded += MediaListPage_Loaded;
        }

        id = (Guid)e.Parameter;
    }

    private void FiltersFiltering_ItemClick(object sender, ItemClickEventArgs e)
    {
        var filterModel = (FiltersModel)e.ClickedItem;
        var index = ViewModel.FilteringFilters.IndexOf(filterModel);

        ViewModel.FilteringFilters[index].IsSelected = !ViewModel.FilteringFilters[index].IsSelected;
    }

    private void GenreFiltering_ItemClick(object sender, ItemClickEventArgs e)
    {
        var genreFiltersModel = (GenreFiltersModel)e.ClickedItem;
        var index = ViewModel.GenresFilterList.IndexOf(genreFiltersModel);

        ViewModel.GenresFilterList[index].IsSelected = !ViewModel.GenresFilterList[index].IsSelected;
    }

    private async void MediaListPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitialLoadAsync(id);
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
