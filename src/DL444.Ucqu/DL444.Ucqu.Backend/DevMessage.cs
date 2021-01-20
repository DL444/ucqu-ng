using System.Threading.Tasks;
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
    public class DevMessageFunction
    {
        public DevMessageFunction(IDataAccessService dataService) => this.dataService = dataService;

        [FunctionName("DevMessage")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            DataAccessResult<DeveloperMessage> messageFetchResult = await dataService.GetDeveloperMessageAsync();
            if (messageFetchResult.Success)
            {
                DeveloperMessage message = messageFetchResult.Resource;
                message.Messages.RemoveAll(x => x.Archived);
                return new OkObjectResult(new BackendResult<DeveloperMessage>(message));
            }
            else
            {
                if (messageFetchResult.StatusCode != 404)
                {
                    log.LogError("Failed to get developer messages from database. Status {statusCode}", messageFetchResult.StatusCode);
                }
                return new OkObjectResult(new BackendResult<DeveloperMessage>(new DeveloperMessage()));
            }
        }

        private IDataAccessService dataService;
    }
}
