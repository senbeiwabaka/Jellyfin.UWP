using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Windows.Storage;

namespace Jellyfin.ViewModels;

public partial class SetupViewModel : ObservableValidator
{
    public delegate void EventHandler();

    public event EventHandler? SuccessfullySetUrl;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompleteCommand))]
    [Required(AllowEmptyStrings = false)]
    [Url]
    public partial string? JellyfinUrl { get; set; }

    private bool CanGoToLoginPage()
    {
        return !string.IsNullOrWhiteSpace(JellyfinUrl) && Uri.IsWellFormedUriString(JellyfinUrl, UriKind.Absolute);
    }

    [RelayCommand(CanExecute = nameof(CanGoToLoginPage))]
    [RequiresUnreferencedCode("Calls CommunityToolkit.Mvvm.ComponentModel.ObservableValidator.ValidateAllProperties()")]
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
