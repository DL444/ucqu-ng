using System.ComponentModel;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.Models;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Configuration;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal class SignInViewModel : INotifyPropertyChanged
    {
        public SignInViewModel(ICredentialService credentialService, ISignInService signInService, IConfiguration config)
        {
            this.credentialService = credentialService;
            this.signInService = signInService;
            tenantId = config.GetValue<string>("Tenant:TenantId");
            pollInterval = config.GetValue<int>("Backend:UserInitPollInterval");
        }

        public string Username
        {
            get => _username;
            set
            {
                _canSignIn = true;
                _username = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSignIn)));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _canSignIn = true;
                _password = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSignIn)));
            }
        }

        public bool CanSignIn => _canSignIn && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        public bool InProgress
        {
            get => _inProgress;
            set
            {
                _inProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InProgress)));
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMessage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            }
        }

        public bool HasMessage => !string.IsNullOrEmpty(Message);

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task<bool> SignInAsync()
        {
            if (!CanSignIn)
            {
                return false;
            }

            Message = null;
            InProgress = true;
            try
            {
                StudentCredential credential = new StudentCredential(Username, StudentCredential.GetPasswordHash(Username, Password, tenantId));
                DataRequestResult<AccessToken> tokenResult = await signInService.SignInAsync(credential, true);
                credentialService.SetCredential(credential.StudentId, credential.PasswordHash);
                if (!tokenResult.Resource.Completed)
                {
                    Message = tokenResult.Message;
                    await signInService.WaitForUserInitializationAsync(tokenResult.Resource.Location, pollInterval);
                }
                return true;
            }
            catch (BackendAuthenticationFailedException ex)
            {
                Crashes.TrackError(ex);
                Analytics.TrackEvent("Authentication failed");
                _canSignIn = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSignIn)));
                throw;
            }
            catch (BackendRequestFailedException ex)
            {
                Crashes.TrackError(ex);
                Message = ex.DisplayMessage;
                return false;
            }
            finally
            {
                InProgress = false;
            }
        }

        private string _username;
        private string _password;
        private bool _canSignIn;
        private bool _inProgress;
        private string _message;

        private ICredentialService credentialService;
        private ISignInService signInService;
        private string tenantId;
        private int pollInterval;
    }
}
