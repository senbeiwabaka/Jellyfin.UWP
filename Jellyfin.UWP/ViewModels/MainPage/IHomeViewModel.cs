using CommunityToolkit.Mvvm.Collections;
using Jellyfin.UWP.Models;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.MainPage
{
    internal interface IHomeViewModel
    {
        Task<ObservableGroupedCollection<MediaGroupItem, UIMediaListItem>> LoadLatestAsync(ObservableCollection<UIMediaListItem> mediaList, CancellationToken cancellationToken = default);

        Task<ObservableCollection<UIMediaListItem>> LoadMediaListAsync(CancellationToken cancellationToken = default);

        Task<ObservableCollection<UIMediaListItemSeries>> LoadNextUpAsync(CancellationToken cancellationToken = default);

        Task<(ObservableCollection<UIMainPageListItem>, bool)> LoadResumeItemsAsync(CancellationToken cancellationToken = default);
    }
}
