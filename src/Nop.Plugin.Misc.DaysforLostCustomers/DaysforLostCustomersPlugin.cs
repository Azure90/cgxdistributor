using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Misc.DaysforLostCustomers
{
    public class DaysforLostCustomersPlugin : BasePlugin, IMiscPlugin
    {
        #region Fields

        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        /// <summary>
        /// Gets a name of the synchronization schedule task
        /// </summary>
        /// 
        // this name showing on Scheduled Task
        public static string SynchronizationTaskName = "Days for Lost Customers Plugin";

        /// <summary>
        /// Gets a type of the synchronization schedule task
        /// </summary>
        ///  this is the uniqe Name, Execute in Auto Task
        public static string SynchronizationTask = "Nop.Plugin.Misc.DaysforLostCustomers.Services.DaysforLostCustomers";

        /// <summary>
        /// Gets a default synchronization period in hours
        /// </summary>
        public static int DefaultSynchronizationPeriod = 13;


        #endregion

        #region Ctor

        public DaysforLostCustomersPlugin(IScheduleTaskService scheduleTaskService)
        {
            _scheduleTaskService = scheduleTaskService;
        }

        #endregion

        #region Methods


        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        //public override string GetConfigurationPageUrl()
        //{
        //    // Controller Name
        //    return $"{_webHelper.GetStoreLocation()}Admin/MiscSAPOrderAutoTask/Configure";
        //}

        /// <summary>
        /// Install plugin
        /// </summary>
        public override async Task InstallAsync()
        {
            //install synchronization task
            if (await _scheduleTaskService.GetTaskByTypeAsync(SynchronizationTask) == null)
            {
                await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
                {
                    Enabled = true,
                    Seconds = DefaultSynchronizationPeriod * 60 * 60,
                    Name = SynchronizationTaskName,
                    Type = SynchronizationTask
                });
            }

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task UninstallAsync()
        {

            //schedule task
            var task = await _scheduleTaskService.GetTaskByTypeAsync(SynchronizationTask);
            if (task != null)
            {
                await _scheduleTaskService.DeleteTaskAsync(task);
            }

            await base.UninstallAsync();
        }

        #endregion

    }
}

