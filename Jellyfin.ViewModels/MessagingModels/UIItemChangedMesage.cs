using CommunityToolkit.Mvvm.Messaging.Messages;
using Jellyfin.Models;

namespace Jellyfin.ViewModels.MessagingModels;

public sealed class UIItemChangedMesage(UIItem value) : ValueChangedMessage<UIItem>(value)
{
}
