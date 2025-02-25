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

namespace Jellyfin.UWP.ViewModels;

internal sealed partial class LoginViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, JellyfinSdkSettings settings) : ObservableValidator
{
    private readonly ILogger Log = LoggerFactory.GetLogger(nameof(LoginViewModel));

    [ObservableProperty]
    private string message;

    [ObservableProperty]
    private bool openPopup;

    public delegate void EventHandler();

    public event EventHandler SuccessfullyLoggedIn;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [Required(AllowEmptyStrings = false)]
    [NotifyDataErrorInfo]
    public partial string Password { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [Required(AllowEmptyStrings = false)]
    [NotifyDataErrorInfo]
    public partial string Username { get; set; }

    private bool CanLogIn()
    {
        return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanLogIn), IncludeCancelCommand = true)]
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

            var baseUrl = ApplicationData.Current.LocalSettings.Values[JellyfinConstants.HostUrlName].ToString()!;

            settings.SetServerUrl(baseUrl);

            var authResult = await apiClient.Users.AuthenticateByName.PostAsync(new AuthenticateUserByName
            {
                Username = Username,
                Pw = Password
            }, cancellationToken: token);

            if (authResult is not null && !string.IsNullOrWhiteSpace(authResult.AccessToken))
            {
                ApplicationData.Current.LocalSettings.Values[JellyfinConstants.AccessTokenName] = authResult.AccessToken;
                settings.SetAccessToken(authResult.AccessToken);

                memoryCache.Set(JellyfinConstants.UserName, authResult.User);

                memoryCache.Set(JellyfinConstants.SessionName, authResult.SessionInfo);
                ApplicationData.Current.LocalSettings.Values[JellyfinConstants.SessionName] = System.Text.Json.JsonSerializer.Serialize(authResult.SessionInfo);

                memoryCache.Set<string>(JellyfinConstants.HostUrlName, baseUrl);

                SuccessfullyLoggedIn?.Invoke();
            }
            else
            {
                Message = "Either username or password were wrong";
            }
        }
        catch (TaskCanceledException)
        {
            // Skipped because it comes from using the cancel button
        }
        catch (Exception e)
        {
            Log.Error("Login error", e);

            Message = "An error occurred. Please try again.";

            OpenPopup = true;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        LoginCommand.Cancel();
    }
}
