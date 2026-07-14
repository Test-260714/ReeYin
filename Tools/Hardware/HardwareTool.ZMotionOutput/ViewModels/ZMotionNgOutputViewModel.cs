using HardwareTool.ZMotionOutput.Models;
using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Windows;
using Timer = System.Timers.Timer;

namespace HardwareTool.ZMotionOutput.ViewModels
{
    [Serializable]
    public class ZMotionNgOutputViewModel : DialogViewModelBase, IViewModuleParam
    {
        [JsonIgnore]
        public new ZMotionNgOutputModel ModelParam
        {
            get => (base.ModelParam as ZMotionNgOutputModel)!;
            set => base.ModelParam = value;
        }

        [JsonIgnore]
        private Timer? _statusTimer;

        public DelegateCommand LoadCommand => new(() =>
        {
            EnsureModelParam();

            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            ModelParam.RefreshControlCards();
            ModelParam.EnsureOutputPointConfig();
            StartStatusTimer();
            if (!string.IsNullOrWhiteSpace(ModelParam.SltModelName))
            {
                foreach (var card in ModelParam.Models)
                {
                    if (card?.NickName == ModelParam.SltModelName || card?.DisplayName == ModelParam.SltModelName)
                    {
                        ModelParam.ControlCard = card;
                        break;
                    }
                }
            }

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<string> GeneralCommand => new(order =>
        {
            switch (order)
            {
                case "切换模块":
                    ModelParam.SltModelName = ModelParam.ControlCard?.DisplayName ?? string.Empty;
                    break;

                case "刷新控制卡":
                    ModelParam.RefreshControlCards();
                    ModelParam.EnsureOutputPointConfig();
                    ModelParam.RefreshOutputPointStates();
                    break;

                case "刷新IO状态":
                    ModelParam.RefreshIoStatusItems();
                    break;

                case "IO置ON":
                    ModelParam.SetSelectedOutputIo(true, out var ioOnMessage);
                    if (!string.IsNullOrWhiteSpace(ioOnMessage))
                        MessageBox.Show(ioOnMessage, "正运动IO调试", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case "IO置OFF":
                    ModelParam.SetSelectedOutputIo(false, out var ioOffMessage);
                    if (!string.IsNullOrWhiteSpace(ioOffMessage))
                        MessageBox.Show(ioOffMessage, "正运动IO调试", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case "测试输出":
                    ModelParam.TestAllEnabledOutputs(out var testMessage);
                    if (!string.IsNullOrWhiteSpace(testMessage))
                        MessageBox.Show(testMessage, "正运动NG输出", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case "全部复位":
                    ModelParam.ResetOutputs(out var message);
                    if (!string.IsNullOrWhiteSpace(message))
                        MessageBox.Show(message, "正运动NG输出", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case "添加OUT点":
                    ModelParam.AddOutputPoint();
                    break;

                case "删除OUT点":
                    ModelParam.RemoveSelectedOutputPoint();
                    break;

                case "添加输入源":
                    ModelParam.AddInputSource();
                    break;

                case "删除输入源":
                    ModelParam.RemoveSelectedInputSource();
                    break;

                case "添加输出规则":
                    ModelParam.AddOutputRule();
                    break;

                case "删除输出规则":
                    ModelParam.RemoveSelectedOutputRule();
                    break;

                case "清空日志":
                    ModelParam.ClearLogs();
                    break;

                case "取消":
                    StopStatusTimer();
                    CloseDialog(ButtonResult.No);
                    break;

                case "确认":
                    ModelParam.EnsureOutputPointConfig();
                    ModelParam.SltModelName = ModelParam.ControlCard?.DisplayName ?? string.Empty;
                    StopStatusTimer();
                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam },
                    });
                    break;
            }
        });
        
        public override void InitParam()
        {
            EnsureModelParam();
        }

        private void EnsureModelParam()
        {
            if (ModelParam != null)
                return;

            var modelParam = ResolveModelParam<ZMotionNgOutputModel>();
            modelParam.Serial = Serial;

            ModelParam = modelParam;

            modelParam.OnceInit();
            modelParam.InitOutputParamResource(Guid);
            modelParam.TransferParam();
            modelParam.IsDebug = true;
        }

        private void StartStatusTimer()
        {
            if (_statusTimer != null)
                return;

            _statusTimer = new Timer(300);
            _statusTimer.Elapsed += (_, _) =>
            {
                try
                {
                    ModelParam.RefreshOutputPointStates();
                }
                catch
                {
                    // 状态刷新失败不影响节点参数配置。
                }
            };
            _statusTimer.AutoReset = true;
            _statusTimer.Start();
        }

        private void StopStatusTimer()
        {
            if (_statusTimer == null)
                return;

            _statusTimer.Stop();
            _statusTimer.Dispose();
            _statusTimer = null;
        }
    }
}
