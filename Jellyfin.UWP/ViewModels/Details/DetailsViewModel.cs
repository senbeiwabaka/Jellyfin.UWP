using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Details
{
    internal sealed partial class DetailsViewModel : MediaDetailsViewModel
    {
        [ObservableProperty]
        private ObservableCollection<UIPersonItem> castAndCrew;

        [ObservableProperty]
        private string director;

        [ObservableProperty]
        private string externalURLs;

        [ObservableProperty]
        private string genres;

        [ObservableProperty]
        private bool isEpisode;

        [ObservableProperty]
        private bool isMovie;

        [ObservableProperty]
        private bool isNotMovie;

        [ObservableProperty]
        private string mediaTagLines;

        [ObservableProperty]
        private string mediaTags;

        [ObservableProperty]
        private string runTime;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> similiarMediaList;

        [ObservableProperty]
        private string writer;

        public DetailsViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
            : base(memoryCache, apiClient, mediaHelpers)
        {
        }

        protected override async Task DetailsExtraExecuteAsync()
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

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
            Director = string.Join(", ", MediaItem.People.Where(x => x.Role == "Director" && x.Type == BaseItemPerson_Type.Director).Select(x => x.Name));
            Writer = string.Join(", ", MediaItem.People.Where(x => x.Role == "Writer" && x.Type == BaseItemPerson_Type.Writer).Select(x => x.Name));
            CastAndCrew = new ObservableCollection<UIPersonItem>(
                MediaItem.People
                .Where(x => x.Type == BaseItemPerson_Type.Actor)
                .Select(x => new UIPersonItem
                {
                    Id = x.Id.Value,
                    Name = x.Name,
                    ImageUrl = MediaHelpers.SetImageUrl(x, "446", "298"), // MediaHelpers.SetImageUrl(x, "446", "298"),
                    Role = x.Role,
                }));

            IsMovie = MediaItem.Type == BaseItemDto_Type.Movie;
            IsEpisode = MediaItem.Type == BaseItemDto_Type.Episode;
            IsNotMovie = MediaItem.Type == BaseItemDto_Type.Series;

            var similiarItems = await apiClient.Items[MediaItem.Id.Value].Similar
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.Limit = 12;
                    options.QueryParameters.Fields = new[] { ItemFields.PrimaryImageAspectRatio, };
                });

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

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task FavoriteStateAsync(CancellationToken cancellationToken)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

            if (MediaItem.UserData.IsFavorite.Value)
            {
                _ = await apiClient.UserFavoriteItems[MediaItem.Id.Value]
                    .DeleteAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
            }
            else
            {
                _ = await apiClient.UserFavoriteItems[MediaItem.Id.Value]
                    .PostAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
            }

            await LoadMediaInformationAsync(MediaItem.Id.Value);
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task PlayedStateAsync(CancellationToken cancellationToken)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

            if (MediaItem.UserData.Played.Value)
            {
                _ = await apiClient.UserPlayedItems[MediaItem.Id.Value]
                    .DeleteAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
            }
            else
            {
                _ = await apiClient.UserPlayedItems[MediaItem.Id.Value]
                    .PostAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
            }

            await LoadMediaInformationAsync(MediaItem.Id.Value);
        }
    }
}
