using System.Threading.Tasks;
using DL444.Ucqu.Backend.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class StudentInfoRefreshFunction : RefreshFunctionBase
    {
        public StudentInfoRefreshFunction(IRefreshFunctionHandlerService refreshService) : base(refreshService) { }

        [FunctionName("StudentInfoRefresh_Client")]
        public async Task Start(
            [TimerTrigger("0 0 17 * * *")] TimerInfo timer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            await StartOrchestratorAsync("StudentInfoRefresh_Orchestrator", starter, log);
        }

        [FunctionName("StudentInfoRefresh_Orchestrator")]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            await StartActivityAsync("StudentInfoRefresh_Activity", context);
        }

        [FunctionName("StudentInfoRefresh_Activity")]
        public async Task Refresh([ActivityTrigger] string username, ILogger log)
        {
            await RefreshService.HandleRequestAsync(
                username,
                dataService => dataService.GetStudentInfoAsync(username),
                (client, context) => client.GetStudentInfoAsync(context),
                (dataService, resource) => dataService.SetStudentInfoAsync(resource),
                (oldRes, newRes) =>
                {
                    newRes.CalendarSubscriptionId = oldRes?.CalendarSubscriptionId;
                    return newRes.Name != null;
                },
                null,
                log
            );
        }
    }
}
