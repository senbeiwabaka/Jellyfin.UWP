using CommunityToolkit.Mvvm.Messaging.Messages;
using Jellyfin.Models;
using System;

namespace Jellyfin.ViewModels.MessagingModels
{
    public sealed class UIMediaListItemRequestMessage(Guid itemId) : AsyncRequestMessage<UIMediaListItem>
    {
        public Guid ItemId { get; } = itemId;
    }
}
