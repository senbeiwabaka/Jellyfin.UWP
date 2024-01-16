using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.UWP.Pages;
using Jellyfin.UWP.ViewModels;
using Jellyfin.UWP.ViewModels.Latest;
using MetroLog;
using MetroLog.Targets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

[assembly: InternalsVisibleTo("Jellyfin.UWP.Tests")]

namespace Jellyfin.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly ILogger Log;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

#if DEBUG
            LogManagerFactory.DefaultConfiguration.AddTarget(MetroLog.LogLevel.Debug, MetroLog.LogLevel.Fatal, new StreamingFileTarget());
#else
            LogManagerFactory.DefaultConfiguration.AddTarget(MetroLog.LogLevel.Info, MetroLog.LogLevel.Fatal, new StreamingFileTarget());
#endif

            GlobalCrashHandler.Configure();

            Log = LogManagerFactory.DefaultLogManager.GetLogger<App>();

            this.UnhandledException += (sender, e) =>
            {
                e.Handled = true;
                Log.Error("unhandled exception", e.Exception);
            };
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var accessToken = localSettings.Values["accessToken"]?.ToString();
            var jellyfinUrl = localSettings.Values["jellyfinUrl"]?.ToString();

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            var settings = new SdkClientSettings
            {
                BaseUrl = jellyfinUrl,
                ClientName = "Jellyfin.UWP",
                ClientVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                DeviceName = Environment.MachineName,
                DeviceId = "Jellyfin.UWP",
            };

            Ioc.Default.ConfigureServices(new ServiceCollection()
               // Services
               .AddSingleton<SdkClientSettings>((serviceProvider) => settings)
               .AddHttpClient()
               .AddMemoryCache()
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
               // ViewModels
               .AddTransient<LoginViewModel>()
               .AddTransient<MainViewModel>()
               .AddTransient<MediaListViewModel>()
               .AddTransient<DetailsViewModel>()
               .AddTransient<MediaItemPlayerViewModel>()
               .AddTransient<SearchViewModel>()
               .AddTransient<SetupViewModel>()
               .AddTransient<SeriesViewModel>()
               .AddTransient<EpisodeViewModel>()
               .AddTransient<ShowsViewModel>()
               .AddTransient<MoviesViewModel>()
               .BuildServiceProvider());

            var resetJellyfinUrl = false;

            // Testing URL to see if it is still active
            if (!string.IsNullOrWhiteSpace(jellyfinUrl) || Uri.IsWellFormedUriString(jellyfinUrl, UriKind.Absolute))
            {
                using (var scope = Ioc.Default.CreateScope())
                {
                    var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient();

                    httpClient.BaseAddress = new Uri(jellyfinUrl);

                    try
                    {
                        var response = await httpClient.GetAsync(string.Empty);

                        resetJellyfinUrl = !response.IsSuccessStatusCode;
                    }
                    catch (Exception ex)
                    {
                        resetJellyfinUrl = true;

                        Log.Error(ex.Message, ex);
                    }
                }
            }
            else
            {
                resetJellyfinUrl = true;
            }

            var localSettingsSession = localSettings.Values["session"]?.ToString();

            if (string.IsNullOrWhiteSpace(localSettingsSession))
            {
                settings.AccessToken = string.Empty;
                accessToken = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(accessToken) && !resetJellyfinUrl)
            {
                var httpClientFactory = Ioc.Default.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();

                try
                {
                    settings.AccessToken = accessToken;

                    var authClient = new UserClient(settings, httpClient);
                    var user = await authClient.GetCurrentUserAsync();
                    var memoryCache = Ioc.Default.GetRequiredService<IMemoryCache>();

                    var session = System.Text.Json.JsonSerializer.Deserialize<SessionInfo>(localSettingsSession);

                    memoryCache.Set("user", user);
                    memoryCache.Set("session", session);
                }
                catch (UserException exception)
                {
                    CleanupValues(localSettings, settings);

                    Log.Error("Failed to get user information on startup", exception);
                }
            }

            if (!args.PrelaunchActivated)
            {
                if (rootFrame.Content == null)
                {
                    if (resetJellyfinUrl)
                    {
                        rootFrame.Navigate(typeof(SetupPage));
                    }
                    else if (string.IsNullOrWhiteSpace(settings.AccessToken))
                    {
                        rootFrame.Navigate(typeof(LoginPage));
                    }
                    else
                    {
                        // When the navigation stack isn't restored navigate to the first page,
                        // configuring the new page by passing required information as a navigation
                        // parameter
                        rootFrame.Navigate(typeof(MainPage), args.Arguments);
                    }
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        private static void CleanupValues(ApplicationDataContainer localSettings, SdkClientSettings settings)
        {
            localSettings.Values.Remove("accessToken");
            localSettings.Values.Remove("session");

            settings.AccessToken = default;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
