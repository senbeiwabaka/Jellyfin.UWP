namespace Jellyfin.Models.Filters;

public sealed class GenreFiltersModel
{
    public string Name { get; set; } = default!;

    public Guid Id { get; set; }

    public bool IsSelected { get; set; }
}