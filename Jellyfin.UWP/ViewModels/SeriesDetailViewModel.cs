using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels
{
    internal sealed partial class SeriesDetailViewModel : DetailsViewModel
    {
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

        protected override async Task ExtraExecuteAsync()
        {
            var user = memoryCache.Get<UserDto>("user");

            await SetupNextUp(user);
        }

        private async Task SetupNextUp(UserDto user)
        {
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
                };

                NextUpItem.UserData.IsFavorite = item.UserData.IsFavorite;
                NextUpItem.UserData.HasBeenWatched = item.UserData.Played;
            }
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
    }
}
