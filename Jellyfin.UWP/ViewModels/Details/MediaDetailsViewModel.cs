using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels.Details
{
    internal partial class MediaDetailsViewModel : MediaViewModel
    {
        [ObservableProperty]
        private bool hasMultipleAudioStreams;

        [ObservableProperty]
        private bool hasMultipleSubtitleStreams;

        [ObservableProperty]
        private bool hasMultipleVideoStreams;

        [ObservableProperty]
        private bool hasSubtitle;

        [ObservableProperty]
        private ObservableCollection<UIMediaStream> audioStreams;

        [ObservableProperty]
        private string audioType;

        [ObservableProperty]
        private UIMediaStream selectedAudioStream;

        [ObservableProperty]
        private UIMediaStream selectedSubtitleStream;

        [ObservableProperty]
        private UIMediaStreamVideo selectedVideoStream;

        [ObservableProperty]
        private ObservableCollection<UIMediaStream> subtitleStreams;

        [ObservableProperty]
        private string subtitleType;

        [ObservableProperty]
        private ObservableCollection<UIMediaStreamVideo> videoStreams;

        [ObservableProperty]
        private string videoType;

        public MediaDetailsViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, IMediaHelpers mediaHelpers)
            : base(memoryCache, apiClient, mediaHelpers)
        {
        }

        protected override async Task ExtraExecuteAsync()
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

            if (HasMultipleVideoStreams)
            {
                SetVideoStreams();
            }

            if (HasMultipleAudioStreams)
            {
                SetAudioStreams();
            }

            await DetailsExtraExecuteAsync();
        }

        protected virtual Task DetailsExtraExecuteAsync()
        {
            return Task.CompletedTask;
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
}
