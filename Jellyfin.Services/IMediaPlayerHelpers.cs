using Jellyfin.Sdk.Generated.Models;
using System.Collections.ObjectModel;

namespace Jellyfin.Services;

public interface IMediaPlayerHelpers
{
    public ReadOnlyDictionary<string, string> SupportedAudioCodecs { get; }

    public ReadOnlyDictionary<string, string> SupportedVideoCodecs { get; }

    public PlaybackInfoDto GetPlaybackInfoBody(UserDto user, long startTimeTicks, string? mediaSourceId, int? selectedAudioMediaStreamIndex);
}
