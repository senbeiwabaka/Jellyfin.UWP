using System;

namespace Jellyfin.UWP.Models.Filters
{
    public sealed class GenreFiltersModel
    {
        public string Name { get; set; }

        public Guid Id { get; set; }

        public bool IsSelected { get; set; }
    }
}