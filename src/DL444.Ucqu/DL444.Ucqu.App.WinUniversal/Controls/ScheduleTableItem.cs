using DL444.Ucqu.App.WinUniversal.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DL444.Ucqu.App.WinUniversal.Controls
{
    public sealed class ScheduleTableItem : Control
    {
        public ScheduleTableItem()
        {
            this.DefaultStyleKey = typeof(ScheduleTableItem);
        }

        public ScheduleEntryViewModel Entry
        {
            get => (ScheduleEntryViewModel)GetValue(EntryProperty);
            set => SetValue(EntryProperty, value);
        }

        public static readonly DependencyProperty EntryProperty =
            DependencyProperty.Register(nameof(Entry), typeof(ScheduleEntryViewModel), typeof(ScheduleTableItem), new PropertyMetadata(null));
    }
}
