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
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels
{
    internal partial class MediaListViewModel : ObservableObject
    {
        private const int Limit = 100;

        private readonly IFilterClient filterClient;
        private readonly IItemsClient itemsClient;
        private readonly IMemoryCache memoryCache;
        private readonly IPlaystateClient playstateClient;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly IUserLibraryClient userLibraryClient;

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

        private Guid? parentId;

        private BaseItemDto parentItem;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadNextCommand))]
        private int totalRecords = 0;

        public MediaListViewModel(
            IItemsClient itemsClient,
            IFilterClient filterClient,
            SdkClientSettings sdkClientSettings,
            IMemoryCache memoryCache,
            IPlaystateClient playstateClient,
            IUserLibraryClient userLibraryClient)
        {
            this.itemsClient = itemsClient;
            this.filterClient = filterClient;
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
            this.playstateClient = playstateClient;
            this.userLibraryClient = userLibraryClient;
        }

        public void FilterReset()
        {
            CurrentIndex = 0;
        }

        public async Task<UIMediaListItem> GetLatestOnItemAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>("user");
            var item = await userLibraryClient.GetItemAsync(user.Id, id);

            return new UIMediaListItem
            {
                Id = item.Id,
                Name = item.Name,
                Url = item.ImageTags.Any(x => x.Key == "Primary") ? $"{sdkClientSettings.BaseUrl}/Items/{item.Id}/Images/Primary?fillHeight=384&fillWidth=210&quality=96&tag={item.ImageTags["Primary"]}" : "https://cdn.onlinewebfonts.com/svg/img_331373.png",
                Type = item.Type,
                CollectionType = item.CollectionType,
                UserData = new UIUserData
                {
                    IsFavorite = item.UserData.IsFavorite,
                    UnplayedItemCount = item.UserData.UnplayedItemCount,
                    HasBeenWatched = item.UserData.Played,
                },
            };
        }

        public BaseItemKind GetMediaType()
        {
            return itemType;
        }

        public string GetTitle()
        {
            return parentItem?.Name ?? "No Title";
        }

        public async Task InitialLoadAsync(Guid id)
        {
            if (parentId is not null && parentItem is not null)
            {
                return;
            }

            parentId = id;

            var user = memoryCache.Get<UserDto>("user");
            var items = await itemsClient.GetItemsAsync(
                userId: user.Id,
                startIndex: 0,
                limit: 1,
                sortBy: new[] { "SortName", },
                sortOrder: new[] { SortOrder.Ascending, },
                ids: new[] { parentId.Value, });

            itemType = BaseItemKind.BoxSet;

            parentItem = items.Items[0];

            if (parentItem.CollectionType == "movies")
            {
                itemType = BaseItemKind.Movie;
            }

            if (parentItem.CollectionType == "tvshows")
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
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, },
                cancellationToken: cancellationToken);

            TotalRecords = itemsResult.TotalRecordCount;

            if (CurrentIndex == 0)
            {
                CountInformation = $"1-{Limit} of {itemsResult.TotalRecordCount}";
            }
            else
            {
                CountInformation = $"{CurrentIndex}-{(CurrentIndex - 1) + Limit} of {itemsResult.TotalRecordCount}";
            }

            MediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x =>
                    {
                        var item = new UIMediaListItem
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Url = x.ImageTags.Any(x => x.Key == "Primary") ? $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=384&fillWidth=210&quality=96&tag={x.ImageTags["Primary"]}" : "https://cdn.onlinewebfonts.com/svg/img_331373.png",
                            Type = x.Type,
                            CollectionType = x.CollectionType,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite,
                                UnplayedItemCount = x.UserData.UnplayedItemCount,
                                HasBeenWatched = x.UserData.Played,
                            },
                        };

                        return item;
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
            if (CurrentIndex < 0)
            {
                CurrentIndex = 0;
            }
            else
            {
                CurrentIndex -= Limit;
            }

            if (CurrentIndex == 1)
            {
                CurrentIndex = 0;
            }

            await LoadMediaAsync(
                GenresFilterList?.Where(x => x.IsSelected).Select(x => x.Id),
                FilteringFilters?.Where(x => x.IsSelected).Select(x => x.Filter),
                cancellationToken);
        }

        public async Task PlayedStateAsync(bool hasBeenViewed, Guid id)
        {
            var user = memoryCache.Get<UserDto>("user");

            if (hasBeenViewed)
            {
                _ = await playstateClient.MarkUnplayedItemAsync(
                    user.Id,
                    id);
            }
            else
            {
                _ = await playstateClient.MarkPlayedItemAsync(
                    user.Id,
                    id,
                    DateTimeOffset.Now);
            }
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
