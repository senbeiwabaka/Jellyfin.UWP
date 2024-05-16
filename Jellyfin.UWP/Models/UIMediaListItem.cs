using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.UWP.Models
{
    public class UIMediaListItem : UIItem
    {
        public string Name { get; internal set; }

        public string Url { get; internal set; }

        public bool IsFolder { get; internal set; }

        public BaseItemDto_CollectionType? CollectionType { get; internal set; }

        public string Year { get; internal set; }

        public int? IndexNumber { get; internal set; }
    }
}
