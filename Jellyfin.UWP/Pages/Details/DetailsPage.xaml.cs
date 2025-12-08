using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels.Details;
using System;
using System.Linq;
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

    public void PlayClick(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MediaItemPlayer), ViewModel.DetailsItemPlayRecord);
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

    private void StackPanel_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var panel = (StackPanel)sender;

        var image = panel.FindChild<Image>()!;

        Canvas.SetZIndex(image, -10);

        var child = panel.Children.Last(x => x.GetType() == typeof(Canvas));

        child.Visibility = Visibility.Visible;
    }

    private void StackPanel_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var panel = (StackPanel)sender;

        var image = panel.FindChild<Image>()!;

        Canvas.SetZIndex(image, 5);

        var child = panel.Children.Last(x => x.GetType() == typeof(Canvas));

        child.Visibility = Visibility.Collapsed;
    }

    private async void ViewedFavoriteButtonControl_ButtonClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadMediaInformationAsync(id);
    }
}
