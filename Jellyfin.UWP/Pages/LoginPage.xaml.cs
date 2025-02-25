using CommunityToolkit.Mvvm.DependencyInjection;
using System.Threading;
using Jellyfin.UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages;

public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<LoginViewModel>();

        Loaded += LoginPage_Loaded;
        Unloaded += LoginPage_Unloaded;
    }

    internal LoginViewModel ViewModel => (LoginViewModel)DataContext;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (Frame.CanGoForward)
        {
            Frame.ForwardStack.Clear();
        }

        if (Frame.CanGoBack)
        {
            Frame.BackStack.Clear();
        }

        base.OnNavigatedTo(e);
    }

    private void btnChangeURL_Click(object sender, RoutedEventArgs e)
    {
        ((Frame)Window.Current.Content).Navigate(typeof(SetupPage));
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenPopup = false;
    }

    private void LoginPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SuccessfullyLoggedIn += LoginPage_SuccessfullyLoggedIn;
    }

    private void LoginPage_SuccessfullyLoggedIn()
    {
        ((Frame)Window.Current.Content).Navigate(typeof(MainPage));
    }

    private void LoginPage_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SuccessfullyLoggedIn -= LoginPage_SuccessfullyLoggedIn;
    }

    private void PasswordBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.LoginCommand.Execute(CancellationToken.None);
        }
    }
}
