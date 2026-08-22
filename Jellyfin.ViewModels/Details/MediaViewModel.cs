using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ViewModels.Details;

public abstract partial class MediaViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : ObservableObject
{
    [ObservableProperty]
    public partial string ImageUrl { get; set; } = "https://cdn.onlinewebfonts.com/svg/img_331373.png";

    [ObservableProperty]
    public partial BaseItemDto MediaItem { get; set; }

    [ObservableProperty]
    public partial bool IsInDebug { get; private set; } = DebugHelpers.IsDebugRelease;

    protected JellyfinApiClient ApiClient { get; } = apiClient;
    protected IMediaHelpers MediaHelpers { get; } = mediaHelpers;
    protected UserDto User { get; } = memoryCache.Get<UserDto>(JellyfinConstants.UserName)!;
    protected IMemoryCache MemoryCache { get; } = memoryCache;

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    public async Task LoadMediaInformationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var UserLibraryItem = await ApiClient.Items[id]
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = User.Id;
            }, cancellationToken);

        MediaItem = UserLibraryItem;

        ImageUrl = MediaHelpers.SetImageUrl(MediaItem, "720", "480", JellyfinConstants.PrimaryName);

        await ExtraExecuteAsync(cancellationToken);
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    internal abstract Task FavoriteStateAsync(CancellationToken cancellationToken);

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    internal abstract Task PlayedStateAsync(CancellationToken cancellationToken);

    protected async Task ChangeFavoriteStateAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        if (isFavorite)
        {
            _ = await ApiClient.UserFavoriteItems[id]
                .DeleteAsync(options =>
                {
                    options.QueryParameters.UserId = User.Id;
                }, cancellationToken: cancellationToken);
        }
        else
        {
            _ = await ApiClient.UserFavoriteItems[id]
                .PostAsync(options =>
                {
                    options.QueryParameters.UserId = User.Id;
                }, cancellationToken: cancellationToken);
        }
    }

    protected async Task ChangePlayStateAsync(Guid id, bool hasBeenWatched, CancellationToken cancellationToken = default)
    {
        if (hasBeenWatched)
        {
            _ = await ApiClient.UserPlayedItems[id]
                .DeleteAsync(options =>
                {
                    options.QueryParameters.UserId = User.Id;
                }, cancellationToken: cancellationToken);
        }
        else
        {
            _ = await ApiClient.UserPlayedItems[id]
                .PostAsync(options =>
                {
                    options.QueryParameters.UserId = User.Id;
                    options.QueryParameters.DatePlayed = DateTimeOffset.Now;
                }, cancellationToken: cancellationToken);
        }
    }

    protected virtual Task ExtraExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
