using Custom.XYHD.Models;
using Custom.XYHD.Services;
using HalconDotNet;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text;

namespace Custom.XYHD.ViewModels
{
    [Serializable]
    public partial class DetectionViewModel : DialogViewModelBase, IViewModuleParam
    {
        private SubscriptionToken _resultToken;
        private const int MaxDisplayWidth = 1024;
        private const int MaxDisplayHeight = 1024;
        private const double MinDefectPreviewCropSize = 120.0;
        private const int FrameWatchdogIntervalMs = 200;
        private const int LiveTimeoutMs = 1200;
        private const int StaleTimeoutMs = 3500;
        private const int MaxPendingFrameUpdates = 8;
        private const int NewFrameBadgeDurationMs = 500;
        [NonSerialized]
        private BatchManager _batchManager;
        [NonSerialized]
        private bool _isDialogContext;

        // 用于自动识别两个 DL 路径
        private int _path1Serial = -1;
        private int _path2Serial = -1;

        private DetectionModel _model;

        public new DetectionModel ModelParam
        {
            get
            {
                if (base.ModelParam is DetectionModel baseModel)
                {
                    if (!ReferenceEquals(_model, baseModel))
                        AttachModel(baseModel);

                    return _model;
                }

                if (_model == null)
                    AttachModel(new DetectionModel());
                else if (!ReferenceEquals(base.ModelParam, _model))
                    base.ModelParam = _model;

                return _model;
            }
            set
            {
                AttachModel(value ?? new DetectionModel());
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Model));
                RaiseModelDependentProperties();
            }
        }

        public DetectionModel Model
        {
            get => ModelParam;
            set => ModelParam = value;
        }

        private void AttachModel(DetectionModel model)
        {
            model ??= new DetectionModel();

            if (ReferenceEquals(_model, model))
            {
                if (!ReferenceEquals(base.ModelParam, model))
                    base.ModelParam = model;
                return;
            }

            if (_model != null)
                _model.PropertyChanged -= OnModelPropertyChanged;

            _model = model;
            base.ModelParam = model;
            XYHDFieldOrientationRuntimeState.Update(_model);

            if (_model != null)
                _model.PropertyChanged += OnModelPropertyChanged;
        }

        private void RaiseModelDependentProperties()
        {
            RaisePropertyChanged(nameof(InputPortSummary));
            RaisePropertyChanged(nameof(OutputPortSummary));
            RaisePropertyChanged(nameof(FieldOrientationSummary));
            RaisePropertyChanged(nameof(LeftPreviewOrientationText));
            RaisePropertyChanged(nameof(RightPreviewOrientationText));
            RaisePropertyChanged(nameof(CurrentModeText));
            RaisePropertyChanged(nameof(ImageSizeText));
            RaisePropertyChanged(nameof(LogItems));
        }

        private void OnModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e == null
                || e.PropertyName == nameof(DetectionModel.SwapLeftRightPaths)
                || e.PropertyName == nameof(DetectionModel.LeftPathXMirror)
                || e.PropertyName == nameof(DetectionModel.RightPathXMirror))
            {
                XYHDFieldOrientationRuntimeState.Update(Model);
                RaisePropertyChanged(nameof(FieldOrientationSummary));
                RaisePropertyChanged(nameof(LeftPreviewOrientationText));
                RaisePropertyChanged(nameof(RightPreviewOrientationText));
                ClearPendingFrames();
                Model.AddLog($"现场方向已更新: {FieldOrientationSummary}，已清空当前帧缓存", "INFO");
            }

            if (e == null
                || e.PropertyName == nameof(DetectionModel.ImageWidth)
                || e.PropertyName == nameof(DetectionModel.ImageHeight))
            {
                RaisePropertyChanged(nameof(ImageSizeText));
            }

            if (e == null
                || e.PropertyName == nameof(DetectionModel.InputOriginalImage)
                || e.PropertyName == nameof(DetectionModel.InputOriginalImageName)
                || e.PropertyName == nameof(DetectionModel.LeftInputImage)
                || e.PropertyName == nameof(DetectionModel.LeftInputImageName)
                || e.PropertyName == nameof(DetectionModel.LeftInputResults)
                || e.PropertyName == nameof(DetectionModel.LeftInputResultsName)
                || e.PropertyName == nameof(DetectionModel.RightInputImage)
                || e.PropertyName == nameof(DetectionModel.RightInputImageName)
                || e.PropertyName == nameof(DetectionModel.RightInputResults)
                || e.PropertyName == nameof(DetectionModel.RightInputResultsName))
            {
                RaisePropertyChanged(nameof(InputPortSummary));
            }
        }

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand ResetStatsCommand { get; }
        public DelegateCommand<string> GeneralCommand { get; }
        public DelegateCommand ChangeBatchCommand { get; }
        public DelegateCommand SaveDefectsCommand { get; }

        public DetectionViewModel()
        {
            LoadCommand = new DelegateCommand(OnLoad);
            ResetStatsCommand = new DelegateCommand(ResetAll);
            GeneralCommand = new DelegateCommand<string>(OnGeneralCommand);
            ChangeBatchCommand = new DelegateCommand(OnChangeBatch);
            SaveDefectsCommand = new DelegateCommand(OnSaveDefects);
            EnsureFrameWatchdog();
            InitBatchManager();
        }

        private void ResetAll()
        {
            Model.ResetStatistics();
            LastResult = "-";
            ConsecutiveNGCount = 0;
            MaxConsecutiveNGCount = 0;
            LastDefectCount = 0;
            LastFramePieceCount = 0;
            LastFrameNgPieceCount = 0;
            LastDefectSummary = "无缺陷";
            DefectDetails.Clear();
            LastFrameId = 0;
            LastFrameIdText = "-";
            FrameCount = 0;
            FrameIntervalMs = -1;
            LastFrameTimeText = "-";
            ShowNewFrameBadge = false;
            _lastFrameUtc = DateTime.MinValue;
            _newFrameBadgeUntilUtc = DateTime.MinValue;
            ResetPathImagePreviewThrottle();
            StreamState = "Idle";
            StreamStatusText = "等待首帧";
            ClearFrameIdTextCache();

            _path1Serial = -1;
            _path2Serial = -1;
            Path1Header = "路1 (等待...)";
            Path2Header = "路2 (等待...)";
            Path1Result = "-";
            Path2Result = "-";
            Path1DefectCount = 0;
            Path2DefectCount = 0;
            Path1PieceCount = 0;
            Path1NgPieceCount = 0;
            Path2PieceCount = 0;
            Path2NgPieceCount = 0;
            Path1DefectDetails.Clear();
            Path2DefectDetails.Clear();
            LeftDisplayImage = null;
            RightDisplayImage = null;
        }

        private void OnLoad()
        {
            if (Model.Serial == -999)
                Model.Serial = Serial;

            var cacheKey = Serial.ToString("D3");
            if (!PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.ContainsKey(cacheKey))
                PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Add(cacheKey, Model);
            else
                PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches[cacheKey] = Model;

            Model.LoadKeyParam();
            Model.EnsureDefaultOutputParam(Guid, Name);
            Model.TryRebindInputLinks();
            Model.SyncInputNamesFromLinks();
            RaisePropertyChanged(nameof(InputPortSummary));
            RaisePropertyChanged(nameof(OutputPortSummary));

            if (Visibility == Visibility.Hidden)
            {
                CloseDialogWhenReady(ButtonResult.OK, new DialogParameters
                {
                    { "Param", Model }
                });
                return;
            }

            EnsureFrameWatchdog();
            ClearPendingFrames();
            ClearFrameIdTextCache();
            ResetPathImagePreviewThrottle();
            SubscribeEvents();
            StatusText = "同步模式";
            Model.AddLog("运行模式: 同步模式（按帧执行 XYHD 节点）", "INFO");
            Model.AddLog("界面初始化完成，等待 XYHD 数据", "INFO");
        }

        public override void OnDialogClosed()
        {
            base.OnDialogClosed();
            StopFrameWatchdog();
            UnsubscribeEvents();
            if (_model != null)
                _model.IsDebug = false;
            if (_model != null)
                _model.PropertyChanged -= OnModelPropertyChanged;
        }

        public override void InitParam()
        {
            _isDialogContext = true;
            base.InitParam();

            ModelParam = InitModelParam<DetectionModel>();

            Model.EnsureDefaultOutputParam(Guid, Name);
            Model.TryRebindInputLinks();
            Model.SyncInputNamesFromLinks();
            Model.LoadKeyParam();
            Model.EnsureDefaultOutputParam(Guid, Name);
            RaisePropertyChanged(nameof(InputPortSummary));
            RaisePropertyChanged(nameof(OutputPortSummary));
        }

        private void OnGeneralCommand(string cmd)
        {
            switch (cmd)
            {
                case "取消":
                    if (!_isDialogContext)
                        return;

                    StopFrameWatchdog();
                    UnsubscribeEvents();
                    CloseDialogWhenReady(ButtonResult.No);
                    break;
                case "确认":
                    Model.TryRebindInputLinks();
                    Model.SyncInputNamesFromLinks();
                    Model.EnsureDefaultOutputParam(Guid, Name);
                    RaisePropertyChanged(nameof(InputPortSummary));
                    RaisePropertyChanged(nameof(OutputPortSummary));
                    Model.moduleOutputParam.TransmitParams = Model.OutputParams.ToDictionary(
                        item => item.Guid.ToString(),
                        item => (object)item);

                    if (!_isDialogContext)
                        return;

                    StopFrameWatchdog();
                    UnsubscribeEvents();
                    CloseDialogWhenReady(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", Model }
                    });
                    break;
                case "清空日志":
                    Model.ClearLogs();
                    break;
                case "选择保存路径":
                    SelectSavePath();
                    break;
            }
        }

        private void CloseDialogWhenReady(ButtonResult buttonResult, IDialogParameters dialogParameters = null)
        {
            if (!_isDialogContext)
                return;

            var dispatcher = PrismProvider.Dispatcher ?? Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                RequestClose.Invoke(dialogParameters, buttonResult);
                return;
            }

            dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(() => RequestClose.Invoke(dialogParameters, buttonResult)));
        }

        private void SelectSavePath()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择图像保存路径",
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrEmpty(Model.SavePath) && Directory.Exists(Model.SavePath))
                dialog.SelectedPath = Model.SavePath;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Model.SavePath = dialog.SelectedPath;
                Model.AddLog($"保存路径已设置: {Model.SavePath}", "INFO");
            }
        }
    }
}
