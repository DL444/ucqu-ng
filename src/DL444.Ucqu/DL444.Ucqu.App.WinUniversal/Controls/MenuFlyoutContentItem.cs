using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DL444.Ucqu.App.WinUniversal.Controls
{
    public sealed class MenuFlyoutContentItem : MenuFlyoutItem
    {
        public MenuFlyoutContentItem()
        {
            this.DefaultStyleKey = typeof(MenuFlyoutContentItem);
        }

        public UIElement Content
        {
            get => GetValue(ContentProperty) as UIElement;
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(UIElement), typeof(MenuFlyoutContentItem), null);
    }
}
