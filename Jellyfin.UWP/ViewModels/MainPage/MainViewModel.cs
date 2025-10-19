using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;

namespace Jellyfin.UWP.ViewModels.MainPage;

internal sealed partial class MainViewModel(IHomeViewModel homeViewModel, IFavoritesViewModel favoritesViewModel, IMemoryCache memoryCache, JellyfinApiClient apiClient) : ObservableObject
{
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
    public partial bool IsHomeSelected { get; set; } = true;

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

        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

        MediaList = await homeViewModel.LoadMediaListAsync(cancellationToken);
        (ResumeMediaList, HasResumeMedia) = await homeViewModel.LoadResumeItemsAsync(cancellationToken);
        NextupMediaList = await homeViewModel.LoadNextUpAsync(cancellationToken);
        MediaListGrouped = await homeViewModel.LoadLatestAsync(MediaList, cancellationToken);
    }

    public async Task LoadInitialAsync(CancellationToken cancellationToken = default)
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);

        UserName = $"User: {user.Name}";

        await HomeLoadAsync(cancellationToken);
    }

    public Task GetUserDisplay(CancellationToken cancellationToken = default)
    {
        var user = memoryCache.Get<UserDto>(JellyfinConstants.UserName);
        //var itemsResult = await apiClient.DisplayPreference[user.]

        return Task.CompletedTask;
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
