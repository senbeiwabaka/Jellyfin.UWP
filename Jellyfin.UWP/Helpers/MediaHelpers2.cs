using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.Helpers
{
    internal sealed class MediaHelpers2 : IMediaHelpers
    {
        private readonly IMemoryCache memoryCache;
        private readonly JellyfinApiClient apiClient;

        public MediaHelpers2(IMemoryCache memoryCache, JellyfinApiClient apiClient)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
        }

        public Task<Guid> GetPlayIdAsync(UIMediaListItem mediaItem)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> GetPlayIdAsync(BaseItemDto mediaItem, UIMediaListItem[] seasonsData, Guid? seriesNextUpId)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> GetSeriesIdFromEpisodeIdAsync(Guid episodeId)
        {
            throw new NotImplementedException();
        }

        public string SetImageUrl(BaseItemDto item, string height, string width, string tagKey)
        {
            var imageTags = item.ImageTags?.AdditionalData;
            if (imageTags is null || imageTags.Count == 0 || !imageTags.ContainsKey(tagKey))
            {
                return "https://cdn.onlinewebfonts.com/svg/img_331373.png";
            }

            var url = memoryCache.Get<string>(JellyfinConstants.HostUrlName);
            var imageTagId = imageTags[tagKey];

            return $"{url}/Items/{item.Id}/Images/{tagKey}?fillHeight={height}&fillWidth={width}&quality=96&tag={imageTagId}";
        }

        public string SetImageUrl(BaseItemPerson person, string height, string width)
        {
            if (string.IsNullOrWhiteSpace(person.PrimaryImageTag))
            {
                return "https://cdn.onlinewebfonts.com/svg/img_331373.png";
            }

            var url = memoryCache.Get<string>(JellyfinConstants.HostUrlName);

            return $"{url}/Items/{person.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight={height}&fillWidth={width}&quality=96&tag={person.PrimaryImageTag}";
        }
    }
}
