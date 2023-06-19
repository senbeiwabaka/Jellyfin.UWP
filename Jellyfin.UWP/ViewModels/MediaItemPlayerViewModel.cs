using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.UWP
{
    public sealed partial class MediaItemPlayerViewModel : ObservableObject
    {
        private readonly IMediaInfoClient mediaInfoClient;
        private readonly IMemoryCache memoryCache;
        private readonly IPlaystateClient playstateClient;
        private readonly ISubtitleClient subtitleClient;
        private readonly IVideosClient videosClient;
        private Guid id;

        public MediaItemPlayerViewModel(
            IMemoryCache memoryCache,
            IVideosClient videosClient,
            IPlaystateClient playstateClient,
            IMediaInfoClient mediaInfoClient,
            ISubtitleClient subtitleClient)
        {
            this.memoryCache = memoryCache;
            this.videosClient = videosClient;
            this.playstateClient = playstateClient;
            this.mediaInfoClient = mediaInfoClient;
            this.subtitleClient = subtitleClient;
        }

        public string GetSubtitleUrl(Guid id, int index, string routeFormat)
        {
            var routeId = id.ToString().Replace("-", string.Empty);

            return subtitleClient.GetSubtitleWithTicksUrl(id, routeId, index, 0, routeFormat);
        }

        public Uri GetVideoUrl(Guid id, int? subtitleIndex = null, string container = null)
        {
            this.id = id;

            var videoUrl = videosClient.GetVideoStreamUrl(
                id,
                container: "mp4",
                @static: true,
                subtitleStreamIndex: subtitleIndex,
                subtitleMethod: SubtitleDeliveryMethod.External);

            return new Uri(videoUrl);
        }

        public async Task<MediaSourceInfo> LoadMediaPlaybackInfoAsync(Guid id)
        {
            var user = memoryCache.Get<UserDto>("user");
            var playbackInfo = await mediaInfoClient.GetPostedPlaybackInfoAsync(
                id,
                body: new PlaybackInfoDto
                {
                    UserId = user.Id,
                    StartTimeTicks = 0,
                    EnableDirectPlay = true,
                    AutoOpenLiveStream = true,
                    SubtitleStreamIndex = 0,
                });

            return playbackInfo.MediaSources.Single();
        }

        public async Task SessionPlayingAsync()
        {
            var session = memoryCache.Get<SessionInfo>("session");

            await playstateClient.ReportPlaybackStartAsync(
                new PlaybackStartInfo
                {
                    MediaSourceId = id.ToString(),
                    ItemId = id,
                    SessionId = session.Id,
                });
        }

        public async Task SessionProgressAsync(long position)
        {
            var session = memoryCache.Get<SessionInfo>("session");

            await playstateClient.ReportPlaybackProgressAsync(
                new PlaybackProgressInfo
                {
                    MediaSourceId = id.ToString(),
                    SessionId = session.Id,
                    ItemId = id,
                    PositionTicks = position,
                });
        }

        public async Task SessionStopAsync(long position)
        {
            var session = memoryCache.Get<SessionInfo>("session");

            await playstateClient.ReportPlaybackStoppedAsync(
                new PlaybackStopInfo
                {
                    PositionTicks = position,
                    ItemId = id,
                    SessionId = session.Id,
                    MediaSourceId = id.ToString(),
                });
        }
    }
}
