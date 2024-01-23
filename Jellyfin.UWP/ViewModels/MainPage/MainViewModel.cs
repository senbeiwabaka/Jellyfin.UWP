using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels.MainPage
{
    internal sealed partial class MainViewModel : ObservableObject
    {
        private readonly IFavoritesViewModel favoritesViewModel;
        private readonly IHomeViewModel homeViewModel;
        private readonly IMemoryCache memoryCache;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> favoriteEpisodesList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> favoriteMoviesList;

        [ObservableProperty]
        private ObservableCollection<UIPersonItem> favoritePersonList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> favoriteSeriesList;

        [ObservableProperty]
        private bool hasEnoughDataToScrollContinueWatching;

        [ObservableProperty]
        private bool hasEnoughDataToScrollEpisodesFavorites;

        [ObservableProperty]
        private bool hasEnoughDataToScrollMoviesFavorites;

        [ObservableProperty]
        private bool hasEnoughDataToScrollNextUp;

        [ObservableProperty]
        private bool hasEnoughDataToScrollPeopleFavorites;

        [ObservableProperty]
        private bool hasEnoughDataToScrollShowsFavorites;

        [ObservableProperty]
        private bool hasResumeMedia;

        [ObservableProperty]
        private bool isFavoriteSelected;

        [ObservableProperty]
        private bool isHomeSelected;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> mediaList;

        [ObservableProperty]
        private ObservableGroupedCollection<MediaGroupItem, UIMediaListItem> mediaListGrouped;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItemSeries> nextupMediaList;

        [ObservableProperty]
        private ObservableCollection<UIMediaListItem> resumeMediaList;

        [ObservableProperty]
        private string userName;

        public MainViewModel(IHomeViewModel homeViewModel, IFavoritesViewModel favoritesViewModel, IMemoryCache memoryCache)
        {
            this.homeViewModel = homeViewModel;
            this.favoritesViewModel = favoritesViewModel;
            this.memoryCache = memoryCache;

            IsHomeSelected = true;
        }

        public async Task FavoriteLoadAsync(CancellationToken cancellationToken = default)
        {
            IsHomeSelected = false;

            IsFavoriteSelected = true;

            FavoriteMoviesList = await favoritesViewModel.GetMoviesAsync(cancellationToken);
            FavoriteSeriesList = await favoritesViewModel.GetSeriesAsync(cancellationToken);
            FavoriteEpisodesList = await favoritesViewModel.GetEpisodesAsync(cancellationToken);
            FavoritePersonList = await favoritesViewModel.GetPeopleAsync(cancellationToken);
        }

        public async Task HomeLoadAsync(CancellationToken cancellationToken = default)
        {
            IsHomeSelected = true;

            IsFavoriteSelected = false;

            MediaListGrouped = null;

            MediaList = await homeViewModel.LoadMediaListAsync(cancellationToken);
            (ResumeMediaList, HasResumeMedia) = await homeViewModel.LoadResumeItemsAsync(cancellationToken);
            NextupMediaList = await homeViewModel.LoadNextUpAsync(cancellationToken);
            MediaListGrouped = await homeViewModel.LoadLatestAsync(MediaList, cancellationToken);
        }

        public async Task LoadInitialAsync()
        {
            var user = memoryCache.Get<UserDto>("user");

            UserName = $"User: {user.Name}";

            await HomeLoadAsync();
        }

        [RelayCommand(IncludeCancelCommand = false, AllowConcurrentExecutions = false)]
        private async Task SwitchToFavorite(CancellationToken cancellationToken)
        {
            await FavoriteLoadAsync(cancellationToken);
        }

        [RelayCommand(IncludeCancelCommand = false, AllowConcurrentExecutions = false)]
        private async Task SwitchToHome(CancellationToken cancellationToken)
        {
            await HomeLoadAsync(cancellationToken);
        }
    }
}
