using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using DL444.Ucqu.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
            secondMajorItem = (PivotItem)ScoreSectionsPivot.Items[1];
            ScoreSectionsPivot.Items.RemoveAt(1);
            localDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.LocalCache);
            remoteDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.Online);
            cacheService = Application.Current.GetService<ILocalCacheService>();
            MajorScoreViewModel = new DataViewModel<ScoreSet, ScoreSetViewModel>(new ScoreSetViewModel());
            SecondMajorScoreViewModel = new DataViewModel<ScoreSet, ScoreSetViewModel>(new ScoreSetViewModel());
            StudentInfoViewModel = new DataViewModel<StudentInfo, StudentInfoViewModel>(new StudentInfoViewModel());
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Task studentInfoUpdateTask = StudentInfoViewModel.UpdateAsync(
                () => localDataService.GetStudentInfoAsync(),
                () => remoteDataService.GetStudentInfoAsync(),
                x => Task.CompletedTask,
                x => new StudentInfoViewModel(x),
                preferLocal: true);
            Task majorUpdateTask = MajorScoreViewModel.UpdateAsync(
                () => localDataService.GetScoreAsync(false),
                () => remoteDataService.GetScoreAsync(false),
                x => cacheService.SetScoreAsync(x),
                x => new ScoreSetViewModel(x)
            );
            await studentInfoUpdateTask;
            if (StudentInfoViewModel.Value.HasSecondMajor && ScoreSectionsPivot.Items.Count == 1)
            {
                ScoreSectionsPivot.Items.Add(secondMajorItem);
            }
            await majorUpdateTask;
        }

        internal DataViewModel<ScoreSet, ScoreSetViewModel> MajorScoreViewModel { get; }
        internal DataViewModel<ScoreSet, ScoreSetViewModel> SecondMajorScoreViewModel { get; }
        internal DataViewModel<StudentInfo, StudentInfoViewModel> StudentInfoViewModel { get; }

        private async void ScoreSectionsPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!secondMajorFetched && e.AddedItems.Contains(secondMajorItem))
            {
                await SecondMajorScoreViewModel.UpdateAsync(
                    () => localDataService.GetScoreAsync(true),
                    () => remoteDataService.GetScoreAsync(true),
                    x => cacheService.SetScoreAsync(x),
                    x => new ScoreSetViewModel(x)
                );
                secondMajorFetched = true;
            }
        }

        private IDataService localDataService;
        private IDataService remoteDataService;
        private ILocalCacheService cacheService;
        private PivotItem secondMajorItem;
        private bool secondMajorFetched;
    }
}
