﻿using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels.MainPage
{
    internal interface IFavoritesViewModel
    {
        Task<ObservableCollection<UIMainPageListItem>> GetEpisodesAsync(CancellationToken cancellationToken = default);

        Task<ObservableCollection<UIMediaListItem>> GetMoviesAsync(CancellationToken cancellationToken = default);

        Task<ObservableCollection<UIPersonItem>> GetPeopleAsync(CancellationToken cancellationToken = default);

        Task<ObservableCollection<UIMediaListItem>> GetSeriesAsync(CancellationToken cancellationToken = default);
    }
}
