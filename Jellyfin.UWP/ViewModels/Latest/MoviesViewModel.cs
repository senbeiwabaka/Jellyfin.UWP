using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Latest
{
    internal sealed partial class MoviesViewModel : ObservableObject
    {
        private readonly IMemoryCache memoryCache;
        private readonly JellyfinApiClient apiClient;
        private readonly IMediaHelpers mediaHelpers;

        [ObservableProperty]
        private bool hasEnoughDataForContinueScrolling;

        [ObservableProperty]
        private bool hasEnoughDataForLatestScrolling;

        [ObservableProperty]
        private bool hasResumeMedia;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> latestMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> resumeMediaList;

        [ObservableProperty]
        private ObservableGroupedCollection<Recommendation, UIMediaListItem> recommendationListGrouped;

        private Guid id;

        public MoviesViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
            this.mediaHelpers = mediaHelpers;
        }

        public async Task LoadInitialAsync(Guid id)
        {
            this.id = id;

            await LoadResumeItemsAsync();
            await LoadLatestAsync();
            await LoadRecommendationsAsync();
        }

        private async Task LoadResumeItemsAsync()
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var itemsResult = await apiClient.UserItems.Resume
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.ParentId = id;
                    options.QueryParameters.EnableTotalRecordCount = false;
                    options.QueryParameters.Limit = 5;
                    options.QueryParameters.IncludeItemTypes = new[] { BaseItemKind.Movie, };
                });

            ResumeMediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x => new UIMediaListItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = GetContinueItemImage(x),
                        Type = x.Type.Value,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite.Value,
                            HasBeenWatched = x.UserData.Played.Value,
                            UnplayedItemCount = 0,
                        },
                    }));

            HasResumeMedia = ResumeMediaList.Count > 0;
        }

        private async Task LoadLatestAsync()
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var itemsResult = await apiClient.Items.Latest
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.Limit = 18;
                    options.QueryParameters.Fields = new[] { ItemFields.PrimaryImageAspectRatio, };
                    options.QueryParameters.ImageTypeLimit = 1;
                    options.QueryParameters.EnableImageTypes = new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, };
                    options.QueryParameters.ParentId = id;
                    options.QueryParameters.IncludeItemTypes = new[] { BaseItemKind.Movie, };
                });

            LatestMediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult.Select(x => new UIMediaListItem
                {
                    Id = x.Id.Value,
                    Name = x.Name,
                    Url = mediaHelpers.SetImageUrl(x, "239", "425", JellyfinConstants.PrimaryName),
                    Type = x.Type.Value,
                    UserData = new UIUserData
                    {
                        IsFavorite = x.UserData.IsFavorite.Value,
                        HasBeenWatched = x.UserData.Played.Value,
                        UnplayedItemCount = 0,
                    },
                }));
        }

        private async Task LoadRecommendationsAsync()
        {
            RecommendationListGrouped = new ObservableGroupedCollection<Recommendation, UIMediaListItem>();

            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var itemsResult = await apiClient.Movies.Recommendations
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.ParentId = id;
                    options.QueryParameters.CategoryLimit = 6;
                    options.QueryParameters.ItemLimit = 8;
                    options.QueryParameters.Fields = new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.MediaSourceCount, };
                });

            foreach (var item in itemsResult)
            {
                var recommendation = new Recommendation();

                switch (item.RecommendationType)
                {
                    case RecommendationDto_RecommendationType.SimilarToRecentlyPlayed:
                        recommendation.DisplayName = $"Because you watched {item.BaselineItemName}";
                        break;

                    case RecommendationDto_RecommendationType.SimilarToLikedItem:
                        recommendation.DisplayName = item.BaselineItemName;
                        break;

                    case RecommendationDto_RecommendationType.HasDirectorFromRecentlyPlayed:
                        recommendation.DisplayName = $"Directed by {item.BaselineItemName}";
                        break;

                    case RecommendationDto_RecommendationType.HasActorFromRecentlyPlayed:
                        recommendation.DisplayName = $"Starring {item.BaselineItemName}";
                        break;

                    case RecommendationDto_RecommendationType.HasLikedDirector:
                        recommendation.DisplayName = item.BaselineItemName;
                        break;

                    case RecommendationDto_RecommendationType.HasLikedActor:
                        recommendation.DisplayName = item.BaselineItemName;
                        break;

                    default:
                        recommendation.DisplayName = $"Uh Oh, issue! {item.BaselineItemName}";
                        break;
                }

                var items = item.Items
                    .Select(x => new UIMediaListItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = mediaHelpers.SetImageUrl(x, "239", "425", JellyfinConstants.PrimaryName),
                        Type = x.Type.Value,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite.Value,
                            HasBeenWatched = x.UserData.Played.Value,
                            UnplayedItemCount = 0,
                        },
                    });

                RecommendationListGrouped.Add(new ObservableGroup<Recommendation, UIMediaListItem>(recommendation, items));
            }
        }

        private string GetContinueItemImage(BaseItemDto item)
        {
            var baseUrl = memoryCache.Get<string>(JellyfinConstants.HostUrlName);
            if (item.BackdropImageTags.Count > 0)
            {
                return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.BackdropName}?fillHeight=239&fillWidth=425&quality=96&tag={item.BackdropImageTags[0]}";
            }

            return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ImageTags.AdditionalData[JellyfinConstants.PrimaryName]}";
        }
    }
}
