using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Pages;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Reflection;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls;

internal sealed partial class TopControl : UserControl
{
    public static readonly DependencyProperty PageTypeProperty =
           DependencyProperty.Register(
               nameof(PageType),
               typeof(Type),
               typeof(TopControl),
               new PropertyMetadata(null));

    private readonly IMemoryCache memoryCache;
    private readonly string userName;
    private readonly string jellyfinVersion;
    private readonly string appVersion;

    public TopControl()
    {
        InitializeComponent();

        memoryCache = Ioc.Default.GetRequiredService<IMemoryCache>();

        userName = memoryCache.Get<UserDto>(JellyfinConstants.UserName).Name;
        jellyfinVersion = memoryCache.Get<string>(JellyfinConstants.ServerVersionName);
        appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        Loaded += TopControl_Loaded;
    }

    private void TopControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (PageType is not null)
        {
            if (PageType == typeof(MainPage))
            {
                BtnBack.Visibility = Visibility.Collapsed;
                BtnHome.Visibility = Visibility.Collapsed;
            }

            if (PageType == typeof(LogsPage))
            {
                BtnLogs.Visibility = Visibility.Collapsed;
            }

            if (PageType == typeof(SearchPage))
            {
                BtnSearch.Visibility = Visibility.Collapsed;
            }
        }
    }

    public Type PageType
    {
        get { return (Type)GetValue(PageTypeProperty); }
        set { SetValue(PageTypeProperty, value); }
    }

    private void BackClick(object sender, RoutedEventArgs e)
    {
        ((Frame)Window.Current.Content).GoBack();
    }

    private void HomeClick(object sender, RoutedEventArgs e)
    {
        memoryCache.Remove("Searched-Text");

        ((Frame)Window.Current.Content).Navigate(typeof(MainPage));
    }

    private async void OpenLogsClick(object sender, RoutedEventArgs e)
    {
        if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
        {
            ((Frame)Window.Current.Content).Navigate(typeof(LogsPage));
        }
        else
        {
            var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("MetroLogs");

            await Launcher.LaunchFolderAsync(folder);
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var localSettings = ApplicationData.Current.LocalSettings;

        localSettings.Values.Remove(JellyfinConstants.AccessTokenName);
        localSettings.Values.Remove(JellyfinConstants.SessionName);

        ((Frame)Window.Current.Content).Navigate(typeof(LoginPage));
    }

    private void SearchClick(object sender, RoutedEventArgs e)
    {
        ((Frame)Window.Current.Content).Navigate(typeof(SearchPage));
    }
}
