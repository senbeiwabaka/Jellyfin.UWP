using Jellyfin.Sdk;

namespace Jellyfin.UWP.Models.Filters
{
    public sealed class FiltersModel
    {
        public string DisplayName { get; set; }

        public ItemFilter Filter { get; set; }

        public bool IsSelected { get; set; }
    }
}