using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.Models;

public class UIItem
{
    public Guid Id { get; set; }

    public BaseItemDto_Type Type { get; set; }

    public UIUserData UserData { get; set; } = new UIUserData();

    public bool IsSelected { get; set; }
}
