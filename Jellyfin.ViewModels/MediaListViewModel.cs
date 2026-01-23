using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Models;
using Jellyfin.Models.Filters;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ViewModels;

public sealed partial class MediaListViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : ObservableObject
{
    private const int Limit = 100;
    private readonly UserDto user = memoryCache.Get<UserDto>(JellyfinConstants.UserName)!;

    private BaseItemKind itemType;

    private Guid? parentId;

    private BaseItemDto? parentItem;

    [ObservableProperty]
    public partial string CountInformation { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadPreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadNextCommand))]
    public partial int CurrentIndex { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<FiltersModel> FilteringFilters { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<GenreFiltersModel> GenresFilterList { get; set; }

    [ObservableProperty]
    public partial bool IsFilteringOpen { get; set; }

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial bool IsSortingOpen { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> MediaList { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SortModel> SortingList { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SortOrderModel> SortOrderList { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadNextCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadPreviousCommand))]
    public partial int TotalRecords { get; set; }

    public void FilterReset()
    {
        CurrentIndex = 0;
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

        SortingList =
        [
            new(){ Name = "Sort Name", Sort = ItemSortBy.SortName, IsSelected = true, },
            new(){ Name = "Name", Sort = ItemSortBy.Name, },
            new(){ Name = "Date Added", Sort = ItemSortBy.DateCreated, },
        ];

        FilteringFilters =
        [
            new() { Name = "Played", Filter = ItemFilter.IsPlayed },
            new() { Name = "UnPlayed",Filter = ItemFilter.IsUnplayed },
            new() { Name = "Resumable", Filter = ItemFilter.IsResumable },
            new() { Name = "Favorites", Filter = ItemFilter.IsFavorite },
            new() { Name = "Likes", Filter = ItemFilter.Likes },
            new() { Name = "Dislikes", Filter = ItemFilter.Dislikes },
        ];

        SortOrderList = [
            new() { Label = "Ascending", Sort = SortOrder.Ascending, IsSelected = true, },
            new() { Label = "Descending", Sort = SortOrder.Descending, },
            ];

        var filtersResult = await apiClient.Items.Filters2
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.ParentId = parentId;
                options.QueryParameters.IncludeItemTypes = [itemType];
            }, cancellationToken);

        GenresFilterList = [.. filtersResult.Genres.Select(x => new GenreFiltersModel { Id = x.Id.Value, Name = x.Name })];

        await LoadMediaAsync(cancellationToken: cancellationToken);
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
            [.. GenresFilterList.Where(x => x.IsSelected).Select(x => x.Id)],
            [.. FilteringFilters.Where(x => x.IsSelected).Select(x => x.Filter)],
            SortingList.SingleOrDefault(x => x.IsSelected)?.Sort ?? ItemSortBy.SortName,
            SortOrderList.SingleOrDefault(x => x.IsSelected)?.Sort ?? SortOrder.Ascending,
            cancellationToken);
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false, CanExecute = nameof(CanLoadPrevious))]
    public async Task LoadPreviousAsync(CancellationToken cancellationToken)
    {
        CurrentIndex -= Limit;

        if (CurrentIndex == 1)
        {
            CurrentIndex = 0;
        }

        if (CurrentIndex < 0)
        {
            CurrentIndex = 0;
        }

        await LoadMediaAsync(
            [.. GenresFilterList.Where(x => x.IsSelected).Select(x => x.Id)],
            [.. FilteringFilters.Where(x => x.IsSelected).Select(x => x.Filter)],
            SortingList.SingleOrDefault(x => x.IsSelected)?.Sort ?? ItemSortBy.SortName,
            SortOrderList.SingleOrDefault(x => x.IsSelected)?.Sort ?? SortOrder.Ascending,
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

    internal async Task LoadMediaAsync(
                   Guid?[]? genreIds = null,
        ItemFilter[]? itemFilters = null,
        ItemSortBy itemSortBy = ItemSortBy.SortName,
        SortOrder sortOrder = SortOrder.Ascending,
        CancellationToken cancellationToken = default)
    {
        IsRunning = true;

        var itemsResult = (await apiClient.Items
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.ParentId = parentId;
                options.QueryParameters.StartIndex = CurrentIndex;
                options.QueryParameters.Limit = Limit;
                options.QueryParameters.SortBy = [itemSortBy,];
                options.QueryParameters.SortOrder = [sortOrder,];
                options.QueryParameters.GenreIds = genreIds;
                options.QueryParameters.Filters = itemFilters;
                options.QueryParameters.IncludeItemTypes = [itemType];
                options.QueryParameters.Fields = [ItemFields.PrimaryImageAspectRatio,];
            },
            cancellationToken: cancellationToken))!;

        TotalRecords = itemsResult.TotalRecordCount ?? 0;

        if (CurrentIndex == 0)
        {
            CountInformation = $"1-{Limit} of {TotalRecords}";
        }
        else
        {
            CountInformation = $"{CurrentIndex}-{(CurrentIndex - 1) + Limit} of {TotalRecords}";
        }

        if (CurrentIndex + Limit > TotalRecords)
        {
            CountInformation = $"{(CurrentIndex == 0 ? 1 : CurrentIndex)}-{TotalRecords} of {TotalRecords}";
        }

        if (TotalRecords == 0)
        {
            CountInformation = "0-0 of 0";
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
                                UnplayedItemCount = x.UserData.UnplayedItemCount ?? 0,
                                HasBeenWatched = x.UserData.Played.Value,
                            },
                        };

                        return item;
                    })];

        IsRunning = false;
    }

    private bool CanLoadNext()
    {
        return (CurrentIndex + Limit) < TotalRecords;
    }

    private bool CanLoadPrevious()
    {
        return (CurrentIndex - Limit) > 0;
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task FilterAndSorting(CancellationToken cancellationToken)
    {
        FilterReset();

        await LoadMediaAsync(
            [.. GenresFilterList.Where(x => x.IsSelected).Select(x => x.Id)],
            [.. FilteringFilters.Where(x => x.IsSelected).Select(x => x.Filter)],
            SortingList.SingleOrDefault(x => x.IsSelected)?.Sort ?? ItemSortBy.SortName,
            SortOrderList.SingleOrDefault(x => x.IsSelected)?.Sort ?? SortOrder.Ascending,
            cancellationToken);
    }

    [RelayCommand]
    private void LoadFiltering()
    {
        IsFilteringOpen = true;
    }

    [RelayCommand]
    private void LoadSort()
    {
        IsSortingOpen = true;
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task Refresh(CancellationToken cancellationToken)
    {
        await LoadMediaAsync(
            [.. GenresFilterList.Where(x => x.IsSelected).Select(x => x.Id)],
            [.. FilteringFilters.Where(x => x.IsSelected).Select(x => x.Filter)],
            SortingList.SingleOrDefault(x => x.IsSelected)?.Sort ?? ItemSortBy.SortName,
            SortOrderList.SingleOrDefault(x => x.IsSelected)?.Sort ?? SortOrder.Ascending,
            cancellationToken);
    }
}
