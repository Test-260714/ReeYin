using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReeYin_V.Config.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.IOC;

namespace ReeYin_V.Config.Services
{
    /// <summary>
    /// 系统配置管理者，作用是整个系统所有配置参数的读写业务
    /// </summary>
    [ExposedService(Lifetime.Singleton)]
    public class SystemConfigManager
    {
        public SystemConfigModel Config { get; set; }

        private IConfigManager ConfigManager { get; }
        public SystemConfigManager(IConfigManager configManager)
        {
            ConfigManager = configManager;

            Load();
        }

        /// <summary>
        /// 从JSON中读取配置
        /// </summary>
        public void Load()
        {
            Config = ConfigManager.Read<SystemConfigModel>(ConfigKey.SystemConfig) ?? new SystemConfigModel();
        }

        /// <summary>
        /// 保存配置到JSON
        /// </summary>
        public void Save()
        {
            ConfigManager.Write(ConfigKey.SystemConfig, Config);
        }
    }
}
