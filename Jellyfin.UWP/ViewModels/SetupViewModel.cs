using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using System;
using System.ComponentModel.DataAnnotations;
using Windows.Storage;

namespace Jellyfin.UWP
{
    internal sealed partial class SetupViewModel : ObservableValidator
    {
        private readonly SdkClientSettings sdkClientSettings;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CompleteCommand))]
        [Required(AllowEmptyStrings = false)]
        [Url]
        private string jellyfinUrl;

        public SetupViewModel(SdkClientSettings sdkClientSettings)
        {
            this.sdkClientSettings = sdkClientSettings;
        }

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
                localSettings.Values["jellyfinUrl"] = JellyfinUrl;

                sdkClientSettings.BaseUrl = JellyfinUrl;

                SuccessfullySetUrl?.Invoke();
            }
        }
    }
}
