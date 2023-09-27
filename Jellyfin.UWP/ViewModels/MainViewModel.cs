using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SdkClientSettings sdkClientSettings;
        private readonly IMemoryCache memoryCache;
        private readonly IItemsClient itemsClient;
        private readonly ITvShowsClient tvShowsClient;
        private readonly IUserLibraryClient userLibraryClient;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> mediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> resumeMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> nextupMediaList;

        [ObservableProperty]
        private bool hasResumeMedia;

        [ObservableProperty]
        private ObservableGroupedCollection<string, UIMediaListItem> mediaListGrouped;

        public MainViewModel(
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

        public async Task LoadMediaListAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var parentItemsResult = await itemsClient.GetItemsAsync(userId: user.Id);

            MediaList = new ObservableCollection<UIMediaListItem>(
                parentItemsResult
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
                        }));

            HasResumeMedia = ResumeMediaList.Count > 0;
        }

        public async Task LoadNextUpAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await tvShowsClient.GetNextUpAsync(userId: user.Id, startIndex: 0, limit: 10);

            NextupMediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=250&fillWidth=300&quality=96&tag={x.ImageTags["Primary"]}",
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
