using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Jellyfin.UWP.Models;

internal sealed class WeakRefMessage : ValueChangedMessage<string>
{
    public WeakRefMessage(string value) : base(value)
    {
    }
}
