using Jellyfin.Sdk.Generated.Models;
using System;

namespace Jellyfin.UWP.Models
{
    public sealed class MediaGroupItem
    {
        public string Name { get; set; }

        public BaseItemDto_Type Type { get; set; }

        public Guid Id { get; set; }

        public string CollectionType {  get; set; }
    }
}
