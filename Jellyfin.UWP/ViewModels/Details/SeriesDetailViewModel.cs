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
    internal sealed partial class SeriesDetailViewModel : DetailsViewModel
    {
        [ObservableProperty]
        private UIMediaListItem nextUpItem;

        public SeriesDetailViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient)
            : base(memoryCache, apiClient)
        {
        }

        public override Task<Guid> GetPlayIdAsync()
        {
            return MediaHelpers.GetPlayIdAsync(MediaItem, SeriesMetadata?.ToArray(), NextUpItem?.Id);
        }

        protected override async Task ExtraExecuteAsync()
        {
            NextUpItem = null;

            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var nextUp = await apiClient.Shows.NextUp
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                    options.QueryParameters.SeriesId = MediaItem.Id;
                    options.QueryParameters.Fields = new[] { ItemFields.MediaSourceCount, };
                });
            var item = nextUp.Items.FirstOrDefault();

            if (item is not null)
            {
                NextUpItem = new UIMediaListItem
                {
                    Id = item.Id.Value,
                    Name = $"S{item.ParentIndexNumber}:E{item.IndexNumber} - {item.Name}",
                    Url = MediaHelpers.SetImageUrl(item, "296", "526", JellyfinConstants.PrimaryName),
                    UserData = new UIUserData
                    {
                        IsFavorite = item.UserData.IsFavorite.Value,
                        HasBeenWatched = item.UserData.Played.Value,
                    },
                    Type = item.Type.Value,
                    CollectionType = item.CollectionType,
                };
            }

            var seasons = await apiClient.Shows[MediaItem.Id.Value].Seasons
                .GetAsync(option =>
                {
                    option.QueryParameters.UserId = user.Id;
                    option.QueryParameters.Fields = new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.MediaSourceCount,
                    };
                });

            SeriesMetadata = new ObservableCollection<UIMediaListItem>(
                seasons.Items.Select(x =>
                {
                    var item = new UIMediaListItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name,
                        Url = SetSeasonImageUrl(x),
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

        private string SetSeasonImageUrl(BaseItemDto item)
        {
            var baseUrl = memoryCache.Get<string>(JellyfinConstants.HostUrlName);
            var imageTags = item.ImageTags.AdditionalData;
            if (imageTags.ContainsKey(JellyfinConstants.PrimaryName))
            {
                return $"{baseUrl}/Items/{item.Id}/Images/{JellyfinConstants.PrimaryName}?fillHeight=446&fillWidth=298&quality=96&tag={imageTags[JellyfinConstants.PrimaryName]}";
            }

            return $"{baseUrl}/Items/{item.SeriesId}/Images/{JellyfinConstants.PrimaryName}?fillHeight=446&fillWidth=298&quality=96&tag={item.SeriesPrimaryImageTag}";
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task NextUpFavoriteStateAsync(CancellationToken cancellationToken)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

            if (NextUpItem != null)
            {
                if (NextUpItem.UserData.IsFavorite)
                {
                    _ = await apiClient.UserFavoriteItems[NextUpItem.Id]
                    .DeleteAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
                }
                else
                {
                    _ = await apiClient.UserFavoriteItems[NextUpItem.Id]
                    .PostAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
                }

                await LoadMediaInformationAsync(MediaItem.Id.Value);
            }
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task NextUpPlayedStateAsync(CancellationToken cancellationToken)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

            if (NextUpItem != null)
            {
                if (NextUpItem.UserData.HasBeenWatched)
                {
                    _ = await apiClient.UserPlayedItems[NextUpItem.Id]
                    .DeleteAsync(options =>
                    {
                        options.QueryParameters.UserId = user.Id;
                    });
                }
                else
                {
                    _ = await apiClient.UserPlayedItems[NextUpItem.Id]
                    .PostAsync(options =>
                     {
                         options.QueryParameters.UserId = user.Id;
                         options.QueryParameters.DatePlayed = DateTimeOffset.Now;
                     });
                }

                await LoadMediaInformationAsync(MediaItem.Id.Value);
            }
        }
    }
}
