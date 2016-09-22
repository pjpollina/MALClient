﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using MALClient.Models.Enums;
using MALClient.XShared.Utils;

namespace MALClient.Shared.Managers
{
    public static class NotificationTaskManager
    {
        private static bool _taskRegistered;
        private const string TaskName = "NotificationsBackgroundTask";
        private const string TaskNamespace = "MALClient.UWP.BGTaskNotifications.NotificationsBackgroundTask";

        private static BackgroundTaskRegistration TaskRegistration { get; set; }

        public static async void StartNotificationTask(bool restart = true)
        {
            if(!Settings.EnableNotifications || !Credentials.Authenticated || Settings.SelectedApiType == ApiType.Hummingbird)
                return;

            if(!_taskRegistered)
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == TaskName)
                    {
                        _taskRegistered = true;
                        TaskRegistration = task.Value as BackgroundTaskRegistration;
                    }
                }

            if(_taskRegistered && !restart)
                return;

            if (_taskRegistered)
                TaskRegistration?.Unregister(true);

            var access = await BackgroundExecutionManager.RequestAccessAsync();
            if(access == BackgroundAccessStatus.DeniedBySystemPolicy || 
               access == BackgroundAccessStatus.DeniedByUser)
                return;

            var builder = new BackgroundTaskBuilder
            {
                Name = TaskName,
                TaskEntryPoint = TaskNamespace
            };

            builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            builder.SetTrigger(new TimeTrigger((uint)Settings.NotificationsRefreshTime,false));
            builder.SetTrigger(new ApplicationTrigger());

            TaskRegistration = builder.Register();

            _taskRegistered = true;
        }


        public static void StopNotificationTask()
        {
            _taskRegistered = false;
            TaskRegistration.Unregister(false);
            TaskRegistration = null;
        }

        public static async void CallBackgroundTask()
        {
            var trigger = TaskRegistration?.Trigger as ApplicationTrigger;
            if(trigger != null)
                await trigger.RequestAsync();
        }
    }
}
