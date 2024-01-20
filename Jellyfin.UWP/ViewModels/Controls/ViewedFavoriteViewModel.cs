using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Controls
{
    internal sealed partial class ViewedFavoriteViewModel : ObservableObject
    {
        private readonly IUserLibraryClient userLibraryClient;
        private readonly IPlaystateClient playstateClient;
        private readonly IMemoryCache memoryCache;

        private UIItem item;

        [ObservableProperty]
        private bool isFavorite;

        [ObservableProperty]
        private bool hasBeenViewed;

        public ViewedFavoriteViewModel(
            IUserLibraryClient userLibraryClient,
            IPlaystateClient playstateClient,
            IMemoryCache memoryCache)
        {
            this.userLibraryClient = userLibraryClient;
            this.playstateClient = playstateClient;
            this.memoryCache = memoryCache;
        }

        public void Initialize(UIItem item)
        {
            this.item = item;

            IsFavorite = this.item.UserData.IsFavorite;
            HasBeenViewed = this.item.UserData.HasBeenWatched;
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        public async Task PlayedStateAsync(CancellationToken cancellationToken)
        {
            var user = memoryCache.Get<UserDto>("user");

            if (HasBeenViewed)
            {
                _ = await playstateClient.MarkUnplayedItemAsync(
                    user.Id,
                    item.Id,
                    cancellationToken: cancellationToken);
            }
            else
            {
                _ = await playstateClient.MarkPlayedItemAsync(
                    user.Id,
                    item.Id,
                    DateTimeOffset.Now,
                    cancellationToken: cancellationToken);
            }

            HasBeenViewed = !HasBeenViewed;
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        public async Task FavoriteStateAsync(CancellationToken cancellationToken)
        {
            var user = memoryCache.Get<UserDto>("user");

            if (IsFavorite)
            {
                _ = await userLibraryClient.UnmarkFavoriteItemAsync(
                    user.Id,
                    item.Id,
                    cancellationToken: cancellationToken);
            }
            else
            {
                _ = await userLibraryClient.MarkFavoriteItemAsync(
                    user.Id,
                    item.Id,
                    cancellationToken: cancellationToken);
            }

            IsFavorite = !IsFavorite;
        }
    }
}
