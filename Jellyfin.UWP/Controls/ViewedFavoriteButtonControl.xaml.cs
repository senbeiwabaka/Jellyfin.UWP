using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls
{
    public sealed partial class ViewedFavoriteButtonControl : UserControl
    {
        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register(
                nameof(Item),
                typeof(UIItem),
                typeof(ViewedFavoriteButtonControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty PositionLeftProperty =
                   DependencyProperty.Register(
               nameof(PositionLeft),
               typeof(string),
               typeof(ViewedFavoriteButtonControl),
               new PropertyMetadata(null));

        public static readonly DependencyProperty PositionTopProperty =
            DependencyProperty.Register(
                nameof(PositionTop),
                typeof(string),
                typeof(ViewedFavoriteButtonControl),
                new PropertyMetadata(null));

        private readonly ViewedFavoriteViewModel context;

        public ViewedFavoriteButtonControl()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<ViewedFavoriteViewModel>();

            Loaded += ViewedFavoriteButtonControl_Loaded;

            context = DataContext as ViewedFavoriteViewModel;
        }

        public event RoutedEventHandler ButtonClick;

        public UIItem Item
        {
            get { return (UIItem)GetValue(ItemProperty); }
            set { SetValue(ItemProperty, value); }
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

        private async void btn_Favorite_Click(object sender, RoutedEventArgs e)
        {
            await context.FavoriteStateAsync();

            ButtonClick?.Invoke(this, new RoutedEventArgs());
        }

        private async void btn_Viewed_Click(object sender, RoutedEventArgs e)
        {
            await context.PlayedStateAsync();

            ButtonClick?.Invoke(this, new RoutedEventArgs());
        }

        private void ViewedFavoriteButtonControl_Loaded(object sender, RoutedEventArgs e)
        {
            context.Initialize(Item);
        }
    }
}
