using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Language;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Core.Services.Module
{
    public enum HardwareType
    {
        None = 0,
        Camera,
        PLC,
        Com,
        ControlCard,
        Sensor,
        LightController
    }

    [ExposedService(Lifetime.Singleton, 4, typeof(IHardwareModuleManager))]
    public class HardwareModuleManager : IHardwareModuleManager
    {
        #region Fields
        private IConfigManager ConfigManager;

        /// <summary>
        /// 被加载的组件
        /// </summary>
        public ObservableCollection<HardwareConfigItem> ConfigItems { get; set; } = new ObservableCollection<HardwareConfigItem>();
        #endregion

        #region Properties
        /// <summary>
        /// 所有硬件
        /// </summary>
        public Dictionary<ConfigKey, IHardwareModule> Modules { set; get; } = new Dictionary<ConfigKey, IHardwareModule>();

        #endregion

        #region Constructor
        public HardwareModuleManager(IConfigManager configManager)
        {
            ConfigManager = configManager;
        }

        #endregion

        #region Methods
        public void Initialize()
        {
            foreach (var config in ConfigItems)
            {
                var module = ConfigManager.Read<object>(config.Config) as IHardwareModule;
                Modules.Add(config.Config, module);
                module?.Init();
            }

            //还没有移植到硬件管理模块
            Modules.Add(ConfigKey.CamConfig, ConfigManager.Read<object>(ConfigKey.CamConfig) as IHardwareModule);
            Modules.Add(ConfigKey.ComConfig, ConfigManager.Read<object>(ConfigKey.ComConfig) as IHardwareModule);

        }

        public void Shutdown()
        {
            foreach (var module in Modules)
            {
                module.Value?.Shutdown();
            }
        }

        public void RefreshStatus()
        {
            foreach (var module in Modules)
            {
                module.Value?.RefreshStatus();
            }
        }


        #endregion

    }

    public struct InitResult
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }
}
