﻿using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels
{
    public sealed partial class EpisodeViewModel : ObservableObject
    {
        private readonly IMemoryCache memoryCache;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly ITvShowsClient tvShowsClient;
        private readonly IUserLibraryClient userLibraryClient;

        [ObservableProperty]
        private ObservableCollection<UIMediaStream> audioStreams;

        [ObservableProperty]
        private string audioType;

        [ObservableProperty]
        private ObservableCollection<UIPersonItem> castAndCrew;

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
        private ObservableCollection<UIMediaListItem> seriesEpisodes;

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

        public EpisodeViewModel(
            IMemoryCache memoryCache,
            IUserLibraryClient userLibraryClient,
            SdkClientSettings sdkClientSettings,
            ITvShowsClient tvShowsClient)
        {
            this.memoryCache = memoryCache;
            this.userLibraryClient = userLibraryClient;
            this.sdkClientSettings = sdkClientSettings;
            this.tvShowsClient = tvShowsClient;
        }

        public Guid GetPlayIdAsync()
        {
            return MediaItem.Id;
        }

        public async Task LoadMediaInformationAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>("user");
            var userLibraryItem = await userLibraryClient.GetItemAsync(user.Id, id);

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

                    SelectedSubtitleStream = SubtitleStreams.SingleOrDefault(x => x.IsSelected) ?? SubtitleStreams.First();
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
            Writer = string.Join(", ", MediaItem.People.Where(x => x.Role == "Writer" && x.Type == PersonKind.Writer).Select(x => x.Name));
            CastAndCrew = new ObservableCollection<UIPersonItem>(
                MediaItem.People
                .Where(x => x.Type == PersonKind.Actor)
                .Select(x => new UIPersonItem { Id = x.Id, Name = x.Name, Url = $"{sdkClientSettings.BaseUrl}/Items/{x.Id}/Images/Primary?fillHeight=446&fillWidth=298&quality=96&tag={x.PrimaryImageTag}", Role = x.Role, }));

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

            ImageUrl = SetImageUrl(MediaItem.Id, "720", "480", "Primary", MediaItem.ImageTags);

            var episodes = await tvShowsClient.GetEpisodesAsync(
                seriesId: MediaItem.SeriesId!.Value,
                userId: user.Id,
                seasonId: MediaItem.ParentId,
                fields: new[] { ItemFields.ItemCounts, ItemFields.PrimaryImageAspectRatio, });

            SeriesEpisodes = new ObservableCollection<UIMediaListItem>(
                episodes.Items.Select(x => new UIMediaListItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Url = SetImageUrl(x.Id, "505", "349", "Primary", x.ImageTags),
                    HasBeenWatched = x.UserData.Played,
                    IndexNumber = x.IndexNumber,
                }));
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

        private string SetImageUrl(Guid id, string height, string width, string tagKey, IDictionary<string, string> imageTages)
        {
            if (imageTages is null || imageTages.Count == 0 || !imageTages.ContainsKey(tagKey))
            {
                return string.Empty;
            }

            var imageTagId = imageTages[tagKey];

            return $"{sdkClientSettings.BaseUrl}/Items/{id}/Images/Primary?fillHeight={height}&fillWidth={width}&quality=96&tag={imageTagId}";
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
