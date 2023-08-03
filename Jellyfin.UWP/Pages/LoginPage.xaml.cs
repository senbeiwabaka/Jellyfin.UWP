using CommunityToolkit.Mvvm.DependencyInjection;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<LoginViewModel>();

            this.Loaded += LoginPage_Loaded;
            this.Unloaded += LoginPage_Unloaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (this.Frame.CanGoForward)
            {
                this.Frame.ForwardStack.Clear();
            }

            if (this.Frame.CanGoBack)
            {
                this.Frame.BackStack.Clear();
            }

            base.OnNavigatedTo(e);
        }

        private void btnChangeURL_Click(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(SetupPage));
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            ((LoginViewModel)DataContext).SuccessfullyLoggedIn += LoginPage_SuccessfullyLoggedIn;
        }

        private void LoginPage_SuccessfullyLoggedIn()
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MainPage));
        }

        private void LoginPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ((LoginViewModel)DataContext).SuccessfullyLoggedIn -= LoginPage_SuccessfullyLoggedIn;
        }

        private void PasswordBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ((LoginViewModel)DataContext).LoginCommand.Execute(CancellationToken.None);
            }
        }
    }
}
