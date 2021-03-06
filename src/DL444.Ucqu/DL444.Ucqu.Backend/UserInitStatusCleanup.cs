using System;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class UserInitStatusCleanupFunction
    {
        public UserInitStatusCleanupFunction(IDataAccessService dataService) => this.dataService = dataService;

        [FunctionName("UserInitStatusCleanup")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo timer, ILogger log)
        {
            try
            {
                DataAccessResult result = await dataService.PurgeUserInitializeStatusAsync();
                if (!result.Success)
                {
                    log.LogWarning("Some user initialize status failed to be removed.");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to purge user initialize status.");
            }
        }

        private IDataAccessService dataService;
    }
}
