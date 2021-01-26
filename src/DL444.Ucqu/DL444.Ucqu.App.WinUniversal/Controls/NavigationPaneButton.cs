using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace DL444.Ucqu.App.WinUniversal.Controls
{
    public sealed class NavigationPaneButton : Button
    {
        public NavigationPaneButton()
        {
            this.DefaultStyleKey = typeof(NavigationPaneButton);
        }

        public IconElement Icon
        {
            get => (IconElement)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(NavigationPaneButton), null);
    }
}
