using Jellyfin.Sdk;
using System;

namespace Jellyfin.UWP.Models
{
    public sealed class MediaGroupItem
    {
        public string Name { get; set; }

        public BaseItemKind Type { get; set; }

        public Guid Id { get; set; }

        public string CollectionType {  get; set; }
    }
}
