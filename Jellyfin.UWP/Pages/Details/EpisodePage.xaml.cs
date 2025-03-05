using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels.Details;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages.Details;

public sealed partial class EpisodePage : Page
{
    private Guid id;

    public EpisodePage()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<EpisodeViewModel>();
    }

    internal EpisodeViewModel ViewModel => (EpisodeViewModel)DataContext;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        id = (Guid)e.Parameter;

        if (Frame.CanGoForward)
        {
            Frame.ForwardStack.Clear();
        }

        base.OnNavigatedTo(e);
    }

    private void MediaClickItemList(object sender, ItemClickEventArgs e)
    {
        var mediaItem = ((UIMediaListItem)e.ClickedItem);

        Frame.Navigate(typeof(EpisodePage), mediaItem.Id);
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        var playId = await ViewModel.GetPlayIdAsync();
        var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

        if (ViewModel.HasMultipleAudioStreams)
        {
            var selectedAudio = ViewModel.SelectedAudioStream;

            detailsItemPlayRecord.SelectedAudioIndex = selectedAudio.Index;
            detailsItemPlayRecord.SelectedAudioMediaStreamIndex = selectedAudio.MediaStreamIndex;
        }

        Frame.Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
    }

    private void SeriesName_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SeriesPage), ViewModel.MediaItem.SeriesId);
    }

    private async void ViewedFavoriteButtonControl_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadMediaInformationAsync(id);
    }
}
