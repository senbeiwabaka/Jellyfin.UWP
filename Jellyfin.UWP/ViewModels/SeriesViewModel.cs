using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels
{
    public partial class SeriesViewModel : ObservableObject
    {
        private readonly IMemoryCache memoryCache;
        private readonly ITvShowsClient tvShowsClient;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly IUserLibraryClient userLibraryClient;

        [ObservableProperty]
        private string imageUrl;

        [ObservableProperty]
        private BaseItemDto mediaItem;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> seriesMetadata;

        public SeriesViewModel(
            IMemoryCache memoryCache,
            IUserLibraryClient userLibraryClient,
            ITvShowsClient tvShowsClient,
            SdkClientSettings sdkClientSettings)
        {
            this.memoryCache = memoryCache;
            this.userLibraryClient = userLibraryClient;
            this.tvShowsClient = tvShowsClient;
            this.sdkClientSettings = sdkClientSettings;
        }

        public async Task LoadMediaInformationAsync(SeasonSeries seasonSeries)
        {
            var user = memoryCache.Get<UserDto>("user");
            var userLibraryItem = await userLibraryClient.GetItemAsync(user.Id, seasonSeries.SeasonId);

            MediaItem = userLibraryItem;

            var episodes = await tvShowsClient.GetEpisodesAsync(
                    seriesId: seasonSeries.SeriesId,
                    userId: user.Id,
                    seasonId: seasonSeries.SeasonId,
                    fields: new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.BasicSyncInfo,
                    });

            SeriesMetadata = new ObservableCollection<UIMediaListItem>(
                episodes.Items.Select(x => new UIMediaListItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Url = SetImageUrl(x.Id, "505", "349", x.ImageTags["Primary"]),
                }));

            ImageUrl = SetImageUrl(MediaItem.Id, "720", "480", MediaItem.ImageTags["Primary"]);
        }

        private string SetImageUrl(Guid id, string height, string width, string imageTagId)
        {
            return $"{sdkClientSettings.BaseUrl}/Items/{id}/Images/Primary?fillHeight={height}&fillWidth={width}&quality=96&tag={imageTagId}";
        }
    }
}
