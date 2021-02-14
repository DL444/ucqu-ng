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
        public ScheduleFunction(IGetFunctionHandlerService getHandler, IWellknownDataService wellknown, IClientAuthenticationService clientAuthService)
        {
            this.getHandler = getHandler;
            this.currentTerm = wellknown.CurrentTerm;
            this.clientAuthService = clientAuthService;
        }

        [FunctionName("Schedule")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (!clientAuthService.Validate(req.HttpContext.Connection.ClientCertificate))
            {
                return new ForbidResult();
            }
            if (username == null)
            {
                return new UnauthorizedResult();
            }

            return await getHandler.HandleRequestAsync(
                username,
                dataService => dataService.GetScheduleAsync(username),
                (client, context) => client.GetScheduleAsync(context, currentTerm),
                (dataService, schedule) => dataService.SetScheduleAsync(schedule),
                schedule => schedule.Weeks.Count > 0,
                log
            );
        }

        private readonly IGetFunctionHandlerService getHandler;
        private readonly string currentTerm;
        private readonly IClientAuthenticationService clientAuthService;
    }
}
