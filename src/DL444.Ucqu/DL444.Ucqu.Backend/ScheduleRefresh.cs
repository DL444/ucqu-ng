using System.Threading.Tasks;
using DL444.Ucqu.Backend.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class ScheduleRefreshFunction : RefreshFunctionBase
    {
        public ScheduleRefreshFunction(IRefreshFunctionHandlerService refreshService, IWellknownDataService wellknown)
            : base(refreshService) => this.wellknown = wellknown;

        [FunctionName("ScheduleRefresh_Client")]
        public async Task Start(
            [TimerTrigger("0 0 18 * * *")] TimerInfo timer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            await StartOrchestratorAsync("ScheduleRefresh_Orchestrator", starter, log);
        }

        [FunctionName("ScheduleRefresh_Orchestrator")]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            await StartActivityAsync("ScheduleRefresh_Activity", context);
        }

        [FunctionName("ScheduleRefresh_Activity")]
        public async Task Refresh([ActivityTrigger] string username, ILogger log)
        {
            await RefreshService.HandleRequestAsync(
                username,
                dataService => dataService.GetScheduleAsync(username),
                (client, context) => client.GetScheduleAsync(context, wellknown.CurrentTerm),
                (dataService, resource) => dataService.SetScheduleAsync(resource),
                (oldRes, newRes) => oldRes == null || (newRes.Weeks.Count > 0),
                null,
                log
            );
        }

        private IWellknownDataService wellknown;
    }
}
