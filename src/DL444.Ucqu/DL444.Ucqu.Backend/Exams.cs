using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DL444.Ucqu.Backend.Bindings;
using DL444.Ucqu.Backend.Services;

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
