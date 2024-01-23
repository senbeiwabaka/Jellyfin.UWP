using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Details
{
    internal sealed partial class EpisodeViewModel : DetailsViewModel
    {
        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> seriesEpisodes;

        public EpisodeViewModel(
            IMemoryCache memoryCache,
            IUserLibraryClient userLibraryClient,
            ILibraryClient libraryClient,
            SdkClientSettings sdkClientSettings,
            ITvShowsClient tvShowsClient,
            IPlaystateClient playstateClient)
            : base(memoryCache, userLibraryClient, libraryClient, sdkClientSettings, tvShowsClient, playstateClient)
        {
        }

        protected override async Task ExtraExecuteAsync()
        {
            var user = memoryCache.Get<UserDto>("user");
            var episodes = await tvShowsClient.GetEpisodesAsync(
                seriesId: MediaItem.SeriesId!.Value,
                userId: user.Id,
                seasonId: MediaItem.ParentId,
                fields: new[] { ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio, });

            SeriesEpisodes = new ObservableCollection<UIMediaListItem>(
                episodes.Items.Select(x =>
                {
                    var item = new UIMediaListItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = SetImageUrl(x.Id, "505", "349", "Primary", x.ImageTags),
                        IndexNumber = x.IndexNumber,
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite,
                            HasBeenWatched = x.UserData.Played,
                        },
                    };

                    return item;
                }));
        }

        protected override string SetImageUrl(Guid id, string height, string width, string tagKey, IDictionary<string, string> imageTages)
        {
            if (imageTages is null || imageTages.Count == 0 || !imageTages.ContainsKey(tagKey))
            {
                return string.Empty;
            }

            var imageTagId = imageTages[tagKey];

            return $"{sdkClientSettings.BaseUrl}/Items/{id}/Images/Primary?fillHeight={height}&fillWidth={width}&quality=96&tag={imageTagId}";
        }
    }
}
