using Newtonsoft.Json;
using Prism.Dialogs;
using ReeYin_V.Core;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.NodifyManager;
using ReeYin_V.Share.Prism;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DelegateCommand = ReeYin_V.NodifyManager.DelegateCommand;

namespace Nodify.FlowApp
{
    [Serializable]
    public class FlowNodeViewModel : NodeViewModel
    {
        #region Fields
        [NonSerialized]
        private DelegateCommand? _openConfigCommand;

        [NonSerialized]
        private int _openConfigExecuting;
        #endregion

        #region Properties

        public NodifyObservableCollection<ConnectorViewModel> Input { get; set; } = new NodifyObservableCollection<ConnectorViewModel>();
        
        public NodifyObservableCollection<ConnectorViewModel> Output { get; set; } = new NodifyObservableCollection<ConnectorViewModel>();
        #endregion

        #region Events

        #endregion

        #region Commands
        /// <summary>
        /// 打开配置命令
        /// </summary>
        [JsonIgnore]
        public ICommand OpenConfigCommand 
        {
            get => _openConfigCommand ??= new DelegateCommand(OpenConfig);
        }
        #endregion

        #region Constructor

        public FlowNodeViewModel()
        {
            Icon = "\ue631"; 
            Orientation = Orientation.Horizontal;

            Input.WhenAdded(c => c.Node = this)
                 .WhenRemoved(c => c.Disconnect());

            Output.WhenAdded(c => c.Node = this)
                 .WhenRemoved(c => c.Disconnect());
        }
        #endregion

        #region Methods
        public void Disconnect()
        {
            Input.Clear();
            Output.Clear();
        }

        private void OpenConfig()
        {
            EnsureNodeId();

            var dispatcher = PrismProvider.Dispatcher ?? Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                ResetOpenConfigState();
                return;
            }

