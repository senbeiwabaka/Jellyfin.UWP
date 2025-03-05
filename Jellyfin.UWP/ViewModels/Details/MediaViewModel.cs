using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels.Details;

internal abstract partial class MediaViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : ObservableObject
{
    [ObservableProperty]
    public partial string ImageUrl { get; set; } = "https://cdn.onlinewebfonts.com/svg/img_331373.png";

    [ObservableProperty]
    public partial BaseItemDto MediaItem { get; set; }

    [ObservableProperty]
    public partial bool IsInDebug { get; private set; } = DebugHelpers.IsDebugRelease;

    protected JellyfinApiClient ApiClient { get; } = apiClient;
    protected IMediaHelpers MediaHelpers { get; } = mediaHelpers;
    protected IMemoryCache MemoryCache { get; } = memoryCache;

    public virtual Task<Guid> GetPlayIdAsync()
    {
        return MediaHelpers.GetPlayIdAsync(MediaItem, null, null);
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    public async Task LoadMediaInformationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(2000, cancellationToken);

        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var userLibraryItem = await ApiClient.Items[id]
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
            }, cancellationToken);

        MediaItem = userLibraryItem;

        ImageUrl = MediaHelpers.SetImageUrl(MediaItem, "720", "480", JellyfinConstants.PrimaryName);

        await ExtraExecuteAsync(cancellationToken);

        WeakReferenceMessenger.Default.Send(new WeakRefMessage("Weak Reference Messenger"));
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    internal abstract Task FavoriteStateAsync(CancellationToken cancellationToken);

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    internal abstract Task PlayedStateAsync(CancellationToken cancellationToken);

    protected async Task ChangeFavoriteStateAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);

        if (isFavorite)
        {
            _ = await ApiClient.UserFavoriteItems[id]
                .DeleteAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                }, cancellationToken: cancellationToken);
        }
        else
        {
            _ = await ApiClient.UserFavoriteItems[id]
                .PostAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                }, cancellationToken: cancellationToken);
        }
    }

    protected async Task ChangePlayStateAsync(Guid id, bool hasBeenWatched, CancellationToken cancellationToken = default)
    {
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);

        if (hasBeenWatched)
        {
            _ = await ApiClient.UserPlayedItems[id]
                .DeleteAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                }, cancellationToken: cancellationToken);
        }
        else
        {
            _ = await ApiClient.UserPlayedItems[id]
                .PostAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.DatePlayed = DateTimeOffset.Now;
                }, cancellationToken: cancellationToken);
        }
    }

    protected virtual Task ExtraExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
