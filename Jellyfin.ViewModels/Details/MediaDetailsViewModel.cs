using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Models;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ViewModels.Details;

public partial class MediaDetailsViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaViewModel(memoryCache, apiClient, mediaHelpers)
{
    [ObservableProperty]
    public partial ObservableCollection<UIMediaStream> AudioStreams { get; set; }

    [ObservableProperty]
    public partial string? AudioType { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIPersonItem> CastAndCrew { get; set; }

    [ObservableProperty]
    public partial string Genres { get; set; }

    [ObservableProperty]
    public partial bool HasMultipleAudioStreams { get; set; }

    [ObservableProperty]
    public partial bool HasMultipleVideoStreams { get; set; }

    [ObservableProperty]
    public partial bool HasSubtitle { get; set; }

    [ObservableProperty]
    public partial bool IsEpisode { get; set; }

    [ObservableProperty]
    public partial bool IsMovie { get; set; }

    [ObservableProperty]
    public partial bool IsNotMovie { get; set; }

    [ObservableProperty]
    public partial string MediaTagLines { get; set; }

    [ObservableProperty]
    public partial string RunTime { get; set; }

    [ObservableProperty]
    public partial UIMediaStream SelectedAudioStream { get; set; } = new UIMediaStream();

    [ObservableProperty]
    public partial UIMediaStream SelectedSubtitleStream { get; set; }

    [ObservableProperty]
    public partial UIMediaStreamVideo SelectedVideoStream { get; set; } = new UIMediaStreamVideo();

    [ObservableProperty]
    public partial ObservableCollection<UIMediaStream> SubtitleStreams { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaStreamVideo> VideoStreams { get; set; }

    [ObservableProperty]
    public partial string? VideoType { get; set; }

    public DetailsItemPlayRecord DetailsItemPlayRecord { get; internal set; } = new DetailsItemPlayRecord();

    internal override async Task FavoriteStateAsync(CancellationToken cancellationToken)
    {
        await ChangeFavoriteStateAsync(MediaItem.Id.Value, MediaItem.UserData.IsFavorite.Value, cancellationToken);

        await LoadMediaInformationAsync(MediaItem.Id.Value, cancellationToken);
    }

    internal override async Task PlayedStateAsync(CancellationToken cancellationToken)
    {
        await ChangePlayStateAsync(MediaItem.Id.Value, MediaItem.UserData.Played.Value, cancellationToken);

        await LoadMediaInformationAsync(MediaItem.Id.Value, cancellationToken);
    }

    protected virtual Task DetailsExtraExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    protected override async Task ExtraExecuteAsync(CancellationToken cancellationToken = default)
    {
        IsMovie = MediaItem.Type == BaseItemDto_Type.Movie;
        IsEpisode = MediaItem.Type == BaseItemDto_Type.Episode;
        IsNotMovie = MediaItem.Type == BaseItemDto_Type.Series;

        HandleMediaSources();

        Genres = string.Join(", ", MediaItem.Genres ?? []);

        if (MediaItem.Taglines is not null && MediaItem.Taglines.Count != 0)
        {
            MediaTagLines = string.Join("", MediaItem.Taglines);
        }

        if (MediaItem.People is not null)
        {
            CastAndCrew = [.. MediaItem.People
                .Where(x => x.Type == BaseItemPerson_Type.Actor)
                .Select(x =>
                    new UIPersonItem
                    {
                        Id = x.Id.Value,
                        Name = x.Name ?? "No Name Found",
                        ImageUrl = MediaHelpers.SetImageUrl(x, "446", "298"),
                        Role = !string.IsNullOrWhiteSpace(x.Role) ? $"as {x.Role}" : string.Empty,
                        Type = x.Type ?? BaseItemPerson_Type.Unknown,
                    })];
        }

        if (MediaItem.RunTimeTicks.HasValue)
        {
            var time = new TimeSpan(MediaItem.RunTimeTicks.Value);
            RunTime = $"{time.Hours}h{time.Minutes}m";
        }

        await DetailsExtraExecuteAsync(cancellationToken);
    }

    [RelayCommand]
    private void ChangeVideoSelection()
    {
        var mediaSource = MediaItem.MediaSources![SelectedVideoStream.MediaSourceListIndex]!;
        var mediaSreams = mediaSource.MediaStreams!;

        VideoType = mediaSreams.Find(x => x.Type == MediaStream_Type.Video && (x.IsDefault.HasValue && x.IsDefault.Value))?.DisplayTitle;

        HasMultipleAudioStreams = mediaSreams.Count(x => x.Type == MediaStream_Type.Audio) > 1;
        HasSubtitle = mediaSreams.Any(x => x.Type == MediaStream_Type.Subtitle);

        DetailsItemPlayRecord.SelectedVideoId = mediaSource.Id;

        if (HasMultipleAudioStreams)
        {
            SetAudioStreams([.. mediaSreams]);
        }
        else
        {
            AudioType = mediaSreams.Find(x => x.Type == MediaStream_Type.Audio)?.DisplayTitle;
        }

        if (HasSubtitle)
        {
            SetSubtitleStreams([.. mediaSreams]);
        }
    }

    [RelayCommand]
    private void ChangeAudioSelection()
    {
        DetailsItemPlayRecord.SelectedAudioIndex = SelectedAudioStream?.MediaStreamIndex;
    }

    private void HandleMediaSources()
    {
        if (MediaItem.MediaSources is not null && MediaItem.MediaSources.Any(x => x.MediaStreams is not null))
        {
            var mediaSource = MediaItem.MediaSources.First(x => x.MediaStreams is not null)!;
            var mediaStreams = mediaSource.MediaStreams!;

            VideoType = mediaStreams.Find(x => x.Type == MediaStream_Type.Video && (x.IsDefault.HasValue && x.IsDefault.Value))?.DisplayTitle;
            AudioType = mediaStreams.Find(x => x.Type == MediaStream_Type.Audio && (x.IsDefault.HasValue && x.IsDefault.Value))?.DisplayTitle;
            HasSubtitle = mediaStreams.Any(x => x.Type == MediaStream_Type.Subtitle);

            HasMultipleVideoStreams = MediaItem.MediaSources?.Count(x => x.MediaStreams is not null) > 1;
            HasMultipleAudioStreams = mediaStreams.Count(x => x.Type == MediaStream_Type.Audio) > 1;

            DetailsItemPlayRecord.SelectedVideoId = mediaSource.Id;

            if (HasMultipleVideoStreams)
            {
                SetVideoStreams();
            }

            if (HasMultipleAudioStreams)
            {
                SetAudioStreams([.. mediaStreams]);
            }

            if (HasSubtitle)
            {
                SetSubtitleStreams([.. mediaStreams]);
            }
        }
    }

    private void SetAudioStreams(List<MediaStream> mediaStreams)
    {
        AudioStreams = new ObservableCollection<UIMediaStream>(mediaStreams
            .Where(x => x.Type == MediaStream_Type.Audio)
            .Select(x => new UIMediaStream
            {
                IsDefault = x.IsDefault ?? false,
                Title = x.DisplayTitle ?? "No Title Found",
                MediaStreamIndex = x.Index ?? 0,
            }));

        SelectedAudioStream = AudioStreams.SingleOrDefault(x => x.IsDefault) ?? AudioStreams[0];
    }

    private void SetSubtitleStreams(List<MediaStream> mediaStreams)
    {
        SubtitleStreams = new ObservableCollection<UIMediaStream>(mediaStreams
            .Where(x => x.Type == MediaStream_Type.Subtitle)
            .Select(x => new UIMediaStream
            {
                IsDefault = x.IsDefault ?? false,
                Title = x.DisplayTitle ?? "No Title Found",
                MediaStreamIndex = x.Index ?? default,
            }));

        SelectedSubtitleStream = SubtitleStreams.SingleOrDefault(x => x.IsDefault) ?? SubtitleStreams[0];
    }

    private void SetVideoStreams()
    {
        var index = 0;

        VideoStreams = [.. MediaItem.MediaSources!
            .Select(x => new UIMediaStreamVideo
            {
                MediaSourceListIndex = index++,
                Title = x.Name ?? "No Title Found",
                MediaSourceId = x.Id??string.Empty,
            })];

        SelectedVideoStream = VideoStreams[0];
    }
}
