using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace DL444.Ucqu.App.WinUniversal.Controls
{
    public sealed partial class ScheduleDayColumn : UserControl
    {
        public ScheduleDayColumn()
        {
            this.InitializeComponent();
        }


        public ScheduleDayViewModel Day
        {
            get => (ScheduleDayViewModel)GetValue(DayProperty);
            set => SetValue(DayProperty, value);
        }

        public static readonly DependencyProperty DayProperty =
            DependencyProperty.Register(nameof(Day), typeof(ScheduleDayViewModel), typeof(ScheduleDayColumn), new PropertyMetadata(null, OnDayChanged));

        private void UpdateDay()
        {
            Bindings.Update();
        }

        private static void OnDayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScheduleDayColumn)d).UpdateDay();
        }
    }
}
