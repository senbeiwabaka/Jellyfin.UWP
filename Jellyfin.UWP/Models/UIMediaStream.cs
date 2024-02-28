using System;

namespace Jellyfin.UWP.Models
{
    public class UIMediaStream
    {
        public string Title { get; set; }

        public int Index { get; set; }

        public bool IsSelected { get; set; }

        public int MediaStreamIndex { get; set; }
    }
}
