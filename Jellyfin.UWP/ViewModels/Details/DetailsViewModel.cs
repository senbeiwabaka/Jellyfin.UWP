using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels.Details;

internal sealed partial class DetailsViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaDetailsViewModel(memoryCache, apiClient, mediaHelpers)
{
    [ObservableProperty]
    public partial string Director { get; set; }

    [ObservableProperty]
    public partial string ExternalURLs { get; set; }

    [ObservableProperty]
    public partial bool IsEpisode { get; set; }

    [ObservableProperty]
    public partial bool IsMovie { get; set; }

    [ObservableProperty]
    public partial bool IsNotMovie { get; set; }

    [ObservableProperty]
    public partial string MediaTagLines { get; set; }

    [ObservableProperty]
    public partial string MediaTags { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> SimiliarMediaList { get; set; }

    [ObservableProperty]
    public partial string Writer { get; set; }

    protected override async Task DetailsExtraExecuteAsync(CancellationToken cancellationToken = default)
    {
        var user = MemoryCache.Get<UserDto>(JellyfinConstants.UserName);

        if (MediaItem.Tags.Count != 0)
        {
            MediaTags = $"Tags: {string.Join(", ", MediaItem.Tags)}";
        }

        Director = string.Join(", ", MediaItem.People.Where(x => x.Role == "Director" && x.Type == BaseItemPerson_Type.Director).Select(x => x.Name));
        Writer = string.Join(", ", MediaItem.People.Where(x => x.Role == "Writer" && x.Type == BaseItemPerson_Type.Writer).Select(x => x.Name));

        IsMovie = MediaItem.Type == BaseItemDto_Type.Movie;
        IsEpisode = MediaItem.Type == BaseItemDto_Type.Episode;
        IsNotMovie = MediaItem.Type == BaseItemDto_Type.Series;

        var similiarItems = await ApiClient.Items[MediaItem.Id.Value].Similar
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = user.Id;
                options.QueryParameters.Limit = 12;
                options.QueryParameters.Fields = [ItemFields.PrimaryImageAspectRatio,];
            }, cancellationToken);

        SimiliarMediaList = new ObservableCollection<UIMediaListItem>(
            similiarItems.Items
            .Select(x =>
            {
                var item = new UIMediaListItem
                {
                    Id = x.Id.Value,
                    Name = x.Name,
                    Url = MediaHelpers.SetImageUrl(x, "446", "298", JellyfinConstants.PrimaryName),// MediaHelpers.SetImageUrl(x, "446", "298", JellyfinConstants.PrimaryName),
                    Year = x.ProductionYear?.ToString() ?? "N/A",
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
}
