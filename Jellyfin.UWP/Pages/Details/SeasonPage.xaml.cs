using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Models;
using Jellyfin.ViewModels.Details;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages.Details;

public partial class SeasonPage : Page
{
    private Guid id;

    public SeasonPage()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<SeasonViewModel>();
    }

    public SeasonViewModel ViewModel => (SeasonViewModel)DataContext;

    [RelayCommand]
    public void Epi()
    {
        Console.Write("test");
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

    private async void btn_EpisodeMarkFavoriteState_Click(object sender, RoutedEventArgs e)
    {
        var item = (UIMediaListItemSeries)((Button)sender).DataContext;
        var items = ViewModel.SeriesMetadata;
        var index = items.IndexOf(item);

        await ViewModel.EpisodeFavoriteStateAsync(item);

        var updateItem = await ViewModel.GetLatestOnSeriesItemAsync(item.Id);

        items[index] = updateItem;
    }

    private async void btn_EpisodeMarkPlayState_Click(object sender, RoutedEventArgs e)
    {
        var item = (UIMediaListItemSeries)((Button)sender).DataContext;
        var items = ViewModel.SeriesMetadata;
        var index = items.IndexOf(item);

        await ViewModel.EpisodePlayStateAsync(item);

        var updateItem = await ViewModel.GetLatestOnSeriesItemAsync(item.Id);

        items[index] = updateItem;
    }

    private void EpisodePlay_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var item = (UIMediaListItem)button.DataContext;

        item.IsSelected = true;

        var detailsItemPlayRecord = new DetailsItemPlayRecord { MediaId = item.Id, };

        Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
    }

    private void PlayClick(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MediaItemPlayer), ViewModel.DetailsItemPlayRecord);
    }

    private void SeriesItems_ItemClick(object sender, ItemClickEventArgs e)
    {
        Frame.Navigate(typeof(EpisodePage), ((UIMediaListItem)e.ClickedItem).Id);
    }
}
