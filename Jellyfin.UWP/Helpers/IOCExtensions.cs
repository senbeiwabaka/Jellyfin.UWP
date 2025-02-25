using Jellyfin.Sdk;
using Jellyfin.UWP.ViewModels;
using Jellyfin.UWP.ViewModels.Controls;
using Jellyfin.UWP.ViewModels.Details;
using Jellyfin.UWP.ViewModels.Latest;
using Jellyfin.UWP.ViewModels.MainPage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;

namespace Jellyfin.UWP.Helpers;

internal static class IOCExtensions
{
    /// <summary>
    /// Adds all of the needed Jellyfin SDK clients to the DI.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection SetupJellyfin(this IServiceCollection services)
    {
        var version = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();
        var settings = new JellyfinSdkSettings();

        settings.Initialize(
            "Jellyfin.UWP",
            version,
            Environment.MachineName,
            "Jellyfin.UWP");

        services
            .AddHttpClient("Default", c =>
            {
                c.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("Jellyfin.UWP", version));

                c.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json", 1.0));
                c.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("*/*", 0.8));
            });

        services
            .AddSingleton<JellyfinSdkSettings>((serviceProvider) => settings)
            .AddSingleton<IAuthenticationProvider, JellyfinAuthenticationProvider>()
            .AddTransient<IRequestAdapter, JellyfinRequestAdapter>(s => new JellyfinRequestAdapter(
                s.GetRequiredService<IAuthenticationProvider>(),
                s.GetRequiredService<JellyfinSdkSettings>(),
                s.GetRequiredService<IHttpClientFactory>().CreateClient("Default")))
            .AddScoped<JellyfinApiClient>()
            .AddScoped<IMediaHelpers, MediaHelpers>();

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
            .AddTransient<SeriesDetailViewModel>()
            ;

        return services;
    }
}
