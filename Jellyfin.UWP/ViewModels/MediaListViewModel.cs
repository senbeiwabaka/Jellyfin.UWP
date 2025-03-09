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

namespace Jellyfin.UWP.ViewModels;

internal partial class MediaListViewModel : ObservableObject
{
    private const int Limit = 100;

    private readonly JellyfinApiClient apiClient;
    private readonly IMediaHelpers mediaHelpers;
    private readonly UserDto user;

    private BaseItemKind itemType;

    private Guid? parentId;

    private BaseItemDto parentItem;

    public MediaListViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
    {
        this.apiClient = apiClient;
        this.mediaHelpers = mediaHelpers;
        user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
    }

    [ObservableProperty]
    public partial string CountInformation { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadPreviousCommand))]
    public partial int CurrentIndex { get; set; } = 0;

    [ObservableProperty]
    public partial ObservableCollection<FiltersModel> FilteringFilters { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SortModel> SortingList { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<GenreFiltersModel> GenresFilterList { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> MediaList { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadNextCommand))]
    public partial int TotalRecords { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsSortingOpen { get; set; }

    [ObservableProperty]
    public partial bool IsFilteringOpen { get; set; }

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
            Url = mediaHelpers.SetImageUrl(item, "384", "210", JellyfinConstants.PrimaryName),
            Type = item.Type.Value,
            CollectionType = item.CollectionType,
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

    public async Task InitialLoadAsync(Guid id, CancellationToken cancellationToken = default)
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
                options.QueryParameters.SortBy = [ItemSortBy.SortName,];
                options.QueryParameters.SortOrder = [SortOrder.Ascending,];
                options.QueryParameters.Ids = [parentId,];
            }, cancellationToken);

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

        await LoadMediaAsync([], null, cancellationToken);
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

    public async Task LoadFiltersAsync(CancellationToken cancellationToken = default)
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
                options.QueryParameters.IncludeItemTypes = [itemType];
            }, cancellationToken);

        GenresFilterList = [.. filtersResult.Genres.Select(x => new GenreFiltersModel { Id = x.Id.Value, Name = x.Name })];

        FilteringFilters =
        [
            new() { DisplayName = "Played", Filter = ItemFilter.IsPlayed },
            new() { DisplayName = "UnPlayed",Filter = ItemFilter.IsUnplayed },
            new() { DisplayName = "Resumable", Filter = ItemFilter.IsResumable },
            new() { DisplayName = "Favorites", Filter = ItemFilter.IsFavorite },
            new() { DisplayName = "Likes", Filter = ItemFilter.Likes },
            new() { DisplayName = "Dislikes", Filter = ItemFilter.Dislikes },
        ];
    }

    public async Task LoadMediaAsync(
        Guid?[] genreIds,
        ItemFilter[]? itemFilters = null,
        CancellationToken cancellationToken = default)
    {
        var itemsResult = await apiClient.Items
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.ParentId = parentId;
                options.QueryParameters.StartIndex = CurrentIndex;
                options.QueryParameters.Limit = Limit;
                options.QueryParameters.SortBy = [ItemSortBy.SortName,];
                options.QueryParameters.SortOrder = [SortOrder.Ascending,];
                options.QueryParameters.GenreIds = genreIds;
                options.QueryParameters.Filters = itemFilters;
                options.QueryParameters.IncludeItemTypes = [itemType];
                options.QueryParameters.Fields = [ItemFields.PrimaryImageAspectRatio,];
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

        MediaList = [.. itemsResult.Items
                    .Select(x =>
                    {
                        var item = new UIMediaListItem
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = mediaHelpers.SetImageUrl(x, "384", "210", JellyfinConstants.PrimaryName),
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
                    })];
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false, CanExecute = nameof(CanLoadNext))]
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

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false, CanExecute = nameof(CanLoadPrevious))]
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

    [RelayCommand]
    private void LoadSort()
    {
        SortingList =
        [
            new(){ Name = "Name", Sort = ItemSortBy.Name, },
            new(){ Name = "Date Added", Sort = ItemSortBy.DateCreated, },
        ];

        IsSortingOpen = true;
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task LoadFiltering(CancellationToken cancellationToken)
    {
        await LoadFiltersAsync(cancellationToken);

        IsFilteringOpen = true;
    }
}
