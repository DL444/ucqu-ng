using System.Threading.Tasks;
using DL444.Ucqu.Backend.Bindings;
using DL444.Ucqu.Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class ScheduleFunction
    {
        public ScheduleFunction(IGetFunctionHandlerService getHandler, IWellknownDataService wellknown)
        {
            this.getHandler = getHandler;
            this.currentTerm = wellknown.CurrentTerm;
        }

        [FunctionName("Schedule")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (username == null)
            {
                return new UnauthorizedResult();
            }

            return await getHandler.HandleRequestAsync(
                username,
                dataService => dataService.GetScheduleAsync(username),
                (client, context) => client.GetScheduleAsync(context, currentTerm),
                (dataService, schedule) => dataService.SetScheduleAsync(schedule),
                schedule => schedule.Weeks.Count > 0
            );
        }

        private IGetFunctionHandlerService getHandler;
        private string currentTerm;
    }
}
