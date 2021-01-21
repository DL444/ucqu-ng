using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Models;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class ScoreRefreshFunction : RefreshFunctionBase
    {
        public ScoreRefreshFunction(IRefreshFunctionHandlerService refreshService) : base(refreshService) { }

        [FunctionName("ScoreRefresh_Client")]
        public async Task Start(
            [TimerTrigger("0 */5 0-14,23 * * *")] TimerInfo timer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            await StartOrchestratorAsync("ScoreRefresh_Orchestrator", starter, log);
        }

        [FunctionName("ScoreRefresh_Orchestrator")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            await StartActivityAsync("ScoreRefresh_Activity", context);
        }

        [FunctionName("ScoreRefresh_Activity")]
        public async Task Refresh(
            [ActivityTrigger] string username,
            [EventGrid(TopicEndpointUri = "EventPublish:TopicUri", TopicKeySetting = "EventPublish:TopicKey")] IAsyncCollector<EventGridEvent> collector,
            ILogger log)
        {
            Task majorTask = RefreshService.HandleRequestAsync(
                username,
                dataService => dataService.GetScoreAsync(username, false),
                (client, context) => client.GetScoreAsync(context, false),
                (dataService, resource) => dataService.SetScoreAsync(resource),
                (oldRes, newRes) => ShouldUpdate(oldRes, newRes),
                (oldRes, newRes) => StartNotificationAsync(username, oldRes, newRes, collector, log),
                log
            );
            Task secondMajorTask = RefreshService.HandleRequestAsync(
                username,
                dataService => dataService.GetScoreAsync(username, true),
                (client, context) => client.GetScoreAsync(context, true),
                (dataService, resource) => dataService.SetScoreAsync(resource),
                (oldRes, newRes) => ShouldUpdate(oldRes, newRes),
                (oldRes, newRes) => StartNotificationAsync(username, oldRes, newRes, collector, log),
                log
            );
            await Task.WhenAll(majorTask, secondMajorTask);
        }

        private bool ShouldUpdate(ScoreSet? oldRes, ScoreSet newRes)
        {
            if (oldRes == null)
            {
                return newRes.Terms.Count > 0;
            }
            if (oldRes.Terms.Count < newRes.Terms.Count)
            {
                return true;
            }
            if (oldRes.Terms.Count > newRes.Terms.Count)
            {
                return false;
            }
            foreach (Term oldTerm in oldRes.Terms)
            {
                Term? newTerm = newRes.Terms.FirstOrDefault(x =>
                    x.BeginningYear == oldTerm.BeginningYear &&
                    x.TermNumber == oldTerm.TermNumber
                );
                if (newTerm == null)
                {
                    return true;
                }
                if (oldTerm.Courses.Count != newTerm.Courses.Count)
                {
                    return true;
                }
                if (GetTermDiff(string.Empty, oldTerm, newTerm).Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task StartNotificationAsync(string username, ScoreSet? prev, ScoreSet current, IAsyncCollector<EventGridEvent> collector, ILogger log)
        {
            if (prev == null)
            {
                if (current.Terms.Count != 1 || current.Terms[0].Courses.Count > 1)
                {
                    return;
                }
            }
            List<ScoreDiffItem> diffs = GetDiff(username, prev, current);
            Task[] addTasks = diffs
                .Where(x => x.DiffType != ScoreDiffType.Remove)
                .Select(x => collector.AddAsync(new EventGridEvent(
                    id: $"ScoreChange-{Guid.NewGuid().ToString()}",
                    subject: x.StudentId,
                    data: x,
                    eventType: "DL444.Ucqu.ScoreChanged",
                    eventTime: DateTime.UtcNow,
                    dataVersion: "1.0"
                )))
                .ToArray();
            await Task.WhenAll(addTasks);
        }

        private List<ScoreDiffItem> GetDiff(string username, ScoreSet? oldRes, ScoreSet newRes)
        {
            List<ScoreDiffItem> diffs = new List<ScoreDiffItem>();
            oldRes = oldRes ?? new ScoreSet(string.Empty);
            Dictionary<string, Term> oldTerms = new Dictionary<string, Term>(
                oldRes.Terms.Select(x => new KeyValuePair<string, Term>($"{x.BeginningYear}{x.TermNumber}", x))
            );
            Dictionary<string, Term> newTerms = new Dictionary<string, Term>(
                newRes.Terms.Select(x => new KeyValuePair<string, Term>($"{x.BeginningYear}{x.TermNumber}", x))
            );

            foreach ((string key, Term value) in oldTerms)
            {
                if (!newTerms.ContainsKey(key))
                {
                    diffs.AddRange(GetTermDiff(username, value, null));
                }
                else
                {
                    diffs.AddRange(GetTermDiff(username, value, newTerms[key]));
                }
            }
            foreach ((string key, Term value) in newTerms)
            {
                if (!oldTerms.ContainsKey(key))
                {
                    diffs.AddRange(GetTermDiff(username, null, value));
                }
            }

            return diffs;
        }

        private List<ScoreDiffItem> GetTermDiff(string username, Term? oldTerm, Term? newTerm)
        {
            List<ScoreDiffItem> diffs = new List<ScoreDiffItem>();
            oldTerm = oldTerm ?? new Term();
            newTerm = newTerm ?? new Term();
            Dictionary<string, Course> oldCourses = new Dictionary<string, Course>(
                oldTerm.Courses.Select(x => new KeyValuePair<string, Course>(GetKey(x), x))
            );
            Dictionary<string, Course> newCourses = new Dictionary<string, Course>(
                newTerm.Courses.Select(x => new KeyValuePair<string, Course>(GetKey(x), x))
            );

            foreach ((string key, Course value) in oldCourses)
            {
                if (!newCourses.ContainsKey(key))
                {
                    diffs.Add(new ScoreDiffItem()
                    {
                        StudentId = username,
                        DiffType = ScoreDiffType.Remove,
                        ShortName = value.ShortName,
                        IsMakeup = value.IsMakeup,
                        OldScore = value.Score
                    });
                }
                else if (value.Score != newCourses[key].Score)
                {
                    diffs.Add(new ScoreDiffItem()
                    {
                        StudentId = username,
                        DiffType = ScoreDiffType.Change,
                        ShortName = value.ShortName,
                        IsMakeup = value.IsMakeup,
                        OldScore = value.Score,
                        NewScore = newCourses[key].Score
                    });
                }
            }
            foreach ((string key, Course value) in newCourses)
            {
                if (!oldCourses.ContainsKey(key))
                {
                    diffs.Add(new ScoreDiffItem()
                    {
                        StudentId = username,
                        DiffType = ScoreDiffType.Add,
                        ShortName = value.ShortName,
                        IsMakeup = value.IsMakeup,
                        NewScore = value.Score
                    });
                }
            }

            return diffs;
        }

        private string GetKey(Course course)
            => $"{course.Name}-{course.Credit}-{course.Category}-{course.IsInitialTake}-{course.Comment}-{course.Lecturer}";
    }
}
