using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels
{
    public partial class DetailsViewModel : ObservableObject
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMemoryCache memoryCache;
        private readonly SdkClientSettings sdkClientSettings;

        [ObservableProperty]
        private ObservableCollection<UIPersonItem> castAndCrew;

        [ObservableProperty]
        private string director;

        [ObservableProperty]
        private string externalURLs;

        [ObservableProperty]
        private string genres;

        [ObservableProperty]
        private string imageUrl;

        [ObservableProperty]
        private BaseItemDto mediaItem;

        [ObservableProperty]
        private string mediaTagLines;

        [ObservableProperty]
        private string mediaTags;

        [ObservableProperty]
        private string runTime;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> similiarMediaList;

        [ObservableProperty]
        private string videoType;

        [ObservableProperty]
        private string writer;

        public DetailsViewModel(IHttpClientFactory httpClientFactory, SdkClientSettings sdkClientSettings, IMemoryCache memoryCache)
        {
            this.httpClientFactory = httpClientFactory;
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
        }

        public async Task LoadMediaInformationAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>("user");
            var httpClient = httpClientFactory.CreateClient();
            var userLibraryClient = new UserLibraryClient(sdkClientSettings, httpClient);
            var userLibraryItem = await userLibraryClient.GetItemAsync(user.Id, id);

            MediaItem = userLibraryItem;

            if (MediaItem.MediaStreams is not null)
            {
                VideoType = MediaItem.MediaStreams.SingleOrDefault(x => x.Type == MediaStreamType.Video)?.DisplayTitle;
            }

            if (MediaItem.RunTimeTicks.HasValue)
            {
                var time = new TimeSpan(MediaItem.RunTimeTicks.Value);
                RunTime = $"{time.Hours}h{time.Minutes}m";
            }

            if (MediaItem.Tags.Any())
            {
                MediaTags = $"Tags: {string.Join(", ", MediaItem.Tags)}";
            }

            if (MediaItem.Taglines.Any())
            {
                MediaTagLines = string.Join("", MediaItem.Taglines);
            }

            Genres = string.Join(", ", MediaItem.Genres);
            Director = string.Join(", ", MediaItem.People.Where(x => x.Role == "Director" && x.Type == "Director").Select(x => x.Name));
            Writer = string.Join(", ", MediaItem.People.Where(x => x.Role == "Writer" && x.Type == "Writer").Select(x => x.Name));
            CastAndCrew = new ObservableCollection<UIPersonItem>(
                MediaItem.People
                .Where(x => x.Type == "Actor")
                .Select(x => new UIPersonItem { Id = x.Id, Name = x.Name, Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=446&fillWidth=298&quality=96&tag={x.PrimaryImageTag}", Role = x.Role, }));

            var libraryClient = new LibraryClient(sdkClientSettings, httpClient);
            var similiarItems = await libraryClient.GetSimilarItemsAsync(MediaItem.Id, limit: 12, fields: new[] { ItemFields.PrimaryImageAspectRatio });

            SimiliarMediaList = new ObservableCollection<UIMediaListItem>(
                similiarItems.Items
                .Select(x => new UIMediaListItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Url = SetImageUrl(x.Id, "446", "298", x.ImageTags["Primary"]),
                    Year = x.ProductionYear?.ToString() ?? "N/A",
                }));

            ImageUrl = SetImageUrl(MediaItem.Id, "720", "480", MediaItem.ImageTags["Primary"]);
        }

        private string SetImageUrl(Guid id, string height, string width, string imageTagId)
        {
            return $"{sdkClientSettings.BaseUrl}/Items/{id}/Images/Primary?fillHeight={height}&fillWidth={width}&quality=96&tag={imageTagId}";
        }
    }
}
