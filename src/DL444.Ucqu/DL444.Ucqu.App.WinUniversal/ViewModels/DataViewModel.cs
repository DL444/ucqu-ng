using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.App.WinUniversal.Services;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal class DataViewModel<TModel, TViewModel> : INotifyPropertyChanged, IMessageListener<SignInMessage>, IDisposable
    {
        public DataViewModel(
            TViewModel defaultValue,
            Func<TModel, TViewModel> viewModelTransform,
            Func<Task<DataRequestResult<TModel>>> localFetchFunc,
            Func<Task<DataRequestResult<TModel>>> remoteFetchFunc,
            Func<TModel, Task> cacheUpdateFunc
        ) : this( defaultValue, viewModelTransform, localFetchFunc, remoteFetchFunc, cacheUpdateFunc, _ => true) { }

        public DataViewModel(
            TViewModel defaultValue,
            Func<TModel, TViewModel> viewModelTransform,
            Func<Task<DataRequestResult<TModel>>> localFetchFunc,
            Func<Task<DataRequestResult<TModel>>> remoteFetchFunc,
            Func<TModel, Task> cacheUpdateFunc,
            Predicate<TModel> shouldFetchRemote,
            bool remoteRequiresAuth = true)
        {
            Value = defaultValue;
            this.viewModelTransform = viewModelTransform;
            this.localFetchFunc = localFetchFunc;
            this.remoteFetchFunc = remoteFetchFunc;
            this.cacheUpdateFunc = cacheUpdateFunc;
            this.shouldFetchRemote = shouldFetchRemote;
            this.remoteRequiresAuth = remoteRequiresAuth;
            if (remoteRequiresAuth)
            {
                Application.Current.GetService<IMessageService<SignInMessage>>().Register(this);
            }
        }

        internal DataStatus Status
        {
            get => status;
            private set
            {
                status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDataOk)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDataInProgress)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDataStale)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDataError)));
            }
        }
        public bool IsDataOk => Status == DataStatus.Ok;
        public bool IsDataInProgress => Status == DataStatus.InProgress;
        public bool IsDataStale => Status == DataStatus.Stale;
        public bool IsDataError => Status == DataStatus.Error;

        public bool IsValueReady { get; private set; }
        public TViewModel Value { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task StartUpdateAsync(bool signedIn)
        {
            Status = DataStatus.InProgress;
            localFetchTask = FetchLocal();
            bool shouldFetchRemote = await localFetchTask;
            if (shouldFetchRemote)
            {
                if (signedIn || !remoteRequiresAuth)
                {
                    await FetchRemote();
                }
                else
                {
                    return;
                }
            }
            else
            {
                Status = DataStatus.Ok;
            }
        }

        public async void OnMessaged(SignInMessage args)
        {
            bool shouldFetchRemote = localFetchTask != null && await localFetchTask;
            if (!shouldFetchRemote)
            {
                return;
            }

            if (args.Success)
            {
                await FetchRemote();
            }
            else
            {
                Status = IsValueReady ? DataStatus.Stale : DataStatus.Error;
            }
        }

        public void Dispose()
        {
            if (remoteRequiresAuth)
            {
                Application.Current.GetService<IMessageService<SignInMessage>>().Unregister(this);
            }
        }

        private async Task<bool> FetchLocal()
        {
            try
            {
                TModel cachedInfo = (await localFetchFunc()).Resource;
                IsValueReady = true;
                Value = viewModelTransform(cachedInfo);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValueReady)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                return shouldFetchRemote(cachedInfo);
            }
            catch (LocalCacheRequestFailedException) 
            {
                return true;
            }
        }
        private async Task FetchRemote()
        {
            Task<DataRequestResult<TModel>> remoteTask = remoteFetchFunc();
            TModel remoteInfo = default;
            bool remoteFetchSuccess = false;
            try
            {
                remoteInfo = (await remoteTask).Resource;
                remoteFetchSuccess = true;
                IsValueReady = true;
                Value = viewModelTransform(remoteInfo);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValueReady)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                Status = DataStatus.Ok;
            }
            catch (BackendAuthenticationFailedException)
            {
                await ((App)Application.Current).SignOutAsync();
                return;
            }
            catch (BackendRequestFailedException)
            {
                Status = IsValueReady ? DataStatus.Stale : DataStatus.Error;
            }

            if (remoteFetchSuccess)
            {
                try
                {
                    await cacheUpdateFunc(remoteInfo);
                }
                catch (LocalCacheRequestFailedException) { }
            }
        }

        private readonly Func<TModel, TViewModel> viewModelTransform;
        private readonly Func<Task<DataRequestResult<TModel>>> localFetchFunc;
        private readonly Func<Task<DataRequestResult<TModel>>> remoteFetchFunc;
        private readonly Func<TModel, Task> cacheUpdateFunc;
        private readonly Predicate<TModel> shouldFetchRemote;
        private readonly bool remoteRequiresAuth;
        private DataStatus status;
        private Task<bool> localFetchTask;
    }

    internal enum DataStatus
    {
        Ok,
        InProgress,
        Stale,
        Error
    }
}
