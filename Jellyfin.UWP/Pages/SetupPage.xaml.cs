using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.UWP.ViewModels;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Jellyfin.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SetupPage : Page
    {
        public SetupPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<SetupViewModel>();

            this.Loaded += SetupPage_Loaded;
            this.Unloaded += SetupPage_Unloaded;
        }

        private void SetupPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ((SetupViewModel)DataContext).SuccessfullySetUrl -= SetupPage_SuccessfullySetUrl;
        }

        private void SetupPage_Loaded(object sender, RoutedEventArgs e)
        {
            ((SetupViewModel)DataContext).SuccessfullySetUrl += SetupPage_SuccessfullySetUrl;
        }

        private void SetupPage_SuccessfullySetUrl()
        {
            ((Frame)Window.Current.Content).Navigate(typeof(LoginPage));
        }

        private void JellyfinUrlTextBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ((SetupViewModel)DataContext).CompleteCommand.Execute(CancellationToken.None);
            }
        }
    }
}
