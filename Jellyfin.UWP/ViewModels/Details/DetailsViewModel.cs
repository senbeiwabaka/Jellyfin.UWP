using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.Details
{
    internal partial class DetailsViewModel : ObservableObject
    {
        protected readonly ILibraryClient libraryClient;
        protected readonly IMemoryCache memoryCache;
        protected readonly IPlaystateClient playstateClient;
        protected readonly SdkClientSettings sdkClientSettings;
        protected readonly ITvShowsClient tvShowsClient;
        protected readonly IUserLibraryClient userLibraryClient;

        [ObservableProperty]
        private ObservableCollection<UIMediaStream> audioStreams;

        [ObservableProperty]
        private string audioType;

        [ObservableProperty]
        private ObservableCollection<UIPersonItem> castAndCrew;

        [ObservableProperty]
        private string director;

        [ObservableProperty]
        private string externalURLs;

        [ObservableProperty]
        private string genres;

        [ObservableProperty]
        private bool hasMultipleAudioStreams;

        [ObservableProperty]
        private bool hasMultipleSubtitleStreams;

        [ObservableProperty]
        private bool hasMultipleVideoStreams;

        [ObservableProperty]
        private bool hasSubtitle;

        [ObservableProperty]
        private string imageUrl;

        [ObservableProperty]
        private bool isEpisode;

        [ObservableProperty]
        private bool isMovie;

        [ObservableProperty]
        private bool isNotMovie;

        [ObservableProperty]
        private BaseItemDto mediaItem;

        [ObservableProperty]
        private string mediaTagLines;

        [ObservableProperty]
        private string mediaTags;

        [ObservableProperty]
        private string runTime;

        [ObservableProperty]
        private UIMediaStream selectedAudioStream;

        [ObservableProperty]
        private UIMediaStream selectedSubtitleStream;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> seriesMetadata;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> similiarMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaStream> subtitleStreams;

        [ObservableProperty]
        private string subtitleType;

        [ObservableProperty]
        private ObservableCollection<UIMediaStream> videoStreams;

        [ObservableProperty]
        private string videoType;

        [ObservableProperty]
        private string writer;

        public DetailsViewModel(
            IMemoryCache memoryCache,
            IUserLibraryClient userLibraryClient,
            ILibraryClient libraryClient,
            SdkClientSettings sdkClientSettings,
            ITvShowsClient tvShowsClient,
            IPlaystateClient playstateClient)
        {
            this.memoryCache = memoryCache;
            this.userLibraryClient = userLibraryClient;
            this.libraryClient = libraryClient;
            this.sdkClientSettings = sdkClientSettings;
            this.tvShowsClient = tvShowsClient;
            this.playstateClient = playstateClient;
        }

        public virtual Task<Guid> GetPlayIdAsync()
        {
            return MediaHelpers.GetPlayIdAsync(MediaItem, SeriesMetadata?.ToArray(), null);
        }

        public async Task LoadMediaInformationAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>("user");
            var userLibraryItem = await userLibraryClient.GetItemAsync(user.Id, id).ConfigureAwait(true);

            MediaItem = userLibraryItem;

            if (MediaItem.MediaStreams is not null)
            {
                VideoType = MediaItem.MediaStreams.FirstOrDefault(x => x.Type == MediaStreamType.Video)?.DisplayTitle;
                AudioType = MediaItem.MediaStreams.FirstOrDefault(x => x.Type == MediaStreamType.Audio && x.IsDefault)?.DisplayTitle;
                SubtitleType = MediaItem.MediaStreams.FirstOrDefault(x => x.Type == MediaStreamType.Subtitle && x.IsDefault)?.DisplayTitle;

                HasSubtitle = MediaItem.HasSubtitles.HasValue && MediaItem.HasSubtitles.Value;
                HasMultipleSubtitleStreams = MediaItem.MediaStreams.Count(x => x.Type == MediaStreamType.Subtitle) > 1;

                if (HasMultipleSubtitleStreams)
                {
                    SubtitleStreams = new ObservableCollection<UIMediaStream>(
                                      MediaItem.MediaStreams
                                      .Where(x => x.Type == MediaStreamType.Subtitle)
                                      .Select(x => new UIMediaStream
                                      {
                                          Index = x.Index,
                                          IsSelected = x.IsDefault,
                                          Title = x.DisplayTitle,
                                      }));

                    SelectedSubtitleStream = SubtitleStreams.SingleOrDefault(x => x.IsSelected) ?? SubtitleStreams[0];
                }
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
            Director = string.Join(", ", MediaItem.People.Where(x => x.Role == "Director" && x.Type == PersonKind.Director).Select(x => x.Name));
            Writer = string.Join(", ", MediaItem.People.Where(x => x.Role == "Writer" && x.Type == PersonKind.Writer).Select(x => x.Name));
            CastAndCrew = new ObservableCollection<UIPersonItem>(
                MediaItem.People
                .Where(x => x.Type == PersonKind.Actor)
                .Select(x => new UIPersonItem { Id = x.Id, Name = x.Name, Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=446&fillWidth=298&quality=96&tag={x.PrimaryImageTag}", Role = x.Role, }));

            if (MediaItem.Type == BaseItemKind.Movie)
            {
                IsMovie = true;

                if (MediaItem.MediaStreams.Count(x => x.Type == MediaStreamType.Video) > 1)
                {
                    HasMultipleVideoStreams = true;

                    SetVideoStreams();
                }

                if (MediaItem.MediaStreams.Count(x => x.Type == MediaStreamType.Audio) > 1)
                {
                    HasMultipleAudioStreams = true;

                    SetAudioStreams();
                }
            }

            if (MediaItem.Type == BaseItemKind.Episode)
            {
                IsEpisode = true;

                if (MediaItem.MediaStreams.Count(x => x.Type == MediaStreamType.Video) > 1)
                {
                    HasMultipleVideoStreams = true;

                    SetVideoStreams();
                }

                if (MediaItem.MediaStreams.Count(x => x.Type == MediaStreamType.Audio) > 1)
                {
                    HasMultipleAudioStreams = true;

                    SetAudioStreams();
                }
            }

            if (MediaItem.Type == BaseItemKind.Series)
            {
                IsNotMovie = true;
            }

            var similiarItems = await libraryClient.GetSimilarItemsAsync(
                MediaItem.Id,
                userId: user.Id,
                limit: 12,
                fields: new[] { ItemFields.PrimaryImageAspectRatio, });

            SimiliarMediaList = new ObservableCollection<UIMediaListItem>(
                similiarItems.Items
                .Select(x =>
                {
                    var item = new UIMediaListItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Url = SetImageUrl(x.Id, "446", "298", JellyfinConstants.PrimaryName, x.ImageTags),
                        Year = x.ProductionYear?.ToString() ?? "N/A",
                        UserData = new UIUserData
                        {
                            IsFavorite = x.UserData.IsFavorite,
                            UnplayedItemCount = x.UserData.UnplayedItemCount,
                            HasBeenWatched = x.UserData.Played,
                        },
                    };

                    return item;
                }));

            ImageUrl = SetImageUrl(MediaItem.Id, "720", "480", JellyfinConstants.PrimaryName, MediaItem.ImageTags);

            await ExtraExecuteAsync();
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task PlayedStateAsync(CancellationToken cancellationToken)
        {
            var user = memoryCache.Get<UserDto>("user");

            if (MediaItem.UserData.Played)
            {
                _ = await playstateClient.MarkUnplayedItemAsync(
                    user.Id,
                    MediaItem.Id,
                    cancellationToken: cancellationToken);
            }
            else
            {
                _ = await playstateClient.MarkPlayedItemAsync(
                    user.Id,
                    MediaItem.Id,
                    DateTimeOffset.Now,
                    cancellationToken: cancellationToken);
            }

            await LoadMediaInformationAsync(MediaItem.Id);
        }

        [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = false)]
        private async Task FavoriteStateAsync(CancellationToken cancellationToken)
        {
            var user = memoryCache.Get<UserDto>("user");

            if (MediaItem.UserData.IsFavorite)
            {
                _ = await userLibraryClient.UnmarkFavoriteItemAsync(
                    user.Id,
                    MediaItem.Id,
                    cancellationToken: cancellationToken);
            }
            else
            {
                _ = await userLibraryClient.MarkFavoriteItemAsync(
                    user.Id,
                    MediaItem.Id,
                    cancellationToken: cancellationToken);
            }

            await LoadMediaInformationAsync(MediaItem.Id);
        }

        protected virtual Task ExtraExecuteAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual string SetImageUrl(Guid id, string height, string width, string tagKey, IDictionary<string, string> imageTages)
        {
            if (imageTages is null || imageTages.Count == 0 || !imageTages.ContainsKey(tagKey))
            {
                return string.Empty;
            }

            var imageTagId = imageTages[tagKey];

            return $"{sdkClientSettings.BaseUrl}/Items/{id}/Images/{JellyfinConstants.PrimaryName}?fillHeight={height}&fillWidth={width}&quality=96&tag={imageTagId}";
        }

        private void SetAudioStreams()
        {
            var index = 0;

            AudioStreams = new ObservableCollection<UIMediaStream>(
                                   MediaItem.MediaStreams
                                   .Where(x => x.Type == MediaStreamType.Audio)
                                   .Select(x => new UIMediaStream
                                   {
                                       Index = index++,
                                       IsSelected = x.IsDefault,
                                       Title = x.DisplayTitle,
                                       MediaStreamIndex = x.Index,
                                   }));

            SelectedAudioStream = AudioStreams.Single(x => x.IsSelected);
        }

        private void SetVideoStreams()
        {
            VideoStreams = new ObservableCollection<UIMediaStream>(
                                   MediaItem.MediaStreams
                                   .Where(x => x.Type == MediaStreamType.Video)
                                   .Select(x => new UIMediaStream
                                   {
                                       Index = x.Index,
                                       IsSelected = x.IsDefault,
                                       Title = x.DisplayTitle,
                                   }));
        }
    }
}
