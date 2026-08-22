using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Jellyfin.Models;
using Jellyfin.ViewModels.Controls;
using Jellyfin.ViewModels.MessagingModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.UWP.Controls;

internal sealed partial class ViewedFavoriteButtonControl : UserControl
{
    public static readonly DependencyProperty PositionLeftProperty =
               DependencyProperty.Register(
           nameof(PositionLeft),
           typeof(string),
           typeof(ViewedFavoriteButtonControl),
           new PropertyMetadata(null));

    public static readonly DependencyProperty PositionTopProperty =
        DependencyProperty.Register(
            nameof(PositionTop),
            typeof(string),
            typeof(ViewedFavoriteButtonControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty UserItemProperty =
                  DependencyProperty.Register(
               nameof(UserItem),
               typeof(UIItem),
               typeof(ViewedFavoriteButtonControl),
               new PropertyMetadata(null));

    public ViewedFavoriteButtonControl()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetRequiredService<ViewedFavoriteViewModel>();

        Loaded += ViewedFavoriteButtonControl_Loaded;
        Unloaded += ViewedFavoriteButtonControl_Unloaded;
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

    public ViewedFavoriteViewModel ViewModel => (ViewedFavoriteViewModel)DataContext;

    private void ViewedFavoriteButtonControl_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<UIItemChangedMesage>(this, (r, m) =>
        {
            // Handle the message here, with r being the recipient and m being the
            // input message. Using the recipient passed as input makes it so that
            // the lambda expression doesn't capture "this", improving performance.

            if (m.Value.Id == UserItem.Id)
            {
                UserItem = m.Value;

                ViewModel.Initialize(UserItem);
            }
        });

        ViewModel.Initialize(UserItem);
    }

    private void ViewedFavoriteButtonControl_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<UIItemChangedMesage>(this);
    }
}
