using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels.Details;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages.Details;

internal sealed partial class SeriesPage : Page
{
    private Guid id;

    public SeriesPage()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<SeriesDetailViewModel>();
    }

    internal SeriesDetailViewModel ViewModel => (SeriesDetailViewModel)DataContext;

    public async void PlayClick(object sender, RoutedEventArgs e)
    {
        var playId = await ViewModel.GetPlayIdAsync();
        var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

        Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
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

    private void NextUpButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(EpisodePage), ViewModel.NextUpItem?.Id);
    }

    private void SeriesItems_ItemClick(object sender, ItemClickEventArgs e)
    {
        Frame.Navigate(typeof(SeasonPage), new SeasonSeries { SeasonId = ((UIMediaListItem)e.ClickedItem).Id, SeriesId = ViewModel.MediaItem.Id.Value, });
    }

    private void SimiliarItems_ItemClick(object sender, ItemClickEventArgs e)
    {
        Frame.Navigate(typeof(SeriesPage), ((UIMediaListItem)e.ClickedItem).Id);
    }

    private async void ViewedFavoriteButtonControl_ButtonClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadMediaInformationAsync(id);
    }
}
