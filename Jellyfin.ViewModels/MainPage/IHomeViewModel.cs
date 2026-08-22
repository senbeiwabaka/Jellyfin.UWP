using CommunityToolkit.Mvvm.Collections;
using Jellyfin.Models;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ViewModels.MainPage;

public interface IHomeViewModel
{
    Task<ObservableGroupedCollection<MediaGroupItem, UIMediaListItem>> LoadLatestAsync(ObservableCollection<UIMediaListItem> mediaList, CancellationToken cancellationToken = default);

    Task<ObservableCollection<UIMediaListItem>> LoadMediaListAsync(CancellationToken cancellationToken = default);

    Task<ObservableCollection<UIMediaListItemSeries>> LoadNextUpAsync(CancellationToken cancellationToken = default);

    Task<(ObservableCollection<UIMainPageListItem>, bool)> LoadResumeItemsAsync(CancellationToken cancellationToken = default);
}
