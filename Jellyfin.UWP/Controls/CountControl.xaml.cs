using Jellyfin.UWP.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls
{
    public sealed partial class CountControl : UserControl
    {
        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register(
                nameof(Item),
                typeof(UIItem),
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
            this.InitializeComponent();
        }

        public UIItem Item
        {
            get { return (UIItem)GetValue(ItemProperty); }
            set { SetValue(ItemProperty, value); }
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
}
