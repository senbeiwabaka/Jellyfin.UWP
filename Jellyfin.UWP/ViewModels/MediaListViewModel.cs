using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.Models.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels
{
    internal partial class MediaListViewModel : ObservableObject
    {
        private const int Limit = 100;

        private readonly JellyfinApiClient apiClient;
        private readonly UserDto user;

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

        public MediaListViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient)
        {
            this.apiClient = apiClient;
            this.user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
        }

        public void FilterReset()
        {
            CurrentIndex = 0;
        }

        public async Task<UIMediaListItem> GetLatestOnItemAsync(Guid id)
        {
            var item = await apiClient.Items[id]
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                });

            return new UIMediaListItem
            {
                Id = item.Id.Value,
                Name = item.Name,
                Url = MediaHelpers.SetImageUrl(item, "384", "210", JellyfinConstants.PrimaryName),
                Type = item.Type.Value,
                CollectionType = item.CollectionType.Value,
                UserData = new UIUserData
                {
                    IsFavorite = item.UserData.IsFavorite.Value,
                    UnplayedItemCount = item.UserData.UnplayedItemCount,
                    HasBeenWatched = item.UserData.Played.Value,
                },
            };
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

            var items = await apiClient.Items
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.StartIndex = 0;
                    options.QueryParameters.Limit = 1;
                    options.QueryParameters.SortBy = new[] { ItemSortBy.SortName, };
                    options.QueryParameters.SortOrder = new[] { SortOrder.Ascending, };
                    options.QueryParameters.Ids = new[] { parentId, };
                });

            itemType = BaseItemKind.BoxSet;

            parentItem = items.Items[0];

            if (parentItem.CollectionType == BaseItemDto_CollectionType.Movies)
            {
                itemType = BaseItemKind.Movie;
            }

            if (parentItem.CollectionType == BaseItemDto_CollectionType.Tvshows)
            {
                itemType = BaseItemKind.Series;
            }

            await LoadMediaAsync();
        }

        public async Task IsFavoriteStateAsync(bool isFavorite, Guid id)
        {
            if (isFavorite)
            {
                _ = await apiClient.UserFavoriteItems[id]
                    .DeleteAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
            }
            else
            {
                _ = await apiClient.UserFavoriteItems[id]
                    .PostAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
            }
        }

        public async Task LoadFiltersAsync()
        {
            if (GenresFilterList is not null && GenresFilterList.Count > 0)
            {
                return;
            }

            var filtersResult = await apiClient.Items.Filters2
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.ParentId = parentId;
                    options.QueryParameters.IncludeItemTypes = new[] { itemType };
                });

            GenresFilterList = new ObservableCollection<GenreFiltersModel>(
                filtersResult.Genres.Select(x => new GenreFiltersModel { Id = x.Id.Value, Name = x.Name }));

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
            Guid?[] genreIds = null,
            ItemFilter[] itemFilters = null,
            CancellationToken cancellationToken = default)
        {
            var itemsResult = await apiClient.Items
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.ParentId = parentId;
                    options.QueryParameters.StartIndex = CurrentIndex;
                    options.QueryParameters.Limit = Limit;
                    options.QueryParameters.SortBy = new[] { ItemSortBy.SortName, };
                    options.QueryParameters.SortOrder = new[] { SortOrder.Ascending, };
                    options.QueryParameters.GenreIds = genreIds;
                    options.QueryParameters.Filters = itemFilters;
                    options.QueryParameters.IncludeItemTypes = new[] { itemType };
                    options.QueryParameters.Fields = new[] { ItemFields.PrimaryImageAspectRatio, };
                },
                cancellationToken: cancellationToken);

            TotalRecords = itemsResult.TotalRecordCount.Value;

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
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = MediaHelpers.SetImageUrl(x, "384", "210", JellyfinConstants.PrimaryName),
                            Type = x.Type.Value,
                            CollectionType = x.CollectionType,
                            UserData = new UIUserData
                            {
                                IsFavorite = x.UserData.IsFavorite.Value,
                                UnplayedItemCount = x.UserData.UnplayedItemCount,
                                HasBeenWatched = x.UserData.Played.Value,
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
                GenresFilterList?.Where(x => x.IsSelected).Select(x => x.Id).Cast<Guid?>().ToArray(),
                FilteringFilters?.Where(x => x.IsSelected).Select(x => x.Filter).ToArray(),
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
                GenresFilterList?.Where(x => x.IsSelected).Select(x => x.Id).Cast<Guid?>().ToArray(),
                FilteringFilters?.Where(x => x.IsSelected).Select(x => x.Filter).ToArray(),
                cancellationToken);
        }

        public async Task PlayedStateAsync(bool hasBeenViewed, Guid id)
        {
            if (hasBeenViewed)
            {
                _ = await apiClient.UserPlayedItems[id]
                    .DeleteAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
            }
            else
            {
                _ = await apiClient.UserPlayedItems[id]
                    .PostAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                        options.QueryParameters.DatePlayed = DateTimeOffset.Now;
                    });
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
