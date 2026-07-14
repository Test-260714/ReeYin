using Custom.WaferFlatnessMeasure.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    [Serializable]
    public class GripperClampControlViewModel : DialogViewModelBase, IViewModuleParam
    {
        private CancellationTokenSource? _operationCts;

        private string? _sltOutputParamName;
        public string? SltOutputParamName
        {
            get => _sltOutputParamName;
            set => SetProperty(ref _sltOutputParamName, value);
        }

        private TransmitParam? _currentOutputParam;
        public TransmitParam? CurrentOutputParam
        {
            get => _currentOutputParam;
            set => SetProperty(ref _currentOutputParam, value);
        }

        public new GripperClampControlModel? ModelParam
        {
            get => base.ModelParam as GripperClampControlModel;
            set => base.ModelParam = value!;
        }

        public override void InitParam()
        {
            var modelParam = InitModelParam<GripperClampControlModel>();
            ModelParam = modelParam;
            modelParam.LoadKeyParam();
            modelParam.RefreshPlcReference();
            SltOutputParamName ??= modelParam.OutputParamNames?.FirstOrDefault();
        }

        public override void OnDialogClosed()
        {
            _operationCts?.Cancel();
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            string command = order ?? string.Empty;
            if (TryRunJogCommand(command))
            {
                return;
            }

            switch (command)
            {
                case "Refresh":
                    if (ModelParam != null)
                    {
                        var modelParam = ModelParam;
                        RunOperation(token => modelParam.RefreshStatusAsync(token), false);
                    }
                    break;
                case "RefreshPressures":
                    if (ModelParam != null)
                    {
                        var modelParam = ModelParam;
                        RunOperation(token => modelParam.RefreshPressuresAsync(token), false);
                    }
                    break;
                case "Clamp":
                    if (ModelParam != null)
                    {
                        var modelParam = ModelParam;
                        RunOperation(token => modelParam.ClampAsync(token), true);
                    }
                    break;
                case "Release":
                    if (ModelParam != null)
                    {
                        var modelParam = ModelParam;
                        RunOperation(token => modelParam.ReleaseAsync(token), true);
                    }
                    break;
                case "ResetCommands":
                    if (ModelParam != null)
                    {
                        var modelParam = ModelParam;
                        RunOperation(token => modelParam.ResetCommandsAsync(token), true);
                    }
                    break;
                case "Stop":
                    _operationCts?.Cancel();
                    ResetCommandsAfterStop();
                    break;
                case "执行":
                    RunExecuteModule();
                    break;
                case "取消":
                case "Cancel":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":
                case "Confirm":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                        item => item.Guid.ToString(),
                        item => (object)item);

                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam }
                    });
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            ModelParam?.RefreshPlcReference();

            if (Visibility == Visibility.Hidden)
            {
                var parameters = new DialogParameters();
                if (ModelParam != null)
                {
                    parameters.Add("Param", ModelParam);
                }

                CloseDialog(ButtonResult.OK, parameters);
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            var modelParam = ModelParam;
            if (modelParam == null)
            {
                return;
            }

            switch (obj?.ToString())
            {
                case "Add":
                    AddOutputParam();
                    break;
                case "Delete":
                    if (CurrentOutputParam == null)
                    {
                        return;
                    }

                    modelParam.OutputParams.Remove(CurrentOutputParam);
                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
                    CurrentOutputParam = null;
                    break;
            }
        });

        private bool TryRunJogCommand(string command)
        {
            if (ModelParam == null || string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            if (TryGetCommandIndex(command, "StartForwardJog", out int forwardIndex))
            {
                var modelParam = ModelParam;
                RunOperation(token => modelParam.StartJogAsync(forwardIndex, "Forward", token), true);
                return true;
            }

            if (TryGetCommandIndex(command, "StartReverseJog", out int reverseIndex))
            {
                var modelParam = ModelParam;
                RunOperation(token => modelParam.StartJogAsync(reverseIndex, "Reverse", token), true);
                return true;
            }

            if (TryGetCommandIndex(command, "StopJog", out int stopIndex))
            {
                RunStopJog(stopIndex);
                return true;
            }

            if (TryGetCommandIndex(command, "RefreshPressure", out int pressureIndex))
            {
                var modelParam = ModelParam;
                RunOperation(token => modelParam.RefreshPressureAsync(pressureIndex, token), false);
                return true;
            }

            return false;
        }

        private static bool TryGetCommandIndex(string command, string commandName, out int index)
        {
            index = 0;
            string prefix = $"{commandName}:";
            return command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(command.Substring(prefix.Length), out index);
        }

        private void RunStopJog(int gripperIndex)
        {
            var modelParam = ModelParam;
            if (modelParam == null)
            {
                return;
            }

            _operationCts?.Cancel();
            _operationCts = new CancellationTokenSource();
            CancellationToken token = _operationCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await modelParam.StopJogAsync(gripperIndex, token);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    ShowMessage($"夹爪点动停止异常：{ex.Message}", "夹爪点动");
                }
            }, token);
        }

        private void RunExecuteModule()
        {
            var modelParam = ModelParam;
            if (modelParam == null)
            {
                return;
            }

            _operationCts?.Cancel();
            _operationCts = new CancellationTokenSource();
            CancellationToken token = _operationCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    await modelParam.ExecuteModule();
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    ShowMessage($"夹爪操作异常：{ex.Message}", "夹爪操作");
                }
            }, token);
        }

        private void RunOperation(Func<CancellationToken, Task<bool>> operation, bool showFailureMessage)
        {
            var modelParam = ModelParam;
            if (modelParam == null || operation == null)
            {
                return;
            }

            if (modelParam.IsOperationInProgress)
            {
                ShowMessage("已有夹爪操作正在执行，请等待完成或先停止。", "夹爪操作");
                return;
            }

            _operationCts?.Cancel();
            _operationCts = new CancellationTokenSource();
            CancellationToken token = _operationCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    bool success = await operation(token);
                    if (!success && showFailureMessage)
                    {
                        ShowMessage(modelParam.LastOperationMessage, "夹爪操作");
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    ShowMessage($"夹爪操作异常：{ex.Message}", "夹爪操作");
                }
            }, token);
        }

        private void ResetCommandsAfterStop()
        {
            var modelParam = ModelParam;
            if (modelParam == null)
            {
                return;
            }

            _operationCts = new CancellationTokenSource();
            CancellationToken token = _operationCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (modelParam.IsOperationInProgress)
                    {
                        await Task.Delay(50, token);
                    }

                    bool success = await modelParam.ResetCommandsAsync(token);
                    if (!success)
                    {
                        ShowMessage(modelParam.LastOperationMessage, "夹爪操作");
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    ShowMessage($"夹爪命令复位异常：{ex.Message}", "夹爪操作");
                }
            }, token);
        }

        private void AddOutputParam()
        {
            if (ModelParam == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SltOutputParamName) ||
                !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object? resourceObject) ||
                resourceObject is not TransmitParam curSltParam)
            {
                return;
            }

            if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
            {
                MessageBox.Show("已包含重名参数，请重新输入！");
                return;
            }

            var outputValues = OutputParamCollector.GetDataPointValues(ModelParam);
            outputValues.TryGetValue(curSltParam.Name, out object? value);

            ModelParam.OutputParams.Add(new TransmitParam
            {
                LinkGuid = Guid,
                ParamName = curSltParam.Name,
                Serial = ModelParam.Serial,
                Name = SltOutputParamName,
                ParentNode = Name,
                Type = DataType._object,
                Value = value,
                ResourcePath = curSltParam.ResourcePath,
            });
        }

        private static void ShowMessage(string message, string caption)
        {
            if (PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess())
            {
                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PrismProvider.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }
    }
}
