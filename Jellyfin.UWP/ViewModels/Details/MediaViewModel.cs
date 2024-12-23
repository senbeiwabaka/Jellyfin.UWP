using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using static CommunityToolkit.Mvvm.ComponentModel.__Internals.__TaskExtensions.TaskAwaitableWithoutEndValidation;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

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
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
            MediaHelpers = mediaHelpers;
        }

        protected JellyfinApiClient apiClient { get; }
        protected IMediaHelpers MediaHelpers { get; }
        protected IMemoryCache memoryCache { get; }

        public virtual Task<Guid> GetPlayIdAsync()
        {
            return MediaHelpers.GetPlayIdAsync(MediaItem, null, null);
        }

        public async Task LoadMediaInformationAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var userLibraryItem = await apiClient.Items[id]
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
