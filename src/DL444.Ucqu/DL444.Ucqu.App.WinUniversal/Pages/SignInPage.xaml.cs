using System;
using System.Numerics;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using Microsoft.AppCenter.Analytics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SignInPage : Page
    {
        public SignInPage()
        {
            this.InitializeComponent();
            ICredentialService credentialService = Application.Current.GetService<ICredentialService>();
            ISignInService signInService = Application.Current.GetService<ISignInService>();
            ViewModel = new SignInViewModel(credentialService, signInService, Application.Current.GetConfiguration());
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Analytics.TrackEvent("Sign in page reached");
            if (e.Parameter is string args)
            {
                arguments = args;
            }
        }

        internal SignInViewModel ViewModel { get; }

        private async void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await TrySignIn();
            }
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            await TrySignIn();
        }

        private async Task TrySignIn()
        {
            try
            {
                bool success = await ViewModel.SignInAsync();
                if (success)
                {
                    Analytics.TrackEvent("Sign in success");
                    ((App)Application.Current).NavigateToFirstPage(arguments, true);
                }
            }
            catch (BackendAuthenticationFailedException)
            {
                Analytics.TrackEvent("Sign in failed");
                var shakeAnimation = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
                shakeAnimation.InsertKeyFrame(0.125f, new Vector3(-10.0f, 0.0f, 0.0f));
                shakeAnimation.InsertKeyFrame(0.375f, new Vector3(010.0f, 0.0f, 0.0f));
                shakeAnimation.InsertKeyFrame(0.625f, new Vector3(-10.0f, 0.0f, 0.0f));
                shakeAnimation.InsertKeyFrame(0.875f, new Vector3(010.0f, 0.0f, 0.0f));
                shakeAnimation.InsertKeyFrame(1.000f, new Vector3(000.0f, 0.0f, 0.0f));
                shakeAnimation.Duration = TimeSpan.FromMilliseconds(500);
                shakeAnimation.Target = "Translation";
                SignInButton.StartAnimation(shakeAnimation);
            }
        }

        private string arguments;
    }
}
