using System;
using DL444.Ucqu.Backend.Models;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DL444.Ucqu.Backend
{
    public abstract class NotifyScoreChangeFunctionBase
    {
        protected bool GetScoreDiffItem(EventGridEvent eventGridEvent, ILogger log, out ScoreDiffItem diff)
        {
            diff = default(ScoreDiffItem);
            if (!eventGridEvent.EventType.Equals("DL444.Ucqu.ScoreChanged", StringComparison.Ordinal))
            {
                log.LogWarning("Event with unsupported type received. Type {eventType}", eventGridEvent.EventType);
                return false;
            }
            if (eventGridEvent.Data == null)
            {
                log.LogError("Event data is null.");
                return false;
            }
            if (eventGridEvent.Data is JObject obj)
            {
                try
                {
                    diff = obj.ToObject<ScoreDiffItem>();
                    return true;
                }
                catch (JsonException ex)
                {
                    log.LogError(ex, "Error trying to deserialize event data.");
                    return false;
                }
            }
            else
            {
                log.LogError("Event data is not of type JObject. Type {dataType}", eventGridEvent.Data.GetType());
                return false;
            }
        }
    }
}
