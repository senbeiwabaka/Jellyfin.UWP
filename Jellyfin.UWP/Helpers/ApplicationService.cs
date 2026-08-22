using Jellyfin.Services;
using Windows.Storage;

namespace Jellyfin.UWP.Helpers;

internal sealed class ApplicationService : IApplicationService
{
    public string GetSettingValue(string key)
    {
        return ApplicationData.Current.LocalSettings.Values[key]!.ToString()!;
    }

    public void SetSettingValue(string key, string value)
    {
        ApplicationData.Current.LocalSettings.Values[key] = value;
    }
}
