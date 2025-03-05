using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels.Details;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages.Details;

public sealed partial class DetailsPage : Page
{
    private Guid id;

    public DetailsPage()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<DetailsViewModel>();
    }

    internal DetailsViewModel ViewModel => (DetailsViewModel)DataContext;

    public async void PlayClick(object sender, RoutedEventArgs e)
    {
        var playId = await ViewModel.GetPlayIdAsync();
        var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

        if (ViewModel.HasMultipleAudioStreams && (ViewModel.IsMovie || ViewModel.IsEpisode))
        {
            var selected = ViewModel.SelectedAudioStream;

            detailsItemPlayRecord.SelectedAudioIndex = selected.Index;
            detailsItemPlayRecord.SelectedAudioMediaStreamIndex = selected.MediaStreamIndex;
        }

        if (ViewModel.HasMultipleVideoStreams && (ViewModel.IsMovie || ViewModel.IsEpisode))
        {
            var selected = ViewModel.SelectedVideoStream;

            detailsItemPlayRecord.SelectedVideoId = selected.VideoId;
        }

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

    private void SimiliarItems_ItemClick(object sender, ItemClickEventArgs e)
    {
        Frame.Navigate(typeof(DetailsPage), ((UIMediaListItem)e.ClickedItem).Id);
    }

    private async void ViewedFavoriteButtonControl_ButtonClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadMediaInformationAsync(id);
    }
}
