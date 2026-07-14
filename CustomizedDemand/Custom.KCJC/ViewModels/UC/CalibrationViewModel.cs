using Custom.KCJC.Models;
using Custom.KCJC.Models.StandardPlate;
using HalconDotNet;
using Microsoft.Win32;
using OpenCvSharp;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using ReeYin_V.Share.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Custom.KCJC.ViewModels
{
    public class CalibrationViewModel : DialogViewModelBase
    {
        #region Fields
        SensorDataCollectionModel model;
        #endregion

        #region Properties

        private CalibrationModel _modelParam = new CalibrationModel();
        public CalibrationModel ModelParam
        {
            get => _modelParam;
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        public int CalibrationNodeSerial
        {
            get => model?.OtherConfig?.CalibrationNodeSerial ?? 1;
            set
            {
                if (model?.OtherConfig == null)
                    return;

                if (model.OtherConfig.CalibrationNodeSerial == value)
                    return;

                model.OtherConfig.CalibrationNodeSerial = value;
                RaisePropertyChanged();
            }
        }

        private bool _isCalibrationRunning;
        public bool IsCalibrationRunning
        {
            get { return _isCalibrationRunning; }
            set
            {
                _isCalibrationRunning = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CanExecuteCalibration));
            }
        }

        public bool CanExecuteCalibration => !IsCalibrationRunning;

        #endregion

        #region Constructor

        public CalibrationViewModel()
        {
            // 标准片流程处理完成后，直接把结果同步到标定页面。
            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Subscribe((pd) =>
            {
                try
                {
                    var calibResult = pd.GetMemoryPara<KCJC0_StandardPlateAlgorithm.KCJC0_StandardPlateMeasureResult>("KCJC0_StandardPlateMeasureResult", null);
                    if (calibResult == null)
                        return;

                    ModelParam.CalibMeasureParam = pd.GetMemoryPara("KCJC0_StandardPlateMeasureParam", ModelParam.CalibMeasureParam);
                    // 刻点标准片三段合并显示时使用；普通标准片流程没有该字段时保持空数组。
                    var bumpSegmentCounts = pd.GetMemoryPara("KCJC0_StandardPlateBumpSegmentCounts", Array.Empty<int>());
                    Logs.LogInfo($"标定页面收到标准片结果：算法类型={ModelParam.CalibMeasureParam?.AlgorithmType}，高度点数={calibResult.BumpHeightPhysicalList?.Length ?? 0}，直径点数={calibResult.BumpDiameterPhysicalList?.Length ?? 0}，分段点数=[{string.Join(",", bumpSegmentCounts)}]，标准点数={ModelParam.BumpStandardRefs?.Count ?? 0}");
                    ModelParam.BumpSegmentCounts = bumpSegmentCounts;
                    ModelParam.CalibResult = calibResult;

                    if (pd.Gray != null && !pd.Gray.Empty())
                    {
                        ModelParam.DisposeImage = Common_Algorithm.MatToHObject(pd.Gray);
                    }
                }
                catch (Exception ex)
                {
                    Logs.LogError(ex);
                }
            }, ThreadOption.UIThread);

            // 标定流程复用全局运行状态，流程结束或异常后恢复“执行标定”按钮。
            PrismProvider.EventAggregator.GetEvent<WorkStatusChangeEvent>().Subscribe((status) =>
            {
                if (status != WorkStatus.Running)
                {
                    IsCalibrationRunning = false;
                }
            }, ThreadOption.UIThread);
        }

        #endregion

        #region Commands

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "从图片加载":
                    ModelParam.BumpSegmentCounts = Array.Empty<int>();
                    ModelParam.ExecuteCalib(true);
                    break;
                case "触发自动标定流程":
                    ModelParam.ExecuteCalib();
                    break;
                case "执行标定":
                    if (IsCalibrationRunning)
                        return;

                    IsCalibrationRunning = true;
                    ModelParam.BumpSegmentCounts = Array.Empty<int>();
                    PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish($"ClearStandardFilmPointCache@{CalibrationNodeSerial}");
                    PrismProvider.EventAggregator.GetEvent<SwitchWorkStatusEvent>().Publish((eRunStatus.Running, CalibrationNodeSerial));
                    break;
                case "保存":
                    MessageBoxResult result = System.Windows.MessageBox.Show("确定要保存吗?", "操作确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                        return;

                    PrismProvider.EventAggregator.GetEvent<SolutionOperationEvent>().Publish("保存");
                    break;

                case "添加槽标准":
                    ModelParam.GrooveStandardRefs?.Add(new GrooveStandardRef
                    {
                        Label         = $"槽{(ModelParam.GrooveStandardRefs?.Count ?? 0) + 1}",
                    });
                    break;

                case "删除槽标准":
                    if (ModelParam.SelectedGrooveStandardRef != null && ModelParam.GrooveStandardRefs != null)
                    {
                        ModelParam.GrooveStandardRefs.Remove(ModelParam.SelectedGrooveStandardRef);
                        ModelParam.SelectedGrooveStandardRef = null;
                        for (int i = 0; i < ModelParam.GrooveStandardRefs.Count; i++)
                            ModelParam.GrooveStandardRefs[i].Label = $"槽{i + 1}";
                    }
                    break;

                case "添加点标准":
                    ModelParam.BumpStandardRefs?.Add(new BumpStandardRef
                    {
                        Label          = $"点{(ModelParam.BumpStandardRefs?.Count ?? 0) + 1}",
                    });
                    break;

                case "删除点标准":
                    if (ModelParam.SelectedBumpStandardRef != null && ModelParam.BumpStandardRefs != null)
                    {
                        ModelParam.BumpStandardRefs.Remove(ModelParam.SelectedBumpStandardRef);
                        ModelParam.SelectedBumpStandardRef = null;
                        for (int i = 0; i < ModelParam.BumpStandardRefs.Count; i++)
                            ModelParam.BumpStandardRefs[i].Label = $"点{i + 1}";
                    }
                    break;

                case "添加台阶标准":
                    ModelParam.StepStandardRefs?.Add(new StepStandardRef
                    {
                        Label          = $"台阶{(ModelParam.StepStandardRefs?.Count ?? 0) + 1}",
                        HeightStandard = ModelParam.CalibResult?.StepHeightPhysicalMean > 0
                            ? ModelParam.CalibResult.StepHeightPhysicalMean : 0,
                        HeightStdDev   = 0,
                    });
                    break;

                case "删除台阶标准":
                    if (ModelParam.SelectedStepStandardRef != null && ModelParam.StepStandardRefs != null)
                    {
                        ModelParam.StepStandardRefs.Remove(ModelParam.SelectedStepStandardRef);
                        ModelParam.SelectedStepStandardRef = null;
                        for (int i = 0; i < ModelParam.StepStandardRefs.Count; i++)
                            ModelParam.StepStandardRefs[i].Label = $"台阶{i + 1}";
                    }
                    break;

                default:
                    break;
            }
        });

        #endregion

        #region Methods

        #endregion

        #region Override
        public override void InitParam()
        {
            model = PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches[$"{Serial.ToString("D3")}"] as SensorDataCollectionModel;

            ModelParam.CalibMeasureParam    = model.CalibMeasureParam;
            ModelParam.GrooveStandardRefs   = model.GrooveStandardRefs;
            ModelParam.BumpStandardRefs     = model.BumpStandardRefs;
            ModelParam.StepStandardRefs     = model.StepStandardRefs;
            RaisePropertyChanged(nameof(CalibrationNodeSerial));
        }
        #endregion
    }
}
