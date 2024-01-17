using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.UWP.Pages;
using Microsoft.Extensions.Caching.Memory;
using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls
{
    public sealed partial class BackHomeControl : UserControl
    {
        private readonly IMemoryCache memoryCache;
        private readonly string userName;

        public BackHomeControl()
        {
            this.InitializeComponent();

            this.memoryCache = Ioc.Default.GetRequiredService<IMemoryCache>();

            var user = memoryCache.Get<UserDto>("user");

            userName = user.Name;
        }

        public void BackClick(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).GoBack();
        }

        public void HomeClick(object sender, RoutedEventArgs e)
        {
            memoryCache.Remove("Searched-Text");

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
