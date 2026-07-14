using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.Language;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Core.Services.Alarm;
using ReeYin_V.Core.Services.Alarm.Config;
using ReeYin_V.Core.Services.Alarm.Definitions;
using ReeYin_V.Core.Services.Alarm.Governance;
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.HardwareRules;
using ReeYin_V.Core.Services.Alarm.Software;
using ReeYin_V.Core.Services.User;
using ReeYin_V.Core.Services.WorkStatus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ReeYin_V.Core.IOC
{
    [ExposedService(Lifetime.Singleton,1, AutoInitialize = true)]
    public sealed class PrismProvider
    {
        public PrismProvider(
            IContainerExtension container,
            IRegionManager regionManager,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            IUserService user,
            IAlarmService alarmService,
            IAlarmConfigService alarmConfigService,
            IAlarmDefinitionService alarmDefinitionService,
            IAlarmGovernanceService alarmGovernanceService,
            IHardwareAlarmRuleService hardwareAlarmRuleService,
            IHardwareAlarmReporter hardwareAlarmReporter,
            ISoftwareAlarmReporter softwareAlarmReporter,
            ILanguageManager languageManager,
            IHardwareModuleManager hardwareModuleManager,
            IWorkStatusManager workStatusManager,
            IDynamicViewManager dynamicViewManager,
            CategoryModuleManager moduleManager,
            CustomProjectManager customProjectManager,
            NodifyMenuManager nodifyMenuManager,
            ProjectManager projectManager)
        {
            User = user;
            Container = container;
            RegionManager = regionManager;
            DialogService = dialogService;
            EventAggregator = eventAggregator;
            AlarmService = alarmService;
            AlarmConfigService = alarmConfigService;
            AlarmDefinitionService = alarmDefinitionService;
            AlarmGovernanceService = alarmGovernanceService;
            HardwareAlarmRuleService = hardwareAlarmRuleService;
            HardwareAlarmReporter = hardwareAlarmReporter;
            SoftwareAlarmReporter = softwareAlarmReporter;
            ModuleManager = moduleManager;
            WorkStatusManager = workStatusManager;
            NodifyMenuManager = nodifyMenuManager;
            HardwareModuleManager = hardwareModuleManager;
            LanguageManager = languageManager;
            ProjectManager = projectManager;
            CustomProjectManager = customProjectManager;
            Dispatcher = Application.Current.Dispatcher;
            DynamicViewManager = dynamicViewManager;

        }

        public static string AppBasePath = "";

        /// <summary>
        /// 容器
        /// </summary>
        public static IContainerExtension Container { get; private set; }

        /// <summary>
        /// 区域管理器接口
        /// </summary>
        public static IRegionManager RegionManager { get; private set; }

        /// <summary>
        /// 对话框管理器
        /// </summary>
        public static IDialogService DialogService { get; private set; }

        /// <summary>
        /// 显示页面动态控件管理
        /// </summary>
        public static IDynamicViewManager DynamicViewManager { get; private set; }

        /// <summary>
        /// 事件聚合器
        /// </summary>
        public static IEventAggregator EventAggregator { get; private set; }

        /// <summary>
        /// 报警服务
        /// </summary>
        public static IAlarmService AlarmService { get; private set; }

        /// <summary>
        /// 报警配置服务
        /// </summary>
        public static IAlarmConfigService AlarmConfigService { get; private set; }

        /// <summary>
        /// 报警定义服务
        /// </summary>
        public static IAlarmDefinitionService AlarmDefinitionService { get; private set; }

        /// <summary>
        /// 报警治理服务
        /// </summary>
        public static IAlarmGovernanceService AlarmGovernanceService { get; private set; }

        /// <summary>
        /// 硬件报警触发规则服务
        /// </summary>
        public static IHardwareAlarmRuleService HardwareAlarmRuleService { get; private set; }

        /// <summary>
        /// 硬件报警上报器
        /// </summary>
        public static IHardwareAlarmReporter HardwareAlarmReporter { get; private set; }

        /// <summary>
        /// 软件报警上报器
        /// </summary>
        public static ISoftwareAlarmReporter SoftwareAlarmReporter { get; private set; }

        /// <summary>
        /// 硬件模块管理器
        /// </summary>
        public static IHardwareModuleManager HardwareModuleManager { get; private set; }

        /// <summary>
        /// 工作状态管理器
        /// </summary>
        public static IWorkStatusManager WorkStatusManager { get; private set; }

        /// <summary>
        /// 语言管理
        /// </summary>
        public static ILanguageManager LanguageManager { get; private set; }

        /// <summary>
        /// 模块管理器
        /// </summary>
        public static CategoryModuleManager ModuleManager { get; private set; }

        /// <summary>
        /// 项目管理器
        /// </summary>
        public static ProjectManager ProjectManager { get; private set; }

        /// <summary>
        /// 定制项目管理
        /// </summary>
        public static CustomProjectManager CustomProjectManager { get; private set; }

        /// <summary>
        /// Nodify菜单模块管理器
        /// </summary>
        public static NodifyMenuManager NodifyMenuManager { get; private set; }

        /// <summary>
        /// 用户助手
        /// </summary>
        public static IUserService User { get; private set; }

        public static Dispatcher Dispatcher { get; private set; }

        public static Window MainWindow { get; private set; }

    }
}
