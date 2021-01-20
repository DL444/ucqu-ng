using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Client;
using DL444.Ucqu.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class InitializeUserFunction
    {
        public InitializeUserFunction(IUcquClient client, IDataAccessService dataService, IConfiguration config, IWellknownDataService wellknownData)
        {
            this.client = client;
            this.dataService = dataService;
            serviceBaseAddress = config.GetValue<string>("Host:ServiceBaseAddress");
            currentTerm = wellknownData.CurrentTerm;
        }

        [FunctionName("InitializeUser")]
        public async Task Run([QueueTrigger("user-init-queue", Connection = "AzureWebJobsStorage")] Models.UserInitializeCommand command, ILogger log)
        {
            // Avoid concurrent requests to upstream server to reduce risks of IP ban.
            SignInContext signInContext = command.SignInContext;
            List<Task<DataAccessResult>> updateTasks = new List<Task<DataAccessResult>>(5);
            
            try
            {
                StudentInfo studentInfo = await client.GetStudentInfoAsync(signInContext);
                updateTasks.Add(dataService.SetStudentInfoAsync(studentInfo));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception occured when initializing user. Step: Fetch user info. User {user}", signInContext.SignedInUser);
            }

            try
            {
                Schedule schedule = await client.GetScheduleAsync(signInContext, currentTerm);
                updateTasks.Add(dataService.SetScheduleAsync(schedule));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception occured when initializing user. Step: Fetch schedule. User {user}", signInContext.SignedInUser);
            }

            try
            {
                ExamSchedule exams = await client.GetExamScheduleAsync(signInContext, currentTerm);
                updateTasks.Add(dataService.SetExamsAsync(exams));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception occured when initializing user. Step: Fetch exams. User {user}", signInContext.SignedInUser);
            }

            try
            {
                ScoreSet majorScore = await client.GetScoreAsync(signInContext, false);
                updateTasks.Add(dataService.SetScoreAsync(majorScore));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception occured when initializing user. Step: Fetch major score. User {user}", signInContext.SignedInUser);
            }

            try
            {
                ScoreSet secondMajorScore = await client.GetScoreAsync(signInContext, true);
                updateTasks.Add(dataService.SetScoreAsync(secondMajorScore));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception occured when initializing user. Step: Fetch second major score. User {user}", signInContext.SignedInUser);
            }

            try
            {
                await Task.WhenAll(updateTasks);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception occured when initializing user. Step: Database update. User {user}", signInContext.SignedInUser);
            }

            UserInitializeStatus newStatus = new UserInitializeStatus(command.StatusId, true, null);
            DataAccessResult statusUpdateResult = await dataService.SetUserInitializeStatusAsync(newStatus);
            if (!statusUpdateResult.Success)
            {
                log.LogError("Cannot update user initialization status. Status {statusCode}", statusUpdateResult.StatusCode);
            }
        }

        private IUcquClient client;
        private IDataAccessService dataService;
        private string serviceBaseAddress;
        private string currentTerm;
    }
}
