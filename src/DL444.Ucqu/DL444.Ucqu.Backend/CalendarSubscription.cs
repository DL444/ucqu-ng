using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Backend.Bindings;
using DL444.Ucqu.Models;
using DL444.Ucqu.Client;

namespace DL444.Ucqu.Backend
{
    public class CalendarSubscriptionFunction
    {
        public CalendarSubscriptionFunction(IUcquClient client, IDataAccessService dataService, ICalendarService calService, ILocalizationService locService)
        {
            this.client = client;
            this.dataService = dataService;
            this.calService = calService;
            this.locService = locService;
        }

        [FunctionName("CalendarSubscriptionGet")]
        public async Task<IActionResult> RunGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Calendar/{username}/{id}")] HttpRequest req,
            string? username,
            string? id,
            ILogger log)
        {
            if (username == null || id == null)
            {
                return new BadRequestResult();
            }
            Task<DataAccessResult<Schedule>> scheduleFetchTask = dataService.GetScheduleAsync(username);
            Task<DataAccessResult<ExamSchedule>> examsFetchTask = dataService.GetExamsAsync(username);
            DataAccessResult<StudentInfo> userFetchResult = await dataService.GetStudentInfoAsync(username);
            if (!userFetchResult.Success)
            {
                if (userFetchResult.StatusCode != 404)
                {
                    log.LogWarning("Failed to fetch user info from database. Status {statusCode}", userFetchResult.StatusCode);
                }
                return new NotFoundResult();
            }
            if (!id.Equals(userFetchResult.Resource.CalendarSubscriptionId))
            {
                return new NotFoundResult();
            }

            DataAccessResult<Schedule> scheduleFetchResult = await scheduleFetchTask;
            if (!scheduleFetchResult.Success)
            {
                log.LogError("Failed to fetch schedule from database. Status {statusCode}", scheduleFetchResult.StatusCode);
                return new NotFoundResult();
            }
            DataAccessResult<ExamSchedule> examsFetchResult = await examsFetchTask;
            ExamSchedule? exams;
            if (examsFetchResult.Success)
            {
                exams = examsFetchResult.Resource;
            }
            else
            {
                exams = null;
                log.LogError("Failed to fetch exams from database. Status {statusCode}", examsFetchResult.StatusCode);
            }
            return new OkObjectResult(calService.GetCalendar(scheduleFetchResult.Resource, exams, 15));
        }
        
        [FunctionName("CalendarSubscriptionPost")]
        public async Task<IActionResult> RunPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Calendar")] HttpRequest req,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (username == null)
            {
                return new UnauthorizedResult();
            }

            DataAccessResult<StudentInfo> userFetchResult = await dataService.GetStudentInfoAsync(username);
            StudentInfo? info = null;
            if (userFetchResult.Success)
            {
                info = userFetchResult.Resource;
            }
            else if (userFetchResult.StatusCode == 404)
            {
                DataAccessResult<StudentCredential> credentialFetchResult = await dataService.GetCredentialAsync(username);
                if (credentialFetchResult.Success)
                {
                    try
                    {
                        StudentCredential credential = credentialFetchResult.Resource;
                        SignInContext signInContext = await client.SignInAsync(credential.StudentId, credential.PasswordHash);
                        info = await client.GetStudentInfoAsync(signInContext);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Failed to fetch sign in or fetch user info from upstream.");
                    }
                }
                else if (credentialFetchResult.StatusCode == 404)
                {
                    return new UnauthorizedResult();
                }
                else
                {
                    log.LogError("Failed to fetch user credential. Status {statusCode}", credentialFetchResult.StatusCode);
                }
            }
            else
            {
                log.LogError("Failed to fetch user info from database. Status {statusCode}", userFetchResult.StatusCode);
            }

            if (info == null)
            {
                return new OkObjectResult(new BackendResult<CalendarSubscription>(locService.GetString("ServiceErrorCannotAddCalendarSub")));
            }

            info.CalendarSubscriptionId = Guid.NewGuid().ToString();
            DataAccessResult userUpdateResult = await dataService.SetStudentInfoAsync(info);
            if (userUpdateResult.Success)
            {
                return new OkObjectResult(new BackendResult<CalendarSubscription>(new CalendarSubscription(info.CalendarSubscriptionId)));
            }
            else
            {
                log.LogError("Failed to update user info. Status {statusCode}", userUpdateResult.StatusCode);
                return new OkObjectResult(new BackendResult<CalendarSubscription>(locService.GetString("ServiceErrorCannotAddCalendarSub")));
            }
        }

        private IUcquClient client;
        private IDataAccessService dataService;
        private ICalendarService calService;
        private ILocalizationService locService;
    }
}
