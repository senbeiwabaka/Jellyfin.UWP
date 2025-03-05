using System;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Jellyfin.UWP.Helpers;

internal static class PageHelpers
{
    internal static ItemsPanelTemplate? GetItemsPanelTemplate()
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

    // TODO: REPLACE WHEN UNO IS REMOVED FROM CommunityToolkit.WinUI
    internal static T? FindParent<T>(this FrameworkElement element)
        where T : notnull, FrameworkElement
    {
        while (true)
        {
            if (element.Parent is not FrameworkElement parent)
            {
                return null;
            }

            if (parent is T result)
            {
                return result;
            }

            element = parent;
        }
    }

    // TODO: REPLACE WHEN UNO IS REMOVED FROM CommunityToolkit.WinUI
    internal static T? FindChild<T>(this FrameworkElement element)
        where T : notnull, FrameworkElement
    {
    // Jump label to manually optimize the tail recursive paths for elements with a single
    // child by just overwriting the current element and jumping back to the start of the
    // method. This avoids a recursive call and one stack frame every time.
    Start:

        if (element is Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                if (child is not FrameworkElement current)
                {
                    continue;
                }

                if (child is T result)
                {
                    return result;
                }

                T? descendant = FindChild<T>(current);

                if (descendant is not null)
                {
                    return descendant;
                }
            }
        }
        else if (element is ItemsControl itemsControl)
        {
            foreach (object item in itemsControl.Items)
            {
                if (item is not FrameworkElement current)
                {
                    continue;
                }

                if (item is T result)
                {
                    return result;
                }

                T? descendant = FindChild<T>(current);

                if (descendant is not null)
                {
                    return descendant;
                }
            }
        }
        else if (element is ContentControl contentControl)
        {
            if (contentControl.Content is FrameworkElement content)
            {
                if (content is T result)
                {
                    return result;
                }

                element = content;

                goto Start;
            }
        }
        else if (element is Border border)
        {
            if (border.Child is FrameworkElement child)
            {
                if (child is T result)
                {
                    return result;
                }

                element = child;

                goto Start;
            }
        }
        else if (element is ContentPresenter contentPresenter)
        {
            // Sometimes ContentPresenter is used in control templates instead of ContentControl,
            // therefore we should still check if its Content is a matching FrameworkElement instance.
            // This also makes this work for SwitchPresenter.
            if (contentPresenter.Content is FrameworkElement content)
            {
                if (content is T result)
                {
                    return result;
                }

                element = content;

                goto Start;
            }
        }
        else if (element is Viewbox viewbox)
        {
            if (viewbox.Child is FrameworkElement child)
            {
                if (child is T result)
                {
                    return result;
                }

                element = child;

                goto Start;
            }
        }
        else if (element is UserControl userControl)
        {
            // We put UserControl right before the slower reflection fallback path as
            // this type is less likely to be used compared to the other ones above.
            if (userControl.Content is FrameworkElement content)
            {
                if (content is T result)
                {
                    return result;
                }

                element = content;

                goto Start;
            }
        }
        else if (element.GetContentControl() is FrameworkElement containedControl)
        {
            if (containedControl is T result)
            {
                return result;
            }

            element = containedControl;

            goto Start;
        }

        return null;
    }

    // TODO: REPLACE WHEN UNO IS REMOVED FROM CommunityToolkit.WinUI
    internal static UIElement? GetContentControl(this FrameworkElement element)
    {
        Type type = element.GetType();
        TypeInfo? typeInfo = type.GetTypeInfo();

        while (typeInfo is not null)
        {
            // We need to manually explore the custom attributes this way as the target one
            // is not returned by any of the other available GetCustomAttribute<T> APIs.
            foreach (CustomAttributeData attribute in typeInfo.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(ContentPropertyAttribute))
                {
                    // If we're finding a ContentPropertyAttribute, this whole path should be set,
                    // don't think we need additional checks here.
                    string propertyName = (string)attribute.NamedArguments![0].TypedValue!.Value!;
                    PropertyInfo? propertyInfo = type.GetProperty(propertyName);

                    return propertyInfo?.GetValue(element) as UIElement;
                }
            }

            typeInfo = typeInfo.BaseType?.GetTypeInfo();
        }

        return null;
    }
}
