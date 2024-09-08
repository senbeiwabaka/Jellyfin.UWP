using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.Helpers
{
    internal sealed class MediaHelpers : IMediaHelpers
    {
        private readonly JellyfinApiClient apiClient;
        private readonly IMemoryCache memoryCache;

        public MediaHelpers(IMemoryCache memoryCache, JellyfinApiClient apiClient)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
        }

        public async Task<Guid> GetPlayIdAsync(UIMediaListItem mediaItem)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var seasons = await apiClient.Shows[mediaItem.Id].Seasons
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.Fields = new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.MediaSourceCount,
                    };
                });
            var nextUp = await apiClient.Shows.NextUp
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.SeriesId = mediaItem.Id;
                    options.QueryParameters.Fields = new[] { ItemFields.MediaSourceCount, };
                });
            var nextUpItem = nextUp.Items.FirstOrDefault();

            return await GetPlayIdAsync(
                mediaItem.Id,
                mediaItem.Type == BaseItemDto_Type.Movie,
                mediaItem.Type == BaseItemDto_Type.Episode,
                seasons.Items
                .Select(x => new UIMediaListItem
                {
                    Id = x.Id ?? Guid.Empty,
                    Name = x.Name,
                })
                .ToArray(),
                nextUpItem?.Id);
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

        public string SetThumbImageUrl(BaseItemDto item, string height, string width)
        {
            var url = memoryCache.Get<string>(JellyfinConstants.HostUrlName);

            return $"{url}/Items/{item.ParentThumbItemId}/Images/{JellyfinConstants.ThumbName}?fillHeight={height}&fillWidth={width}&quality=96&tag={item.ParentThumbImageTag}";
        }

        private async Task<Guid> GetPlayIdAsync(
            Guid mediaId,
            bool isMovie,
            bool isEpisode,
            UIMediaListItem[] seriesData,
            Guid? seriesNextUpId)
        {
            if (isMovie || isEpisode)
            {
                return mediaId;
            }

            if (seriesData != null && Array.Exists(seriesData, x => x.IsSelected))
            {
                return await GetSeriesEpisodeIdAsync(mediaId, seriesData);
            }

            if (seriesNextUpId.HasValue)
            {
                return seriesNextUpId.Value;
            }

            return await GetSeriesEpisodeIdAsync(mediaId, seriesData);
        }

        private async Task<Guid> GetSeriesEpisodeIdAsync(Guid mediaId, UIMediaListItem[] seriesData)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var episodes = await apiClient.Shows[mediaId].Episodes
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.SeasonId = seriesData.SingleOrDefault(x => x.IsSelected)?.Id ?? seriesData[0].Id;
                    options.QueryParameters.Fields = new ItemFields[] { ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio, };
                });

            return episodes.Items.First(x => !x.UserData.Played.Value && x.UserData.PlayedPercentage < 90).Id ?? Guid.Empty;
        }
    }
}
