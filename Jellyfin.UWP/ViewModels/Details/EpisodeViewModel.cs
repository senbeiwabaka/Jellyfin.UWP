using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Details
{
    internal sealed partial class EpisodeViewModel : DetailsViewModel
    {
        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> seriesEpisodes;

        public EpisodeViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
            : base(memoryCache, apiClient, mediaHelpers)
        {
        }

        protected override async Task ExtraExecuteAsync()
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var episodes = await apiClient.Shows[MediaItem.SeriesId.Value].Episodes
                .GetAsync(options =>
                {
                    options.QueryParameters.SeasonId = MediaItem.ParentId;
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.Fields = new[] { ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio, };
                });

            SeriesEpisodes = new ObservableCollection<UIMediaListItem>(
                episodes.Items.Select(x =>
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
                    };

                    return item;
                }));
        }
    }
}
