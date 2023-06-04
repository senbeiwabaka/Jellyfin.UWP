using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels;
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
    public sealed partial class DetailsPage : Page
    {
        private Guid id;

        public DetailsPage()
        {
            this.InitializeComponent();

            this.DataContext = Ioc.Default.GetRequiredService<DetailsViewModel>();

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
            ((Frame)Window.Current.Content).Navigate(typeof(MediaItemPlayer), ((DetailsViewModel)DataContext).MediaItem.Id);
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
            var context = ((DetailsViewModel)DataContext);

            await context.LoadMediaInformationAsync(id);

            ApplicationView.GetForCurrentView().Title = context.MediaItem.Name;
        }

        private void SimiliarItems_ItemClick(object sender, ItemClickEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(DetailsPage), ((UIMediaListItem)e.ClickedItem).Id);
        }
    }
}
