using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.UWP.Models
{
    public sealed class UIMainPageListItem : UIMediaListItem
    {
        public string TextLine1
        {
            get
            {
                if (Type == BaseItemDto_Type.Episode)
                {
                    return SeriesName ?? "N/A";
                }

                return Name;
            }
        }

        public string TextLine2
        {
            get
            {
                switch (Type)
                {
                    case BaseItemDto_Type.Episode:
                        {
                            if (ParentIndexNumber.HasValue || IndexNumber.HasValue)
                            {
                                return $"S{ParentIndexNumber ?? 0}:E{IndexNumber ?? 0} - {Name}";
                            } 

                            return Name;
                        }
                    case BaseItemDto_Type.AggregateFolder:
                    case BaseItemDto_Type.Audio:
                    case BaseItemDto_Type.AudioBook:
                    case BaseItemDto_Type.BasePluginFolder:
                    case BaseItemDto_Type.Book:
                    case BaseItemDto_Type.BoxSet:
                    case BaseItemDto_Type.Channel:
                    case BaseItemDto_Type.ChannelFolderItem:
                    case BaseItemDto_Type.CollectionFolder:
                    case BaseItemDto_Type.Folder:
                    case BaseItemDto_Type.Genre:
                    case BaseItemDto_Type.ManualPlaylistsFolder:
                    case BaseItemDto_Type.Movie:
                    case BaseItemDto_Type.LiveTvChannel:
                    case BaseItemDto_Type.LiveTvProgram:
                    case BaseItemDto_Type.MusicAlbum:
                    case BaseItemDto_Type.MusicArtist:
                    case BaseItemDto_Type.MusicGenre:
                    case BaseItemDto_Type.MusicVideo:
                    case BaseItemDto_Type.Person:
                    case BaseItemDto_Type.Photo:
                    case BaseItemDto_Type.PhotoAlbum:
                    case BaseItemDto_Type.Playlist:
                    case BaseItemDto_Type.PlaylistsFolder:
                    case BaseItemDto_Type.Program:
                    case BaseItemDto_Type.Recording:
                    case BaseItemDto_Type.Season:
                    case BaseItemDto_Type.Series:
                    case BaseItemDto_Type.Studio:
                    case BaseItemDto_Type.Trailer:
                    case BaseItemDto_Type.TvChannel:
                    case BaseItemDto_Type.TvProgram:
                    case BaseItemDto_Type.UserRootFolder:
                    case BaseItemDto_Type.UserView:
                    case BaseItemDto_Type.Video:
                    case BaseItemDto_Type.Year:
                    default:
                        {
                            return Year;
                        }
                }
            }
        }

        public string SeriesName { get; internal set; }
        public int? ParentIndexNumber { get; internal set; }
    }
}
