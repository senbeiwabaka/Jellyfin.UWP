using System;
using Jellyfin.UWP.Pages;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls
{
    public sealed partial class BackHomeControl : UserControl
    {
        public BackHomeControl()
        {
            this.InitializeComponent();
        }

        public void BackClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        public void HomeClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MainPage));
        }

        public async void OpenLogsClick(object sender, RoutedEventArgs e)
        {
            var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("MetroLogs");

            await Launcher.LaunchFolderAsync(folder);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            localSettings.Values.Remove("accessToken");
            localSettings.Values.Remove("session");

            ((Frame)Window.Current.Content).Navigate(typeof(LoginPage));
        }
    }
}
