using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
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
    internal sealed partial class SeriesDetailViewModel : DetailsViewModel
    {
        [ObservableProperty]
        private UIMediaListItem nextUpItem;

        public SeriesDetailViewModel(
            IMemoryCache memoryCache,
            IUserLibraryClient userLibraryClient,
            ILibraryClient libraryClient,
            SdkClientSettings sdkClientSettings,
            ITvShowsClient tvShowsClient,
            IPlaystateClient playstateClient)
            : base(memoryCache, userLibraryClient, libraryClient, sdkClientSettings, tvShowsClient, playstateClient)
        {
        }

        public override Task<Guid> GetPlayIdAsync()
        {
            return MediaHelpers.GetPlayIdAsync(MediaItem, SeriesMetadata?.ToArray(), NextUpItem?.Id);
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        public async Task NextUpPlayedStateAsync(CancellationToken cancellationToken)
        {
            var user = memoryCache.Get<UserDto>("user");

            if (NextUpItem != null)
            {
                if (NextUpItem.UserData.HasBeenWatched)
                {
                    _ = await playstateClient.MarkUnplayedItemAsync(
                        user.Id,
                        NextUpItem.Id,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    _ = await playstateClient.MarkPlayedItemAsync(
                        user.Id,
                        NextUpItem.Id,
                        DateTimeOffset.Now,
                        cancellationToken: cancellationToken);
                }

                await LoadMediaInformationAsync(MediaItem.Id);
            }
        }

        protected override async Task ExtraExecuteAsync()
        {
            NextUpItem = null;

            var user = memoryCache.Get<UserDto>("user");
            var nextUp = await tvShowsClient.GetNextUpAsync(
                               user.Id,
                               seriesId: MediaItem.Id,
                               fields: new[] { ItemFields.MediaSourceCount, });
            var item = nextUp.Items.FirstOrDefault();

            if (item is not null)
            {
                NextUpItem = new UIMediaListItem
                {
                    Id = item.Id,
                    Name = $"S{item.ParentIndexNumber}:E{item.IndexNumber} - {item.Name}",
                    Url = SetImageUrl(item.Id, "296", "526", JellyfinConstants.PrimaryName, item.ImageTags),
                    UserData = new UIUserData
                    {
                        IsFavorite = item.UserData.IsFavorite,
                        HasBeenWatched = item.UserData.Played,
                    },
                };
            }

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

            SeriesMetadata = new ObservableCollection<UIMediaListItem>(
                seasons.Items.Select(x =>
                {
                    var item = new UIMediaListItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = SetSeasonImageUrl(sdkClientSettings, x),
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite,
                            UnplayedItemCount = x.UserData.UnplayedItemCount,
                            HasBeenWatched = x.UserData.Played,
                        },
                    };

                    return item;
                }));
        }

        private static string SetSeasonImageUrl(SdkClientSettings settings, BaseItemDto item)
        {
            if (item.ImageTags.ContainsKey(JellyfinConstants.PrimaryName))
            {
                return $"{settings.BaseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=446&fillWidth=298&quality=96&tag={item.ImageTags[JellyfinConstants.PrimaryName]}";
            }

            return $"{settings.BaseUrl}/Items/{item.SeriesId}/Images/{JellyfinConstants.PrimaryName}?fillHeight=446&fillWidth=298&quality=96&tag={item.SeriesPrimaryImageTag}";
        }
    }
}
