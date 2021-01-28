using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Models;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal class DataViewModel<TModel, TViewModel> : INotifyPropertyChanged
    {
        public DataViewModel(TViewModel defaultValue) => Value = defaultValue;

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

        public async Task UpdateAsync(
            Func<Task<DataRequestResult<TModel>>> localFetchFunc,
            Func<Task<DataRequestResult<TModel>>> remoteFetchFunc,
            Func<TModel, Task> cacheUpdateFunc,
            Func<TModel, TViewModel> viewModelCreateFunc)
        {
            await UpdateAsync(localFetchFunc, remoteFetchFunc, cacheUpdateFunc, viewModelCreateFunc, false);
        }

        public async Task UpdateAsync(
            Func<Task<DataRequestResult<TModel>>> localFetchFunc,
            Func<Task<DataRequestResult<TModel>>> remoteFetchFunc,
            Func<TModel, Task> cacheUpdateFunc,
            Func<TModel, TViewModel> viewModelCreateFunc,
            bool preferLocal)
        {
            await UpdateAsync(localFetchFunc, remoteFetchFunc, cacheUpdateFunc, viewModelCreateFunc, preferLocal, _ => !preferLocal);
        }

        public async Task UpdateAsync(
            Func<Task<DataRequestResult<TModel>>> localFetchFunc,
            Func<Task<DataRequestResult<TModel>>> remoteFetchFunc,
            Func<TModel, Task> cacheUpdateFunc,
            Func<TModel, TViewModel> viewModelCreateFunc,
            bool preferLocal,
            Func<TModel, bool> shouldFetchRemote)
        {
            Status = DataStatus.InProgress;
            bool localFetchSuccess = false;
            Task<DataRequestResult<TModel>> remoteTask = null;
            if (!preferLocal)
            {
                remoteTask = remoteFetchFunc();
            }
            try
            {
                TModel cachedInfo = (await localFetchFunc()).Resource;
                localFetchSuccess = true;
                IsValueReady = true;
                Value = viewModelCreateFunc(cachedInfo);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValueReady)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                if (preferLocal && !shouldFetchRemote(cachedInfo))
                {
                    Status = DataStatus.Ok;
                    return;
                }
            }
            catch (LocalCacheRequestFailedException) { }

            if (remoteTask == null)
            {
                remoteTask = remoteFetchFunc();
            }
            TModel remoteInfo = default;
            bool remoteFetchSuccess = false;
            try
            {
                remoteInfo = (await remoteTask).Resource;
                remoteFetchSuccess = true;
                IsValueReady = true;
                Value = viewModelCreateFunc(remoteInfo);
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
                Status = localFetchSuccess ? DataStatus.Stale : DataStatus.Error;
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

        private DataStatus status;
    }

    internal enum DataStatus
    {
        Ok,
        InProgress,
        Stale,
        Error
    }
}
