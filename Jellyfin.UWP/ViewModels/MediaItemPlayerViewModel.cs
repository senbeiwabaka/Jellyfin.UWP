using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Jellyfin.UWP
{
    public sealed partial class MediaItemPlayerViewModel : ObservableObject
    {
        private readonly IMemoryCache memoryCache;
        private readonly IVideosClient videosClient;
        private readonly IPlaystateClient playstateClient;

        private Guid id;

        public MediaItemPlayerViewModel(
            IMemoryCache memoryCache,
            IVideosClient videosClient,
            IPlaystateClient playstateClient)
        {
            this.memoryCache = memoryCache;
            this.videosClient = videosClient;
            this.playstateClient = playstateClient;
        }

        public Uri GetVideoUrl(Guid id)
        {
            this.id = id;

            var videoUrl = videosClient.GetVideoStreamUrl(id, container: "mp4", @static: true);

            return new Uri(videoUrl);
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
