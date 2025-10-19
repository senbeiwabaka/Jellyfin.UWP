using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels;

internal sealed partial class SearchViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : ObservableObject
{
    private const int Limit = 24;

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> EpisodesMediaList { get; set; }

    [ObservableProperty]
    public partial bool HasEpisodesResult { get; set; }

    [ObservableProperty]
    public partial bool HasMoviesResult { get; set; }

    [ObservableProperty]
    public partial bool HasSeriesResult { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> MovieMediaList { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> SeriesMediaList { get; set; }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task LoadSearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var movieItemsResult = await apiClient.Items
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.SearchTerm = query;
                options.QueryParameters.Limit = Limit;
                options.QueryParameters.Recursive = true;
                options.QueryParameters.EnableTotalRecordCount = false;
                options.QueryParameters.ImageTypeLimit = 1;
                options.QueryParameters.IncludeItemTypes = [BaseItemKind.Movie];
            }, cancellationToken);

        MovieMediaList = new ObservableCollection<UIMediaListItem>(
            movieItemsResult
                .Items
                .Select(x =>
                    new UIMediaListItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = mediaHelpers.SetImageUrl(x, "330", "220", JellyfinConstants.PrimaryName),
                        IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                        CollectionType = BaseItemDto_CollectionType.Movies,
                        Type = x.Type.Value,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite.Value,
                            HasBeenWatched = x.UserData.Played.Value,
                            UnplayedItemCount = x.UserData.UnplayedItemCount,
                        },
                    }));

        HasMoviesResult = MovieMediaList.Count > 0;

        var seriesItemsResult = await apiClient.Items
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.SearchTerm = query;
                options.QueryParameters.Limit = Limit;
                options.QueryParameters.Recursive = true;
                options.QueryParameters.EnableTotalRecordCount = false;
                options.QueryParameters.ImageTypeLimit = 1;
                options.QueryParameters.IncludeItemTypes = [BaseItemKind.Series];
            }, cancellationToken);

        SeriesMediaList = new ObservableCollection<UIMediaListItem>(
            seriesItemsResult
                .Items
                .Select(x =>
                {
                    var item = new UIMediaListItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = mediaHelpers.SetImageUrl(x, "330", "220", JellyfinConstants.PrimaryName),
                        IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                        CollectionType = BaseItemDto_CollectionType.Tvshows,
                        Type = x.Type.Value,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite.Value,
                            HasBeenWatched = x.UserData.Played.Value,
                            UnplayedItemCount = x.UserData.UnplayedItemCount,
                        },
                    };

                    return item;
                }));

        HasSeriesResult = SeriesMediaList.Count > 0;

        var episodesItemsResult = await apiClient.Items
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.SearchTerm = query;
                options.QueryParameters.Limit = Limit;
                options.QueryParameters.Recursive = true;
                options.QueryParameters.EnableTotalRecordCount = false;
                options.QueryParameters.ImageTypeLimit = 1;
                options.QueryParameters.IncludeItemTypes = [BaseItemKind.Episode];
            }, cancellationToken);

        EpisodesMediaList = new ObservableCollection<UIMediaListItem>(
            episodesItemsResult
                .Items
                .Select(x =>
                    new UIMediaListItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = mediaHelpers.SetImageUrl(x, "215", "380", JellyfinConstants.PrimaryName),
                        IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                        Type = x.Type.Value,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite.Value,
                            HasBeenWatched = x.UserData.Played.Value,
                            UnplayedItemCount = x.UserData.UnplayedItemCount,
                        },
                    }));

        HasEpisodesResult = EpisodesMediaList.Count > 0;
    }

    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
    private async Task TypeTextSearch(string query, CancellationToken cancellationToken)
    {
        await LoadSearchAsync(query, cancellationToken);
    }
}
