using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaItemPage : Page
    {
        private Guid id;

        public MediaItemPage()
        {
            this.InitializeComponent();

            this.DataContext = Ioc.Default.GetRequiredService<MediaItemViewModel>();

            this.Loaded += MediaItemPage_Loaded;
        }

        public void BackClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        public void HomeClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MainPage));
        }

        public void PlayClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), ((MediaItemViewModel)DataContext).MediaItem.Id);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            id = (Guid)e.Parameter;

            if (this.Frame.CanGoForward)
            {
                this.Frame.ForwardStack.Clear();
            }

            base.OnNavigatedTo(e);
        }

        private async void MediaItemPage_Loaded(object sender, RoutedEventArgs e)
        {
            var context = ((MediaItemViewModel)DataContext);

            await context.LoadMediaInformationAsync(id);

            ApplicationView.GetForCurrentView().Title = context.MediaItem.Name;
        }

        private void SimiliarItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPage), ((UIMediaListItem)e.ClickedItem).Id);
        }
    }
}