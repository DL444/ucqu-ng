using DL444.Ucqu.App.WinUniversal.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DL444.Ucqu.App.WinUniversal.Controls
{
    public sealed class ScoreTable : Control
    {
        public ScoreTable()
        {
            this.DefaultStyleKey = typeof(ScoreTable);
        }

        internal ScoreSetViewModel ScoreSet
        {
            get => (ScoreSetViewModel)GetValue(ScoreSetProperty);
            set => SetValue(ScoreSetProperty, value);
        }

        public static readonly DependencyProperty ScoreSetProperty =
            DependencyProperty.Register(nameof(ScoreSet), typeof(ScoreSetViewModel), typeof(ScoreTable), new PropertyMetadata(null));

        public string Major
        {
            get => (string)GetValue(MajorProperty);
            set => SetValue(MajorProperty, value);
        }

        public static readonly DependencyProperty MajorProperty =
            DependencyProperty.Register(nameof(Major), typeof(string), typeof(ScoreTable), new PropertyMetadata(null));
    }
}
