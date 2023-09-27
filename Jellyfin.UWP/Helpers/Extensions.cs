using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Jellyfin.UWP.Helpers
{
    internal static class Extensions
    {
        public static T FindVisualChild<T>(this DependencyObject element)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(element, i);

                if (child is T)
                {
                    return (T)child;
                }
                else
                {
                    T childOfChild = FindVisualChild<T>(child);

                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }

            return null;
        }
    }
}
