using System;
using System.Linq;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace DL444.Ucqu.App.WinUniversal.Controls
{
    public sealed class ScheduleTable : Control
    {
        public ScheduleTable()
        {
            this.DefaultStyleKey = typeof(ScheduleTable);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            flipView = (FlipView)GetTemplateChild("Flip");
            flipView.SelectionChanged += FlipView_SelectionChanged;
            header = (TextBlock)GetTemplateChild("ScheduleWeekHeader");
            Button nextBtn = (Button)GetTemplateChild("NextButton");
            nextBtn.Click += NextBtn_Click;
            Button prevBtn = (Button)GetTemplateChild("PrevButton");
            prevBtn.Click += PrevBtn_Click;
            GoToToday();
        }

        public ScheduleViewModel Schedule
        {
            get => (ScheduleViewModel)GetValue(ScheduleProperty);
            set
            {
                SetValue(ScheduleProperty, value);
                GoToToday();
            }
        }

        public static readonly DependencyProperty ScheduleProperty =
            DependencyProperty.Register(nameof(Schedule), typeof(ScheduleViewModel), typeof(ScheduleTable), new PropertyMetadata(null));

        private async Task PlayHeaderAnimation(string weekNumberDisplay, AnimationDirection direction)
        {
            string fadeAnimation = direction == AnimationDirection.ToLeft ? "ScheduleWeekHeaderFadeToLeftAnimation" : "ScheduleWeekHeaderFadeToRightAnimation";
            string showAnimation = direction == AnimationDirection.ToLeft ? "ScheduleWeekHeaderShowFromRightAnimation" : "ScheduleWeekHeaderShowFromLeftAnimation";
            Storyboard storyboard = (Storyboard)Application.Current.Resources[fadeAnimation];
            storyboard.Stop();
            Storyboard.SetTarget(storyboard, header);
            storyboard.Begin();
            await Task.Delay(100);
            storyboard.Stop();
            header.Text = weekNumberDisplay;
            storyboard = (Storyboard)Application.Current.Resources[showAnimation];
            storyboard.Stop();
            Storyboard.SetTarget(storyboard, header);
            storyboard.Begin();
            await Task.Delay(100);
            storyboard.Stop();
        }

        private async void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            {
                return;
            }
            ScheduleWeekViewModel prev = (ScheduleWeekViewModel)e.RemovedItems[0];
            ScheduleWeekViewModel next = (ScheduleWeekViewModel)e.AddedItems[0];
            if (prev.WeekNumber == next.WeekNumber)
            {
                header.Text = next.WeekNumberDisplay;
                return;
            }
            await PlayHeaderAnimation(next.WeekNumberDisplay, prev.WeekNumber < next.WeekNumber ? AnimationDirection.ToLeft : AnimationDirection.ToRight);
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            flipView.SelectedIndex = Math.Min(flipView.SelectedIndex + 1, Schedule.Weeks.Count - 1);
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            flipView.SelectedIndex = Math.Max(flipView.SelectedIndex - 1, 0);
        }

        private void GoToToday()
        {
            if (flipView == null || Schedule.Weeks == null || Schedule.Weeks.Count == 0)
            {
                return;
            }
            DateTimeOffset today = DateTimeOffset.Now.GetLocalDate();
            DateTimeOffset startDay = Schedule.Weeks.First().Day0.LocalDate;
            if (today < startDay)
            {
                flipView.SelectedIndex = 0;
                return;
            }
            DateTimeOffset endDay = Schedule.Weeks.Last().Day6.LocalDate;
            if (today > endDay)
            {
                flipView.SelectedIndex = Schedule.Weeks.Count - 1;
                return;
            }
            int weekCount = (int)(today - startDay).TotalDays / 7;
            flipView.SelectedIndex = weekCount;
        }

        private FlipView flipView;
        private TextBlock header;

        private enum AnimationDirection
        {
            ToLeft,
            ToRight
        }
    }
}
