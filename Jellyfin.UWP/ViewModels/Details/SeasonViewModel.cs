using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels.Details;

internal sealed partial class SeasonViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaViewModel(memoryCache, apiClient, mediaHelpers)
{
    public DetailsItemPlayRecord DetailsItemPlayRecord { get; internal set; } = new DetailsItemPlayRecord();

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
            Type = item.Type.Value,
        };
    }

    internal override async Task FavoriteStateAsync(CancellationToken cancellationToken)
    {
        await ChangeFavoriteStateAsync(MediaItem.Id.Value, MediaItem.UserData.IsFavorite.Value, cancellationToken);

        await LoadMediaInformationAsync(MediaItem.Id.Value, cancellationToken);
    }

    internal override async Task PlayedStateAsync(CancellationToken cancellationToken)
    {
        await ChangePlayStateAsync(MediaItem.Id.Value, MediaItem.UserData.Played.Value, cancellationToken);

        await LoadMediaInformationAsync(MediaItem.Id.Value, cancellationToken);
    }

    protected override async Task ExtraExecuteAsync(CancellationToken cancellationToken = default)
    {
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var episodes = await ApiClient.Shows[MediaItem.ParentId.Value].Episodes
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.SeasonId = MediaItem.Id.Value;
                options.QueryParameters.Fields =
                [
                    ItemFields.ItemCounts,
                    ItemFields.PrimaryImageAspectRatio,
                    ItemFields.Overview,
                ];
            }, cancellationToken);

        SeriesMetadata = [.. episodes.Items.Select(x =>
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
                    Type = x.Type.Value,
                };

                return item;
            })];

        DetailsItemPlayRecord.MediaId = await GetPlayIdAsync();
    }

    private async Task<Guid> GetPlayIdAsync()
    {
        if (!SeriesMetadata.Any(x => x.IsSelected))
        {
            return await GetSeriesEpisodeIdAsync();
        }

        return SeriesMetadata.Single(x => x.IsSelected).Id;
    }

    private async Task<Guid> GetSeriesEpisodeIdAsync()
    {
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName)!;
        var episodes = await ApiClient.Shows[MediaItem.ParentId!.Value].Episodes
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.SeasonId = MediaItem.Id;
                options.QueryParameters.Fields =
                [
                    ItemFields.ItemCounts,
                ];
            });

        return episodes.Items.First(x => !x.UserData.Played.Value && (x.UserData.PlayedPercentage ?? 0) < 90).Id.Value;
    }
}
