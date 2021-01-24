using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.Models;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class BackendService : IDataService, ISignInService, ICalendarSubscriptionService
    {
        public BackendService(HttpClient httpClient, ICredentialService credentialService)
        {
            client = httpClient;
            this.credentialService = credentialService;
            retryWithAuthPolicy = Policy
                .HandleResult<HttpResponseMessage>(x => x.StatusCode == HttpStatusCode.Unauthorized)
                .RetryAsync(onRetryAsync: (result, count) => SignInAsync());
        }

        public DataSource DataSource => DataSource.Online;

        public async Task<DataRequestResult<AccessToken>> SignInAsync(StudentCredential credential, bool createAccount = false)
        {
            _ = credential?.StudentId ?? throw new InvalidOperationException("User credential is not yet configured.");
            _ = credential?.PasswordHash ?? throw new InvalidOperationException("User credential is not yet configured.");

            try
            {
                HttpResponseMessage response = await client.PostAsync($"signIn/{createAccount}", new JsonStringContent(credential));
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                {
                    var result = await response.Content.ReadAsJsonObjectAsync<BackendResult<AccessToken>>();
                    credentialService.Token = result.Resource.Token;
                    return new DataRequestResult<AccessToken>(result.Resource, result.Message);
                }
                else
                {
                    BackendRequestFailedException exception;
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        exception = new BackendAuthenticationFailedException("Request failed due to incorrect credentials. Endpoint: signIn.");
                    }
                    else
                    {
                        exception = new BackendRequestFailedException($"Request failed due to an unexpected response status {response.StatusCode}. Endpoint: signIn.");
                    }
                    exception.DisplayMessage = await GetBackendMessageAsync(response.Content);
                    throw exception;
                }
            }
            catch (HttpRequestException ex)
            {
                throw GetDefaultException($"signIn", ex);
            }
        }

        public Task<DataRequestResult<AccessToken>> SignInAsync(bool createAccount = false) 
            => SignInAsync(new StudentCredential(credentialService.Username, credentialService.PasswordHash), createAccount);

        public async Task WaitForUserInitializationAsync(string location, int pollInterval)
        {
            for (int i = 0; i < 60 / pollInterval + 1; i++)
            {
                await Task.Delay(pollInterval * 1000);
                try
                {
                    HttpResponseMessage response = await client.GetAsync(location);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return;
                    }
                }
                catch (HttpRequestException) { }
            }
        }

        public async Task<DataRequestResult<WellknownData>> GetWellknownDataAsync()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync("wellknown");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = await response.Content.ReadAsJsonObjectAsync<BackendResult<WellknownData>>();
                    return new DataRequestResult<WellknownData>(result.Resource, result.Message);
                }
                else
                {
                    BackendRequestFailedException exception = new BackendRequestFailedException($"Request failed due to an unexpected response status {response.StatusCode}. Endpoint: wellknown.");
                    exception.DisplayMessage = await GetBackendMessageAsync(response.Content);
                    throw exception;
                }
            }
            catch (HttpRequestException ex)
            {
                throw GetDefaultException("wellknown", ex);
            }
        }

        public Task<DataRequestResult<StudentInfo>> GetStudentInfoAsync() => SendGenericRequestAsync<StudentInfo>(HttpMethod.Get, "studentInfo", true);

        public Task<DataRequestResult<Schedule>> GetScheduleAsync() => SendGenericRequestAsync<Schedule>(HttpMethod.Get, "schedule", true);

        public Task<DataRequestResult<ExamSchedule>> GetExamsAsync() => SendGenericRequestAsync<ExamSchedule>(HttpMethod.Get, "exams", true);

        public Task<DataRequestResult<ScoreSet>> GetScoreAsync(bool isSecondMajor) => SendGenericRequestAsync<ScoreSet>(HttpMethod.Get, $"score/{(isSecondMajor ? 2 : 1)}", true);

        public Task<DataRequestResult<object>> DeleteUserAsync() => SendGenericRequestAsync<object>(HttpMethod.Delete, "user", true);

        public Task<DataRequestResult<DeveloperMessage>> GetDeveloperMessagesAsync() => SendGenericRequestAsync<DeveloperMessage>(HttpMethod.Get, "devMessage/windows", false);

        public async Task<DataRequestResult<CalendarSubscription>> GetCalendarSubscriptionIdAsync()
        {
            DataRequestResult<StudentInfo> info = await GetStudentInfoAsync();
            return new DataRequestResult<CalendarSubscription>(new CalendarSubscription(info.Resource.CalendarSubscriptionId), null);
        }

        public async Task<DataRequestResult<string>> GetCalendarSubscriptionContentAsync(string username, string subscriptionId)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"calendar/{username}/{subscriptionId}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return new DataRequestResult<string>(result, null);
                }
                else
                {
                    throw new BackendRequestFailedException($"Request failed due to an unexpected response status {response.StatusCode}. Endpoint: calendar/{{user}}/{{id}}.")
                    {
                        // TODO: Add localization service.
                        DisplayMessage = string.Empty
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                throw GetDefaultException("calendar/{user}/{id}", ex);
            }
        }

        public Task<DataRequestResult<CalendarSubscription>> ResetCalendarSubscriptionAsync() => SendGenericRequestAsync<CalendarSubscription>(HttpMethod.Post, "calendar", true);

        private async Task<DataRequestResult<T>> SendGenericRequestAsync<T>(HttpMethod method, string endpoint, bool isAuthenticated)
        {
            try
            {
                HttpResponseMessage response;
                if (isAuthenticated)
                {
                    response = await retryWithAuthPolicy.ExecuteAsync(() =>
                    {
                        HttpRequestMessage request = new HttpRequestMessage(method, endpoint).AddToken(credentialService.Token);
                        return client.SendAsync(request);
                    });
                }
                else
                {
                    HttpRequestMessage request = new HttpRequestMessage(method, endpoint);
                    response = await client.SendAsync(request);
                }
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = await response.Content.ReadAsJsonObjectAsync<BackendResult<T>>();
                    return new DataRequestResult<T>(result.Resource, result.Message);
                }
                else
                {
                    BackendRequestFailedException exception = new BackendRequestFailedException($"Request failed due to an unexpected response status {response.StatusCode}. Endpoint: {endpoint}.");
                    exception.DisplayMessage = await GetBackendMessageAsync(response.Content);
                    throw exception;
                }
            }
            catch (HttpRequestException ex)
            {
                throw GetDefaultException(endpoint, ex);
            }
        }

        private async Task<string> GetBackendMessageAsync(HttpContent content)
        {
            // TODO: Add localization service.
            try
            {
                var result = await content.ReadAsJsonObjectAsync<BackendResult<object>>();
                return result.Message ?? string.Empty;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        // TODO: Add localization service.
        private BackendRequestFailedException GetDefaultException(string endpoint, Exception innerException)
            => new BackendRequestFailedException($"Request failed due to network error. Endpoint: {endpoint}.", innerException, null);

        private readonly HttpClient client;

        private readonly ICredentialService credentialService;

        private readonly AsyncRetryPolicy<HttpResponseMessage> retryWithAuthPolicy;

        private class JsonStringContent : StringContent
        {
            public JsonStringContent(object obj) : base(Serialize(obj))
            {
                Headers.ContentType = mediaType;
            }

            private static string Serialize(object obj)
            {
                using (var writer = new StringWriter())
                {
                    new JsonSerializer().Serialize(writer, obj);
                    return writer.ToString();
                }
            }

            private static readonly MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/json");
        }
    }
}
