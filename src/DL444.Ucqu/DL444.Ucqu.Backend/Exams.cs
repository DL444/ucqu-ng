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
    public class ExamsFunction
    {
        public ExamsFunction(IGetFunctionHandlerService getHandler, IWellknownDataService wellknown)
        {
            this.getHandler = getHandler;
            this.currentTerm = wellknown.CurrentTerm;
        }

        [FunctionName("Exams")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (username == null)
            {
                return new UnauthorizedResult();
            }

            return await getHandler.HandleRequestAsync(
                username,
                dataService => dataService.GetExamsAsync(username),
                (client, context) => client.GetExamScheduleAsync(context, currentTerm),
                (dataService, exams) => dataService.SetExamsAsync(exams),
                log
            );
        }

        private IGetFunctionHandlerService getHandler;
        private string currentTerm;
    }
}
