using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
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
            var user = memoryCache.Get<UserDto>("user");
            var tvShowsClient = Ioc.Default.GetService<ITvShowsClient>();
            var seasons = await tvShowsClient.GetSeasonsAsync(
                    mediaItem.Id,
                    user.Id,
                    fields: new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.BasicSyncInfo,
                        ItemFields.MediaSourceCount,
                    });
            var nextUp = await tvShowsClient.GetNextUpAsync(
                user.Id,
                seriesId: mediaItem.Id,
                fields: new[] { ItemFields.MediaSourceCount, });
            var nextUpItem = nextUp.Items.FirstOrDefault();

            return await GetPlayIdAsync(
                mediaItem.Id,
                mediaItem.Type == BaseItemKind.Movie,
                mediaItem.Type == BaseItemKind.Episode,
                seasons.Items
                .Select(x => new UIMediaListItem
                {
                    Id = x.Id,
                    Name = x.Name,
                })
                .ToArray(),
                nextUpItem?.Id);
        }

        public static Task<Guid> GetPlayIdAsync(BaseItemDto mediaItem, UIMediaListItem[] seriesData, Guid? seriesNextUpId)
        {
            return GetPlayIdAsync(
                mediaItem.Id,
                mediaItem.Type == BaseItemKind.Movie,
                mediaItem.Type == BaseItemKind.Episode,
                seriesData,
                seriesNextUpId);
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

            if (seriesData != null && seriesData.Any(x => x.IsSelected))
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
            var user = memoryCache.Get<UserDto>("user");
            var tvShowsClient = Ioc.Default.GetService<ITvShowsClient>();
            var episodes = await tvShowsClient.GetEpisodesAsync(
                seriesId: mediaId,
                userId: user.Id,
                seasonId: seriesData.SingleOrDefault(x => x.IsSelected)?.Id ?? seriesData[0].Id,
                fields: new[] { ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio, });

            return episodes.Items.First(x => !x.UserData.Played && x.UserData.PlayedPercentage < 90).Id;
        }
    }
}
