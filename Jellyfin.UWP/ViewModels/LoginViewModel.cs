using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using MetroLog;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Jellyfin.UWP.ViewModels
{
    internal sealed partial class LoginViewModel : ObservableValidator
    {
        private readonly IMemoryCache memoryCache;
        private readonly JellyfinApiClient apiClient;
        private readonly JellyfinSdkSettings settings;
        private readonly ILogger Log = LogManagerFactory.DefaultLogManager.GetLogger<LoginViewModel>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [Required(AllowEmptyStrings = false)]
        [NotifyDataErrorInfo]
        private string password;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [Required(AllowEmptyStrings = false)]
        [NotifyDataErrorInfo]
        private string username;

        [ObservableProperty]
        private string message;

        [ObservableProperty]
        private bool openPopup;

        public LoginViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, JellyfinSdkSettings settings)
        {
            this.memoryCache = memoryCache;
            this.apiClient = apiClient;
            this.settings = settings;
        }

        public delegate void EventHandler();

        public event EventHandler SuccessfullyLoggedIn;

        private bool CanLogIn()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanLogIn), AllowConcurrentExecutions = false)]
        private async Task LoginAsync(CancellationToken token)
        {
            try
            {
                ValidateAllProperties();

                if (HasErrors)
                {
                    Log.Debug("has errors so can't log in.");

                    Message = "Either username or password were wrong";

                    OpenPopup = true;

                    return;
                }

                var localSettings = ApplicationData.Current.LocalSettings;
                var baseUrl = localSettings.Values[JellyfinConstants.HostUrlName].ToString();

                settings.SetServerUrl(baseUrl);

                var authResult = await apiClient.Users.AuthenticateByName.PostAsync(new AuthenticateUserByName
                {
                    Username = Username,
                    Pw = Password
                });

                if (authResult is not null && !string.IsNullOrWhiteSpace(authResult.AccessToken))
                {
                    localSettings.Values[JellyfinConstants.AccessTokenName] = authResult.AccessToken;
                    settings.SetAccessToken(authResult.AccessToken);

                    memoryCache.Set(JellyfinConstants.UserName, authResult.User);

                    memoryCache.Set(JellyfinConstants.SessionName, authResult.SessionInfo);
                    localSettings.Values[JellyfinConstants.SessionName] = System.Text.Json.JsonSerializer.Serialize(authResult.SessionInfo);

                    memoryCache.Set<string>(JellyfinConstants.HostUrlName, baseUrl);

                    SuccessfullyLoggedIn?.Invoke();
                }
                else
                {
                    Message = "Either username or password were wrong";
                }
            }
            catch (Exception e)
            {
                Log.Error("Login error", e);

                Message = "An error occurred. Please try again.";

                OpenPopup = true;
            }
        }
    }
}
