using Windows.UI.Xaml;

namespace Jellyfin.UWP.Converters
{
    internal partial class DoubleToVisibilityConverter : DoubleToObjectConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleToVisibilityConverter"/> class.
        /// </summary>
        public DoubleToVisibilityConverter()
        {
            TrueValue = Visibility.Visible;
            FalseValue = Visibility.Collapsed;
            NullValue = Visibility.Collapsed;
        }
    }
}
