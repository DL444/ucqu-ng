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

            MajorScoreViewModel = new DataViewModel<ScoreSet, ScoreSetViewModel>(
                new ScoreSetViewModel(),
                x => new ScoreSetViewModel(x),
                () => localDataService.GetScoreAsync(false),
                () => remoteDataService.GetScoreAsync(false),
                x => cacheService.SetScoreAsync(x)
            );

            SecondMajorScoreViewModel = new DataViewModel<ScoreSet, ScoreSetViewModel>(
                new ScoreSetViewModel(),
                x => new ScoreSetViewModel(x),
                () => localDataService.GetScoreAsync(true),
                () => remoteDataService.GetScoreAsync(true),
                x => cacheService.SetScoreAsync(x)
            );

            StudentInfoViewModel = new DataViewModel<StudentInfo, StudentInfoViewModel>(
                new StudentInfoViewModel(),
                x => new StudentInfoViewModel(x),
                () => localDataService.GetStudentInfoAsync(),
                () => remoteDataService.GetStudentInfoAsync(),
                x => Task.CompletedTask,
                _ => false
            );
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Application.Current.GetService<INotificationService>().ClearToast(ToastTypes.ScoreChange);
            bool signedIn = Application.Current.GetService<ICredentialService>().IsSignedIn;
            Task studentInfoUpdateTask = StudentInfoViewModel.StartUpdateAsync(signedIn);
            Task majorUpdateTask = MajorScoreViewModel.StartUpdateAsync(signedIn);
            await studentInfoUpdateTask;
            if (StudentInfoViewModel.Value.HasSecondMajor && ScoreSectionsPivot.Items.Count == 1)
            {
                ScoreSectionsPivot.Items.Add(secondMajorItem);
            }
            await majorUpdateTask;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            MajorScoreViewModel.Dispose();
            SecondMajorScoreViewModel.Dispose();
            StudentInfoViewModel.Dispose();
        }

        internal DataViewModel<ScoreSet, ScoreSetViewModel> MajorScoreViewModel { get; }
        internal DataViewModel<ScoreSet, ScoreSetViewModel> SecondMajorScoreViewModel { get; }
        internal DataViewModel<StudentInfo, StudentInfoViewModel> StudentInfoViewModel { get; }

        private async void ScoreSectionsPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!secondMajorFetched && e.AddedItems.Contains(secondMajorItem))
            {
                bool signedIn = Application.Current.GetService<ICredentialService>().IsSignedIn;
                await SecondMajorScoreViewModel.StartUpdateAsync(signedIn);
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
