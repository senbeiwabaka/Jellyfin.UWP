using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels
{
    internal sealed partial class SeasonViewModel : ObservableObject
    {
        private readonly IMemoryCache memoryCache;
        private readonly JellyfinApiClient apiClient;

        [ObservableProperty]
        private string imageUrl;

        [ObservableProperty]
        private BaseItemDto mediaItem;

        private SeasonSeries seasonSeries;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItemSeries> seriesMetadata;

        public SeasonViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
        }

        public async Task EpisodeFavoriteStateAsync(UIItem item)
        {
            await ChangeFavoriteStateAsync(item.Id, item.UserData.HasBeenWatched);
        }

        public async Task EpisodePlayStateAsync(UIItem item)
        {
            await ChangePlayStateAsync(item.Id, item.UserData.HasBeenWatched);
        }

        public async Task<UIMediaListItemSeries> GetLatestOnSeriesItemAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var item = await apiClient.Items[id]
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                });

            return new UIMediaListItemSeries
            {
                Id = item.Id.Value,
                Name = item.Name,
                Url = MediaHelpers.SetImageUrl(item, "500", "500", JellyfinConstants.PrimaryName),
                Description = item.Overview,
                UserData = new UIUserData
                {
                    IsFavorite = item.UserData.IsFavorite.Value,
                    HasBeenWatched = item.UserData.Played.Value,
                },
            };
        }

        public async Task<Guid> GetPlayIdAsync()
        {
            if (!SeriesMetadata.Any(x => x.IsSelected))
            {
                return await GetSeriesEpisodeIdAsync();
            }

            return SeriesMetadata.Single(x => x.IsSelected).Id;
        }

        public async Task LoadMediaInformationAsync(SeasonSeries seasonSeries)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var userLibraryItem = await apiClient.Items.
                GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.
                })
            var userLibraryItem = await userLibraryClient.GetItemAsync(user.Id, seasonSeries.SeasonId);

            MediaItem = userLibraryItem;

            var episodes = await tvShowsClient.GetEpisodesAsync(
                    seriesId: seasonSeries.SeriesId,
                    userId: user.Id,
                    seasonId: seasonSeries.SeasonId,
                    fields: new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.BasicSyncInfo,
                        ItemFields.Overview,
                    });

            SeriesMetadata = new ObservableCollection<UIMediaListItemSeries>(
                episodes.Items.Select(x =>
                {
                    var item = new UIMediaListItemSeries
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = MediaHelpers.SetImageUrl(x, "500", "500", JellyfinConstants.PrimaryName),
                        Description = x.Overview,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite,
                            HasBeenWatched = x.UserData.Played,
                        },
                    };

                    return item;
                }));

            ImageUrl = MediaHelpers.SetImageUrl(MediaItem, "720", "480", JellyfinConstants.PrimaryName);

            this.seasonSeries = seasonSeries;
        }

        private async Task ChangeFavoriteStateAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

            if (isFavorite)
            {
                _ = await userLibraryClient.UnmarkFavoriteItemAsync(
                    user.Id,
                    id,
                    cancellationToken: cancellationToken);
            }
            else
            {
                _ = await userLibraryClient.MarkFavoriteItemAsync(
                    user.Id,
                    id,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ChangePlayStateAsync(Guid id, bool hasBeenWatched, CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

            if (hasBeenWatched)
            {
                _ = await playstateClient.MarkUnplayedItemAsync(
                    user.Id,
                    id,
                    cancellationToken: cancellationToken);
            }
            else
            {
                _ = await playstateClient.MarkPlayedItemAsync(
                    user.Id,
                    id,
                    DateTimeOffset.Now,
                    cancellationToken: cancellationToken);
            }
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task FavoriteStateAsync(CancellationToken cancellationToken)
        {
            await ChangeFavoriteStateAsync(MediaItem.Id, MediaItem.UserData.IsFavorite, cancellationToken);

            await LoadMediaInformationAsync(seasonSeries);
        }

        private async Task<Guid> GetSeriesEpisodeIdAsync()
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var episodes = await tvShowsClient.GetEpisodesAsync(
                    seriesId: seasonSeries.SeriesId,
                    userId: user.Id,
                    seasonId: seasonSeries.SeasonId,
                    fields: new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.BasicSyncInfo,
                    });

            return episodes.Items.First(x => !x.UserData.Played && (x.UserData.PlayedPercentage ?? 0) < 90).Id;
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task PlayedStateAsync(CancellationToken cancellationToken)
        {
            await ChangePlayStateAsync(MediaItem.Id, MediaItem.UserData.Played, cancellationToken);

            await LoadMediaInformationAsync(seasonSeries);
        }
    }
}
