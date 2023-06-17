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
    public partial class DetailsViewModel : ObservableObject
    {
        private readonly IMemoryCache memoryCache;
        private readonly IUserLibraryClient userLibraryClient;
        private readonly ILibraryClient libraryClient;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly ITvShowsClient tvShowsClient;

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

        [ObservableProperty]
        private bool isMovie;

        [ObservableProperty]
        private string seriesNextUpUrl;

        [ObservableProperty]
        private Guid? seriesNextUpId;

        [ObservableProperty]
        private string seriesNextUpName;

        [ObservableProperty]
        private bool isNotMovie;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> seriesMetadata;

        public DetailsViewModel(
            IMemoryCache memoryCache,
            IUserLibraryClient userLibraryClient,
            ILibraryClient libraryClient,
            SdkClientSettings sdkClientSettings,
            ITvShowsClient tvShowsClient)
        {
            this.memoryCache = memoryCache;
            this.userLibraryClient = userLibraryClient;
            this.libraryClient = libraryClient;
            this.sdkClientSettings = sdkClientSettings;
            this.tvShowsClient = tvShowsClient;
        }

        public async Task LoadMediaInformationAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>("user");
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

            if (MediaItem.Type == BaseItemKind.Movie)
            {
                IsMovie = true;
            }

            if (MediaItem.Type == BaseItemKind.Series)
            {
                IsNotMovie = true;

                var seasons = await tvShowsClient.GetSeasonsAsync(
                    MediaItem.Id,
                    user.Id,
                    fields: new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.BasicSyncInfo,
                        ItemFields.MediaSourceCount,
                    });
                var nextUp = await tvShowsClient.GetNextUpAsync(
                    user.Id,
                    seriesId: MediaItem.Id.ToString(),
                    fields: new[] { ItemFields.MediaSourceCount, });
                var nextUpItem = nextUp.Items.First();

                SeriesNextUpUrl = SetImageUrl(nextUpItem.Id, "296", "526", nextUpItem.ImageTags["Primary"]);
                SeriesNextUpId = nextUpItem.Id;
                SeriesNextUpName = $"S{nextUpItem.ParentIndexNumber}:E{nextUpItem.IndexNumber} - {nextUpItem.Name}";
                SeriesMetadata = new ObservableCollection<UIMediaListItem>(
                    seasons.Items.Select(x => new UIMediaListItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = SetImageUrl(x.Id, "505", "349", x.ImageTags["Primary"]),
                    }));
            }

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

        // TODO: Figure out how to get series episode id to play.
        public async Task<Guid> GetPlayId()
        {
            if (IsMovie)
            {
                return MediaItem.Id;
            }

            if (SeriesMetadata.Any(x => x.IsSelected))
            {
                var user = memoryCache.Get<UserDto>("user");

                var episodes = await tvShowsClient.GetEpisodesAsync(
                    seriesId: MediaItem.Id,
                    userId: user.Id,
                    seasonId: SeriesMetadata.Single(x => x.IsSelected).Id,
                    fields: new[] { ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio, });

                return episodes.Items.First(x => !x.UserData.Played).Id;
            }

            return SeriesNextUpId.Value;
        }

        private string SetImageUrl(Guid id, string height, string width, string imageTagId)
        {
            return $"{sdkClientSettings.BaseUrl}/Items/{id}/Images/Primary?fillHeight={height}&fillWidth={width}&quality=96&tag={imageTagId}";
        }
    }
}
