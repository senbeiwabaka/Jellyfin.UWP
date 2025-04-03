using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls;

internal sealed partial class PlayButtonControl : UserControl
{
    public static readonly DependencyProperty PositionLeftProperty =
        DependencyProperty.Register(
            nameof(PositionLeft),
            typeof(string),
            typeof(PlayButtonControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PositionTopProperty =
        DependencyProperty.Register(
            nameof(PositionTop),
            typeof(string),
            typeof(PlayButtonControl),
            new PropertyMetadata(null));

    private readonly IMediaHelpers mediaHelpers;
    private readonly bool isPlaybackEnabled;

    public PlayButtonControl()
    {
        InitializeComponent();

        mediaHelpers = Ioc.Default.GetRequiredService<IMediaHelpers>();

        var memoryCache = Ioc.Default.GetRequiredService<IMemoryCache>();

        isPlaybackEnabled = memoryCache.Get<UserDto>(JellyfinConstants.UserName)!.Policy?.EnableMediaPlayback ?? false;
    }

    public string PositionLeft
    {
        get { return (string)GetValue(PositionLeftProperty); }
        set { SetValue(PositionLeftProperty, value); }
    }

    public string PositionTop
    {
        get { return (string)GetValue(PositionTopProperty); }
        set { SetValue(PositionTopProperty, value); }
    }

    private async void MediaPlayButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var item = (UIMediaListItem)button.DataContext;
        DetailsItemPlayRecord detailsItemPlayRecord;

        if (item.Type == BaseItemDto_Type.Season)
        {
            var playId = await mediaHelpers.GetPlayIdAsync(item);
            detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };
        }
        else
        {
            detailsItemPlayRecord = new DetailsItemPlayRecord { Id = item.Id, };
        }

        ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
    }
}
