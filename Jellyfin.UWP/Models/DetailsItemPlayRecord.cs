using System;

namespace Jellyfin.UWP.Models
{
    public sealed class DetailsItemPlayRecord
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the selected audio index for UI visualization.
        /// </summary>
        public int? SelectedAudioIndex { get; set; }

        /// <summary>
        /// Gets or sets the selected audio index for Jellyfin data.
        /// </summary>
        public int? SelectedAudioMediaStreamIndex { get; set; }

        public int? SelectedVideoIndex { get; set; }

        public int? SelectedVideoMediaStreamIndex { get; set; }
    }
}
