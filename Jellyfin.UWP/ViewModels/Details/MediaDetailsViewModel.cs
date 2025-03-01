using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels.Details;

internal partial class MediaDetailsViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers) : MediaViewModel(memoryCache, apiClient, mediaHelpers)
{
    [ObservableProperty]
    public partial ObservableCollection<UIMediaStream> AudioStreams { get; set; }

    [ObservableProperty]
    public partial string AudioType { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIPersonItem> CastAndCrew { get; set; }

    [ObservableProperty]
    public partial string Genres { get; set; }

    [ObservableProperty]
    public partial bool HasMultipleAudioStreams { get; set; }

    [ObservableProperty]
    public partial bool HasMultipleSubtitleStreams { get; set; }

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
    public partial string SubtitleType { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaStreamVideo> VideoStreams { get; set; }

    [ObservableProperty]
    public partial string VideoType { get; set; }

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

            HasMultipleVideoStreams = MediaItem.MediaStreams.Count(x => x.Type == MediaStream_Type.Video) > 1;
            HasMultipleAudioStreams = MediaItem.MediaStreams.Count(x => x.Type == MediaStream_Type.Audio) > 1;
        }

        if (HasMultipleSubtitleStreams)
        {
            SubtitleStreams = [.. MediaItem.MediaStreams
                              .Where(x => x.Type == MediaStream_Type.Subtitle)
                              .Select(x => new UIMediaStream
                              {
                                  Index = x.Index.Value,
                                  IsSelected = x.IsDefault.Value,
                                  Title = x.DisplayTitle,
                              })];

            SelectedSubtitleStream = SubtitleStreams.SingleOrDefault(x => x.IsSelected) ?? SubtitleStreams[0];
        }

        if (HasMultipleVideoStreams)
        {
            SetVideoStreams();
        }

        if (HasMultipleAudioStreams)
        {
            SetAudioStreams();
        }

        Genres = string.Join(", ", MediaItem.Genres ?? []);

        if (MediaItem.Taglines.Count != 0)
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
                        Name = x.Name,
                        ImageUrl = MediaHelpers.SetImageUrl(x, "446", "298"), // MediaHelpers.SetImageUrl(x, "446", "298"),
                        Role = x.Role,
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
