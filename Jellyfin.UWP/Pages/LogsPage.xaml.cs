using System;
using System.Linq;
using System.Text;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Pages;

internal sealed partial class LogsPage : Page
{
    public LogsPage()
    {
        this.InitializeComponent();

        this.Loaded += LogsPage_Loaded;
    }

    public Type PageType { get; } = typeof(LogsPage);

    private async void LogsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Xbox")
        {
            ApplicationView.GetForCurrentView().Title = "Logs";
        }

        var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("MetroLogs");
        var files = (await folder.GetFilesAsync()).OrderByDescending(x => x.DateCreated).Take(3);

        var content = new StringBuilder();

        foreach (var file in files.OrderBy(x => x.DateCreated))
        {
            content.Append(await FileIO.ReadTextAsync(file));
        }

        tbLobs.Text = content.ToString();
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        ((Frame)Window.Current.Content).GoBack();
    }
}
