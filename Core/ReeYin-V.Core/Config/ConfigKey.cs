using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Config
{
    /// <summary>
    /// 配置键名
    /// </summary>
    public enum ConfigKey
    {
        /// <summary>
        /// 系统配置
        /// </summary>
        SystemConfig,

        /// <summary>
        /// 缓存配置
        /// </summary>
        CacheConfig,

        /// <summary>
        /// 解决方案配置
        /// </summary>
        SolutionConfig,

        /// <summary>
        /// 新解决方案配置
        /// </summary>
        ProjectConfig,

        /// <summary>
        /// 配方配置
        /// </summary>
        RecipeConfig,

        /// <summary>
        /// 相机配置
        /// </summary>
        CamConfig,

        /// <summary>
        /// 通信配置
        /// </summary>
        ComConfig,

        /// <summary>
        /// 控制卡配置
        /// </summary>
        ControlCard,

        /// <summary>
        /// 轴配置
        /// </summary>
        AxisModel,

        /// <summary>
        /// 控制卡IO配置管理
        /// </summary>
        IOManagerModel,

        /// <summary>
        /// 缓存的坐标信息
        /// </summary>
        CoordinateCacheModel,

        /// <summary>
        /// 固高模块配置
        /// </summary>
        GoogolModel,

        /// <summary>
        /// PLC模块配置
        /// </summary>
        PLCConfig,

        /// <summary>
        /// 传感器模块配置
        /// </summary>
        SensorConfig,

        /// <summary>
        /// 光源控制器配置
        /// </summary>
        LightControllerConfig,

        ///用户管理信息
        UserManagementConfig
    }
}
