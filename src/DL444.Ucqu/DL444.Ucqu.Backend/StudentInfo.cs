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
    public class StudentInfoFunction
    {
        public StudentInfoFunction(IGetFunctionHandlerService getHandler, IClientAuthenticationService clientAuthService)
        {
            this.getHandler = getHandler;
            this.clientAuthService = clientAuthService;
        }

        [FunctionName("StudentInfo")]
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
                dataService => dataService.GetStudentInfoAsync(username),
                (client, context) => client.GetStudentInfoAsync(context),
                (dataService, info) => dataService.SetStudentInfoAsync(info),
                log
            );
        }

        private readonly IGetFunctionHandlerService getHandler;
        private readonly IClientAuthenticationService clientAuthService;
    }
}
