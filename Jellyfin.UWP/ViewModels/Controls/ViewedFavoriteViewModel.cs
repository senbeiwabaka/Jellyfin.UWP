﻿using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Controls
{
    internal sealed partial class ViewedFavoriteViewModel : ObservableObject
    {
        private readonly JellyfinApiClient apiClient;
        private readonly IMemoryCache memoryCache;

        [ObservableProperty]
        private bool isFavorite;

        [ObservableProperty]
        private bool hasBeenWatched;

        private UIItem item;

        public ViewedFavoriteViewModel(JellyfinApiClient apiClient, IMemoryCache memoryCache)
        {
            this.apiClient = apiClient;
            this.memoryCache = memoryCache;
        }

        public async Task FavoriteStateAsync()
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

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
        }

        public void Initialize(UIItem item)
        {
            this.item = item;

            IsFavorite = this.item.UserData.IsFavorite;
            HasBeenWatched = this.item.UserData.HasBeenWatched;
        }

        public async Task PlayedStateAsync()
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

            if (item.UserData.HasBeenWatched)
            {
                _ = await apiClient.UserPlayedItems[item.Id]
                    .DeleteAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
            }
            else
            {
                _ = await apiClient.UserPlayedItems[item.Id]
                    .PostAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                        options.QueryParameters.DatePlayed = DateTimeOffset.Now;
                    });
            }
        }
    }
}
