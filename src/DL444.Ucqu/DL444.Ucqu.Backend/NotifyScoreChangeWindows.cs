// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using DL444.Ucqu.Backend.Models;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class NotifyScoreChangeWindowsFunction : NotifyScoreChangeFunctionBase
    {
        [FunctionName("NotifyScoreChangeWindows")]
        public void Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            bool success = GetScoreDiffItem(eventGridEvent, log, out ScoreDiffItem diff);
            if (!success)
            {
                return;
            }
            // TODO: Implement notification.
        }
    }
}
