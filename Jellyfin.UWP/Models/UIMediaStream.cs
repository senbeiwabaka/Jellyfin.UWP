using WinRT;

namespace Jellyfin.UWP.Models
{
    [GeneratedBindableCustomProperty(propertyNames: [nameof(IsSelected), nameof(Title)], indexerPropertyTypes: [typeof(bool), typeof(string)])]
    public partial class UIMediaStream
    {
        public string Title { get; set; } = default!;

        public int MediaSourceIndex { get; set; }

        public bool IsSelected { get; set; }

        public int MediaStreamIndex { get; set; }
    }
}
