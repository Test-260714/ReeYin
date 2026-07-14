using ImageTool.GrabImage.Models;
using ReeYin_V.Hardware.Camera.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Linq;
using System.Windows.Forms;

namespace ImageTool.GrabImage.ViewModels
{
    [Serializable]
    public class ContinuousGrabViewModel : DialogViewModelBase, IViewModuleParam
    {
        private const string RealOutputSourceDisplay = "真机输出";
        private const string PseudoOutputSourceDisplay = "伪采输出";
        private const string ManualOutputSourceDisplay = "手动添加";

        #region Properties
        public new ContinuousGrabModel ModelParam
        {
            get => base.ModelParam as ContinuousGrabModel;
            set
            {
                base.ModelParam = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Model));
            }
        }

        // Preserve the legacy binding path used by the existing XAML.
        public ContinuousGrabModel Model
        {
            get => ModelParam;
            set => ModelParam = value;
        }

        private string _sltOutputParamName;
        public string SltOutputParamName
        {
            get => _sltOutputParamName;
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        private CameraBase _selectedMultiCameraCandidate;
        public CameraBase SelectedMultiCameraCandidate
        {
            get => _selectedMultiCameraCandidate;
            set { _selectedMultiCameraCandidate = value; RaisePropertyChanged(); }
        }

        private ContinuousGrabCameraStatusItem _selectedMultiCameraItem;
        public ContinuousGrabCameraStatusItem SelectedMultiCameraItem
        {
            get => _selectedMultiCameraItem;
            set { _selectedMultiCameraItem = value; RaisePropertyChanged(); }
        }

        private ContinuousGrabCameraStatusItem _selectedPseudoCameraItem;
        public ContinuousGrabCameraStatusItem SelectedPseudoCameraItem
        {
            get => _selectedPseudoCameraItem;
            set { _selectedPseudoCameraItem = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public ContinuousGrabViewModel()
        {
        }
        #endregion

        #region Methods
        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
                ModelParam.StopDebugRawImageSave();
            }
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<ContinuousGrabModel>();
        }
        #endregion

        #region Commands
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam?.Serial == -999)
                ModelParam.Serial = Serial;

            ModelParam?.LoadKeyParam();
            EnsureOutputSourceDescriptions();
            SelectedMultiCameraCandidate ??= ModelParam?.CameraModels?.FirstOrDefault();
        });

        public DelegateCommand StartGrabCommand => new DelegateCommand(() =>
        {
            Model.StartRealGrab();
        });

        public DelegateCommand StopGrabCommand => new DelegateCommand(() =>
        {
            Model.StopRealGrab();
        });

        public DelegateCommand StartPseudoGrabCommand => new DelegateCommand(() =>
        {
            Model.StartPseudoGrab();
        });

        public DelegateCommand StopPseudoGrabCommand => new DelegateCommand(() =>
        {
            Model.StopPseudoGrab();
        });

        public DelegateCommand SelectDebugRawImageSaveDirectoryCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            if (Model.IsDebugRawImageSaving)
            {
                System.Windows.MessageBox.Show("原图保存中不能切换目录，请先停止保存。");
                return;
            }

            using var dialog = new FolderBrowserDialog
            {
                Description = "选择连续采集原图保存根目录",
                ShowNewFolderButton = true
            };

            var currentDirectory = Model.DebugRawImageSaveRootDirectoryDisplay;
            if (!string.IsNullOrWhiteSpace(currentDirectory) && System.IO.Directory.Exists(currentDirectory))
                dialog.SelectedPath = currentDirectory;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Model.DebugRawImageSaveRootDirectory = dialog.SelectedPath;
        });

        public DelegateCommand StartDebugRawImageSaveCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            if (!Model.StartDebugRawImageSave(out var error))
            {
                System.Windows.MessageBox.Show(error);
                return;
            }

            System.Windows.MessageBox.Show($"原图保存已开始。\r\n保存目录：{Model.DebugRawImageSaveDirectory}");
        });

        public DelegateCommand StopDebugRawImageSaveCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            if (!Model.IsDebugRawImageSaving)
            {
                System.Windows.MessageBox.Show("原图保存未开启。");
                return;
            }

            var saveDirectory = Model.DebugRawImageSaveDirectory;
            var savedFrames = Model.DebugRawImageSavedFrameCount;
            var savedImages = Model.DebugRawImageSavedImageCount;
            var droppedFrames = Model.DebugRawImageDroppedFrameCount;
            Model.StopDebugRawImageSave();
            System.Windows.MessageBox.Show(
                $"原图保存已停止。\r\n已保存：{savedFrames} 帧 / {savedImages} 张\r\n丢弃：{droppedFrames} 帧\r\n保存目录：{saveDirectory}");
        });

        public DelegateCommand AddMultiCameraCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            if (!Model.AddMultiCamera(SelectedMultiCameraCandidate, out var error))
            {
                System.Windows.MessageBox.Show(error);
                return;
            }

            SelectedMultiCameraItem = Model.MultiCameraItems.LastOrDefault();
        });

        public DelegateCommand RemoveMultiCameraCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            if (!Model.RemoveMultiCamera(SelectedMultiCameraItem, out var error))
            {
                System.Windows.MessageBox.Show(error);
                return;
            }

            SelectedMultiCameraItem = Model.MultiCameraItems.LastOrDefault();
        });

        public DelegateCommand AddMultiCameraOutputsCommand => new DelegateCommand(() =>
        {
            AddOutputs(Model?.GetRealCameraOutputNames(), RealOutputSourceDisplay);
        });

        public DelegateCommand AddRealCameraOutputsCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            AddOutputs(Model.GetRealCameraOutputNames(), RealOutputSourceDisplay);
        });

        public DelegateCommand AddPseudoCameraOutputsCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            AddOutputs(Model.GetPseudoCameraOutputNames(), PseudoOutputSourceDisplay);
        });

        private void AddOutputs(System.Collections.Generic.IEnumerable<string> outputNames, string sourceDisplay)
        {
            if (Model == null)
            {
                System.Windows.MessageBox.Show("节点参数未初始化，无法生成输出。");
                return;
            }

            var requestedNames = (outputNames ?? Enumerable.Empty<string>())
                .Where(outputName => !string.IsNullOrWhiteSpace(outputName))
                .Select(outputName => outputName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (requestedNames.Count == 0)
            {
                System.Windows.MessageBox.Show("没有可生成的输出：请先在真机/伪相机列表添加相机。");
                return;
            }

            Model.OutputParamResource ??= new System.Collections.Generic.Dictionary<string, object>();
            if (Model.OutputParamResource.Count == 0)
            {
                Model.InitOutputParamResource(Guid);
                Model.OutputParamNames = Model.OutputParamResource.Select(item => item.Key).ToList();
            }

            var outputResources = Model.OutputParamResource.Values
                .OfType<TransmitParam>()
                .Where(param => !string.IsNullOrWhiteSpace(param.Name))
                .GroupBy(param => param.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var dataPointValues = OutputParamCollector.GetDataPointValues(Model);
            var addedNames = new System.Collections.Generic.List<string>();
            var existingNames = new System.Collections.Generic.List<string>();
            var missingNames = new System.Collections.Generic.List<string>();

            foreach (var outputName in requestedNames)
            {
                if (Model.OutputParams.Any(param =>
                        param != null &&
                        (string.Equals(param.ParamName, outputName, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(param.Name, outputName, StringComparison.OrdinalIgnoreCase))))
                {
                    existingNames.Add(outputName);
                    continue;
                }

                if (!outputResources.TryGetValue(outputName, out var resource))
                {
                    missingNames.Add(outputName);
                    continue;
                }

                Model.OutputParams.Add(new TransmitParam
                {
                    LinkGuid = Guid,
                    ParamName = resource.Name,
                    Serial = Model.Serial,
                    ParentNode = Name,
                    Name = resource.Name,
                    Type = DataType._object,
                    Value = dataPointValues.TryGetValue(resource.Name, out var value) ? value?.DeepCopy() : null,
                    ResourcePath = resource.ResourcePath,
                    Describe = sourceDisplay,
                });
                addedNames.Add(resource.Name);
            }

            if (addedNames.Count > 0)
            {
                System.Windows.MessageBox.Show($"已生成输出：{string.Join("、", addedNames)}");
                return;
            }

            if (missingNames.Count > 0)
            {
                System.Windows.MessageBox.Show($"生成未完成：未找到输出资源 {string.Join("、", missingNames)}。请关闭后重新打开节点配置窗口。");
                return;
            }

            if (existingNames.Count > 0)
            {
                System.Windows.MessageBox.Show($"输出已存在，无需重复生成：{string.Join("、", existingNames)}");
            }
        }

        public DelegateCommand<TransmitParam> RemoveOutputParamCommand => new DelegateCommand<TransmitParam>((param) =>
        {
            if (Model == null || param == null)
                return;

            Model.OutputParams.Remove(param);
        });

        public DelegateCommand<ContinuousGrabCameraStatusItem> SelectPseudoFolderForCameraCommand => new DelegateCommand<ContinuousGrabCameraStatusItem>((item) =>
        {
            if (Model == null || item == null)
                return;

            using var dialog = new FolderBrowserDialog
            {
                Description = $"选择 {item.OutputName}/{item.CameraNo} 的伪采集图片目录",
                ShowNewFolderButton = false
            };

            if (!string.IsNullOrWhiteSpace(item.PseudoImageFolder))
                dialog.SelectedPath = item.PseudoImageFolder;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Model.SetPseudoCameraFolder(item, dialog.SelectedPath);
        });

        public DelegateCommand AddPseudoCameraCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            if (!Model.AddPseudoCamera(out var error))
            {
                System.Windows.MessageBox.Show(error);
                return;
            }

            SelectedPseudoCameraItem = Model.PseudoCameraItems.LastOrDefault();
        });

        public DelegateCommand RemovePseudoCameraCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            if (!Model.RemovePseudoCamera(SelectedPseudoCameraItem, out var error))
            {
                System.Windows.MessageBox.Show(error);
                return;
            }

            SelectedPseudoCameraItem = Model.PseudoCameraItems.LastOrDefault();
        });

        public DelegateCommand GrabOnceCommand => new DelegateCommand(() =>
        {
            Model.GrabPseudoOnce();
        });

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    Model.StopGrab();
                    CloseDialog(ButtonResult.No);
                    break;
                case "执行":
                    Model.ExecuteModule();
                    break;
                case "确认":
                    var globalParamsToAdd = Model.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid));
                    foreach (var param in globalParamsToAdd)
                    {
                        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(param);
                    }

                    Model.moduleOutputParam.TransmitParams = Model.OutputParams.ToDictionary(
                        item => item.Guid.ToString(),
                        item => (object)item);

                    CloseDialog(ButtonResult.OK, new DialogParameters { { "Param", Model } });
                    break;
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    if (string.IsNullOrEmpty(SltOutputParamName))
                    {
                        System.Windows.MessageBox.Show("请先选择输出参数");
                        return;
                    }

                    if (!Model.OutputParamResource.ContainsKey(SltOutputParamName))
                        return;

                    var curSltParam = Model.OutputParamResource[SltOutputParamName] as TransmitParam;
                    if (curSltParam == null)
                        return;

                    if (Model.OutputParams.Any(item => item.ParamName == curSltParam.Name))
                    {
                        System.Windows.MessageBox.Show("该参数已添加");
                        return;
                    }

                    Model.OutputParams.Add(new TransmitParam
                    {
                        LinkGuid = Guid,
                        ParamName = curSltParam.Name,
                        Serial = Model.Serial,
                        ParentNode = Name,
                        Name = curSltParam.Name,
                        Type = DataType._object,
                        Value = OutputParamCollector.GetDataPointValues(Model)[curSltParam.Name]?.DeepCopy(),
                        ResourcePath = curSltParam.ResourcePath,
                        Describe = ManualOutputSourceDisplay,
                    });
                    break;
            }
        });

        private void EnsureOutputSourceDescriptions()
        {
            if (Model?.OutputParams == null)
                return;

            var realOutputNames = BuildOutputNameSet(Model.GetRealCameraOutputNames());
            var pseudoOutputNames = BuildOutputNameSet(Model.GetPseudoCameraOutputNames());

            foreach (var outputParam in Model.OutputParams.Where(item => item != null && string.IsNullOrWhiteSpace(item.Describe)))
            {
                var outputName = string.IsNullOrWhiteSpace(outputParam.ParamName)
                    ? outputParam.Name
                    : outputParam.ParamName;
                bool isRealOutput = !string.IsNullOrWhiteSpace(outputName) && realOutputNames.Contains(outputName);
                bool isPseudoOutput = !string.IsNullOrWhiteSpace(outputName) && pseudoOutputNames.Contains(outputName);

                if (isRealOutput && !isPseudoOutput)
                    outputParam.Describe = RealOutputSourceDisplay;
                else if (isPseudoOutput && !isRealOutput)
                    outputParam.Describe = PseudoOutputSourceDisplay;
            }
        }

        private static System.Collections.Generic.HashSet<string> BuildOutputNameSet(System.Collections.Generic.IEnumerable<string> outputNames)
        {
            return (outputNames ?? Enumerable.Empty<string>())
                .Where(outputName => !string.IsNullOrWhiteSpace(outputName))
                .Select(outputName => outputName.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        #endregion
    }
}
