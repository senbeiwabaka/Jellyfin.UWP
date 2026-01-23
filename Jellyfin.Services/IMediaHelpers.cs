using System;
using System.Threading.Tasks;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Models;

namespace Jellyfin.Services;

public interface IMediaHelpers
{
    Task<Guid> GetPlayIdAsync(UIMediaListItem mediaItem);

    Task<Guid> GetPlayIdAsync(BaseItemDto mediaItem, UIMediaListItem[] seriesData, Guid? seriesNextUpId = null);

    Task<Guid> GetSeriesIdFromEpisodeIdAsync(Guid episodeId);

    string SetImageUrl(BaseItemDto item, string height, string width, string tagKey);

    string SetImageUrl(BaseItemPerson person, string height, string width);

    string SetThumbImageUrl(BaseItemDto item, string height, string width);
}
