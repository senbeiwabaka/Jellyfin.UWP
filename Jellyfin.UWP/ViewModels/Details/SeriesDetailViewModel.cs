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

namespace Jellyfin.UWP.ViewModels.Details;

internal sealed partial class SeriesDetailViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaDetailsViewModel(memoryCache, apiClient, mediaHelpers)
{
    [ObservableProperty]
    private UIMediaListItem nextUpItem;

    [ObservableProperty]
    private ObservableCollection<UIMediaListItem> seriesMetadata;

    public override Task<Guid> GetPlayIdAsync()
    {
        return MediaHelpers.GetPlayIdAsync(MediaItem, SeriesMetadata?.ToArray(), NextUpItem?.Id);
    }

    protected override async Task DetailsExtraExecuteAsync(CancellationToken cancellationToken = default)
    {
        NextUpItem = null;

        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var nextUp = await ApiClient.Shows.NextUp
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.SeriesId = MediaItem.Id;
                options.QueryParameters.Fields = new[] { ItemFields.MediaSourceCount, };
            }, cancellationToken);
        var item = nextUp.Items.FirstOrDefault();

        if (item is not null)
        {
            var name = string.Empty;

            if (!item.ParentIndexNumber.HasValue || !item.IndexNumber.HasValue)
            {
                name = item.Name;
            }
            else
            {
                name = $"S{item.ParentIndexNumber}:E{item.IndexNumber} - {item.Name}";
            }

            NextUpItem = new UIMediaListItem
            {
                Id = item.Id.Value,
                Name = name,
                Url = MediaHelpers.SetImageUrl(item, "296", "526", JellyfinConstants.PrimaryName),
                UserData = new UIUserData
                {
                    IsFavorite = item.UserData.IsFavorite.Value,
                    HasBeenWatched = item.UserData.Played.Value,
                },
                Type = item.Type.Value,
                CollectionType = item.CollectionType,
            };
        }

        var seasons = await ApiClient.Shows[MediaItem.Id.Value].Seasons
            .GetAsync(option =>
            {
                option.QueryParameters.UserId = user.Id;
                option.QueryParameters.Fields =
                [
                    ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.MediaSourceCount,
                ];
            }, cancellationToken);

        SeriesMetadata = new ObservableCollection<UIMediaListItem>(
            seasons.Items.Select(x =>
            {
                var item = new UIMediaListItem
                {
                    Id = x.Id.Value,
                    Name = x.Name,
                    Url = SetSeasonImageUrl(x),
                    UserData = new UIUserData
                    {
                        IsFavorite = x.UserData.IsFavorite.Value,
                        UnplayedItemCount = x.UserData.UnplayedItemCount,
                        HasBeenWatched = x.UserData.Played.Value,
                    },
                    CollectionType = x.CollectionType,
                    IsFolder = x.IsFolder ?? false,
                    IndexNumber = x.IndexNumber.Value,
                    Type = x.Type ?? BaseItemDto_Type.AggregateFolder,
                };

                return item;
            }));
    }

    private string SetSeasonImageUrl(BaseItemDto item)
    {
        var baseUrl = MemoryCache.Get<string>(JellyfinConstants.HostUrlName);
        var imageTags = item.ImageTags.AdditionalData;
        if (imageTags.ContainsKey(JellyfinConstants.PrimaryName))
        {
            return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=446&fillWidth=298&quality=96&tag={imageTags[JellyfinConstants.PrimaryName]}";
        }

        return $"{baseUrl}/Items/{item.SeriesId}/Images/{JellyfinConstants.PrimaryName}?fillHeight=446&fillWidth=298&quality=96&tag={item.SeriesPrimaryImageTag}";
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task NextUpFavoriteStateAsync(CancellationToken cancellationToken)
    {
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);

        if (NextUpItem != null)
        {
            if (NextUpItem.UserData.IsFavorite)
            {
                _ = await ApiClient.UserFavoriteItems[NextUpItem.Id]
                .DeleteAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                }, cancellationToken);
            }
            else
            {
                _ = await ApiClient.UserFavoriteItems[NextUpItem.Id]
                .PostAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                }, cancellationToken);
            }

            await LoadMediaInformationAsync(MediaItem.Id.Value, cancellationToken);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task NextUpPlayedStateAsync(CancellationToken cancellationToken)
    {
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);

        if (NextUpItem != null)
        {
            if (NextUpItem.UserData.HasBeenWatched)
            {
                _ = await ApiClient.UserPlayedItems[NextUpItem.Id]
                .DeleteAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                }, cancellationToken);
            }
            else
            {
                _ = await ApiClient.UserPlayedItems[NextUpItem.Id]
                .PostAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.DatePlayed = DateTimeOffset.Now;
                }, cancellationToken);
            }

            await LoadMediaInformationAsync(MediaItem.Id.Value, cancellationToken);
        }
    }
}
