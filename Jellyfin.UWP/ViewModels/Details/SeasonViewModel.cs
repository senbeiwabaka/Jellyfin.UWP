using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Details;

internal sealed partial class SeasonViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaViewModel(memoryCache, apiClient, mediaHelpers)
{
    private SeasonSeries seasonSeries;

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItemSeries> SeriesMetadata { get; set; }

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
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var item = await ApiClient.Items[id]
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

    public override async Task<Guid> GetPlayIdAsync()
    {
        if (!SeriesMetadata.Any(x => x.IsSelected))
        {
            return await GetSeriesEpisodeIdAsync();
        }

        return SeriesMetadata.Single(x => x.IsSelected).Id;
    }

    public async Task LoadMediaInformationAsync(SeasonSeries seasonSeries)
    {
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var userLibraryItem = await ApiClient.Items[seasonSeries.SeasonId].
            GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
            });

        MediaItem = userLibraryItem;

        var episodes = await ApiClient.Shows[seasonSeries.SeriesId].Episodes
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
                    Url = MediaHelpers.SetImageUrl(x, "500", "500", JellyfinConstants.PrimaryName),
                    Description = x.Overview,
                    UserData = new UIUserData
                    {
                        IsFavorite = x.UserData.IsFavorite.Value,
                        HasBeenWatched = x.UserData.Played.Value,
                    },
                };

                return item;
            }));

        ImageUrl = MediaHelpers.SetImageUrl(MediaItem, "720", "480", JellyfinConstants.PrimaryName);

        this.seasonSeries = seasonSeries;
    }

    internal override async Task FavoriteStateAsync(CancellationToken cancellationToken)
    {
        await ChangeFavoriteStateAsync(MediaItem.Id.Value, MediaItem.UserData.IsFavorite.Value, cancellationToken);

        await LoadMediaInformationAsync(seasonSeries);
    }

    private async Task<Guid> GetSeriesEpisodeIdAsync()
    {
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var episodes = await ApiClient.Shows[seasonSeries.SeriesId].Episodes
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

    internal override async Task PlayedStateAsync(CancellationToken cancellationToken)
    {
        await ChangePlayStateAsync(MediaItem.Id.Value, MediaItem.UserData.Played.Value, cancellationToken);

        await LoadMediaInformationAsync(seasonSeries);
    }
}
