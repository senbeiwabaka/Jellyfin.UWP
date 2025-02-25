using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel.DataAnnotations;
using Jellyfin.UWP.Helpers;
using Windows.Storage;
using WinRT;

namespace Jellyfin.UWP.ViewModels;

[GeneratedBindableCustomProperty]
public partial class SetupViewModel : ObservableValidator
{
    public delegate void EventHandler();

    public event EventHandler? SuccessfullySetUrl;

    public IRelayCommand CompleteCommand => field ??= new RelayCommand(Complete, CanGoToLoginPage);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompleteCommand))]
    [Required(AllowEmptyStrings = false)]
    [Url]
    public partial string? JellyfinUrl { get; set; }

    private bool CanGoToLoginPage()
    {
        return !string.IsNullOrWhiteSpace(JellyfinUrl) && Uri.IsWellFormedUriString(JellyfinUrl, UriKind.Absolute);
    }

    private void Complete()
    {
        ValidateAllProperties();

        if (CanGoToLoginPage())
        {
            ApplicationData.Current.LocalSettings.Values[JellyfinConstants.HostUrlName] = JellyfinUrl;

            SuccessfullySetUrl?.Invoke();
        }
    }
}
