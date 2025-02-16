using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls
{
    public sealed partial class PlayButtonControl : UserControl
    {
        private readonly IMediaHelpers mediaHelpers;

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

        public PlayButtonControl()
        {
            this.InitializeComponent();

            mediaHelpers = Ioc.Default.GetRequiredService<IMediaHelpers>();
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

                ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
            }
            else
            {
                detailsItemPlayRecord = new DetailsItemPlayRecord { Id = item.Id, };
            }

            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
        }
    }
}
