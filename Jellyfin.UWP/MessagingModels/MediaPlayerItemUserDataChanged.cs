using CommunityToolkit.Mvvm.Messaging.Messages;
using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.UWP.MessagingModels;

internal sealed class MediaPlayerItemUserDataChanged : ValueChangedMessage<UserItemDataDto>
{
    public MediaPlayerItemUserDataChanged(UserItemDataDto value) : base(value)
    {
    }
}
