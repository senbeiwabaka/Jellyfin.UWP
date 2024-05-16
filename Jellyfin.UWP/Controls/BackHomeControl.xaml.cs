using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
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

            this.memoryCache = Ioc.Default.GetService<IMemoryCache>();

            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

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

            localSettings.Values.Remove(JellyfinConstants.AccessTokenName);
            localSettings.Values.Remove(JellyfinConstants.SessionName);

            ((Frame)Window.Current.Content).Navigate(typeof(LoginPage));
        }
    }
}
