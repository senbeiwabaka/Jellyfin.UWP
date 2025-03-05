using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls;

internal sealed partial class ViewedIconControl : UserControl
{
    public static readonly DependencyProperty HasBeenViewedProperty =
        DependencyProperty.Register(
            nameof(HasBeenViewed),
            typeof(bool),
            typeof(ViewedFavoriteButtonControl),
            new PropertyMetadata(null));

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

    public ViewedIconControl()
    {
        InitializeComponent();
    }

    public bool HasBeenViewed
    {
        get { return (bool)GetValue(HasBeenViewedProperty); }
        set { SetValue(HasBeenViewedProperty, value); }
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
