using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Jellyfin.UWP
{
    public partial class LoginViewModel : ObservableValidator
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly SdkClientSettings sdkClientSettings;
        private readonly IMemoryCache memoryCache;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [Required(AllowEmptyStrings = false)]
        private string username;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [Required(AllowEmptyStrings = false)]
        private string password;

        public delegate void EventHandler();

        public event EventHandler SuccessfullyLoggedIn;

        public LoginViewModel(IHttpClientFactory httpClientFactory, SdkClientSettings sdkClientSettings, IMemoryCache memoryCache)
        {
            this.httpClientFactory = httpClientFactory;
            this.sdkClientSettings = sdkClientSettings;
            this.memoryCache = memoryCache;
        }

        [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanLogIn))]
        private async Task LoginAsync(CancellationToken token)
        {
            try
            {
                ValidateAllProperties();

                var httpClient = httpClientFactory.CreateClient();
                var authClient = new UserClient(sdkClientSettings, httpClient);
                var authResult = await authClient.AuthenticateUserByNameAsync(new AuthenticateUserByName { Username = Username, Pw = Password }, cancellationToken: token);

                if (authResult is not null && !string.IsNullOrWhiteSpace(authResult.AccessToken))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values["accessToken"] = authResult.AccessToken;

                    sdkClientSettings.AccessToken = authResult.AccessToken;

                    var user = authResult.User;
                    memoryCache.Set("user", user);

                    SuccessfullyLoggedIn?.Invoke();
                }
            }
            catch (UserException e)
            {
            }
        }

        private bool CanLogIn()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }
    }
}