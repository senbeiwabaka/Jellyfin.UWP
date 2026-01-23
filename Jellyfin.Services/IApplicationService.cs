namespace Jellyfin.Services;

public interface IApplicationService
{
    public string GetSettingValue(string key);

    public void SetSettingValue(string key, string value);
}
