﻿using System;
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
        private readonly IMediaHelpers mediaHelpers;

        [ObservableProperty]
        private string imageUrl;

        [ObservableProperty]
        private BaseItemDto mediaItem;

        private SeasonSeries seasonSeries;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItemSeries> seriesMetadata;

        public SeasonViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
            this.mediaHelpers = mediaHelpers;
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
                Url = mediaHelpers.SetImageUrl(item, "500", "500", JellyfinConstants.PrimaryName),
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
            var userLibraryItem = await apiClient.Items[seasonSeries.SeasonId].
                GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                });

            MediaItem = userLibraryItem;

            var episodes = await apiClient.Shows[seasonSeries.SeriesId].Episodes
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.SeasonId = seasonSeries.SeasonId;
                    options.QueryParameters.Fields = new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.Overview,
                    };
                });

            SeriesMetadata = new ObservableCollection<UIMediaListItemSeries>(
                episodes.Items.Select(x =>
                {
                    var item = new UIMediaListItemSeries
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = mediaHelpers.SetImageUrl(x, "500", "500", JellyfinConstants.PrimaryName),
                        Description = x.Overview,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite.Value,
                            HasBeenWatched = x.UserData.Played.Value,
                        },
                    };

                    return item;
                }));

            ImageUrl = mediaHelpers.SetImageUrl(MediaItem, "720", "480", JellyfinConstants.PrimaryName);

            this.seasonSeries = seasonSeries;
        }

        private async Task ChangeFavoriteStateAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

            if (isFavorite)
            {
                _ = await apiClient.UserFavoriteItems[id]
                    .DeleteAsync(options =>
                     {
                         options.QueryParameters.UserId = user.Id;
                     }, cancellationToken: cancellationToken);
            }
            else
            {
                _ = await apiClient.UserFavoriteItems[id]
                    .PostAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    }, cancellationToken: cancellationToken);
            }
        }

        private async Task ChangePlayStateAsync(Guid id, bool hasBeenWatched, CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

            if (hasBeenWatched)
            {
                _ = await apiClient.UserPlayedItems[id]
                    .DeleteAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    }, cancellationToken: cancellationToken);
            }
            else
            {
                _ = await apiClient.UserPlayedItems[id]
                    .PostAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                        options.QueryParameters.DatePlayed = DateTimeOffset.Now;
                    }, cancellationToken: cancellationToken);
            }
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task FavoriteStateAsync(CancellationToken cancellationToken)
        {
            await ChangeFavoriteStateAsync(MediaItem.Id.Value, MediaItem.UserData.IsFavorite.Value, cancellationToken);

            await LoadMediaInformationAsync(seasonSeries);
        }

        private async Task<Guid> GetSeriesEpisodeIdAsync()
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var episodes = await apiClient.Shows[seasonSeries.SeriesId].Episodes
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.SeasonId = seasonSeries.SeasonId;
                    options.QueryParameters.Fields = new[]
                    {
                        ItemFields.ItemCounts,
                    };
                });

            return episodes.Items.First(x => !x.UserData.Played.Value && (x.UserData.PlayedPercentage ?? 0) < 90).Id.Value;
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task PlayedStateAsync(CancellationToken cancellationToken)
        {
            await ChangePlayStateAsync(MediaItem.Id.Value, MediaItem.UserData.Played.Value, cancellationToken);

            await LoadMediaInformationAsync(seasonSeries);
        }
    }
}
