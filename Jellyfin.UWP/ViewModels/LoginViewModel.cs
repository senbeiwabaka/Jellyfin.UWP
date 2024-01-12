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

namespace Jellyfin.UWP.ViewModels
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
                else
                {
                    Message = "Either username or password were wrong";
                }
            }
            catch (UserException e)
            {
                Log.Error("Login error", e);

                Message = "An error occurred. Please try again.";

                OpenPopup = true;
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
