using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
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
        private readonly IMemoryCache memoryCache;
        private readonly JellyfinApiClient apiClient;
        private readonly IMediaHelpers mediaHelpers;

        public FavoritesViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
            this.mediaHelpers = mediaHelpers;
        }

        public async Task<ObservableCollection<UIMainPageListItem>> GetEpisodesAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var itemsResult = await apiClient.Items
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.SortBy = new[] { ItemSortBy.SeriesSortName, ItemSortBy.SortName, };
                    options.QueryParameters.Filters = new[] { ItemFilter.IsFavorite };
                    options.QueryParameters.Recursive = true;
                    options.QueryParameters.Fields = new[] { ItemFields.PrimaryImageAspectRatio, };
                    options.QueryParameters.ExcludeLocationTypes = new[] { LocationType.Virtual, };
                    options.QueryParameters.EnableTotalRecordCount = false;
                    options.QueryParameters.Limit = 20;
                    options.QueryParameters.IncludeItemTypes = new[] { BaseItemKind.Episode, };
                }, cancellationToken: cancellationToken);

            var items = new ObservableCollection<UIMainPageListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                    {
                        var item = new UIMainPageListItem
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = mediaHelpers.SetImageUrl(x, "250", "300", JellyfinConstants.PrimaryName),
                            Type = x.Type.Value,
                            SeriesName = x.SeriesName,
                            UserData = new UIUserData
                            {
                                HasBeenWatched = x.UserData.Played.Value,
                                IsFavorite = true,
                            },
                            IndexNumber = x.IndexNumber,
                            ParentIndexNumber = x.ParentIndexNumber,
                        };

                        return item;
                    }));

            return items;
        }

        public async Task<ObservableCollection<UIMediaListItem>> GetMoviesAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var itemsResult = await apiClient.Items
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.SortBy = new[] { ItemSortBy.SeriesSortName, ItemSortBy.SortName, };
                    options.QueryParameters.Filters = new[] { ItemFilter.IsFavorite };
                    options.QueryParameters.Recursive = true;
                    options.QueryParameters.Fields = new[] { ItemFields.PrimaryImageAspectRatio, };
                    options.QueryParameters.ExcludeLocationTypes = new[] { LocationType.Virtual, };
                    options.QueryParameters.EnableTotalRecordCount = false;
                    options.QueryParameters.Limit = 20;
                    options.QueryParameters.IncludeItemTypes = new[] { BaseItemKind.Movie, };
                }, cancellationToken: cancellationToken);

            var items = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                    {
                        var item = new UIMediaListItem
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = mediaHelpers.SetImageUrl(x, "250", "300", JellyfinConstants.PrimaryName),
                            Type = x.Type.Value,
                            UserData = new UIUserData
                            {
                                HasBeenWatched = x.UserData.Played.Value,
                                IsFavorite = true,
                            },
                        };

                        return item;
                    }));

            return items;
        }

        public async Task<ObservableCollection<UIPersonItem>> GetPeopleAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var result = await apiClient.Persons
                .GetAsync(options =>
                {
                    options.QueryParameters.Limit = 20;
                    options.QueryParameters.Fields = new[] { ItemFields.PrimaryImageAspectRatio, };
                    options.QueryParameters.IsFavorite = true;
                    options.QueryParameters.UserId = user.Id;
                }, cancellationToken: cancellationToken);

            var items = new ObservableCollection<UIPersonItem>(
                result
                    .Items
                    .Select(x =>
                    new UIPersonItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        ImageUrl = mediaHelpers.SetImageUrl(x, "250", "300", JellyfinConstants.PrimaryName),
                    }));

            return items;
        }

        public async Task<ObservableCollection<UIMediaListItem>> GetSeriesAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var itemsResult = await apiClient.Items
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.SortBy = new[] { ItemSortBy.SeriesSortName, ItemSortBy.SortName, };
                    options.QueryParameters.Filters = new[] { ItemFilter.IsFavorite };
                    options.QueryParameters.Recursive = true;
                    options.QueryParameters.Fields = new[] { ItemFields.PrimaryImageAspectRatio, };
                    options.QueryParameters.ExcludeLocationTypes = new[] { LocationType.Virtual, };
                    options.QueryParameters.EnableTotalRecordCount = false;
                    options.QueryParameters.Limit = 20;
                    options.QueryParameters.IncludeItemTypes = new[] { BaseItemKind.Series, };
                }, cancellationToken: cancellationToken);

            var items = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                    {
                        var item = new UIMediaListItemSeries
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = mediaHelpers.SetImageUrl(x, "250", "300", JellyfinConstants.PrimaryName),
                            Type = x.Type.Value,
                            SeriesName = x.SeriesName,
                            UserData = new UIUserData
                            {
                                HasBeenWatched = x.UserData.Played.Value,
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
