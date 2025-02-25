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

namespace Jellyfin.UWP.ViewModels.Latest;

internal sealed partial class ShowsViewModel : ObservableObject
{
    private readonly IMemoryCache memoryCache;
    private readonly JellyfinApiClient apiClient;
    private readonly IMediaHelpers mediaHelpers;

    [ObservableProperty]
    private bool hasEnoughDataForContinueScrolling;

    [ObservableProperty]
    private bool hasEnoughDataForLatestScrolling;

    [ObservableProperty]
    private bool hasEnoughDataForNextUpScrolling;

    [ObservableProperty]
    private bool hasResumeMedia;

    private Guid id;

    [ObservableProperty]
    private ObservableCollection<UIMediaListItem> latestMediaList;

    [ObservableProperty]
    private ObservableCollection<UIMediaListItemSeries> nextupMediaList;

    [ObservableProperty]
    private ObservableCollection<UIMediaListItem> resumeMediaList;

    public ShowsViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
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
        await LoadNextUpAsync();
    }

    private string GetContinueItemImage(BaseItemDto item)
    {
        var baseUrl = memoryCache.Get<string>(JellyfinConstants.HostUrlName);
        if (item.ParentThumbItemId != null)
        {
            return $"{baseUrl}/Items/{item.ParentThumbItemId}/Images/{JellyfinConstants.ThumbName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ParentThumbImageTag}";
        }

        if (item.ParentBackdropItemId != null)
        {
            return $"{baseUrl}/Items/{item.ParentBackdropItemId}/Images/{JellyfinConstants.BackdropName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ParentBackdropImageTags[0]}";
        }

        return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ImageTags.AdditionalData[JellyfinConstants.PrimaryName]}";
    }

    private string GetItemImage(BaseItemDto item)
    {
        var baseUrl = memoryCache.Get<string>(JellyfinConstants.HostUrlName);
        if (item.ImageTags.AdditionalData.ContainsKey(JellyfinConstants.ThumbName))
        {
            return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.ThumbName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ImageTags.AdditionalData["Thumb"]}";
        }

        if (item.BackdropImageTags.Count > 0)
        {
            return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.BackdropName}?fillHeight=239&fillWidth=425&quality=96&tag={item.BackdropImageTags[0]}";
        }

        return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=239&fillWidth=425&quality=96&tag={item.ImageTags.AdditionalData[JellyfinConstants.PrimaryName]}";
    }

    private async Task LoadLatestAsync()
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var itemsResult = await apiClient.Items.Latest
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.Limit = 30;
                options.QueryParameters.Fields = [ItemFields.PrimaryImageAspectRatio,];
                options.QueryParameters.ImageTypeLimit = 1;
                options.QueryParameters.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb,];
                options.QueryParameters.ParentId = id;
                options.QueryParameters.IncludeItemTypes = [BaseItemKind.Episode,];
            });

        LatestMediaList = [.. itemsResult
        .Select(x =>
        {
            var item = new UIMediaListItemSeries
            {
                Id = x.Id.Value,
                Name = x.Name,
                Url = GetItemImage(x),
                CollectionType = x.CollectionType,
            };

            item.UserData.IsFavorite = x.UserData.IsFavorite.Value;
            item.UserData.HasBeenWatched = x.UserData.Played.Value;
            item.UserData.UnplayedItemCount = x.UserData.UnplayedItemCount;

            return item;
        })];
    }

    private async Task LoadNextUpAsync()
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var itemsResult = await apiClient.Shows.NextUp
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.StartIndex = 0;
                options.QueryParameters.Limit = 24;
                options.QueryParameters.ParentId = id;
                options.QueryParameters.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.DateCreated, ItemFields.MediaSourceCount,];
                options.QueryParameters.ImageTypeLimit = 1;
                options.QueryParameters.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb,];
                options.QueryParameters.EnableTotalRecordCount = false;
            });

        NextupMediaList = [.. itemsResult
                .Items
                .Select(x =>
                    new UIMediaListItemSeries
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = mediaHelpers.SetThumbImageUrl(x, "317", "564"),
                        Type = x.Type.Value,
                        SeriesName = x.SeriesName,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite.Value,
                            HasBeenWatched = x.UserData.Played.Value,
                            UnplayedItemCount = x.ChildCount ?? 0,
                        },
                    })];
    }

    private async Task LoadResumeItemsAsync()
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
        var itemsResult = await apiClient.UserItems.Resume
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.Limit = 24;
                options.QueryParameters.ParentId = id;
                options.QueryParameters.EnableTotalRecordCount = false;
            });

        ResumeMediaList = [.. itemsResult
                .Items
                .Select(x =>
                    new UIMediaListItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = GetContinueItemImage(x),
                        Type = x.Type.Value,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite.Value,
                            HasBeenWatched = x.UserData.Played.Value,
                            UnplayedItemCount = x.ChildCount ?? 0,
                        },
                    })];

        HasResumeMedia = ResumeMediaList.Count > 0;
    }
}
