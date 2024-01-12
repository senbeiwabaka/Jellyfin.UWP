using System;
using Jellyfin.Sdk;

namespace Jellyfin.UWP.Models
{
    public class UIMediaListItem
    {
        public Guid Id { get; internal set; }

        public string Name { get; internal set; }

        public string Url { get; internal set; }

        public bool IsFolder { get; internal set; }

        public string CollectionType { get; internal set; }

        public string Year { get; internal set; }

        public bool IsSelected { get; internal set; }

        public BaseItemKind Type { get; internal set; }

        public int? IndexNumber { get; internal set; }

        public UIUserData UserData { get; internal set; } = new UIUserData();
    }
}
