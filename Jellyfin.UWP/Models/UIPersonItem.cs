using System;

namespace Jellyfin.UWP.Models
{
    public sealed class UIPersonItem
    {
        public Guid Id { get; internal set; }

        public string Name { get; internal set; }

        public string Url { get; internal set; }

        public string Role { get; internal set; }
    }
}