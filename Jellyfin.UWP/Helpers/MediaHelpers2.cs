using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.UWP.Helpers
{
    internal sealed class MediaHelpers2 : IMediaHelpers
    {
        private readonly IMemoryCache memoryCache;
        private readonly JellyfinApiClient apiClient;

        public MediaHelpers2(IMemoryCache memoryCache, JellyfinApiClient apiClient)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
        }
        public Task<Guid> GetPlayIdAsync(UIMediaListItem mediaItem)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> GetPlayIdAsync(BaseItemDto mediaItem, UIMediaListItem[] seasonsData, Guid? seriesNextUpId)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> GetSeriesIdFromEpisodeIdAsync(Guid episodeId)
        {
            throw new NotImplementedException();
        }
    }
}
