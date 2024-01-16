using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Latest
{
    internal sealed partial class MoviesViewModel : ObservableObject
    {
        private const string backdropName = "Backdrop";
        private const string primaryName = "Primary";

        private readonly SdkClientSettings sdkClientSettings;
        private readonly IMemoryCache memoryCache;
        private readonly IItemsClient itemsClient;
        private readonly IUserLibraryClient userLibraryClient;

        [ObservableProperty]
        private bool hasEnoughDataForContinueScrolling;

        [ObservableProperty]
        private bool hasEnoughDataForLatestScrolling;

        [ObservableProperty]
        private bool hasResumeMedia;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> latestMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> resumeMediaList;

        private Guid id;

        public MoviesViewModel(
            SdkClientSettings sdkClientSettings,
            IMemoryCache memoryCache,
            IItemsClient itemsClient,
            IUserLibraryClient userLibraryClient)
        {
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
            this.itemsClient = itemsClient;
            this.userLibraryClient = userLibraryClient;
        }

        public async Task LoadInitialAsync(Guid id)
        {
            this.id = id;

            await LoadResumeItemsAsync();
            await LoadLatestAsync();
        }

        private async Task LoadResumeItemsAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await itemsClient.GetResumeItemsAsync(
                userId: user.Id,
                parentId: id,
                enableTotalRecordCount: false,
                limit: 5,
                includeItemTypes: new[] { BaseItemKind.Movie, });

            ResumeMediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x => new UIMediaListItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = GetContinueItemImage(sdkClientSettings, x),
                        Type = x.Type,
                    }));

            HasResumeMedia = ResumeMediaList.Count > 0;
        }

        private async Task LoadLatestAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await userLibraryClient.GetLatestMediaAsync(
                userId: user.Id,
                limit: 18,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, },
                imageTypeLimit: 1,
                enableImageTypes: new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, },
                parentId: id,
                includeItemTypes: new[] { BaseItemKind.Movie, });

            LatestMediaList = new ObservableCollection<UIMediaListItem>(
            itemsResult
            .Select(x => new UIMediaListItem
            {
                Id = x.Id,
                Name = x.Name,
                Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{primaryName}?fillHeight=239&fillWidth=425&quality=96&tag={x.ImageTags[primaryName]}",
                Type = x.Type,
            }));
        }

        private static string GetContinueItemImage(SdkClientSettings settings, BaseItemDto item)
        {
            if (item.BackdropImageTags.Count > 0)
            {
                return $"{settings.BaseUrl}/Items/{item.Id}/Images/{backdropName}?fillHeight=239&fillWidth=425&quality=96&tag={item.BackdropImageTags[0]}";
            }

            return $"{settings.BaseUrl}/Items/{item.Id}/Images/{primaryName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ImageTags[primaryName]}";
        }
    }
}
