using CommunityToolkit.Mvvm.Messaging;
using Jellyfin.Models;
using Jellyfin.ViewModels.MessagingModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls;

internal sealed partial class ViewedIconControl : UserControl
{
    public static readonly DependencyProperty PositionLeftProperty =
        DependencyProperty.Register(
            nameof(PositionLeft),
            typeof(string),
            typeof(ViewedIconControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PositionTopProperty =
        DependencyProperty.Register(
            nameof(PositionTop),
            typeof(string),
            typeof(ViewedIconControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty UserItemProperty =
                      DependencyProperty.Register(
           nameof(UserItem),
           typeof(UIItem),
           typeof(CountControl),
           new PropertyMetadata(null));

    public ViewedIconControl()
    {
        InitializeComponent();

        Loaded += ViewedIconControl_Loaded;
        Unloaded += ViewedIconControl_Unloaded;
    }

    public string PositionLeft
    {
        get { return (string)GetValue(PositionLeftProperty); }
        set { SetValue(PositionLeftProperty, value); }
    }

    public string PositionTop
    {
        get { return (string)GetValue(PositionTopProperty); }
        set { SetValue(PositionTopProperty, value); }
    }

    public UIItem UserItem
    {
        get { return (UIItem)GetValue(UserItemProperty); }
        set { SetValue(UserItemProperty, value); }
    }

    private void ViewedIconControl_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<UIItemChangedMesage>(this, (r, m) =>
        {
            if (m.Value.Id == UserItem.Id)
            {
                UserItem = m.Value;
            }
        });
    }

    private void ViewedIconControl_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<UIItemChangedMesage>(this);
    }
}
