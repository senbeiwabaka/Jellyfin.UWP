using Jellyfin.Sdk;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.MainPage
{
    internal sealed class FavoritesViewModel : IFavoritesViewModel
    {
        private readonly IItemsClient itemsClient;
        private readonly IMemoryCache memoryCache;
        private readonly IPersonsClient personsClient;
        private readonly SdkClientSettings sdkClientSettings;

        public FavoritesViewModel(
            SdkClientSettings sdkClientSettings,
            IMemoryCache memoryCache,
            IItemsClient itemsClient,
            IPersonsClient personsClient)
        {
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
            this.itemsClient = itemsClient;
            this.personsClient = personsClient;
        }

        public async Task<ObservableCollection<UIMediaListItem>> GetEpisodesAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await itemsClient.GetItemsAsync(
                userId: user.Id,
                sortBy: new[] { "SeriesName", "SortName" },
                filters: new[] { ItemFilter.IsFavorite },
                recursive: true,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, },
                excludeLocationTypes: new[] { LocationType.Virtual, },
                enableTotalRecordCount: false,
                limit: 20,
                includeItemTypes: new[] { BaseItemKind.Episode, },
                cancellationToken: cancellationToken);

            var items = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                    {
                        var item = new UIMediaListItemSeries
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                            Type = x.Type,
                            SeriesName = x.SeriesName,
                            UserData = new UIUserData
                            {
                                HasBeenWatched = x.UserData.Played,
                                IsFavorite = true,
                            },
                        };

                        return item;
                    }));

            return items;
        }

        public async Task<ObservableCollection<UIMediaListItem>> GetMoviesAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");
            var result = await itemsClient.GetItemsAsync(
                userId: user.Id,
                sortBy: new[] { "SeriesName", "SortName" },
                filters: new[] { ItemFilter.IsFavorite },
                recursive: true,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, },
                excludeLocationTypes: new[] { LocationType.Virtual, },
                enableTotalRecordCount: false,
                limit: 20,
                includeItemTypes: new[] { BaseItemKind.Movie, },
                cancellationToken: cancellationToken);

            var items = new ObservableCollection<UIMediaListItem>(
                result
                    .Items
                    .Select(x =>
                    {
                        var item = new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                            Type = x.Type,
                            UserData = new UIUserData
                            {
                                HasBeenWatched = x.UserData.Played,
                                IsFavorite = true,
                            },
                        };

                        return item;
                    }));

            return items;
        }

        public async Task<ObservableCollection<UIPersonItem>> GetPeopleAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");
            var result = await personsClient.GetPersonsAsync(
                limit: 20,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, },
                isFavorite: true,
                userId: user.Id,
                cancellationToken: cancellationToken);

            var items = new ObservableCollection<UIPersonItem>(
                result
                    .Items
                    .Select(x =>
                    new UIPersonItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                    }));

            return items;
        }

        public async Task<ObservableCollection<UIMediaListItem>> GetSeriesAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await itemsClient.GetItemsAsync(
                userId: user.Id,
                sortBy: new[] { "SeriesName", "SortName" },
                filters: new[] { ItemFilter.IsFavorite },
                recursive: true,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, },
                excludeLocationTypes: new[] { LocationType.Virtual, },
                enableTotalRecordCount: false,
                limit: 20,
                includeItemTypes: new[] { BaseItemKind.Series, },
                cancellationToken: cancellationToken);

            var items = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                    {
                        var item = new UIMediaListItemSeries
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                            Type = x.Type,
                            SeriesName = x.SeriesName,
                            UserData = new UIUserData
                            {
                                HasBeenWatched = x.UserData.Played,
                                IsFavorite = true,
                                UnplayedItemCount = x.UserData.UnplayedItemCount ?? 0,
                            },
                        };

                        return item;
                    }));

            return items;
        }
    }
}
