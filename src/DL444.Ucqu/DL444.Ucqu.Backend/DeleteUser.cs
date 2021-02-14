using System.Threading.Tasks;
using DL444.Ucqu.Backend.Bindings;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class DeleteUser
    {
        public DeleteUser(IDataAccessService dataService, ILocalizationService locService, IClientAuthenticationService clientAuthService)
        {
            this.dataService = dataService;
            this.locService = locService;
            this.clientAuthService = clientAuthService;
        }

        [FunctionName("DeleteUser")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "user")] HttpRequest req,
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
            DataAccessResult result = await dataService.DeleteUserAsync(username);
            if (result.Success)
            {
                return new OkObjectResult(new BackendResult<object>(true, new object(), null));
            }
            else
            {
                log.LogError("Failed to delete user {user}.", username);
                return new ObjectResult(new BackendResult<object>(false, new object(), locService.GetString("ServiceErrorCannotDeleteUser")))
                {
                    StatusCode = 503
                };
            }
        }

        private readonly IDataAccessService dataService;
        private readonly ILocalizationService locService;
        private readonly IClientAuthenticationService clientAuthService;
    }
}
