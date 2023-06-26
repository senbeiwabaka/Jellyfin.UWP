using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Models.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP
{
    public partial class MediaListViewModel : ObservableObject
    {
        private const int Limit = 100;

        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMemoryCache memoryCache;
        private readonly SdkClientSettings sdkClientSettings;

        [ObservableProperty]
        private string countInformation;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadPreviousCommand))]
        private int currentIndex = 0;

        [ObservableProperty]
        private ObservableCollection<FiltersModel> filteringFilters;

        [ObservableProperty]
        private ObservableCollection<GenreFiltersModel> genresFilterList;

        private BaseItemKind itemType;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> mediaList;

        private Guid parentId;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadNextCommand))]
        private int totalRecords = 0;

        public MediaListViewModel(IHttpClientFactory httpClientFactory, SdkClientSettings sdkClientSettings, IMemoryCache memoryCache)
        {
            this.httpClientFactory = httpClientFactory;
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
        }

        public async Task InitialLoadAsync(Guid id)
        {
            parentId = id;

            var user = memoryCache.Get<UserDto>("user");
            var httpClient = httpClientFactory.CreateClient();
            var itemsClient = new ItemsClient(sdkClientSettings, httpClient);
            var parentItem = await itemsClient.GetItemsAsync(
                userId: user.Id,
                startIndex: 0,
                limit: 1,
                sortBy: new[] { "SortName", },
                sortOrder: new[] { SortOrder.Ascending, },
                ids: new[] { parentId, });

            var firstItem = parentItem.Items.First();
            if (firstItem.CollectionType == "movies")
            {
                itemType = BaseItemKind.Movie;
            }

            if (firstItem.CollectionType == "tvshows")
            {
                itemType = BaseItemKind.Series;
            }

            await LoadMediaAsync();
        }

        public async Task LoadFiltersAsync()
        {
            if (GenresFilterList is not null && GenresFilterList.Count > 0)
            {
                return;
            }

            var user = memoryCache.Get<UserDto>("user");
            var httpClient = httpClientFactory.CreateClient();
            var filterClient = new FilterClient(sdkClientSettings, httpClient);

            var filtersResult = await filterClient.GetQueryFiltersAsync(
                userId: user.Id,
                parentId: parentId,
                includeItemTypes: new[] { itemType });

            GenresFilterList = new ObservableCollection<GenreFiltersModel>(
                filtersResult.Genres.Select(x => new GenreFiltersModel { Id = x.Id, Name = x.Name }));

            FilteringFilters = new ObservableCollection<FiltersModel>
            {
                new FiltersModel { DisplayName = "Played", Filter = ItemFilter.IsPlayed },
                new FiltersModel { DisplayName = "UnPlayed",Filter = ItemFilter.IsUnplayed },
                new FiltersModel { DisplayName = "Resumable", Filter = ItemFilter.IsResumable },
                new FiltersModel { DisplayName = "Favorites", Filter = ItemFilter.IsFavorite },
                new FiltersModel { DisplayName = "Likes", Filter = ItemFilter.Likes },
                new FiltersModel { DisplayName = "Dislikes", Filter = ItemFilter.Dislikes },
            };
        }

        public async Task LoadMediaAsync(
            IEnumerable<Guid> genreIds = null,
            IEnumerable<ItemFilter> itemFilters = null,
            CancellationToken cancellationToken = default)
        {
            var user = memoryCache.Get<UserDto>("user");
            var httpClient = httpClientFactory.CreateClient();
            var itemsClient = new ItemsClient(sdkClientSettings, httpClient);
            var itemsResult = await itemsClient.GetItemsAsync(
                userId: user.Id,
                parentId: parentId,
                startIndex: CurrentIndex,
                limit: Limit,
                sortBy: new[] { "SortName", },
                sortOrder: new[] { SortOrder.Ascending, },
                genreIds: genreIds,
                filters: itemFilters,
                includeItemTypes: new[] { itemType },
                cancellationToken: cancellationToken);

            TotalRecords = itemsResult.TotalRecordCount;

            if (CurrentIndex == 0)
            {
                CountInformation = $"1-{Limit} of {itemsResult.TotalRecordCount}";
            }
            else
            {
                CountInformation = $"{CurrentIndex + 1}-{CurrentIndex + Limit} of {itemsResult.TotalRecordCount}";
            }

            MediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = x.ImageTags.Any(x => x.Key == "Primary") ? $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=384&fillWidth=256&quality=96&tag={x.ImageTags["Primary"]}" : "https://cdn.onlinewebfonts.com/svg/img_331373.png",
                        }));
        }

        [RelayCommand(CanExecute = nameof(CanLoadNext))]
        public async Task LoadNextAsync(CancellationToken cancellationToken)
        {
            if (CurrentIndex == 0)
            {
                CurrentIndex += Limit + 1;
            }
            else
            {
                CurrentIndex += Limit;
            }

            await LoadMediaAsync(
                GenresFilterList?.Where(x => x.IsSelected).Select(x => x.Id),
                FilteringFilters?.Where(x => x.IsSelected).Select(x => x.Filter),
                cancellationToken);
        }

        [RelayCommand(CanExecute = nameof(CanLoadPrevious))]
        public async Task LoadPreviousAsync(CancellationToken cancellationToken)
        {
            if (CurrentIndex <= 0)
            {
                CurrentIndex += 1;
            }
            else
            {
                CurrentIndex -= Limit + 1;
            }

            await LoadMediaAsync(
                GenresFilterList?.Where(x => x.IsSelected).Select(x => x.Id),
                FilteringFilters?.Where(x => x.IsSelected).Select(x => x.Filter),
                cancellationToken);
        }

        public void FilterReset()
        {
            CurrentIndex = 0;
        }

        private bool CanLoadNext()
        {
            return (CurrentIndex + Limit) < TotalRecords;
        }

        private bool CanLoadPrevious()
        {
            return (CurrentIndex - Limit) > 0;
        }
    }
}