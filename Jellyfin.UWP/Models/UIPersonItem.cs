using Jellyfin.Sdk.Generated.Models;
using System;

namespace Jellyfin.UWP.Models;

internal sealed class UIPersonItem
{
    public Guid Id { get; internal set; }

    public required BaseItemPerson_Type Type { get; set; }

    public string Name { get; internal set; } = default!;

    public string ImageUrl { get; internal set; } = default!;

    public string Role { get; internal set; } = default!;
}
