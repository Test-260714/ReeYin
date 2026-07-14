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
            AddOutputs(Model?.GetRealCameraOutputNames());
        });

        public DelegateCommand AddRealCameraOutputsCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            AddOutputs(Model.GetRealCameraOutputNames());
        });

        public DelegateCommand AddPseudoCameraOutputsCommand => new DelegateCommand(() =>
        {
            if (Model == null)
                return;

            AddOutputs(Model.GetPseudoCameraOutputNames());
        });

        private void AddOutputs(System.Collections.Generic.IEnumerable<string> outputNames)
        {
            if (Model == null)
                return;

            var dataPointValues = OutputParamCollector.GetDataPointValues(Model);
            foreach (var outputName in outputNames ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(outputName))
                    continue;

                if (Model.OutputParams.Any(param => param.ParamName == outputName))
                    continue;

                var resource = Model.OutputParamResource.Values
                    .OfType<TransmitParam>()
                    .FirstOrDefault(param => param.Name == outputName);
                if (resource == null)
                    continue;

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
                });
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
                    });
                    break;
            }
        });
        #endregion
    }
}
