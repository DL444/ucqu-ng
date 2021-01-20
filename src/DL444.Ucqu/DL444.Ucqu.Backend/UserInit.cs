using System.Threading.Tasks;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class UserInitFunction
    {
        public UserInitFunction(IDataAccessService dataService, ILocalizationService locService, IConfiguration config)
        {
            this.dataService = dataService;
            this.locService = locService;
            this.serviceBaseAddress = config.GetValue<string>("Host:ServiceBaseAddress");
        }

        [FunctionName("UserInit")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "UserInit/{id}")] HttpRequest req,
            string id,
            ILogger log)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new BadRequestResult();
            }
            DataAccessResult<UserInitializeStatus> result = await dataService.GetUserInitializeStatusAsync(id);
            if (result.Success)
            {
                if (result.Resource.Completed)
                {
                    return new OkObjectResult(new BackendResult<UserInitializeStatus>(result.Resource));
                }
                else
                {
                    return new AcceptedResult($"{serviceBaseAddress}/UserInit/{id}", new BackendResult<UserInitializeStatus>(true, result.Resource, locService.GetString("UserInitPrepare")));
                }
            }
            else
            {
                return new NotFoundResult();
            }
        }

        private IDataAccessService dataService;
        private ILocalizationService locService;
        private string serviceBaseAddress;
    }
}
