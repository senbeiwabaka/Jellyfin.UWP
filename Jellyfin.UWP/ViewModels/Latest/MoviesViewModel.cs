using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
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
        private readonly SdkClientSettings sdkClientSettings;
        private readonly IMemoryCache memoryCache;
        private readonly IItemsClient itemsClient;
        private readonly IUserLibraryClient userLibraryClient;
        private readonly IMoviesClient moviesClient;

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

        public MoviesViewModel(
            SdkClientSettings sdkClientSettings,
            IMemoryCache memoryCache,
            IItemsClient itemsClient,
            IUserLibraryClient userLibraryClient,
            IMoviesClient moviesClient)
        {
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
            this.itemsClient = itemsClient;
            this.userLibraryClient = userLibraryClient;
            this.moviesClient = moviesClient;
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
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await itemsClient.GetResumeItemsAsync(
                userId: user.Id,
                parentId: id,
                enableTotalRecordCount: false,
                limit: 5,
                includeItemTypes: new[] { BaseItemKind.Movie, });

            ResumeMediaList = new ObservableCollection<UIMediaListItem>(
                itemsResult
                    .Items
                    .Select(x => new UIMediaListItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = GetContinueItemImage(sdkClientSettings, x),
                        Type = x.Type,
                    }));

            HasResumeMedia = ResumeMediaList.Count > 0;
        }

        private async Task LoadLatestAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await userLibraryClient.GetLatestMediaAsync(
                userId: user.Id,
                limit: 18,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.BasicSyncInfo, },
                imageTypeLimit: 1,
                enableImageTypes: new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, },
                parentId: id,
                includeItemTypes: new[] { BaseItemKind.Movie, });

            LatestMediaList = new ObservableCollection<UIMediaListItem>(
            itemsResult
            .Select(x => new UIMediaListItem
            {
                Id = x.Id,
                Name = x.Name,
                Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=239&fillWidth=425&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                Type = x.Type,
            }));
        }

        private async Task LoadRecommendationsAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var itemsResult = await moviesClient.GetMovieRecommendationsAsync(
                userId: user.Id,
                parentId: id,
                categoryLimit: 6,
                itemLimit: 8,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.MediaSourceCount, ItemFields.BasicSyncInfo, });

            RecommendationListGrouped = new ObservableGroupedCollection<Recommendation, UIMediaListItem>();

            foreach (var item in itemsResult)
            {
                var recommendation = new Recommendation();

                switch (item.RecommendationType)
                {
                    case RecommendationType.SimilarToRecentlyPlayed:
                        recommendation.DisplayName = $"Because you watched {item.BaselineItemName}";
                        break;

                    case RecommendationType.SimilarToLikedItem:
                        recommendation.DisplayName = item.BaselineItemName;
                        break;

                    case RecommendationType.HasDirectorFromRecentlyPlayed:
                        recommendation.DisplayName = $"Directed by {item.BaselineItemName}";
                        break;

                    case RecommendationType.HasActorFromRecentlyPlayed:
                        recommendation.DisplayName = $"Starring {item.BaselineItemName}";
                        break;

                    case RecommendationType.HasLikedDirector:
                        recommendation.DisplayName = item.BaselineItemName;
                        break;

                    case RecommendationType.HasLikedActor:
                        recommendation.DisplayName = item.BaselineItemName;
                        break;

                    default:
                        recommendation.DisplayName = $"Uh Oh, issue! {item.BaselineItemName}";
                        break;
                }

                var items = item.Items
                    .Select(x => new UIMediaListItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=239&fillWidth=425&quality=96&tag={x.ImageTags[JellyfinConstants.PrimaryName]}",
                        Type = x.Type,
                    });

                RecommendationListGrouped.Add(new ObservableGroup<Recommendation, UIMediaListItem>(recommendation, items));
            }
        }

        private static string GetContinueItemImage(SdkClientSettings settings, BaseItemDto item)
        {
            if (item.BackdropImageTags.Count > 0)
            {
                return $"{settings.BaseUrl}/Items/{item.Id}/Images/{JellyfinConstants.BackdropName}?fillHeight=239&fillWidth=425&quality=96&tag={item.BackdropImageTags[0]}";
            }

            return $"{settings.BaseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ImageTags[JellyfinConstants.PrimaryName]}";
        }
    }
}
