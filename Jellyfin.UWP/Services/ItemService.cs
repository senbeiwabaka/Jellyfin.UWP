using CommunityToolkit.Mvvm.Messaging;
using Jellyfin.Models;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.ViewModels.MessagingModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.Services;

internal sealed class ItemService
{
    private readonly IMemoryCache memoryCache;
    private readonly JellyfinApiClient apiClient;

    public ItemService(IMemoryCache memoryCache, JellyfinApiClient apiClient)
    {
        WeakReferenceMessenger.Default.Register<ItemService, UIMediaListItemRequestMessage>(this, (r, m) =>
        {
            m.Reply(r.GetItem(m.ItemId));
        });

        this.memoryCache = memoryCache;
        this.apiClient = apiClient;
    }

    private async Task<UIMediaListItem> GetItem(Guid itemId, CancellationToken cancellationToken = default)
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName)!;
        var jellyfinItem = (await apiClient.Items[itemId]
            .GetAsync(option =>
            {
                option.QueryParameters.UserId = user.Id;
            }, cancellationToken))!;

        var item = new UIMediaListItem
        {
            Id = jellyfinItem.Id.Value,
            Name = jellyfinItem.Name,
            UserData = new UIUserData
            {
                IsFavorite = jellyfinItem.UserData.IsFavorite.Value,
                UnplayedItemCount = jellyfinItem.UserData.UnplayedItemCount ?? 0,
                HasBeenWatched = jellyfinItem.UserData.Played.Value,
            },
            CollectionType = jellyfinItem.CollectionType,
            IsFolder = jellyfinItem.IsFolder ?? false,
            Type = jellyfinItem.Type ?? BaseItemDto_Type.AggregateFolder,
        };

        return item;
    }
}
