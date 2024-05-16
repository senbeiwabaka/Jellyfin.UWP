﻿using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
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
        private readonly IMemoryCache memoryCache;
        private readonly JellyfinApiClient apiClient;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> episodesMediaList;

        [ObservableProperty]
        private bool hasEpisodesResult;

        [ObservableProperty]
        private bool hasMoviesResult;

        [ObservableProperty]
        private bool hasSeriesResult;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> movieMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> seriesMediaList;

        public SearchViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
        }

        public async Task LoadSearchAsync(string query)
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
                    options.QueryParameters.IsMovie = true;
                    options.QueryParameters.IncludeItemTypes = new[] { BaseItemKind.Movie };
                });

            MovieMediaList = new ObservableCollection<UIMediaListItem>(
                movieItemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = MediaHelpers.SetImageUrl(x, "330", "220", JellyfinConstants.PrimaryName),
                            IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                            CollectionType = x.CollectionType.Value,
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
                    options.QueryParameters.IsMovie = true;
                    options.QueryParameters.IncludeItemTypes = new[] { BaseItemKind.Series };
                });

            SeriesMediaList = new ObservableCollection<UIMediaListItem>(
                seriesItemsResult
                    .Items
                    .Select(x =>
                    {
                        var item = new UIMediaListItem
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = MediaHelpers.SetImageUrl(x, "330", "220", JellyfinConstants.PrimaryName),
                            IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                            CollectionType = x.CollectionType.Value,
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
                    options.QueryParameters.IsMovie = true;
                    options.QueryParameters.IncludeItemTypes = new[] { BaseItemKind.Episode };
                });

            EpisodesMediaList = new ObservableCollection<UIMediaListItem>(
                episodesItemsResult
                    .Items
                    .Select(x =>
                        new UIMediaListItem
                        {
                            Id = x.Id.Value,
                            Name = x.Name,
                            Url = MediaHelpers.SetImageUrl(x, "215", "380", JellyfinConstants.PrimaryName),
                            IsFolder = x.IsFolder.HasValue && x.IsFolder.Value,
                            CollectionType = x.CollectionType.Value,
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
    }
}
