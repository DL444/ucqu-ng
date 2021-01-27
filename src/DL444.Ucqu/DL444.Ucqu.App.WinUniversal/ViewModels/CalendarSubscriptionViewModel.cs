using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.Models;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal class CalendarSubscriptionViewModel : INotifyPropertyChanged
    {
        public CalendarSubscriptionViewModel(string username, ICalendarSubscriptionService calendarService)
        {
            this.username = username;
            this.calendarService = calendarService;
        }

        public string SubscriptionId { get; private set; }
        public string GenericHttpsUri { get; private set; }
        public Uri OutlookUri { get; private set; }
        public Uri GoogleUri { get; private set; }

        public bool UpdateInProgress
        {
            get => _updateInProgress;
            private set
            {
                _updateInProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateInProgress)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReady)));
            }
        }

        public bool UpdateFailed
        {
            get => _updateFailed;
            private set
            {
                _updateFailed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateFailed)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReady)));
            }
        }

        public bool IsReady => !UpdateInProgress && !UpdateFailed;

        public bool ResetInProgress
        {
            get => _resetInProgress;
            private set
            {
                _resetInProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanReset)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResetInProgress)));
            }
        }

        public bool ResetSuccess
        {
            get => _resetSuccess;
            private set
            {
                _resetSuccess = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanReset)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResetSuccess)));
            }
        }

        public bool ResetFailed
        {
            get => _resetFailed;
            private set
            {
                _resetFailed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResetFailed)));
            }
        }

        public bool CanReset => !ResetInProgress && !ResetSuccess;

        public bool GetContentInProgress
        {
            get => _getContentInProgress;
            private set
            {
                _getContentInProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GetContentInProgress)));
            }
        }

        public bool GetContentFailed
        {
            get => _getContentFailed;
            set
            {
                _getContentFailed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GetContentFailed)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task Update()
        {
            UpdateInProgress = true;
            try
            {
                DataRequestResult<CalendarSubscription> infoResult = await calendarService.GetCalendarSubscriptionIdAsync();
                if (infoResult.Resource.SubscriptionId == null)
                {
                    bool success = await ResetSubscription();
                    ResetSuccess = false;
                    if (!success)
                    {
                        UpdateFailed = true;
                        return;
                    }
                }
                else
                {
                    Initialize(infoResult.Resource.SubscriptionId);
                }
            }
            catch (BackendAuthenticationFailedException)
            {
                await ((App)Application.Current).SignOut();
                return;
            }
            catch (BackendRequestFailedException)
            {
                UpdateFailed = true;
            }
            finally
            {
                UpdateInProgress = false;
            }
        }

        public async Task<bool> ResetSubscription()
        {
            ResetInProgress = true;
            try
            {
                DataRequestResult<CalendarSubscription> result = await calendarService.ResetCalendarSubscriptionAsync();
                Initialize(result.Resource.SubscriptionId);
                ResetSuccess = true;
                return true;
            }
            catch (BackendAuthenticationFailedException)
            {
                await ((App)Application.Current).SignOut();
                return false;
            }
            catch (BackendRequestFailedException)
            {
                ResetFailed = true;
                return false;
            }
            finally
            {
                ResetInProgress = false;
            }
        }

        public async Task<string> GetSubscriptionContent()
        {
            _ = username ?? throw new InvalidOperationException("Username is not yet set. Call Update method to fetch latest info.");
            _ = SubscriptionId ?? throw new InvalidOperationException("Subscription ID is not yet set. Call Update method to fetch latest info.");
            GetContentInProgress = true;
            try
            {
                if (string.IsNullOrEmpty(subscriptionContent))
                {
                    subscriptionContent = (await calendarService.GetCalendarSubscriptionContentAsync(username, SubscriptionId)).Resource;
                }
                return subscriptionContent;
            }
            catch (BackendRequestFailedException)
            {
                GetContentFailed = true;
                return null;
            }
            finally
            {
                GetContentInProgress = false;
            }
        }

        private void Initialize(string subscriptionId)
        {
            _ = username ?? throw new InvalidOperationException("Username is not yet set. Call Update method to fetch latest info.");
            SubscriptionId = subscriptionId;
            Application app = Application.Current;
            string serviceBaseAddress = app.GetConfigurationValue<string>("Backend:BaseAddress");
            string endpointTemplate = app.GetConfigurationValue<string>("CalendarSubscription:Endpoint");
            endpointTemplate = string.Concat(serviceBaseAddress, endpointTemplate);
            GenericHttpsUri = string.Format(endpointTemplate, username, subscriptionId);
            string endpoint = HttpUtility.UrlEncode(GenericHttpsUri);
            string calendarName = HttpUtility.UrlEncode(app.GetService<ILocalizationService>().GetString("CalendarSubscriptionDefaultName"));
            string outlookUriTemplate = app.GetConfigurationValue<string>("CalendarSubscription:OutlookUri");
            string outlookUri = string.Format(outlookUriTemplate, endpoint, calendarName);
            OutlookUri = new Uri(outlookUri);
            string googleUri = app.GetConfigurationValue<string>("CalendarSubscription:GoogleUri");
            GoogleUri = new Uri(googleUri);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GenericHttpsUri)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OutlookUri)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GoogleUri)));
        }

        private string username;
        private ICalendarSubscriptionService calendarService;
        private string subscriptionContent;
        private bool _updateInProgress;
        private bool _updateFailed;
        private bool _resetInProgress;
        private bool _resetSuccess;
        private bool _resetFailed;
        private bool _getContentInProgress;
        private bool _getContentFailed;
    }
}
