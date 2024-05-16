using CommunityToolkit.Mvvm.Collections;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.MainPage
{
    internal sealed class HomeViewModel : IHomeViewModel
    {
        private readonly IMemoryCache memoryCache;
        private readonly JellyfinApiClient apiClient;

        public HomeViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
        }

        public async Task<ObservableGroupedCollection<MediaGroupItem, UIMediaListItem>> LoadLatestAsync(ObservableCollection<UIMediaListItem> mediaList, CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var mediaListGrouped = new ObservableGroupedCollection<MediaGroupItem, UIMediaListItem>();

            foreach (var record in mediaList.Where(x => x.CollectionType != BaseItemDto_CollectionType.Boxsets))
            {
                var itemsResult = await apiClient.Items.Latest
                    .GetAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                        options.QueryParameters.Limit = 16;
                        options.QueryParameters.Fields = new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.Path, };
                        options.QueryParameters.ImageTypeLimit = 1;
                        options.QueryParameters.EnableImageTypes = new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, };
                        options.QueryParameters.ParentId = record.Id;
                    }, cancellationToken: cancellationToken);
                var itemType = new MediaGroupItem { Id = record.Id, Name = record.Name, Type = record.Type, CollectionType = record.CollectionType.ToString(), };

                if (record.CollectionType == BaseItemDto_CollectionType.Tvshows)
                {
                    mediaListGrouped.Add(new ObservableGroup<MediaGroupItem, UIMediaListItem>(
                    itemType,
                    itemsResult
                    .Select(x =>
                    {
                        var item = new UIMediaListItemSeries
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = MediaHelpers.SetImageUrl(x, "250", "300", JellyfinConstants.PrimaryName),
                            CollectionType = record.CollectionType,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite.Value,
                                HasBeenWatched = x.UserData.Played.Value,
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
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = MediaHelpers.SetImageUrl(x, "250", "300", JellyfinConstants.PrimaryName),
                            CollectionType = x.CollectionType.Value,
                            Type = x.Type.Value,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite.Value,
                                HasBeenWatched = x.UserData.Played.Value,
                                UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0,
                            },
                        })));
                }
            }

            return mediaListGrouped;
        }

        public async Task<ObservableCollection<UIMediaListItem>> LoadMediaListAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var viewsItemsResult = await apiClient.UserViews
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                }, cancellationToken: cancellationToken);

            var mediaList = new ObservableCollection<UIMediaListItem>(
                viewsItemsResult
                    .Items
                    .Select(x =>
                    new UIMediaListItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = MediaHelpers.SetImageUrl(x, "250", "300", JellyfinConstants.PrimaryName),
                        IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                        CollectionType = x.CollectionType.Value,
                        Type = x.Type.Value,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite.Value,
                            HasBeenWatched = x.UserData.Played.Value,
                            UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0,
                        },
                    }));

            return mediaList;
        }

        public async Task<ObservableCollection<UIMediaListItemSeries>> LoadNextUpAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var itemsResult = await apiClient.Shows.NextUp
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.StartIndex = 0;
                    options.QueryParameters.Limit = 10;
                }, cancellationToken: cancellationToken);

            var nextupMediaList = new ObservableCollection<UIMediaListItemSeries>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItemSeries
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = MediaHelpers.SetImageUrl(x, "250", "300", JellyfinConstants.PrimaryName),
                            Type = x.Type.Value,
                            SeriesName = x.SeriesName,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite.Value,
                                HasBeenWatched = x.UserData.Played.Value,
                                UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0,
                            },
                        }));

            return nextupMediaList;
        }

        public async Task<(ObservableCollection<UIMediaListItem>, bool)> LoadResumeItemsAsync(CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var itemsResult = await apiClient.UserItems.Resume
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.EnableTotalRecordCount = false;
                }, cancellationToken: cancellationToken);

            var resumeMediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = GetResumeImage(x),
                            Type = x.Type.Value,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite.Value,
                                HasBeenWatched = x.UserData.Played.Value,
                                UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0,
                            },
                        }));

            var hasResumeMedia = resumeMediaList.Count > 0;

            return (resumeMediaList, hasResumeMedia);
        }

        private string GetResumeImage(BaseItemDto item)
        {
            var baseUrl = memoryCache.Get<string>(JellyfinConstants.HostUrlName);

            if (item.ParentThumbItemId != null)
            {
                return $"{baseUrl}/Items/{item.ParentThumbItemId}/Images/{JellyfinConstants.ThumbName}?fillHeight=266&fillWidth=472&quality=96&tag={item.ParentThumbImageTag}";
            }

            if (item.ParentBackdropItemId != null)
            {
                return $"{baseUrl}/Items/{item.ParentBackdropItemId}/Images/{JellyfinConstants.BackdropName}?fillHeight=266&fillWidth=472&quality=96&tag={item.ParentBackdropImageTags[0]}";
            }

            if (item.BackdropImageTags.Count > 0)
            {
                return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.BackdropName}?fillHeight=266&fillWidth=472&quality=96&tag={item.BackdropImageTags[0]}";
            }

            return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=266&fillWidth=472&quality=96&tag={item.ImageTags.AdditionalData[JellyfinConstants.PrimaryName]}";
        }
    }
}
