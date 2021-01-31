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

        public ScheduleConsolidationViewModel ConsolidatedEntry
        {
            get => (ScheduleConsolidationViewModel)GetValue(ConsolidatedEntryProperty);
            set => SetValue(ConsolidatedEntryProperty, value);
        }

        public static readonly DependencyProperty ConsolidatedEntryProperty =
            DependencyProperty.Register(nameof(ConsolidatedEntry), typeof(ScheduleConsolidationViewModel), typeof(ScheduleTableItem), new PropertyMetadata(null));
    }
}
