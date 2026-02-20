using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Jellyfin.Models;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.ViewModels.MessagingModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ViewModels.Controls;

public sealed partial class ViewedFavoriteViewModel(JellyfinApiClient apiClient, IMemoryCache memoryCache) : ObservableObject
{
    private UIItem item;

    [ObservableProperty]
    public partial bool HasBeenWatched { get; set; }

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task FavoriteStateAsync(CancellationToken cancellationToken)
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName)!;

        if (item.UserData.IsFavorite)
        {
            _ = await apiClient.UserFavoriteItems[item.Id]
                .DeleteAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                });
        }
        else
        {
            _ = await apiClient.UserFavoriteItems[item.Id]
                .PostAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                });
        }

        var updateItem = await WeakReferenceMessenger.Default.Send<UIMediaListItemRequestMessage>(new UIMediaListItemRequestMessage(item.Id));

        //Initialize(updateItem);

        WeakReferenceMessenger.Default.Send(new UIItemChangedMesage(updateItem));
    }

    public void Initialize(UIItem item)
    {
        this.item = item;

        IsFavorite = this.item.UserData.IsFavorite;
        HasBeenWatched = this.item.UserData.HasBeenWatched;
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task ChangePlayStateAsync(CancellationToken cancellationToken)
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName)!;

        if (item.UserData.HasBeenWatched)
        {
            _ = await apiClient.UserPlayedItems[item.Id]
                .DeleteAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                }, cancellationToken: cancellationToken);
        }
        else
        {
            _ = await apiClient.UserPlayedItems[item.Id]
                .PostAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.DatePlayed = DateTimeOffset.Now;
                }, cancellationToken: cancellationToken);
        }

        var updateItem = await WeakReferenceMessenger.Default.Send<UIMediaListItemRequestMessage>(new UIMediaListItemRequestMessage(item.Id));

        //Initialize(updateItem);

        WeakReferenceMessenger.Default.Send(new UIItemChangedMesage(updateItem));
    }
}
