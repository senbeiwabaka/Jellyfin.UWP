using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.Collections;
using Jellyfin.Sdk;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels.MainPage
{
    internal sealed class HomeViewModel : IHomeViewModel
    {
        private readonly IItemsClient itemsClient;
        private readonly IMemoryCache memoryCache;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly ITvShowsClient tvShowsClient;
        private readonly IUserLibraryClient userLibraryClient;
        private readonly IUserViewsClient userViewsClient;

        public HomeViewModel(
            SdkClientSettings sdkClientSettings,
            IMemoryCache memoryCache,
            IItemsClient itemsClient,
            ITvShowsClient tvShowsClient,
            IUserLibraryClient userLibraryClient,
            IUserViewsClient userViewsClient)
        {
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
            this.itemsClient = itemsClient;
            this.tvShowsClient = tvShowsClient;
            this.userLibraryClient = userLibraryClient;
            this.userViewsClient = userViewsClient;
        }

        public async Task<ObservableGroupedCollection<MediaGroupItem, UIMediaListItem>> LoadLatestAsync(ObservableCollection<UIMediaListItem> mediaList, CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");

            var mediaListGrouped = new ObservableGroupedCollection<MediaGroupItem, UIMediaListItem>();

            foreach (var record in mediaList.Where(x => !string.Equals(CollectionTypeOptions.BoxSets.ToString(), x.CollectionType, System.StringComparison.CurrentCultureIgnoreCase)))
            {
                var itemsResult = await userLibraryClient.GetLatestMediaAsync(
                    userId: user.Id,
                    limit: 16,
                    fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, ItemFields.Path, },
                    imageTypeLimit: 1,
                    enableImageTypes: new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, },
                    parentId: record.Id,
                    cancellationToken: cancellationToken);
                var itemType = new MediaGroupItem { Id = record.Id, Name = record.Name, Type = record.Type, CollectionType = record.CollectionType, };

                if (string.Equals(CollectionTypeOptions.TvShows.ToString(), record.CollectionType, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    mediaListGrouped.Add(new ObservableGroup<MediaGroupItem, UIMediaListItem>(
                    itemType,
                    itemsResult
                    .Select(x =>
                    {
                        var item = new UIMediaListItemSeries
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                            CollectionType = record.CollectionType,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite,
                                HasBeenWatched = x.UserData.Played,
                                UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0,
                            },
                        };

                        return item;
                    })));
                }
                else
                {
                    mediaListGrouped.Add(new ObservableGroup<MediaGroupItem, UIMediaListItem>(
                        itemType,
                        itemsResult
                        .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                            CollectionType = x.CollectionType,
                            Type = x.Type,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite,
                                HasBeenWatched = x.UserData.Played,
                                UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0,
                            },
                        })));
                }
            }

            return mediaListGrouped;
        }

        public async Task<ObservableCollection<UIMediaListItem>> LoadMediaListAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");
            var viewsItemsResult = await userViewsClient.GetUserViewsAsync(userId: user.Id, cancellationToken: cancellationToken);

            var mediaList = new ObservableCollection<UIMediaListItem>(
                viewsItemsResult
                    .Items
                    .Select(x =>
                    new UIMediaListItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                        IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                        CollectionType = x.CollectionType,
                        Type = x.Type,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite,
                            HasBeenWatched = x.UserData.Played,
                            UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0,
                        },
                    }));

            return mediaList;
        }

        public async Task<ObservableCollection<UIMediaListItemSeries>> LoadNextUpAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await tvShowsClient.GetNextUpAsync(
                userId: user.Id,
                startIndex: 0,
                limit: 10,
                cancellationToken: cancellationToken);

            var nextupMediaList = new ObservableCollection<UIMediaListItemSeries>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItemSeries
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                            Type = x.Type,
                            SeriesName = x.SeriesName,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite,
                                HasBeenWatched = x.UserData.Played,
                                UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0,
                            },
                        }));

            return nextupMediaList;
        }

        public async Task<(ObservableCollection<UIMediaListItem>, bool)> LoadResumeItemsAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await itemsClient.GetResumeItemsAsync(
                userId: user.Id,
                enableTotalRecordCount: false,
                cancellationToken: cancellationToken);

            var resumeMediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = GetResumeImage(sdkClientSettings, x),
                            Type = x.Type,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite,
                                HasBeenWatched = x.UserData.Played,
                                UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0,
                            },
                        }));

            var hasResumeMedia = resumeMediaList.Count > 0;

            return (resumeMediaList, hasResumeMedia);
        }

        private static string GetResumeImage(SdkClientSettings settings, BaseItemDto item)
        {
            if (item.ParentThumbItemId != null)
            {
                return $"{settings.BaseUrl}/Items/{item.ParentThumbItemId}/Images/{JellyfinConstants.ThumbName}?fillHeight=266&fillWidth=472&quality=96&tag={item.ParentThumbImageTag}";
            }

            if (item.ParentBackdropItemId != null)
            {
                return $"{settings.BaseUrl}/Items/{item.ParentBackdropItemId}/Images/{JellyfinConstants.BackdropName}?fillHeight=266&fillWidth=472&quality=96&tag={item.ParentBackdropImageTags[0]}";
            }

            if (item.BackdropImageTags.Count > 0)
            {
                return $"{settings.BaseUrl}/Items/{item.Id}/Images/{JellyfinConstants.BackdropName}?fillHeight=266&fillWidth=472&quality=96&tag={item.BackdropImageTags[0]}";
            }

            return $"{settings.BaseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=266&fillWidth=472&quality=96&tag={item.ImageTags[JellyfinConstants.PrimaryName]}";
        }
    }
}
