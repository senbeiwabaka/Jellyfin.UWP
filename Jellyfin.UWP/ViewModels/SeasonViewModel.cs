using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels
{
    internal sealed partial class SeasonViewModel : ObservableObject
    {
        private readonly IMemoryCache memoryCache;
        private readonly IPlaystateClient playstateClient;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly ITvShowsClient tvShowsClient;
        private readonly IUserLibraryClient userLibraryClient;

        [ObservableProperty]
        private string imageUrl;

        [ObservableProperty]
        private BaseItemDto mediaItem;

        private SeasonSeries seasonSeries;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItemSeries> seriesMetadata;

        public SeasonViewModel(
            IMemoryCache memoryCache,
            IUserLibraryClient userLibraryClient,
            ITvShowsClient tvShowsClient,
            SdkClientSettings sdkClientSettings,
            IPlaystateClient playstateClient)
        {
            this.memoryCache = memoryCache;
            this.userLibraryClient = userLibraryClient;
            this.tvShowsClient = tvShowsClient;
            this.sdkClientSettings = sdkClientSettings;
            this.playstateClient = playstateClient;
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
            var user = memoryCache.Get<UserDto>("user");
            var item = await userLibraryClient.GetItemAsync(user.Id, id);

            return new UIMediaListItemSeries
            {
                Id = item.Id,
                Name = item.Name,
                Url = SetImageUrl(item.Id, "500", "500", item.ImageTags["Primary"]),
                Description = item.Overview,
                UserData = new UIUserData
                {
                    IsFavorite = item.UserData.IsFavorite,
                    HasBeenWatched = item.UserData.Played,
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
            var user = memoryCache.Get<UserDto>("user");
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
                        Url = SetImageUrl(x.Id, "500", "500", x.ImageTags["Primary"]),
                        Description = x.Overview,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite,
                            HasBeenWatched = x.UserData.Played,
                        },
                    };

                    return item;
                }));

            ImageUrl = SetImageUrl(MediaItem.Id, "720", "480", MediaItem.ImageTags["Primary"]);

            this.seasonSeries = seasonSeries;
        }

        private async Task ChangeFavoriteStateAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");

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
            var user = memoryCache.Get<UserDto>("user");

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
            var user = memoryCache.Get<UserDto>("user");
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

        private string SetImageUrl(Guid id, string height, string width, string imageTagId)
        {
            return $"{sdkClientSettings.BaseUrl}/Items/{id}/Images/Primary?fillHeight={height}&fillWidth={width}&quality=96&tag={imageTagId}";
        }
    }
}
