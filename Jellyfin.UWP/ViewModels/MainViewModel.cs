using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jellyfin.UWP
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly IMemoryCache memoryCache;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> mediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> resumeMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> nextupMediaList;

        [ObservableProperty]
        private bool hasResumeMedia;

        public MainViewModel(IHttpClientFactory httpClientFactory, SdkClientSettings sdkClientSettings, IMemoryCache memoryCache)
        {
            this.httpClientFactory = httpClientFactory;
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
        }

        public async Task LoadMediaListAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var httpClient = httpClientFactory.CreateClient();
            var client = new ItemsClient(sdkClientSettings, httpClient);
            var parentItemsResult = await client.GetItemsAsync(userId: user.Id);

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
                        }));
        }

        public async Task LoadResumeItemsAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var httpClient = httpClientFactory.CreateClient();
            var client = new ItemsClient(sdkClientSettings, httpClient);
            var itemsResult = await client.GetResumeItemsAsync(userId: user.Id);

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
            var httpClient = httpClientFactory.CreateClient();
            var client = new TvShowsClient(sdkClientSettings, httpClient);
            var itemsResult = await client.GetNextUpAsync(userId: user.Id, startIndex: 0, limit: 10);

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
    }
}