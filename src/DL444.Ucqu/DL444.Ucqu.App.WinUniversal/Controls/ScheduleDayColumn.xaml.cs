using System;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DL444.Ucqu.App.WinUniversal.Controls
{
    public sealed partial class ScheduleDayColumn : UserControl
    {
        public ScheduleDayColumn()
        {
            this.InitializeComponent();
            this.SizeChanged += ScheduleDayColumn_SizeChanged;
        }

        public ScheduleDayViewModel Day
        {
            get => (ScheduleDayViewModel)GetValue(DayProperty);
            set => SetValue(DayProperty, value);
        }

        public static readonly DependencyProperty DayProperty =
            DependencyProperty.Register(nameof(Day), typeof(ScheduleDayViewModel), typeof(ScheduleDayColumn), new PropertyMetadata(null, OnDayChanged));

        private async Task UpdateDay(bool force)
        {
            Bindings.Update();

            bool shouldUpdate = false;
            if (!force && EntryCanvas.Children.Count == Day.ConsolidatedEntries.Count)
            {
                for (int i = 0; i < EntryCanvas.Children.Count; i++)
                {
                    ScheduleConsolidationViewModel curr = ((ScheduleTableItem)EntryCanvas.Children[i]).ConsolidatedEntry;
                    ScheduleConsolidationViewModel next = Day.ConsolidatedEntries[i];
                    if (curr.ConflictCount != next.ConflictCount
                        || !curr.DisplayEntry.Name.Equals(next.DisplayEntry.Name, StringComparison.Ordinal)
                        || !curr.DisplayEntry.Room.Equals(next.DisplayEntry.Room, StringComparison.Ordinal)
                        || curr.DisplayEntry.StartSession != next.DisplayEntry.StartSession
                        || curr.DisplayEntry.EndSession != next.DisplayEntry.EndSession)
                    {
                        shouldUpdate = true;
                        break;
                    }
                }
            }
            else
            {
                shouldUpdate = true;
            }

            if (!shouldUpdate)
            {
                return;
            }
            EntryCanvas.Children.Clear();
            // To prevent first-frame flickering.
            await Task.Delay(10);

            foreach (ScheduleConsolidationViewModel entry in Day.ConsolidatedEntries)
            {
                int startSession = Math.Min(entry.DisplayEntry.StartSession, 12) - 1;
                int endSession = Math.Min(entry.DisplayEntry.EndSession, 12) - 1;
                ScheduleTableItem item = new ScheduleTableItem();
                item.ConsolidatedEntry = entry;
                item.Width = EntryCanvas.ActualWidth;
                item.Height = EntryCanvas.ActualHeight / 12 * (endSession - startSession + 1);
                Canvas.SetTop(item, EntryCanvas.ActualHeight / 12 * startSession);
                Canvas.SetLeft(item, 0);
                EntryCanvas.Children.Add(item);
            }
        }

        private static async void OnDayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            await ((ScheduleDayColumn)d).UpdateDay(false);
        }

        private async void ScheduleDayColumn_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Width == 0)
            {
                await UpdateDay(true);
            }
            else
            {
                foreach (UIElement child in EntryCanvas.Children)
                {
                    if (child is ScheduleTableItem item)
                    {
                        item.Width = e.NewSize.Width;
                        item.Height *= e.NewSize.Height / e.PreviousSize.Height;
                        double prevTop = Canvas.GetTop(item);
                        Canvas.SetTop(item, prevTop * (e.NewSize.Height / e.PreviousSize.Height));
                    }
                }
            }
        }
    }
}
