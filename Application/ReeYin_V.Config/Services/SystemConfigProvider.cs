using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReeYin_V.Core.IOC;
using ReeYin_V.Share.Config;

namespace ReeYin_V.Config.Services
{
    /// <summary>
    /// 系统配置提供者，作用是将系统配置参数传给不同的模块，用接口实现数据通讯
    /// </summary>
    [ExposedService(Lifetime.Singleton, 3,typeof(ISystemConfigProvider), typeof(ISoftwareConfigProvider))]
    public class SystemConfigProvider : ISystemConfigProvider, ISoftwareConfigProvider
    {
        private readonly SystemConfigManager SystemConfigManager;
        public SystemConfigProvider(SystemConfigManager systemConfigManager)
        {
            SystemConfigManager = systemConfigManager;
            Initialize();

        }

        private void Initialize()
        {
            SoftwareConfig = SystemConfigManager.Config.SoftwareConfig;
        }

        internal void Invoke()
        {
            OnChanged?.Invoke();
        }

        public SoftwareConfig SoftwareConfig { get; set; }

        public event Action OnChanged;
    }
}
