using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Models;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ViewModels.Details;

public sealed partial class DetailsViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaDetailsViewModel(memoryCache, apiClient, mediaHelpers)
{
    [ObservableProperty]
    public partial string Director { get; set; }

    [ObservableProperty]
    public partial string ExternalURLs { get; set; }

    [ObservableProperty]
    public partial string MediaTags { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> SimiliarMediaList { get; set; }

    [ObservableProperty]
    public partial string Writer { get; set; }

    protected override async Task DetailsExtraExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (MediaItem.Tags.Count != 0)
        {
            MediaTags = $"Tags: {string.Join(", ", MediaItem.Tags)}";
        }

        Director = string.Join(", ", MediaItem.People.Where(x => x.Type == BaseItemPerson_Type.Director).Select(x => x.Name));
        Writer = string.Join(", ", MediaItem.People.Where(x => x.Type == BaseItemPerson_Type.Writer).Select(x => x.Name));

        var similiarItems = await ApiClient.Items[MediaItem.Id.Value].Similar
            .GetAsync(options =>
            {
                options.QueryParameters.UserId = User.Id;
                options.QueryParameters.Limit = 12;
                options.QueryParameters.Fields = [ItemFields.PrimaryImageAspectRatio,];
            }, cancellationToken);

        SimiliarMediaList = new ObservableCollection<UIMediaListItem>(
            (similiarItems?.Items ?? [])
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
                        IsFavorite = x.UserData.IsFavorite ?? false,
                        UnplayedItemCount = x.UserData.UnplayedItemCount ?? 0,
                        HasBeenWatched = x.UserData.Played ?? false,
                    },
                    Type = x.Type.Value,
                };

                return item;
            }));

        DetailsItemPlayRecord.MediaId = MediaItem.Id!.Value;
    }
}
