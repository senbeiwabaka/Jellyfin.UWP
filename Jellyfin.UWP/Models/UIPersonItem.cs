namespace Jellyfin.UWP.Models
{
    public sealed class UIPersonItem : UIItem
    {
        public string Name { get; internal set; }

        public string Url { get; internal set; }

        public string Role { get; internal set; }
    }
}
