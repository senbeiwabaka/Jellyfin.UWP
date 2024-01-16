using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Helpers
{
    internal static class PageHelpers
    {
        internal static bool IsThereEnoughDataForScrolling(ListView listView)
        {
            listView.UpdateLayout();

            var scrollViewer = listView.FindVisualChild<ScrollViewer>();

            if (scrollViewer != null)
            {
                var itemsPanelChildren = listView.ItemsPanelRoot.Children;
                var viewportWidth = scrollViewer.ViewportWidth;
                var totalWidthOfChildren = itemsPanelChildren.Sum(x => x.ActualSize.X);

                return viewportWidth < totalWidthOfChildren;
            }

            return false;
        }
    }
}
