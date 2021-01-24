using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using DL444.Ucqu.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using WinUI = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            localDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.LocalCache);
            remoteDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.Online);
            cacheService = Application.Current.GetService<ILocalCacheService>();
            NavigationView.SelectedItem = NavigationView.MenuItems.First();
            StudentInfoViewModel = new DataViewModel<StudentInfo, StudentInfoViewModel>(new StudentInfoViewModel());
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await StudentInfoViewModel.UpdateAsync(
                () => localDataService.GetStudentInfoAsync(),
                () => remoteDataService.GetStudentInfoAsync(),
                x => cacheService.SetStudentInfoAsync(x),
                x => new StudentInfoViewModel(x)
            );
        }

        internal DataViewModel<StudentInfo, StudentInfoViewModel> StudentInfoViewModel { get; }

        private void NavigationView_SelectionChanged(WinUI.NavigationView sender, WinUI.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is WinUI.NavigationViewItem item && item.Tag is string key)
            {
                Navigate(key);
            }
        }

        private void Navigate(string key)
        {
            switch (key)
            {
                case "Schedule":
                    if (ContentFrame.CurrentSourcePageType == typeof(ScorePage))
                    {
                        ContentFrame.Navigate(typeof(SchedulePage), null, new SlideNavigationTransitionInfo()
                        {
                            Effect = SlideNavigationTransitionEffect.FromLeft
                        });
                    }
                    else
                    {
                        ContentFrame.Navigate(typeof(SchedulePage), null, new EntranceNavigationTransitionInfo());
                    }
                    break;
                case "Score":
                    if (ContentFrame.CurrentSourcePageType == typeof(SchedulePage))
                    {
                        ContentFrame.Navigate(typeof(ScorePage), null, new SlideNavigationTransitionInfo()
                        {
                            Effect = SlideNavigationTransitionEffect.FromRight
                        });
                    }
                    else
                    {
                        ContentFrame.Navigate(typeof(ScorePage), null, new EntranceNavigationTransitionInfo());
                    }
                    break;
            }
        }

        private async Task SignOut() => await ((App)Application.Current).SignOut();

        private IDataService localDataService;
        private IDataService remoteDataService;
        private ILocalCacheService cacheService;
    }
}
