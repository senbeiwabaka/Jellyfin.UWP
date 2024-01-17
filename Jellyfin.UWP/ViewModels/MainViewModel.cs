using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels
{
    internal sealed partial class MainViewModel : ObservableObject
    {
        private readonly IItemsClient itemsClient;
        private readonly IMemoryCache memoryCache;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly ITvShowsClient tvShowsClient;
        private readonly IUserLibraryClient userLibraryClient;
        private readonly IUserViewsClient userViewsClient;

        [ObservableProperty]
        private bool hasResumeMedia;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> mediaList;

        [ObservableProperty]
        private ObservableGroupedCollection<MediaGroupItem, UIMediaListItem> mediaListGrouped;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItemEpisode> nextupMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> resumeMediaList;

        [ObservableProperty]
        private string userName;

        public MainViewModel(
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

        public void LoadInitial()
        {
            var user = memoryCache.Get<UserDto>("user");

            UserName = $"User: {user.Name}";
        }

        public async Task LoadLatestAsync()
        {
            var user = memoryCache.Get<UserDto>("user");

            MediaListGrouped = new ObservableGroupedCollection<MediaGroupItem, UIMediaListItem>();

            foreach (var record in MediaList.Where(x => !string.Equals(CollectionTypeOptions.BoxSets.ToString(), x.CollectionType, System.StringComparison.CurrentCultureIgnoreCase)))
            {
                var itemsResult = await userLibraryClient.GetLatestMediaAsync(
                    userId: user.Id,
                    limit: 16,
                    fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, ItemFields.Path, },
                    imageTypeLimit: 1,
                    enableImageTypes: new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, },
                    parentId: record.Id);
                var itemType = new MediaGroupItem { Id = record.Id, Name = record.Name, Type = record.Type, CollectionType = record.CollectionType, };

                if (string.Equals(CollectionTypeOptions.TvShows.ToString(), record.CollectionType, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    MediaListGrouped.Add(new ObservableGroup<MediaGroupItem, UIMediaListItem>(
                    itemType,
                    itemsResult
                    .Select(x =>
                    {
                        var item = new UIMediaListItemEpisode
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                            CollectionType = record.CollectionType,
                        };

                        item.UserData.IsFavorite = x.UserData.IsFavorite;
                        item.UserData.HasBeenWatched = x.UserData.Played;
                        item.UserData.UnplayedItemCount = x.ChildCount.HasValue ? x.ChildCount.Value : 0;

                        return item;
                    })));
                }
                else
                {
                    MediaListGrouped.Add(new ObservableGroup<MediaGroupItem, UIMediaListItem>(
                        itemType,
                        itemsResult
                        .Select(x =>
                            new UIMediaListItem
                            {
                                Id = x.Id,
                                Name = x.Name,
                                Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                                CollectionType = x.CollectionType,
                                Type = x.Type
                            })));
                }
            }
        }

        public async Task LoadMediaListAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var viewsItemsResult = await userViewsClient.GetUserViewsAsync(userId: user.Id);

            MediaList = new ObservableCollection<UIMediaListItem>(
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
                        }));
        }

        public async Task LoadNextUpAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await tvShowsClient.GetNextUpAsync(userId: user.Id, startIndex: 0, limit: 10);

            NextupMediaList = new ObservableCollection<UIMediaListItemEpisode>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItemEpisode
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                            Type = x.Type,
                            SeriesName = x.SeriesName,
                        }));
        }

        public async Task LoadResumeItemsAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await itemsClient.GetResumeItemsAsync(
                userId: user.Id,
                enableTotalRecordCount: false);

            ResumeMediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = GetResumeImage(sdkClientSettings, x),
                            Type = x.Type,
                        }));

            HasResumeMedia = ResumeMediaList.Count > 0;
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
