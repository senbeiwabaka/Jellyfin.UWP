using Jellyfin.Sdk;
using Jellyfin.UWP.ViewModels;
using Jellyfin.UWP.ViewModels.Controls;
using Jellyfin.UWP.ViewModels.Details;
using Jellyfin.UWP.ViewModels.Latest;
using Jellyfin.UWP.ViewModels.MainPage;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace Jellyfin.UWP.Helpers
{
    internal static class IOCExtensions
    {
        /// <summary>
        /// Adds all of the needed Jellyfin SDK clients to the DI.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddSdkClients(this IServiceCollection services)
        {
            services
                .AddTransient<IUserLibraryClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new UserLibraryClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<ILibraryClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new LibraryClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<ITvShowsClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new TvShowsClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IVideosClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new VideosClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<ISessionClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new SessionClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IPlaystateClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new PlaystateClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IMediaInfoClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new MediaInfoClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<ISubtitleClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new SubtitleClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IDynamicHlsClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new DynamicHlsClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IApiKeyClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new ApiKeyClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IUserClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new UserClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IItemsClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new ItemsClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IUserViewsClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new UserViewsClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IFilterClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new FilterClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IMoviesClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new MoviesClient(sdkSettings, httpClientFactory.CreateClient());
                })
                .AddTransient<IPersonsClient>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    var sdkSettings = serviceProvider.GetService<SdkClientSettings>();

                    return new PersonsClient(sdkSettings, httpClientFactory.CreateClient());
                });

            return services;
        }

        /// <summary>
        /// Adds all of this applications ViewModels to the DI.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            services
                .AddTransient<LoginViewModel>()
                .AddTransient<MainViewModel>()
                .AddTransient<MediaListViewModel>()
                .AddTransient<DetailsViewModel>()
                .AddTransient<MediaItemPlayerViewModel>()
                .AddTransient<SearchViewModel>()
                .AddTransient<SetupViewModel>()
                .AddTransient<SeasonViewModel>()
                .AddTransient<EpisodeViewModel>()
                .AddTransient<ShowsViewModel>()
                .AddTransient<MoviesViewModel>()
                .AddTransient<IHomeViewModel, HomeViewModel>()
                .AddTransient<IFavoritesViewModel, FavoritesViewModel>()
                .AddTransient<ViewedFavoriteViewModel>()
                .AddTransient<SeriesDetailViewModel>();

            return services;
        }
    }
}
