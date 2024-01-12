using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels
{
    internal sealed partial class SearchViewModel : ObservableObject
    {
        private const int Limit = 24;
        private readonly IItemsClient itemsClient;
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

        public SearchViewModel(IItemsClient itemsClient, SdkClientSettings sdkClientSettings, IMemoryCache memoryCache)
        {
            this.itemsClient = itemsClient;
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
        }

        public async Task LoadSearchAsync(string query)
        {
            var user = memoryCache.Get<UserDto>("user");
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
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=330&fillWidth=220&quality=96&tag={x.ImageTags["Primary"]}",
                            IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                            CollectionType = x.CollectionType,
                            Type = x.Type,
                        }));

            HasMoviesResult = MovieMediaList.Count > 0;

            var seriesItemsResult = await itemsClient.GetItemsByUserIdAsync(
                user.Id,
                searchTerm: query,
                limit: Limit,
                recursive: true,
                enableTotalRecordCount: false,
                imageTypeLimit: 1,
                includeItemTypes: new[] { BaseItemKind.Series });

            SeriesMediaList = new ObservableCollection<UIMediaListItem>(
                seriesItemsResult
                    .Items
                    .Select(x =>
                    {
                        var item = new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=330&fillWidth=220&quality=96&tag={x.ImageTags["Primary"]}",
                            IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                            CollectionType = x.CollectionType,
                            Type = x.Type,
                        };

                        item.UserData.IsFavorite = x.UserData.IsFavorite;
                        item.UserData.HasBeenWatched = x.UserData.Played;
                        item.UserData.UnplayedItemCount = x.UserData.UnplayedItemCount;

                        return item;
                    }));

            HasSeriesResult = SeriesMediaList.Count > 0;

            var episodesItemsResult = await itemsClient.GetItemsByUserIdAsync(
                user.Id,
                searchTerm: query,
                limit: Limit,
                recursive: true,
                enableTotalRecordCount: false,
                imageTypeLimit: 1,
                includeItemTypes: new[] { BaseItemKind.Episode });

            EpisodesMediaList = new ObservableCollection<UIMediaListItem>(
                episodesItemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=215&fillWidth=380&quality=96&tag={x.ImageTags["Primary"]}",
                            IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                            CollectionType = x.CollectionType,
                            Type = x.Type,
                        }));

            HasEpisodesResult = EpisodesMediaList.Count > 0;
        }
    }
}
