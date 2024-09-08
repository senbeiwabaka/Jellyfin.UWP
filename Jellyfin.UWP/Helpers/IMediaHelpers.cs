using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Models;
using System;
using System.Threading.Tasks;

namespace Jellyfin.UWP.Helpers
{
    public interface IMediaHelpers
    {
        Task<Guid> GetPlayIdAsync(UIMediaListItem mediaItem);

        Task<Guid> GetPlayIdAsync(BaseItemDto mediaItem, UIMediaListItem[] seasonsData, Guid? seriesNextUpId);

        Task<Guid> GetSeriesIdFromEpisodeIdAsync(Guid episodeId);

        string SetImageUrl(BaseItemDto item, string height, string width, string tagKey);

        string SetImageUrl(BaseItemPerson person, string height, string width);

        string SetThumbImageUrl(BaseItemDto item, string height, string width);
    }
}
