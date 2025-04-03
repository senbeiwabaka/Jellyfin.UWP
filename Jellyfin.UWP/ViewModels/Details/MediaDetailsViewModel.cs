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

namespace Jellyfin.UWP.ViewModels.Details;

internal partial class MediaDetailsViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaViewModel(memoryCache, apiClient, mediaHelpers)
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
    public partial string MediaTagLines { get; set; }

    [ObservableProperty]
    public partial string RunTime { get; set; }

    [ObservableProperty]
    public partial UIMediaStream SelectedAudioStream { get; set; }

    [ObservableProperty]
    public partial UIMediaStream SelectedSubtitleStream { get; set; }

    [ObservableProperty]
    public partial UIMediaStreamVideo SelectedVideoStream { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaStream> SubtitleStreams { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaStreamVideo> VideoStreams { get; set; }

    [ObservableProperty]
    public partial string? VideoType { get; set; }

    internal override async Task FavoriteStateAsync(CancellationToken cancellationToken)
    {
        await ChangeFavoriteStateAsync(MediaItem.Id.Value, MediaItem.UserData.IsFavorite.Value, cancellationToken);

        await LoadMediaInformationAsync(MediaItem.Id.Value);
    }

    internal override async Task PlayedStateAsync(CancellationToken cancellationToken)
    {
        await ChangePlayStateAsync(MediaItem.Id.Value, MediaItem.UserData.Played.Value, cancellationToken);

        await LoadMediaInformationAsync(MediaItem.Id.Value);
    }

    protected virtual Task DetailsExtraExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    protected override async Task ExtraExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (MediaItem.MediaSources is not null && MediaItem.MediaSources.Any(x => x.MediaStreams is not null))
        {
            var mediaSource = MediaItem.MediaSources.First(x => x.MediaStreams is not null);

            VideoType = mediaSource.MediaStreams?.Find(x => x.Type == MediaStream_Type.Video && (x.IsDefault.HasValue && x.IsDefault.Value))?.DisplayTitle;
            AudioType = mediaSource.MediaStreams?.Find(x => x.Type == MediaStream_Type.Audio && (x.IsDefault.HasValue && x.IsDefault.Value))?.DisplayTitle;
            HasSubtitle = mediaSource.MediaStreams?.Any(x => x.Type == MediaStream_Type.Subtitle) ?? false;

            HasMultipleVideoStreams = MediaItem.MediaSources.Where(x => x.MediaStreams is not null).SelectMany(x => x.MediaStreams!).Count(x => x.Type == MediaStream_Type.Video) > 1;
            HasMultipleAudioStreams = MediaItem.MediaSources.Where(x => x.MediaStreams is not null).SelectMany(x => x.MediaStreams!).Count(x => x.Type == MediaStream_Type.Audio) > 1;

            SelectedVideoStream = new UIMediaStreamVideo
            {
                IsSelected = true,
                MediaSourceIndex = 0,
                MediaStreamIndex = mediaSource.MediaStreams!.Single(x => x.Type == MediaStream_Type.Video).Index ?? default,
            };
        }

        if (HasMultipleVideoStreams)
        {
            SetVideoStreams();
        }

        if (HasMultipleAudioStreams)
        {
            SetAudioStreams();
        }

        if (HasSubtitle)
        {
            SetSubtitleStreams();
        }

        Genres = string.Join(", ", MediaItem.Genres ?? []);

        if (MediaItem.Taglines is not null && MediaItem.Taglines.Count != 0)
        {
            MediaTagLines = string.Join("", MediaItem.Taglines);
        }

        if (MediaItem.People is not null)
        {
            CastAndCrew = [.. MediaItem.People
                .Where(x => x.Type == BaseItemPerson_Type.Actor)
                .Select(x => new UIPersonItem
                {
                    Id = x.Id.Value,
                    Name = x.Name ?? "No Name Found",
                    ImageUrl = MediaHelpers.SetImageUrl(x, "446", "298"),
                    Role = x.Role ?? string.Empty,
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
        SetAudioStreams();
        SetSubtitleStreams();
    }

    private void SetAudioStreams()
    {
        AudioStreams = [.. MediaItem.MediaSources![SelectedVideoStream.MediaSourceIndex]!.MediaStreams!
            .Where(x => x.Type == MediaStream_Type.Audio)
            .Select(x => new UIMediaStream
            {
                MediaSourceIndex = SelectedVideoStream.MediaSourceIndex,
                IsSelected = x.IsDefault ?? false,
                Title = x.DisplayTitle ?? "No Title Found",
                MediaStreamIndex = x.Index ?? 0,
            })];

        SelectedAudioStream = AudioStreams.Single(x => x.IsSelected);
    }

    private void SetSubtitleStreams()
    {
        HasSubtitle = MediaItem.MediaSources![SelectedVideoStream.MediaSourceIndex].MediaStreams?.Any(x => x.Type == MediaStream_Type.Subtitle) ?? false;

        if (HasSubtitle)
        {
            SubtitleStreams = [.. MediaItem.MediaSources![SelectedVideoStream.MediaSourceIndex]!.MediaStreams!
            .Where(x => x.Type == MediaStream_Type.Subtitle)
            .Select(x => new UIMediaStream
            {
                MediaSourceIndex = SelectedVideoStream.MediaSourceIndex,
                IsSelected = x.IsDefault ?? false,
                Title = x.DisplayTitle ?? "No Title Found",
                MediaStreamIndex = x.Index ?? default,
            })];

            SelectedSubtitleStream = SubtitleStreams.SingleOrDefault(x => x.IsSelected) ?? SubtitleStreams[0];
        }
    }

    private void SetVideoStreams()
    {
        var index = 0;

        VideoStreams = [.. MediaItem.MediaSources!
            .Select(x => new UIMediaStreamVideo
            {
                MediaSourceIndex = index++,
                Title = x.Name ?? "No Title Found",
                VideoId = x.Id,
                MediaStreamIndex = x.MediaStreams.Single(y => y.Type == MediaStream_Type.Video).Index ?? default,
            })];

        VideoStreams[0].IsSelected = true;

        SelectedVideoStream = VideoStreams.Single(x => x.IsSelected);
    }
}
