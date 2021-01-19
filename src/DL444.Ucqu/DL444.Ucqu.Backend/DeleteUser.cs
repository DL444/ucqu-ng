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
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Backend
{
    public class DeleteUser
    {
        public DeleteUser(IDataAccessService dataService, ILocalizationService locService)
        {
            this.dataService = dataService;
            this.locService = locService;
        }

        [FunctionName("DeleteUser")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user")] HttpRequest req,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (username == null)
            {
                return new UnauthorizedResult();
            }
            DataAccessResult result = await dataService.DeleteUserAsync(username);
            if (result.Success)
            {
                return new OkObjectResult(new BackendResult<object>(true, null, null));
            }
            else
            {
                log.LogError("Failed to delete user {user}.", username);
                return new OkObjectResult(new BackendResult<object>(false, null, locService.GetString("ServiceErrorCannotDeleteUser")));
            }
        }

        private IDataAccessService dataService;
        private ILocalizationService locService;
    }
}
