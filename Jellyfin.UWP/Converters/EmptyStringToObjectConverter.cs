namespace Jellyfin.UWP.Converters
{
    internal partial class EmptyStringToObjectConverter : EmptyObjectToObjectConverter
    {
        /// <summary>
        /// Checks string for emptiness.
        /// </summary>
        /// <param name="value">Value to be checked.</param>
        /// <returns>True if value is null or empty string, false otherwise.</returns>
        protected override bool CheckValueIsEmpty(object value)
        {
            return string.IsNullOrEmpty(value?.ToString());
        }
    }
}
