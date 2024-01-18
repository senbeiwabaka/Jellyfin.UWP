using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Latest
{
    internal sealed partial class ShowsViewModel : ObservableObject
    {
        private readonly IItemsClient itemsClient;
        private readonly IMemoryCache memoryCache;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly ITvShowsClient tvShowsClient;
        private readonly IUserLibraryClient userLibraryClient;

        [ObservableProperty]
        private bool hasEnoughDataForContinueScrolling;

        [ObservableProperty]
        private bool hasEnoughDataForLatestScrolling;

        [ObservableProperty]
        private bool hasEnoughDataForNextUpScrolling;

        [ObservableProperty]
        private bool hasResumeMedia;

        private Guid id;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> latestMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItemSeries> nextupMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> resumeMediaList;

        public ShowsViewModel(
            SdkClientSettings sdkClientSettings,
            IMemoryCache memoryCache,
            IItemsClient itemsClient,
            ITvShowsClient tvShowsClient,
            IUserLibraryClient userLibraryClient)
        {
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
            this.itemsClient = itemsClient;
            this.tvShowsClient = tvShowsClient;
            this.userLibraryClient = userLibraryClient;
        }

        public async Task LoadInitialAsync(Guid id)
        {
            this.id = id;

            await LoadResumeItemsAsync();
            await LoadLatestAsync();
            await LoadNextUpAsync();
        }

        private static string GetContinueItemImage(SdkClientSettings settings, BaseItemDto item)
        {
            if (item.ParentThumbItemId != null)
            {
                return $"{settings.BaseUrl}/Items/{item.ParentThumbItemId}/Images/{JellyfinConstants.ThumbName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ParentThumbImageTag}";
            }

            if (item.ParentBackdropItemId != null)
            {
                return $"{settings.BaseUrl}/Items/{item.ParentBackdropItemId}/Images/{JellyfinConstants.BackdropName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ParentBackdropImageTags[0]}";
            }

            return $"{settings.BaseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ImageTags[JellyfinConstants.PrimaryName]}";
        }

        private static string GetItemImage(SdkClientSettings settings, BaseItemDto item)
        {
            if (item.ImageTags.ContainsKey(JellyfinConstants.ThumbName))
            {
                return $"{settings.BaseUrl}/Items/{item.Id}/Images/{JellyfinConstants.ThumbName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ImageTags["Thumb"]}";
            }

            if (item.BackdropImageTags.Count > 0)
            {
                return $"{settings.BaseUrl}/Items/{item.Id}/Images/{JellyfinConstants.BackdropName}?fillHeight=239&fillWidth=425&quality=96&tag={item.BackdropImageTags[0]}";
            }

            return $"{settings.BaseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ImageTags[JellyfinConstants.PrimaryName]}";
        }

        private async Task LoadLatestAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await userLibraryClient.GetLatestMediaAsync(
                userId: user.Id,
                limit: 30,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, },
                imageTypeLimit: 1,
                enableImageTypes: new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, },
                parentId: id,
                includeItemTypes: new[] { BaseItemKind.Episode, });

            LatestMediaList = new ObservableCollection<UIMediaListItem>(
            itemsResult
            .Select(x =>
            {
                var item = new UIMediaListItemSeries
                {
                    Id = x.Id,
                    Name = x.Name,
                    Url = GetItemImage(sdkClientSettings, x),
                    CollectionType = x.CollectionType,
                };

                item.UserData.IsFavorite = x.UserData.IsFavorite;
                item.UserData.HasBeenWatched = x.UserData.Played;
                item.UserData.UnplayedItemCount = x.UserData.UnplayedItemCount;

                return item;
            }));
        }

        private async Task LoadNextUpAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await tvShowsClient.GetNextUpAsync(
                userId: user.Id,
                startIndex: 0,
                limit: 24,
                parentId: id,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.DateCreated, ItemFields.BasicSyncInfo, ItemFields.MediaSourceCount, },
                imageTypeLimit: 1,
                enableImageTypes: new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, },
                enableTotalRecordCount: false);

            NextupMediaList = new ObservableCollection<UIMediaListItemSeries>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItemSeries
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.ParentBackdropItemId}/Images/{JellyfinConstants.ThumbName}?fillHeight=239&fillWidth=425&quality=96&tag={x.ParentBackdropImageTags[0]}",
                            Type = x.Type,
                            SeriesName = x.SeriesName,
                        }));
        }

        private async Task LoadResumeItemsAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await itemsClient.GetResumeItemsAsync(
                userId: user.Id,
                parentId: id,
                enableTotalRecordCount: false,
                limit: 5);

            ResumeMediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = GetContinueItemImage(sdkClientSettings, x),
                            Type = x.Type,
                        }));

            HasResumeMedia = ResumeMediaList.Count > 0;
        }
    }
}
