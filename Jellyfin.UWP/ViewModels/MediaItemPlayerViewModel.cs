using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jellyfin.UWP
{
    public sealed partial class MediaItemPlayerViewModel : ObservableObject
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly IMemoryCache memoryCache;

        public MediaItemPlayerViewModel(IHttpClientFactory httpClientFactory, SdkClientSettings sdkClientSettings, IMemoryCache memoryCache)
        {
            this.httpClientFactory = httpClientFactory;
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
        }

        public Uri GetVideoUrl(Guid id)
        {
            var httpClient = httpClientFactory.CreateClient();
            var videosClient = new VideosClient(sdkClientSettings, httpClient);
            var videoUrl = videosClient.GetVideoStreamUrl(id, container: "mp4", @static: true);

            return new Uri(videoUrl);
        }

        public async Task<Stream> GetVideoUrlV2(Guid id)
        {
            var httpClient = httpClientFactory.CreateClient();
            var videosClient = new VideosClient(sdkClientSettings, httpClient);
            var videoUrl = await videosClient.GetVideoStreamAsync(id);

            return videoUrl.Stream;
        }
    }
}