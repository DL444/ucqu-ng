// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;

namespace DL444.Ucqu.Backend
{
    public class NotifyScoreChangeWindowsFunction : NotifyScoreChangeFunctionBase
    {
        public NotifyScoreChangeWindowsFunction(IPushNotificationService<WindowsPushNotification> notificationService, ILocalizationService locService)
        {
            this.notificationService = notificationService;
            this.locService = locService;
        }

        [FunctionName("NotifyScoreChangeWindows")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            bool success = GetScoreDiffItem(eventGridEvent, log, out ScoreDiffItem diff);
            if (!success || diff.DiffType == ScoreDiffType.Remove)
            {
                return;
            }
            await notificationService.SendNotificationAsync(diff.StudentId, () => GetNotification(diff), log);
        }

        private WindowsPushNotification GetNotification(ScoreDiffItem diff)
        {
            string title = GetNotificationTitle(diff);
            string primaryDescription = GetNotificationPrimaryDescription(diff);
            string secondaryDescription = GetNotificationSecondaryDescription(diff);
            string emojiUri = GetNotificationEmojiUri(diff);

            ToastContent toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title,
                                HintStyle = AdaptiveTextStyle.Title
                            },
                            new AdaptiveText()
                            {
                                Text = primaryDescription,
                                HintMaxLines = 2
                            },
                            new AdaptiveText()
                            {
                                Text = secondaryDescription,
                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                            }
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = emojiUri,
                        }
                    }
                },
                Actions = new ToastActionsCustom()
                {
                    ContextMenuItems =
                    {
                        new ToastContextMenuItem("ms-resource:ScoreChangedToastNeverShow", "NeverShowScoreChanged")
                        {
                            ActivationType = ToastActivationType.Background
                        }
                    }
                },
                Launch = "ScoreChanged"
            };
            return new WindowsPushNotification(WindowsNotificationType.Toast, toastContent.GetContent());
        }

        private string GetNotificationTitle(ScoreDiffItem diff)
        {
            string title;
            switch (diff.DiffType)
            {
                case ScoreDiffType.Add:
                    title = locService.GetString("ScoreChangedNotificationTitleAdd");
                    break;
                case ScoreDiffType.Change:
                    title = locService.GetString("ScoreChangedNotificationTitleModify");
                    break;
                default:
                    title = string.Empty;
                    break;
            }
            return title;
        }
        private string GetNotificationPrimaryDescription(ScoreDiffItem diff)
        {
            string key = diff.DiffType == ScoreDiffType.Change ? "ScoreChangedNotificationDescModify" : "ScoreChangedNotificationDescAdd";
            return locService.GetString(key, locService.DefaultCulture, diff.ShortName);
        }
        private string GetNotificationSecondaryDescription(ScoreDiffItem diff)
        {
            string key;
            int score = diff.NewScore;
            if (diff.IsMakeup)
            {
                key = score < 60 ? "ScoreChangedNotificationDescMakeupFail" : "ScoreChangedNotificationDescMakeupPass";
            }
            else
            {
                switch (score)
                {
                    case < 60:
                        key = "ScoreChangedNotificationDescInitialFail";
                        break;
                    case < 80:
                        key = "ScoreChangedNotificationDescInitialMediocre";
                        break;
                    case < 90:
                        key = "ScoreChangedNotificationDescInitialGood";
                        break;
                    default:
                        key = "ScoreChangedNotificationDescInitialExcellent";
                        break;
                }
            }
            return locService.GetString(key);
        }
        private string GetNotificationEmojiUri(ScoreDiffItem diff)
        {
            string key;
            int score = diff.NewScore;
            if (diff.IsMakeup)
            {
                key = score < 60 ? "ScoreChangedNotificationEmojiFail" : "ScoreChangedNotificationEmojiMediocre";
            }
            else
            {
                switch (score)
                {
                    case < 60:
                        key = "ScoreChangedNotificationEmojiFail";
                        break;
                    case < 80:
                        key = "ScoreChangedNotificationEmojiMediocre";
                        break;
                    case < 90:
                        key = "ScoreChangedNotificationEmojiGood";
                        break;
                    default:
                        key = "ScoreChangedNotificationEmojiExcellent";
                        break;
                }
            }
            return locService.GetString(key);
        }

        private readonly IPushNotificationService<WindowsPushNotification> notificationService;
        private readonly ILocalizationService locService;
    }
}
