using Jellyfin.Sdk.Generated.Models;
using System;

namespace Jellyfin.UWP.Models
{
    public class UIItem
    {
        public Guid Id { get; internal set; }

        public BaseItemDto_Type Type { get; internal set; }

        public UIUserData UserData { get; internal set; } = new UIUserData();

        public bool IsSelected { get; internal set; }
    }
}
