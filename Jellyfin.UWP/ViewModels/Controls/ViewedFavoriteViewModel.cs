using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Controls
{
    internal sealed partial class ViewedFavoriteViewModel : ObservableObject
    {
        private readonly IMemoryCache memoryCache;
        private readonly IPlaystateClient playstateClient;
        private readonly IUserLibraryClient userLibraryClient;

        [ObservableProperty]
        private bool isFavorite;

        [ObservableProperty]
        private bool hasBeenWatched;

        private UIItem item;

        public ViewedFavoriteViewModel(
            IUserLibraryClient userLibraryClient,
            IPlaystateClient playstateClient,
            IMemoryCache memoryCache)
        {
            this.userLibraryClient = userLibraryClient;
            this.playstateClient = playstateClient;
            this.memoryCache = memoryCache;
        }

        public async Task FavoriteStateAsync()
        {
            var user = memoryCache.Get<UserDto>("user");

            if (item.UserData.IsFavorite)
            {
                _ = await userLibraryClient.UnmarkFavoriteItemAsync(
                    user.Id,
                    item.Id);
            }
            else
            {
                _ = await userLibraryClient.MarkFavoriteItemAsync(
                    user.Id,
                    item.Id);
            }
        }

        public void Initialize(UIItem item)
        {
            this.item = item;

            IsFavorite = this.item.UserData.IsFavorite;
            HasBeenWatched = this.item.UserData.HasBeenWatched;
        }

        public async Task PlayedStateAsync()
        {
            var user = memoryCache.Get<UserDto>("user");

            if (item.UserData.HasBeenWatched)
            {
                _ = await playstateClient.MarkUnplayedItemAsync(
                    user.Id,
                    item.Id);
            }
            else
            {
                _ = await playstateClient.MarkPlayedItemAsync(
                    user.Id,
                    item.Id,
                    DateTimeOffset.Now);
            }
        }
    }
}