            if (Interlocked.CompareExchange(ref _openConfigExecuting, 1, 0) != 0)
            {
                dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => TryActivateOpenedConfigWindow()));
                return;
            }

            _openConfigCommand?.RaiseCanExecuteChanged();

            dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(OpenConfigCore));
        }

        private void OpenConfigCore()
        {
            var keepLockedUntilDialogClosed = false;
            try
            {
                var curNode = this;
                var menuInfo = curNode.MenuInfo;

                if (menuInfo == null)
                {
                    MessageBox.Show("当前节点缺少菜单信息，无法打开配置。");
                    return;
                }

                if (menuInfo.TargetType == null || string.IsNullOrWhiteSpace(menuInfo.TargetType.Name))
                {
                    MessageBox.Show("当前节点缺少配置界面信息，无法打开配置。");
                    return;
                }

                if (TryActivateOpenedConfigWindow())
                {
                    return;
                }

                bool hasPersistentOutputCache = HasPersistentOutputCache(menuInfo.Serial);
                if (curNode.ModuleParam == null)
                {
                    if (hasPersistentOutputCache)
                    {
                        Logs.LogError(
                            $"打开组件被阻止：Serial={menuInfo.Serial:D3}, Title={menuInfo.Title}, " +
                            $"Reason=ModuleParam为空但存在持久输出缓存，可能反序列化失败");
                        ShowBlockedPlaceholderModuleParamMessage(menuInfo.Serial);
                        return;
                    }

                    Logs.LogInfo(
                        $"打开组件创建占位参数：Serial={menuInfo.Serial:D3}, Title={menuInfo.Title}, " +
                        $"TargetView={menuInfo.TargetType.Name}, Reason=新节点或无持久参数");
                    curNode.ModuleParam = new ModuleParamBase
                    {
                        Serial = menuInfo.Serial
                    };
                }
                else if (ShouldBlockPlaceholderModuleParam(curNode.ModuleParam, menuInfo.Serial, hasPersistentOutputCache))
                {
                    Logs.LogError(
                        $"打开组件被阻止：Serial={menuInfo.Serial:D3}, Title={menuInfo.Title}, " +
                        $"ParamType={curNode.ModuleParam.GetType().FullName}, Reason=占位参数且存在持久输出缓存");
                    ShowBlockedPlaceholderModuleParamMessage(menuInfo.Serial);
                    return;
                }

                curNode.ModuleParam.moduleInputParam ??= new ModuleParam();
                curNode.ModuleParam.moduleOutputParam ??= new ModuleParam();

                // 打开配置前同步上一节点的输出参数，便于当前节点预览入参。
                if (curNode.Graph != null)
                {
                    curNode.Graph.RefreshNodeInputParameters(curNode);
                }
                else if (curNode.LastNodes.Count != 0
                    && curNode.LastNode?.ModuleParam?.moduleOutputParam?.TransmitParams != null)
                {
                    curNode.ModuleParam.moduleInputParam.TransmitParams =
                        new Dictionary<string, object>(curNode.LastNode.ModuleParam.moduleOutputParam.TransmitParams);
                }

                PrismProvider.DialogService.Show(menuInfo.TargetType.Name, new DialogParameters
                {
                    { "Guid", curNode.Id },
                    { "Title", menuInfo.Serial.ToString("D3") + "_" + menuInfo.Title },
                    { "Icon", menuInfo.Icon },
                    { "Param", curNode.ModuleParam },
                    { "Serial", menuInfo.Serial },
                }, result =>
                {
                    try
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            var param = result.Parameters.GetValue<object>("Param") as IModuleParam;
                            if (param != null)
                            {
                                Logs.LogInfo(
                                    $"组件参数返回：Serial={menuInfo.Serial:D3}, Title={menuInfo.Title}, " +
                                    $"ParamType={param.GetType().FullName}");
                                curNode.ModuleParam = param;
                                SyncDialogOutputParameters(param);
                            }
                        }
                    }
                    finally
                    {
                        ResetOpenConfigState();
                    }
                }, nameof(SingleInstanceDialogWindowView));

                keepLockedUntilDialogClosed = true;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex.ToString());
            }
            finally
            {
                if (!keepLockedUntilDialogClosed)
                {
                    ResetOpenConfigState();
                }
            }
        }

        private void ResetOpenConfigState()
        {
            Interlocked.Exchange(ref _openConfigExecuting, 0);
            _openConfigCommand?.RaiseCanExecuteChanged();
        }

        private void EnsureNodeId()
        {
            if (Id == Guid.Empty)
            {
                Id = Guid.NewGuid();
            }
        }

        private bool TryActivateOpenedConfigWindow()
        {
            if (Id == Guid.Empty || Application.Current?.Windows == null)
            {
                return false;
            }

            var existingWindow = Application.Current.Windows
                .OfType<SingleInstanceDialogWindowView>()
                .FirstOrDefault(window => IsConfigWindowForCurrentNode(window));

            if (existingWindow == null)
            {
                return false;
            }

            return TryActivateWindow(existingWindow);
        }

        private bool IsConfigWindowForCurrentNode(Window window)
        {
            return TryGetDialogViewModel(window.DataContext, out var dialogViewModel)
                    && dialogViewModel.Guid == Id
                || TryGetDialogViewModel(window.Content, out dialogViewModel)
                    && dialogViewModel.Guid == Id
                || window.Content is FrameworkElement element
                    && TryGetDialogViewModel(element.DataContext, out dialogViewModel)
                    && dialogViewModel.Guid == Id;
        }

        private static bool TryGetDialogViewModel(object? source, out DialogViewModelBase dialogViewModel)
        {
            if (source is DialogViewModelBase viewModel)
            {
                dialogViewModel = viewModel;
                return true;
            }

            dialogViewModel = null!;
            return false;
        }

        public static bool ShouldBlockPlaceholderModuleParam(
            IModuleParam moduleParam,
            int serial,
            bool hasPersistentOutputCache)
        {
            return moduleParam != null &&
                moduleParam.GetType() == typeof(ModuleParamBase) &&
                serial >= 0 &&
                hasPersistentOutputCache;
        }

        private static bool HasPersistentOutputCache(int serial)
        {
            if (serial < 0)
            {
                return false;
            }

            var outputCache = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodesOutputCache;
            if (outputCache == null || outputCache.Count == 0)
            {
                return false;
            }

            return HasOutputCache(outputCache, serial.ToString()) ||
                HasOutputCache(outputCache, serial.ToString("D3"));
        }

        private static bool HasOutputCache(
            Dictionary<string, System.Collections.ObjectModel.ObservableCollection<TransmitParam>> outputCache,
            string key)
        {
            return outputCache.TryGetValue(key, out var values) && values != null && values.Count > 0;
        }

        private static void ShowBlockedPlaceholderModuleParamMessage(int serial)
        {
            string message = $"节点{serial:D3}参数恢复失败，已阻止创建空参数覆盖旧配置。请检查项目文件、模块版本或加载备份项目。";
            Logs.LogError(message);
            MessageBox.Show(message);
        }

        private static bool TryActivateWindow(Window window)
        {
            try
            {
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }

                if (!window.IsVisible)
                {
                    window.Show();
                }

                window.Activate();
                window.Focus();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        internal static void SyncDialogOutputParameters(IModuleParam moduleParam)
        {
            try
            {
                if (moduleParam is not ModelParamBase modelParam)
                {
                    return;
                }

                if (PrismProvider.ProjectManager?.SltCurSolutionItem == null)
                {
                    return;
                }

                PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.AddRange(
                    modelParam.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)));

                modelParam.moduleOutputParam ??= new ModuleParam();
                modelParam.moduleOutputParam.TransmitParams = modelParam.OutputParams.ToDictionary(
                    item => item.Guid.ToString(),
                    item => (object)item);
            }
            catch (Exception ex)
            {
                Logs.LogError(ex.StackTrace.ToString());
            }

        }
        #endregion
    }
}
