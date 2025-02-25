using CommunityToolkit.Mvvm.DependencyInjection;
using System.Threading;
using Jellyfin.UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Pages;

public sealed partial class SetupPage : Page
{
    public SetupPage()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<SetupViewModel>();

        Loaded += SetupPage_Loaded;
        Unloaded += SetupPage_Unloaded;
    }

    internal SetupViewModel ViewModel => (SetupViewModel)DataContext;

    private void SetupPage_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SuccessfullySetUrl -= SetupPage_SuccessfullySetUrl;
    }

    private void SetupPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SuccessfullySetUrl += SetupPage_SuccessfullySetUrl;
    }

    private void SetupPage_SuccessfullySetUrl()
    {
        ((Frame)Window.Current.Content).Navigate(typeof(LoginPage));
    }

    private void JellyfinUrlTextBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.CompleteCommand.Execute(CancellationToken.None);
        }
    }
}
