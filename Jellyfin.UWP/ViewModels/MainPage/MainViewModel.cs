using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels.MainPage;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.ViewModels.MainPage;

internal sealed partial class MainViewModel : ObservableObject
{
    private readonly IFavoritesViewModel favoritesViewModel;
    private readonly IHomeViewModel homeViewModel;
    private readonly IMemoryCache memoryCache;

    [ObservableProperty]
    public partial ObservableCollection<UIMainPageListItem> FavoriteEpisodesList { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> FavoriteMoviesList { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIPersonItem> FavoritePersonList { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> FavoriteSeriesList { get; set; }

    [ObservableProperty]
    public partial bool HasEnoughDataToScrollContinueWatching { get; set; }

    [ObservableProperty]
    public partial bool HasEnoughDataToScrollEpisodesFavorites { get; set; }

    [ObservableProperty]
    public partial bool HasEnoughDataToScrollMoviesFavorites { get; set; }

    [ObservableProperty]
    public partial bool HasEnoughDataToScrollNextUp { get; set; }

    [ObservableProperty]
    public partial bool HasEnoughDataToScrollPeopleFavorites { get; set; }

    [ObservableProperty]
    public partial bool HasEnoughDataToScrollShowsFavorites { get; set; }

    [ObservableProperty]
    public partial bool HasResumeMedia { get; set; }

    [ObservableProperty]
    public partial bool IsFavoriteSelected { get; set; }

    [ObservableProperty]
    public partial bool IsHomeSelected { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItem> MediaList { get; set; }

    [ObservableProperty]
    public partial ObservableGroupedCollection<MediaGroupItem, UIMediaListItem> MediaListGrouped { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMediaListItemSeries> NextupMediaList { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UIMainPageListItem> ResumeMediaList { get; set; }

    [ObservableProperty]
    public partial string UserName { get; set; }

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

        MediaListGrouped?.Clear();

        MediaList = await homeViewModel.LoadMediaListAsync(cancellationToken);
        (ResumeMediaList, HasResumeMedia) = await homeViewModel.LoadResumeItemsAsync(cancellationToken);
        NextupMediaList = await homeViewModel.LoadNextUpAsync(cancellationToken);
        MediaListGrouped = await homeViewModel.LoadLatestAsync(MediaList, cancellationToken);
    }

    public async Task LoadInitialAsync()
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

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
