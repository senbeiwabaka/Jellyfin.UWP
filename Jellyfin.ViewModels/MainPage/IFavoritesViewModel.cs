using Jellyfin.Models;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ViewModels.MainPage;

public interface IFavoritesViewModel
{
    Task<ObservableCollection<UIMainPageListItem>> GetEpisodesAsync(CancellationToken cancellationToken = default);

    Task<ObservableCollection<UIMediaListItem>> GetMoviesAsync(CancellationToken cancellationToken = default);

    Task<ObservableCollection<UIMediaListItem>> GetSeriesAsync(CancellationToken cancellationToken = default);
}
