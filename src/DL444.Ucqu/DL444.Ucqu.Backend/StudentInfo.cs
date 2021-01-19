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
        public StudentInfoFunction(IGetFunctionHandlerService getHandler) => this.getHandler = getHandler;

        [FunctionName("StudentInfo")]
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
                dataService => dataService.GetStudentInfoAsync(username),
                (client, context) => client.GetStudentInfoAsync(context),
                (dataService, info) => dataService.SetStudentInfoAsync(info)
            );
        }

        private IGetFunctionHandlerService getHandler;
    }
}
