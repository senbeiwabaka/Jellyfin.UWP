using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Models;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ViewModels.Details;

public sealed partial class SeasonViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaViewModel(memoryCache, apiClient, mediaHelpers)
{
    public DetailsItemPlayRecord DetailsItemPlayRecord { get; internal set; } = new DetailsItemPlayRecord();

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItemSeries> SeriesMetadata { get; set; }

    public async Task EpisodeFavoriteStateAsync(UIItem item)
    {
        await ChangeFavoriteStateAsync(item.Id, item.UserData.HasBeenWatched);
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    public async Task EpisodePlayStateAsync(UIMediaListItemSeries? item, CancellationToken cancellationToken = default)
    {
        if (item is not null)
        {
            await ChangePlayStateAsync(item.Id, item.UserData.HasBeenWatched, cancellationToken);

            var updateItem = await GetLatestOnSeriesItemAsync(item.Id, cancellationToken);
            var items = SeriesMetadata;
            var index = items.IndexOf(item);

            items[index] = updateItem;
        }
    }

    public async Task<UIMediaListItemSeries> GetLatestOnSeriesItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await ApiClient.Items[id]
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = User.Id;
            }, cancellationToken);

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
        var episodes = await ApiClient.Shows[MediaItem.ParentId.Value].Episodes
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = User.Id;
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

        DetailsItemPlayRecord.MediaId = await GetPlayIdAsync(cancellationToken);
    }

    private async Task<Guid> GetPlayIdAsync(CancellationToken cancellationToken = default)
    {
        if (!SeriesMetadata.Any(x => x.IsSelected))
        {
            var id = await GetSeriesEpisodeIdAsync(true, cancellationToken);

            id ??= await GetSeriesEpisodeIdAsync(false, cancellationToken);

            return id.Value;
        }

        return SeriesMetadata.Single(x => x.IsSelected).Id;
    }

    private async Task<Guid?> GetSeriesEpisodeIdAsync(bool onlyUnwatched = true, CancellationToken cancellationToken = default)
    {
        var episodes = await ApiClient.Shows[MediaItem.ParentId!.Value].Episodes
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = User.Id;
                options.QueryParameters.SeasonId = MediaItem.Id;
                options.QueryParameters.Fields = [];
            }, cancellationToken);

        return episodes?.Items?.FirstOrDefault(x => (x.UserData.Played.Value == onlyUnwatched || x.UserData.Played.Value == false) && (x.UserData.PlayedPercentage ?? 0) < 90)?.Id;
    }
}
