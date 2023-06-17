using System;

namespace Jellyfin.UWP.Models
{
    public sealed class SeasonSeries
    {
        public Guid SeasonId { get; set; }

        public Guid SeriesId { get; set; }
    }
}
