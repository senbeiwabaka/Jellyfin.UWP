using System;

namespace Jellyfin.UWP.Models
{
    public sealed class UIMediaListItem
    {
        public Guid Id { get; internal set; }

        public string Name { get; internal set; }

        public string Url { get; internal set; }

        public bool IsFolder { get; internal set; }

        public string CollectionType { get; internal set; }

        public string Year { get; internal set; }
    }
}