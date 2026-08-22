using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ViewModels;

public sealed partial class LoginViewModel(IMemoryCache memoryCache, JellyfinApiClient apiClient, JellyfinSdkSettings settings, ILogger<LoginViewModel> logger, IApplicationService applicationService) : ObservableValidator
{
    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial bool OpenPopup { get; set; }

    public delegate void EventHandler();

    public event EventHandler? SuccessfullyLoggedIn;

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
    [RequiresUnreferencedCode("Calls CommunityToolkit.Mvvm.ComponentModel.ObservableValidator.ValidateAllProperties()")]
    private async Task LoginAsync(CancellationToken token)
    {
        try
        {
            ValidateAllProperties();

            if (HasErrors)
            {
                logger.LogDebug("has errors so can't log in.");

                Message = "Either username or password were wrong";

                OpenPopup = true;

                return;
            }

            var baseUrl = applicationService.GetSettingValue(JellyfinConstants.HostUrlName);

            settings.SetServerUrl(baseUrl);

            var authResult = await apiClient.Users.AuthenticateByName.PostAsync(new AuthenticateUserByName
            {
                Username = Username,
                Pw = Password
            }, cancellationToken: token);

            if (authResult is not null && !string.IsNullOrWhiteSpace(authResult.AccessToken))
            {
                applicationService.SetSettingValue(JellyfinConstants.AccessTokenName, authResult.AccessToken);
                settings.SetAccessToken(authResult.AccessToken);

                memoryCache.Set(JellyfinConstants.UserName, authResult.User);

                memoryCache.Set(JellyfinConstants.SessionName, authResult.SessionInfo);
                applicationService.SetSettingValue(JellyfinConstants.SessionName, System.Text.Json.JsonSerializer.Serialize(authResult.SessionInfo));

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
            logger.LogError(e, "Login error");

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
