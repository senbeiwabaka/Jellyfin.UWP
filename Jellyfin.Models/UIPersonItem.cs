using Jellyfin.Sdk.Generated.Models;

namespace Jellyfin.Models;

public sealed class UIPersonItem
{
    public Guid Id { get; set; }

    public required BaseItemPerson_Type Type { get; set; }

    public string Name { get; set; } = default!;

    public string ImageUrl { get; set; } = default!;

    public string Role { get; set; } = default!;
}
