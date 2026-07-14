using ALGO.LineScanSheetCounter.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ALGO.LineScanSheetCounter;

/// <summary>
/// 线扫片材计数模块入口。
/// 负责向 ReeYin 平台注册模块、菜单和对话框视图。
/// </summary>
public sealed class LineScanSheetCounter : IModule
{
    #region Prism 模块生命周期

    /// <summary>
    /// 模块初始化时注册当前程序集内的资源。
    /// </summary>
    public void OnInitialized(IContainerProvider containerProvider)
    {
        containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// 注册模块类型、菜单信息和配置视图。
    /// </summary>
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

        containerRegistry.RegisterDialogAndMenu<LineScanSheetCounterView>(null, new MenuInfo
        {
            NodeType = NodeType.General,
            TranslateKey = "",
            Title = "线扫片材计数",
            Icon = "\xe677",
            Type = "02.AlgorithmicTool",
            Description = "线扫片材实时计数工具",
            TargetType = typeof(LineScanSheetCounterView),
        });
    }

    #endregion
}
