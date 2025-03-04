﻿using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Pages;
using MetroLog;
using MetroLog.Targets;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;
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

            HdrCheck.RunHDRCode(Log);

            Ioc.Default.ConfigureServices(new ServiceCollection()
               .AddMemoryCache()
               .SetupJellyfin()
               .AddViewModels()
               .BuildServiceProvider());

            var accessToken = ApplicationData.Current.LocalSettings.Values[JellyfinConstants.AccessTokenName]?.ToString();
            var jellyfinUrl = ApplicationData.Current.LocalSettings.Values[JellyfinConstants.HostUrlName]?.ToString();
            var resetJellyfinUrl = false;

            using (var scope = Ioc.Default.CreateScope())
            {
                var apiClient = scope.ServiceProvider.GetRequiredService<JellyfinApiClient>();
                var settings = scope.ServiceProvider.GetRequiredService<JellyfinSdkSettings>();
                var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

                // Testing URL to see if it is still active
                if (!string.IsNullOrWhiteSpace(jellyfinUrl) || Uri.IsWellFormedUriString(jellyfinUrl, UriKind.Absolute))
                {
                    settings.SetServerUrl(jellyfinUrl);

                    try
                    {
                        var systemInfo = await apiClient.System.Info.Public
                            .GetAsync();

                        resetJellyfinUrl = false;

                        memoryCache.Set(JellyfinConstants.HostUrlName, jellyfinUrl);
                        memoryCache.Set(JellyfinConstants.ServerVersionName, systemInfo.Version);

                        Log.Debug("Server Version: {0}", systemInfo.Version);

                    }
                    catch (Exception ex)
                    {
                        resetJellyfinUrl = true;

                        Log.Error(ex.Message, ex);
                    }
                }
                else
                {
                    resetJellyfinUrl = true;
                }

                var localSettingsSession = ApplicationData.Current.LocalSettings.Values[JellyfinConstants.SessionName]?.ToString();

                if (string.IsNullOrWhiteSpace(localSettingsSession))
                {
                    CleanupValues(settings);

                    accessToken = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(accessToken) && !resetJellyfinUrl)
                {
                    try
                    {
                        settings.SetAccessToken(accessToken);

                        var user = await apiClient.Users.Me.GetAsync();
                        var session = System.Text.Json.JsonSerializer.Deserialize<SessionInfoDto>(localSettingsSession);

                        memoryCache.Set(JellyfinConstants.UserName, user);
                        memoryCache.Set(JellyfinConstants.SessionName, session);
                    }
                    catch (Exception exception)
                    {
                        CleanupValues(settings);

                        accessToken = string.Empty;

                        Log.Error("Failed to get user information on startup", exception);
                    }
                }
            }

            if(DebugHelpers.IsDebugRelease)
            {
                Log.Debug("Is reset jellyfin true: {0}", resetJellyfinUrl);
                Log.Debug("Is access token missing: {0}", string.IsNullOrWhiteSpace(accessToken));
            }

            if (!args.PrelaunchActivated)
            {
                if (rootFrame.Content == null)
                {
                    if (resetJellyfinUrl)
                    {
                        rootFrame.Navigate(typeof(SetupPage));
                    }
                    else if (string.IsNullOrWhiteSpace(accessToken))
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

                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        private static void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
                e.Handled = true;
            }
        }

        private static void CleanupValues(JellyfinSdkSettings settings)
        {
            ApplicationData.Current.LocalSettings.Values.Remove(JellyfinConstants.AccessTokenName);
            ApplicationData.Current.LocalSettings.Values.Remove(JellyfinConstants.SessionName);

            settings.SetAccessToken(string.Empty);
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Log.Error("Failed to navigate", e.Exception);

            ((Frame)Window.Current.Content).ForwardStack.Clear();
            ((Frame)Window.Current.Content).BackStack.Clear();
            ((Frame)Window.Current.Content).Navigate(typeof(MainPage));

            //throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
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

            Log.Debug("suspending");

            deferral.Complete();
        }
    }
}
