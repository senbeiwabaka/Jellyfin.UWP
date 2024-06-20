using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Jellyfin.UWP.Helpers
{
    internal static class PageHelpers
    {
        internal static ItemsPanelTemplate GetItemsPanelTemplate()
        {
            string xaml = @"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                            <StackPanel Background=""Transparent"" Orientation=""Horizontal"" />
                    </ItemsPanelTemplate>";
            return XamlReader.LoadWithInitialTemplateValidation(xaml) as ItemsPanelTemplate;
        }

        internal static bool IsThereEnoughDataForScrolling(ListView listView)
        {
            listView.UpdateLayout();

            var scrollViewer = listView.FindVisualChild<ScrollViewer>();

            if (scrollViewer is not null && listView.ItemsPanelRoot is not null)
            {
                var itemsPanelChildren = listView.ItemsPanelRoot.Children;
                var viewportWidth = scrollViewer.ViewportWidth;
                var totalWidthOfChildren = itemsPanelChildren.Sum(x => x.ActualSize.X);

                return viewportWidth < totalWidthOfChildren;
            }

            return false;
        }

        internal static void ResetPageCache()
        {
            int cacheSize = ((Frame)Window.Current.Content).CacheSize;

            ((Frame)Window.Current.Content).CacheSize = 0;
            ((Frame)Window.Current.Content).CacheSize = cacheSize;
        }
    }
}
