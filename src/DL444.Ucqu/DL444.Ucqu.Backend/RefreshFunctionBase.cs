using System.Collections.Generic;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Services;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public abstract class RefreshFunctionBase
    {
        protected RefreshFunctionBase(IRefreshFunctionHandlerService refreshService) => RefreshService = refreshService;

        protected IRefreshFunctionHandlerService RefreshService { get; }

        protected async Task StartOrchestratorAsync(string orchestratorName, IDurableOrchestrationClient starter, ILogger log)
        {
            List<string>? users = await RefreshService.GetUsersAsync(log);
            if (users != null)
            {
                await starter.StartNewAsync(orchestratorName, null, users);
            }
            else
            {
                log.LogError("Failed to get a list of users,");
            }
        }

        protected async Task StartActivityAsync(string activityName, IDurableOrchestrationContext context)
        {
            List<string> users = context.GetInput<List<string>>();
            foreach (string username in users)
            {
                await context.CallActivityAsync(activityName, username);
            }
        }
    }
}
