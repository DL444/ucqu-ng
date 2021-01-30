using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using DL444.Ucqu.Models;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class NotificationService : INotificationService
    {
        public NotificationService()
        {
            settingsService = Application.Current.GetService<ILocalSettingsService>();
        }

        public async Task UpdateScheduleSummaryNotificationAsync()
        {
            if (!ShouldUpdateScheduleSummary)
            {
                return;
            }

            List<ScheduleEntryViewModel> schedule;
            try
            {
                schedule = await GetTodayScheduleAsync();
            }
            catch (LocalCacheRequestFailedException)
            {
                return;
            }

            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
            if (schedule.Count == 0)
            {
                return;
            }

            var tile = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileSmall = new TileBinding()
                    {
                        Content = new TileBindingContentIconic()
                    },
                    TileMedium = GetTile(schedule, TileSize.Medium),
                    TileWide = GetTile(schedule, TileSize.Wide),
                    TileLarge = GetTile(schedule, TileSize.Large),
                    LockDetailedStatus1 = schedule[0].Name,
                    LockDetailedStatus2 = schedule[0].TimeRangeDisplay,
                    LockDetailedStatus3 = schedule[0].Room
                }
            };
            var tileNotification = new TileNotification(tile.GetXml());
            tileNotification.ExpirationTime = schedule[0].LocalEndTime;
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotification);

            var badge = new BadgeNumericContent((uint)schedule.Count);
            var badgeNotification = new BadgeNotification(badge.GetXml());
            badgeNotification.ExpirationTime = schedule[0].LocalEndTime;
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeNotification);

            if (ShouldSendScheduleSummaryToast)
            {
                ToastContent toast = GetToast(schedule);
                var toastNotification = new ToastNotification(toast.GetXml())
                {
                    Group = ToastTypes.ScheduleSummary.ToString(),
                    Tag = Guid.NewGuid().ToString(),
                    ExpirationTime = schedule.Max(x => x.LocalEndTime)
                };
                ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
                SetScheduleSummaryToastSentStatus();
            }
        }

        public void ClearToast(ToastTypes types)
        {
            if ((types & ToastTypes.ScheduleSummary) != 0)
            {
                ToastNotificationManager.History.RemoveGroup(ToastTypes.ScheduleSummary.ToString());
            }
            if ((types & ToastTypes.ScoreChange) != 0)
            {
                ToastNotificationManager.History.RemoveGroup(ToastTypes.ScoreChange.ToString());
            }
        }

        private bool ShouldUpdateScheduleSummary
        {
            get
            {
                int updateStartHour = Application.Current.GetConfigurationValue("Notification:TimerTaskStartHour", 7);
                int updateEndHour = Application.Current.GetConfigurationValue("Notification:TimerTaskEndHour", 22);
                if (DateTimeOffset.Now.Hour < updateStartHour || DateTimeOffset.Now.Hour >= updateEndHour)
                {
                    return false;
                }
                return true;
            }
        }

        private bool ShouldSendScheduleSummaryToast
        {
            get
            {
                if (!settingsService.GetValue("DailyToastEnabled", true))
                {
                    return false;
                }
                DateTimeOffset lastSent = settingsService.GetValue("ScheduleSummaryToastLastSent", DateTimeOffset.Now.GetLocalDate());
                return lastSent != DateTimeOffset.Now.GetLocalDate();
            }
        }

        private async Task<List<ScheduleEntryViewModel>> GetTodayScheduleAsync()
        {
            IDataService dataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.LocalCache);
            Task<DataRequestResult<WellknownData>> wellknownDataTask = dataService.GetWellknownDataAsync();
            Task<DataRequestResult<Schedule>> scheduleTask = dataService.GetScheduleAsync();
            WellknownData wellknown = (await wellknownDataTask).Resource;
            Schedule schedule = (await scheduleTask).Resource;
            var scheduleVm = new ScheduleViewModel(schedule, wellknown);
            int previewMins = Application.Current.GetConfigurationValue("Notification:NextSessionPreviewMinutes", 10);
            DateTimeOffset threshold = DateTimeOffset.Now.AddMinutes(previewMins);
            return scheduleVm.Today.Where(x => x.LocalEndTime > threshold).ToList();
        }

        private void SetScheduleSummaryToastSentStatus()
        {
            settingsService.SetValue("ScheduleSummaryToastLastSent", DateTimeOffset.Now.GetLocalDate());
        }

        private TileBinding GetTile(List<ScheduleEntryViewModel> schedule, TileSize size)
        {
            var content = new TileBindingContentAdaptive();
            content.Children.Add(GetAdaptiveGroup(schedule.First(), size, false, true));
            foreach (ScheduleEntryViewModel entry in schedule.Skip(1))
            {
                content.Children.Add(GetAdaptiveGroup(entry, size, true, false));
            }
            return new TileBinding()
            {
                Content = content
            };
        }

        private ToastContent GetToast(List<ScheduleEntryViewModel> schedule)
        {
            var content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "ms-resource:ScheduleSummaryToastTitle",
                                HintStyle = AdaptiveTextStyle.Base
                            },
                            new AdaptiveText()
                            {
                                Text = Application.Current.GetService<ILocalizationService>().Format("ScheduleSummaryToastDescriptionFormat", schedule.Count),
                                HintWrap = true,
                                HintMaxLines = 2
                            }
                        }
                    }
                },
                Actions = new ToastActionsCustom()
                {
                    ContextMenuItems =
                    {
                        new ToastContextMenuItem("ms-resource:ScheduleSummaryToastNeverShow", "NeverShowScheduleSummary")
                        {
                            ActivationType = ToastActivationType.Background
                        }
                    }
                }
            };
            foreach (ScheduleEntryViewModel entry in schedule)
            {
                content.Visual.BindingGeneric.Children.Add(GetAdaptiveGroup(entry, TileSize.Large, false, false));
            }
            return content;
        }

        private AdaptiveGroup GetAdaptiveGroup(ScheduleEntryViewModel entry, TileSize size, bool subtle, bool addSpacing)
        {
            AdaptiveSubgroup subgroup;
            switch (size)
            {
                case TileSize.Large:
                    subgroup = new AdaptiveSubgroup()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = entry.Name,
                                HintStyle = subtle ? AdaptiveTextStyle.BaseSubtle : AdaptiveTextStyle.Base
                            },
                            new AdaptiveText()
                            {
                                Text = entry.TimeRangeRoomDisplay,
                                HintStyle = subtle ? AdaptiveTextStyle.CaptionSubtle : AdaptiveTextStyle.Caption
                            }
                        }
                    };
                    break;
                case TileSize.Wide:
                    subgroup = new AdaptiveSubgroup()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = entry.Name,
                                HintStyle = subtle ? AdaptiveTextStyle.BaseSubtle : AdaptiveTextStyle.Base
                            },
                            new AdaptiveText()
                            {
                                Text = entry.TimeRangeRoomDisplay,
                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                            }
                        }
                    };
                    break;
                case TileSize.Medium:
                    subgroup = new AdaptiveSubgroup()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = entry.Name,
                                HintWrap = true,
                                HintMaxLines = 2,
                                HintStyle = subtle ? AdaptiveTextStyle.BaseSubtle : AdaptiveTextStyle.Base
                            },
                            new AdaptiveText()
                            {
                                Text = entry.TimeRangeDisplay,
                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                            },
                            new AdaptiveText()
                            {
                                Text = entry.Room,
                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                            }
                        }
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size));
            }
            
            if (addSpacing)
            {
                subgroup.Children.Add(new AdaptiveText()
                {
                    HintStyle = AdaptiveTextStyle.Caption
                });
            }

            return new AdaptiveGroup()
            {
                Children =
                {
                    subgroup
                }
            };
        }

        private enum TileSize
        {
            Small,
            Medium,
            Wide,
            Large
        }

        private ILocalSettingsService settingsService;
    }
}
