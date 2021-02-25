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
    public class ScoreFunction
    {
        public ScoreFunction(IGetFunctionHandlerService getHandler) => this.getHandler = getHandler;

        [FunctionName("Score")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Score/{secondMajor:int}")] HttpRequest req,
            int secondMajor,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (username == null)
            {
                return new UnauthorizedResult();
            }
            bool isSecondMajor;
            if (secondMajor == 0 || secondMajor == 1)
            {
                isSecondMajor = false;
            }
            else if (secondMajor == 2)
            {
                isSecondMajor = true;
            }
            else
            {
                return new NotFoundResult();
            }

            return await getHandler.HandleRequestAsync(
                username,
                dataService => dataService.GetScoreAsync(username, isSecondMajor),
                (client, context) => client.GetScoreAsync(context, isSecondMajor),
                (dataService, score) => dataService.SetScoreAsync(score),
                score => score.Terms.Count > 0,
                log
            );
        }

        private readonly IGetFunctionHandlerService getHandler;
    }
}
