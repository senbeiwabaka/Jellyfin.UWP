using Jellyfin.Sdk;
using System;

namespace Jellyfin.UWP.Models
{
    public class UIItem
    {
        public Guid Id { get; internal set; }

        public BaseItemKind Type { get; internal set; }

        public UIUserData UserData { get; internal set; } = new UIUserData();

        public bool IsSelected { get; internal set; }
    }
}
