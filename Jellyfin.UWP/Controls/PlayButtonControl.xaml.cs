using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls
{
    public sealed partial class PlayButtonControl : UserControl
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

        public PlayButtonControl()
        {
            this.InitializeComponent();
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

            if (item.Type == BaseItemDto_Type.AggregateFolder)
            {
                var playId = await MediaHelpers.GetPlayIdAsync(item);
                var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = playId, };

                ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
            }

            if (item.Type == BaseItemDto_Type.Episode || item.Type == BaseItemDto_Type.Movie)
            {
                var detailsItemPlayRecord = new DetailsItemPlayRecord { Id = item.Id, };

                ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), detailsItemPlayRecord);
            }
        }
    }
}
