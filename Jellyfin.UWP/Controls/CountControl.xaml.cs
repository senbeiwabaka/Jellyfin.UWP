using Jellyfin.UWP.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls;

internal sealed partial class CountControl : UserControl
{
    public static readonly DependencyProperty UserDataProperty =
        DependencyProperty.Register(
            nameof(UserData),
            typeof(UIUserData),
            typeof(CountControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PositionLeftProperty =
               DependencyProperty.Register(
           nameof(PositionLeft),
           typeof(string),
           typeof(CountControl),
           new PropertyMetadata(null));

    public static readonly DependencyProperty PositionTopProperty =
        DependencyProperty.Register(
            nameof(PositionTop),
            typeof(string),
            typeof(CountControl),
            new PropertyMetadata(null));

    public CountControl()
    {
        InitializeComponent();
    }

    public UIUserData UserData
    {
        get { return (UIUserData)GetValue(UserDataProperty); }
        set { SetValue(UserDataProperty, value); }
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
}
