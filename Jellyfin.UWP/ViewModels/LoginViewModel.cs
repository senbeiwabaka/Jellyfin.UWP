using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using MetroLog;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Jellyfin.UWP
{
    internal sealed partial class LoginViewModel : ObservableValidator
    {
        private readonly IUserClient userClient;
        private readonly IMemoryCache memoryCache;
        private readonly SdkClientSettings settings;
        private readonly ILogger Log = LogManagerFactory.DefaultLogManager.GetLogger<LoginViewModel>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [Required(AllowEmptyStrings = false)]
        private string password;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [Required(AllowEmptyStrings = false)]
        private string username;

        public LoginViewModel(IUserClient userClient, IMemoryCache memoryCache, SdkClientSettings settings)
        {
            this.userClient = userClient;
            this.memoryCache = memoryCache;
            this.settings = settings;
        }

        public delegate void EventHandler();

        public event EventHandler SuccessfullyLoggedIn;

        private bool CanLogIn()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanLogIn))]
        private async Task LoginAsync(CancellationToken token)
        {
            try
            {
                ValidateAllProperties();

                var authResult = await userClient.AuthenticateUserByNameAsync(new AuthenticateUserByName { Username = Username, Pw = Password }, cancellationToken: token);

                if (authResult is not null && !string.IsNullOrWhiteSpace(authResult.AccessToken))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values["accessToken"] = authResult.AccessToken;

                    settings.AccessToken = authResult.AccessToken;

                    var user = authResult.User;
                    memoryCache.Set("user", user);

                    var session = authResult.SessionInfo;
                    memoryCache.Set("session", session);
                    localSettings.Values["session"] = System.Text.Json.JsonSerializer.Serialize(session);

                    SuccessfullyLoggedIn?.Invoke();
                }
            }
            catch (UserException e)
            {
                Log.Error("Login error", e);
            }
            catch (Exception e)
            {
                Log.Error("Login error", e);
            }
        }
    }
}
