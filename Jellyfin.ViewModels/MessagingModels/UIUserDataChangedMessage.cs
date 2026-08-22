using CommunityToolkit.Mvvm.Messaging.Messages;
using Jellyfin.Models;

namespace Jellyfin.ViewModels.MessagingModels;

public sealed class UIUserDataChangedMessage(UIUserData userData) : ValueChangedMessage<UIUserData>(userData)
{
}
