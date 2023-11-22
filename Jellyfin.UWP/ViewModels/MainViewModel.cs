using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SdkClientSettings sdkClientSettings;
        private readonly IMemoryCache memoryCache;
        private readonly IItemsClient itemsClient;
        private readonly ITvShowsClient tvShowsClient;
        private readonly IUserLibraryClient userLibraryClient;
        private readonly IUserViewsClient userViewsClient;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> mediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> resumeMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItemEpisode> nextupMediaList;

        [ObservableProperty]
        private bool hasResumeMedia;

        [ObservableProperty]
        private ObservableGroupedCollection<string, UIMediaListItem> mediaListGrouped;

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
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags["Primary"]}",
                            IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                            CollectionType = x.CollectionType,
                            Type = x.Type,
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
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags["Primary"]}",
                            Type = x.Type,
                        }));

            HasResumeMedia = ResumeMediaList.Count > 0;
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
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags["Primary"]}",
                            Type = x.Type,
                            SeriesName = x.SeriesName,
                        }));
        }

        public async Task LoadLatestAsync()
        {
            var user = memoryCache.Get<UserDto>("user");

            MediaListGrouped = new ObservableGroupedCollection<string, UIMediaListItem>();

            foreach (var item in MediaList.Where(x => !string.Equals(CollectionTypeOptions.BoxSets.ToString(), x.CollectionType, System.StringComparison.CurrentCultureIgnoreCase)))
            {
                var itemsResult = await userLibraryClient.GetLatestMediaAsync(
                    userId: user.Id,
                    limit: 16,
                    fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, ItemFields.Path, },
                    imageTypeLimit: 1,
                    enableImageTypes: new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, },
                    parentId: item.Id);

                MediaListGrouped.Add(new ObservableGroup<string, UIMediaListItem>(
                    item.Name,
                    itemsResult
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags["Primary"]}",
                        })));
            }
        }
    }
}
