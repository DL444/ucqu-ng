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
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScorePage : Page
    {
        public ScorePage()
        {
            this.InitializeComponent();
            localDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.LocalCache);
            remoteDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.Online);
            cacheService = Application.Current.GetService<ILocalCacheService>();
            MajorScoreViewModel = new DataViewModel<ScoreSet, ScoreSetViewModel>(new ScoreSetViewModel());
            SecondMajorScoreViewModel = new DataViewModel<ScoreSet, ScoreSetViewModel>(new ScoreSetViewModel());
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Task majorUpdateTask = MajorScoreViewModel.UpdateAsync(
                () => localDataService.GetScoreAsync(false),
                () => remoteDataService.GetScoreAsync(false),
                x => cacheService.SetScoreAsync(x),
                x => new ScoreSetViewModel(x)
            );
            Task secondMajorUpdateTask = MajorScoreViewModel.UpdateAsync(
                () => localDataService.GetScoreAsync(true),
                () => remoteDataService.GetScoreAsync(true),
                x => cacheService.SetScoreAsync(x),
                x => new ScoreSetViewModel(x)
            );
            await Task.WhenAll(majorUpdateTask, secondMajorUpdateTask);
        }

        internal DataViewModel<ScoreSet, ScoreSetViewModel> MajorScoreViewModel { get; }
        internal DataViewModel<ScoreSet, ScoreSetViewModel> SecondMajorScoreViewModel { get; }

        private IDataService localDataService;
        private IDataService remoteDataService;
        private ILocalCacheService cacheService;
    }
}
