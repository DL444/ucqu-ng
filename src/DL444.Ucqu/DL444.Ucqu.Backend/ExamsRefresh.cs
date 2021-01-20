using System.Threading.Tasks;
using DL444.Ucqu.Backend.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class ExamsRefreshFunction : RefreshFunctionBase
    {
        public ExamsRefreshFunction(IRefreshFunctionHandlerService refreshService, IWellknownDataService wellknown)
            : base(refreshService) => this.wellknown = wellknown;

        [FunctionName("ExamsRefresh_Client")]
        public async Task Start(
            [TimerTrigger("0 0 3 * * *")] TimerInfo timer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            await StartOrchestratorAsync("ExamsRefresh_Orchestrator", starter, log);
        }

        [FunctionName("ExamsRefresh_Orchestrator")]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            await StartActivityAsync("ExamsRefresh_Activity", context);
        }

        [FunctionName("ExamsRefresh_Activity")]
        public async Task Refresh([ActivityTrigger] string username, ILogger log)
        {
            await RefreshService.HandleRequestAsync(
                username,
                dataService => dataService.GetExamsAsync(username),
                (client, context) => client.GetExamScheduleAsync(context, wellknown.CurrentTerm),
                (dataService, resource) => dataService.SetExamsAsync(resource),
                (_, _) => true,
                null,
                log
            );
        }

        private IWellknownDataService wellknown;
    }
}
