using System.Linq;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class WellknownDataFunction
    {
        public WellknownDataFunction(IWellknownDataService wellknown)
        {
            data = new WellknownData()
            {
                CurrentTerm = wellknown.CurrentTerm,
                TermStartDate = wellknown.TermStartDate,
                TermEndDate = wellknown.TermEndDate,
                Schedule = wellknown.Schedule.ToList()
            };
        }

        [FunctionName("WellknownData")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "wellknown")] HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult(new BackendResult<WellknownData>(data));
        }

        private readonly WellknownData data;
    }
}
