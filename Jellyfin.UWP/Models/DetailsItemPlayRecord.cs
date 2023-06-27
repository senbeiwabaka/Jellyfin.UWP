using System;

namespace Jellyfin.UWP.Models
{
    public sealed class DetailsItemPlayRecord
    {
        public Guid Id { get; set; }

        public int? SelectedAudioIndex { get; set; }

        public int? SelectedMediaStreamIndex { get; set; }
    }
}
