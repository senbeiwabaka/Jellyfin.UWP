using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.UWP.Helpers;
using Windows.Storage;

namespace Jellyfin.UWP.ViewModels
{
    internal sealed partial class SetupViewModel : ObservableValidator
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CompleteCommand))]
        [Required(AllowEmptyStrings = false)]
        [Url]
        private string jellyfinUrl;

        public delegate void EventHandler();

        public event EventHandler SuccessfullySetUrl;

        private bool CanGoToLoginPage()
        {
            return !string.IsNullOrWhiteSpace(JellyfinUrl) && Uri.IsWellFormedUriString(JellyfinUrl, UriKind.Absolute);
        }

        [RelayCommand(CanExecute = nameof(CanGoToLoginPage))]
        private void Complete()
        {
            ValidateAllProperties();

            if (CanGoToLoginPage())
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[JellyfinConstants.HostUrlName] = JellyfinUrl;

                SuccessfullySetUrl?.Invoke();
            }
        }
    }
}
