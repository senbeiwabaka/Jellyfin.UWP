using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.Helpers
{
    internal static class MediaHelpers
    {
        public static async Task<Guid> GetPlayIdAsync(UIMediaListItem mediaItem)
        {
            var memoryCache = Ioc.Default.GetService<IMemoryCache>();
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var apiClient = Ioc.Default.GetService<JellyfinApiClient>();
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

        public static Task<Guid> GetPlayIdAsync(BaseItemDto mediaItem, UIMediaListItem[] seasonsData, Guid? seriesNextUpId)
        {
            return GetPlayIdAsync(
                mediaItem.Id ?? Guid.Empty,
                mediaItem.Type == BaseItemDto_Type.Movie,
                mediaItem.Type == BaseItemDto_Type.Episode,
                seasonsData,
                seriesNextUpId);
        }

        public static async Task<Guid> GetSeriesIdFromEpisodeIdAsync(Guid episodeId)
        {
            var memoryCache = Ioc.Default.GetService<IMemoryCache>();
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var apiClient = Ioc.Default.GetService<JellyfinApiClient>();
            var episodeItem = await apiClient.Items[episodeId]
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                });
            //var userLibraryClient = Ioc.Default.GetService<IUserLibraryClient>();
            //var episodeItem = await userLibraryClient.GetItemAsync(user.Id, episodeId);

            return episodeItem.SeriesId.Value;
        }

        public static string SetImageUrl(BaseItemDto item, string height, string width, string tagKey)
        {
            var imageTags = item.ImageTags?.AdditionalData;
            if (imageTags is null || imageTags.Count == 0 || !imageTags.ContainsKey(tagKey))
            {
                return "https://cdn.onlinewebfonts.com/svg/img_331373.png";
            }

            var memoryCache = Ioc.Default.GetService<IMemoryCache>();
            var url = memoryCache.Get<string>(JellyfinConstants.HostUrlName);
            var imageTagId = imageTags[tagKey];

            return $"{url}/Items/{item.Id}/Images/{tagKey}?fillHeight={height}&fillWidth={width}&quality=96&tag={imageTagId}";
        }

        public static string SetImageUrl(BaseItemPerson person, string height, string width)
        {
            if (string.IsNullOrWhiteSpace(person.PrimaryImageTag))
            {
                return "https://cdn.onlinewebfonts.com/svg/img_331373.png";
            }

            var memoryCache = Ioc.Default.GetService<IMemoryCache>();
            var url = memoryCache.Get<string>(JellyfinConstants.HostUrlName);

            return $"{url}/Items/{person.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight={height}&fillWidth={width}&quality=96&tag={person.PrimaryImageTag}";
        }

        private static async Task<Guid> GetPlayIdAsync(
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

        private static async Task<Guid> GetSeriesEpisodeIdAsync(Guid mediaId, UIMediaListItem[] seriesData)
        {
            var memoryCache = Ioc.Default.GetService<IMemoryCache>();
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var apiClient = Ioc.Default.GetService<JellyfinApiClient>();
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
