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
    internal sealed partial class SearchViewModel : ObservableObject
    {
        private const int Limit = 24;

        private readonly IHttpClientFactory httpClientFactory;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly IMemoryCache memoryCache;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> movieMediaList;

        [ObservableProperty]
        private bool hasMoviesResult;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> seriesMediaList;

        [ObservableProperty]
        private bool hasSeriesResult;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> episodesMediaList;

        [ObservableProperty]
        private bool hasEpisodesResult;

        public SearchViewModel(IHttpClientFactory httpClientFactory, SdkClientSettings sdkClientSettings, IMemoryCache memoryCache)
        {
            this.httpClientFactory = httpClientFactory;
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
        }

        public async Task LoadSearchAsync(string query)
        {
            var user = memoryCache.Get<UserDto>("user");
            var httpClient = httpClientFactory.CreateClient();
            var itemsClient = new ItemsClient(sdkClientSettings, httpClient);

            var movieItemsResult = await itemsClient.GetItemsByUserIdAsync(
                user.Id,
                searchTerm: query,
                limit: Limit,
                recursive: true,
                enableTotalRecordCount: false,
                imageTypeLimit: 1,
                isMovie: true,
                includeItemTypes: new[] { BaseItemKind.Movie });

            MovieMediaList = new ObservableCollection<UIMediaListItem>(
                movieItemsResult
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

            HasMoviesResult = MovieMediaList.Count > 0;

            var seriesItemsResult = await itemsClient.GetItemsByUserIdAsync(
                user.Id,
                searchTerm: query,
                limit: Limit,
                recursive: true,
                enableTotalRecordCount: false,
                imageTypeLimit: 1,
                //isMovie: false,
                //isSeries: true,
                includeItemTypes: new[] { BaseItemKind.Series });

            SeriesMediaList = new ObservableCollection<UIMediaListItem>(
                seriesItemsResult
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

            HasSeriesResult = SeriesMediaList.Count > 0;

            var episodesItemsResult = await itemsClient.GetItemsByUserIdAsync(
                user.Id,
                searchTerm: query,
                limit: Limit,
                recursive: true,
                enableTotalRecordCount: false,
                imageTypeLimit: 1,
                //isMovie: false,
                //isSeries: false,
                includeItemTypes: new[] { BaseItemKind.Episode });

            EpisodesMediaList = new ObservableCollection<UIMediaListItem>(
                episodesItemsResult
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

            HasEpisodesResult = EpisodesMediaList.Count > 0;
        }
    }
}