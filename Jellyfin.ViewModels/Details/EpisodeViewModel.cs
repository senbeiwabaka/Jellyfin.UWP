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

public sealed partial class EpisodeViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaDetailsViewModel(memoryCache, apiClient, mediaHelpers)
{
    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> SeriesEpisodes { get; set; }

    protected override async Task DetailsExtraExecuteAsync(CancellationToken cancellationToken = default)
    {
        var episodes = await ApiClient.Shows[MediaItem.SeriesId.Value].Episodes
            .GetAsync(options =>
            {
                options.QueryParameters.SeasonId = MediaItem.ParentId;
                options.QueryParameters.UserId = User.Id;
                options.QueryParameters.Fields = [ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio,];
            }, cancellationToken: cancellationToken);

        SeriesEpisodes = [.. episodes.Items.Select(x =>
            {
                var item = new UIMediaListItem
                {
                    Id = x.Id.Value,
                    Name = x.Name,
                    Url = MediaHelpers.SetImageUrl(x, "505", "349", JellyfinConstants.PrimaryName),
                    IndexNumber = x.IndexNumber,
                    UserData = new UIUserData
                    {
                        IsFavorite = x.UserData.IsFavorite.Value,
                        HasBeenWatched = x.UserData.Played.Value,
                    },
                    Type = x.Type.Value,
                    CollectionType = x.CollectionType,
                };

                return item;
            })];

        DetailsItemPlayRecord.MediaId = MediaItem.Id!.Value;
    }
}
