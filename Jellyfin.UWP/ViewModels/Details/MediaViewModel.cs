using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Details
{
    internal partial class MediaViewModel : ObservableObject
    {
        [ObservableProperty]
        private BaseItemDto mediaItem;

        [ObservableProperty]
        private string imageUrl;

        public MediaViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
        {
            MemoryCache = memoryCache;
            ApiClient = apiClient;
            MediaHelpers = mediaHelpers;
        }

        protected JellyfinApiClient ApiClient { get; }
        protected IMediaHelpers MediaHelpers { get; }
        protected IMemoryCache MemoryCache { get; }

        public virtual Task<Guid> GetPlayIdAsync()
        {
            return MediaHelpers.GetPlayIdAsync(MediaItem, null, null);
        }

        public async Task LoadMediaInformationAsync(Guid id)
        {
            var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var userLibraryItem = await ApiClient.Items[id]
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                });

            MediaItem = userLibraryItem;

            ImageUrl = MediaHelpers.SetImageUrl(MediaItem, "720", "480", JellyfinConstants.PrimaryName); //MediaHelpers.SetImageUrl(MediaItem, "720", "480", JellyfinConstants.PrimaryName);

            await ExtraExecuteAsync();
        }

        protected virtual Task ExtraExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }
}
