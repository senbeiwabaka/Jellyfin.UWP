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
    internal partial class DetailsViewModel : ObservableObject
    {
        protected readonly IMemoryCache memoryCache;
        protected readonly JellyfinApiClient apiClient;

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
        private UIMediaStreamVideo selectedVideoStream;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> seriesMetadata;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> similiarMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaStream> subtitleStreams;

        [ObservableProperty]
        private string subtitleType;

        [ObservableProperty]
        private ObservableCollection<UIMediaStreamVideo> videoStreams;

        [ObservableProperty]
        private string videoType;

        [ObservableProperty]
        private string writer;

        public DetailsViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
            MediaHelpers = mediaHelpers;
        }

        protected IMediaHelpers MediaHelpers { get; }

        public virtual Task<Guid> GetPlayIdAsync()
        {
            return MediaHelpers.GetPlayIdAsync(MediaItem, SeriesMetadata?.ToArray(), null);
        }

        public async Task LoadMediaInformationAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
            var userLibraryItem = await apiClient.Items[id]
                .GetAsync(options =>
                {
                    options.QueryParameters.UserId = user.Id;
                });

            MediaItem = userLibraryItem;

            if (MediaItem.MediaSourceCount > 0)
            {
                VideoType = MediaItem.MediaSources?[0].MediaStreams?[0].DisplayTitle;
            }
            else
            {
                VideoType = MediaItem.MediaStreams?.Find(x => x.Type == MediaStream_Type.Video && x.IsDefault.Value)?.DisplayTitle;

                SelectedVideoStream = new UIMediaStreamVideo
                {
                    Index = 0,
                    IsSelected = true,
                };
            }

            if (MediaItem.MediaStreams is not null)
            {
                AudioType = MediaItem.MediaStreams.Find(x => x.Type == MediaStream_Type.Audio && x.IsDefault.Value)?.DisplayTitle;
                SubtitleType = MediaItem.MediaStreams.Find(x => x.Type == MediaStream_Type.Subtitle && x.IsDefault.Value)?.DisplayTitle;

                HasSubtitle = MediaItem.HasSubtitles.HasValue && MediaItem.HasSubtitles.Value;
                HasMultipleSubtitleStreams = MediaItem.MediaStreams.Count(x => x.Type == MediaStream_Type.Subtitle) > 1;

                if (HasMultipleSubtitleStreams)
                {
                    SubtitleStreams = new ObservableCollection<UIMediaStream>(
                                      MediaItem.MediaStreams
                                      .Where(x => x.Type == MediaStream_Type.Subtitle)
                                      .Select(x => new UIMediaStream
                                      {
                                          Index = x.Index.Value,
                                          IsSelected = x.IsDefault.Value,
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

            if (MediaItem.Type == BaseItemDto_Type.Movie || MediaItem.Type == BaseItemDto_Type.Episode)
            {
                if (MediaItem.MediaSourceCount > 1)
                {
                    HasMultipleVideoStreams = true;

                    SetVideoStreams();
                }

                if (MediaItem.MediaStreams.Count(x => x.Type == MediaStream_Type.Audio) > 1)
                {
                    HasMultipleAudioStreams = true;

                    SetAudioStreams();
                }
            }

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

            ImageUrl = MediaHelpers.SetImageUrl(MediaItem, "720", "480", JellyfinConstants.PrimaryName); //MediaHelpers.SetImageUrl(MediaItem, "720", "480", JellyfinConstants.PrimaryName);

            await ExtraExecuteAsync();
        }

        protected virtual Task ExtraExecuteAsync()
        {
            return Task.CompletedTask;
        }

        [RelayCommand]
        private void ChangeVideoSelection()
        {
            SetAudioStreams();
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

        private void SetAudioStreams()
        {
            var index = 0;

            AudioStreams = new ObservableCollection<UIMediaStream>(
                                   MediaItem.MediaSources[SelectedVideoStream.Index]
                                   .MediaStreams
                                   .Where(x => x.Type == MediaStream_Type.Audio)
                                   .Select(x => new UIMediaStream
                                   {
                                       Index = index++,
                                       IsSelected = x.IsDefault.Value,
                                       Title = x.DisplayTitle,
                                       MediaStreamIndex = x.Index.Value,
                                   }));

            SelectedAudioStream = AudioStreams.Single(x => x.IsSelected);
        }

        private void SetVideoStreams()
        {
            var index = 0;

            VideoStreams = new ObservableCollection<UIMediaStreamVideo>(
                                   MediaItem.MediaSources
                                   .Select(x => new UIMediaStreamVideo
                                   {
                                       Index = index++,
                                       Title = x.Name,
                                       VideoId = x.Id,
                                   }));

            VideoStreams[0].IsSelected = true;

            SelectedVideoStream = VideoStreams.Single(x => x.IsSelected);
        }
    }
}
