using ALGO.LineScanSheetCounter.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Share;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.LineScanSheetCounter.ViewModels;

/// <summary>
/// 线扫片材计数模块视图模型。
/// </summary>
[Serializable]
public sealed class LineScanSheetCounterViewModel : DialogViewModelBase, IViewModuleParam
{
    private LineScanSheetCounterDialogState? _dialogState;
    private bool _dialogAccepted;
    private TransmitParam? _inputImage;
    private TransmitParam? _maskImage;
    private TransmitParam? _currentOutputParam;
    private string _sltOutputParamName = string.Empty;
    private double _scaleFactor;
    private double _smoothSigma;
    private double _edgeThreshold;
    private double _measureCenterColumn;
    private double _measureRoiWidth;
    private double _cropRatio;
    private bool _resetBeforeNextRun;
    private bool _isFastModeEnabled;
    private bool _isLoadingDialogSettings;
    private bool _saveSheetImages;
    private bool _saveConcatImage;
    private bool _saveRemainImage;
    private string _saveDirectory = string.Empty;

    [NonSerialized]
    private DispatcherTimer? _runtimePreviewTimer;

    public new LineScanSheetCounterModel ModelParam
    {
        get { return base.ModelParam as LineScanSheetCounterModel; }
        set { base.ModelParam = value; }
    }

    public TransmitParam? CurrentOutputParam
    {
        get => _currentOutputParam;
        set => SetProperty(ref _currentOutputParam, value);
    }

    public TransmitParam? InputImage
    {
        get => _inputImage;
        set => SetProperty(ref _inputImage, value);
    }

    public TransmitParam? MaskImage
    {
        get => _maskImage;
        set => SetProperty(ref _maskImage, value);
    }

    public string SltOutputParamName
    {
        get => _sltOutputParamName;
        set => SetProperty(ref _sltOutputParamName, value);
    }

    public double ScaleFactor
    {
        get => _scaleFactor;
        set => SetProperty(ref _scaleFactor, value);
    }

    public double SmoothSigma
    {
        get => _smoothSigma;
        set => SetProperty(ref _smoothSigma, value);
    }

    public double EdgeThreshold
    {
        get => _edgeThreshold;
        set => SetProperty(ref _edgeThreshold, value);
    }

    public double MeasureCenterColumn
    {
        get => _measureCenterColumn;
        set => SetProperty(ref _measureCenterColumn, value);
    }

    public double MeasureRoiWidth
    {
        get => _measureRoiWidth;
        set => SetProperty(ref _measureRoiWidth, value);
    }

    public double CropRatio
    {
        get => _cropRatio;
        set => SetProperty(ref _cropRatio, value);
    }

    public bool ResetBeforeNextRun
    {
        get => _resetBeforeNextRun;
        set => SetProperty(ref _resetBeforeNextRun, value);
    }

    public bool IsFastModeEnabled
    {
        get => _isFastModeEnabled;
        set
        {
            if (SetProperty(ref _isFastModeEnabled, value))
            {
                if (_isLoadingDialogSettings || ModelParam == null)
                {
                    return;
                }

                ModelParam.IsFastModeEnabled = value;
                ModelParam.RefreshRuntimePreviewForFastModeChange();
            }
        }
    }

    public bool SaveSheetImages
    {
        get => _saveSheetImages;
        set => SetProperty(ref _saveSheetImages, value);
    }

    public bool SaveConcatImage
    {
        get => _saveConcatImage;
        set => SetProperty(ref _saveConcatImage, value);
    }

    public bool SaveRemainImage
    {
        get => _saveRemainImage;
        set => SetProperty(ref _saveRemainImage, value);
    }

    public string SaveDirectory
    {
        get => _saveDirectory;
        set => SetProperty(ref _saveDirectory, value ?? string.Empty);
    }

    public string CounterSummaryText =>
        $"本次新增 {ModelParam.IncrementCount}  累计 {ModelParam.TotalCount}";

    public override void InitParam()
    {
        ModelParam = InitModelParam<LineScanSheetCounterModel>();
        if (Param is IModuleParam moduleParam)
        {
            ModelParam.moduleInputParam = moduleParam.moduleInputParam;
        }

        ModelParam.LoadKeyParam();

        PrepareModelForDialog();
        RegisterOutputResources();
        RefreshInputParamsForDialog();
        _dialogState = ModelParam.CaptureDialogState();
        InputImage = _dialogState.InputImage;
        MaskImage = _dialogState.MaskImage;
        LoadDialogSettingsFromModel();
        ModelParam.SetRuntimePreviewEnabled(true);
        StartRuntimePreviewTimer();
        _dialogAccepted = false;
    }

    public override void OnDialogClosed()
    {
        StopRuntimePreviewTimer();
        if (!_dialogAccepted)
        {
            ModelParam.RestoreDialogState(_dialogState);
        }

        ModelParam.SetRuntimePreviewEnabled(false);
        base.OnDialogClosed();
    }

    private void StartRuntimePreviewTimer()
    {
        StopRuntimePreviewTimer();
        _runtimePreviewTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _runtimePreviewTimer.Tick += OnRuntimePreviewTimerTick;
        _runtimePreviewTimer.Start();
    }

    private void StopRuntimePreviewTimer()
    {
        if (_runtimePreviewTimer == null)
        {
            return;
        }

        _runtimePreviewTimer.Stop();
        _runtimePreviewTimer.Tick -= OnRuntimePreviewTimerTick;
        _runtimePreviewTimer = null;
    }

    private void OnRuntimePreviewTimerTick(object? sender, EventArgs e)
    {
        try
        {
            ModelParam.ApplyDialogInputLinks(InputImage, MaskImage);
            ModelParam.RefreshRuntimePreviewFromLinkedInputs();
        }
        catch
        {
            // 实时预览失败不影响计数主流程，部署时不向控制台输出噪声。
        }
    }

    public DelegateCommand<string> GeneralCommand => new(order =>
    {
        switch (order)
        {
            case "执行":
                if (IsSolutionRunning())
                {
                    return;
                }

                ModelParam.ApplyDialogInputLinks(InputImage, MaskImage);
                ApplyDialogSettingsToModel();
                ModelParam.ExecuteModule(clearRemainBeforeRun: true);
                RaiseCounterProperties();
                break;
            case "重置":
                ModelParam.ResetCounter();
                RaiseCounterProperties();
                break;
            case "取消":
                CloseDialog(ButtonResult.No);
                break;
            case "确认":
                _dialogAccepted = true;
                ModelParam.ApplyDialogInputLinks(InputImage, MaskImage);
                ApplyDialogSettingsToModel();
                ModelParam.LoadKeyParam();

                foreach (TransmitParam outputParam in ModelParam.OutputParams.Where(item => item.IsGlobal))
                {
                    if (!PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == outputParam.Guid))
                    {
                        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(outputParam);
                    }
                }

                ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                    item => item.Guid.ToString(),
                    item => (object)item);
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam },
                });
                break;
        }
    });

    private static bool IsSolutionRunning()
    {
        return PrismProvider.WorkStatusManager?.CurStatus == WorkStatus.Running;
    }

    public DelegateCommand LoadCommand => new(() =>
    {
        if (Visibility == Visibility.Hidden)
        {
            _dialogAccepted = true;
            ModelParam.ApplyDialogInputLinks(InputImage, MaskImage);
            ApplyDialogSettingsToModel();
            ModelParam.LoadKeyParam();
            CloseDialog(ButtonResult.OK, new DialogParameters
            {
                { "Param", ModelParam },
            });
        }
    });

    public DelegateCommand<object> DataOperateCommand => new(obj =>
    {
        switch (obj?.ToString())
        {
            case "Add":
                AddSelectedOutputParam();
                break;
            case "Delete":
                DeleteSelectedOutputParam();
                break;
        }
    });

    private void RegisterOutputResources()
    {
        ModelParam.OutputParamResource.Clear();
        var dataPoints = OutputParamCollector.GetDataPoints(typeof(LineScanSheetCounterModel));
        string[] validOutputNames = dataPoints.Select(point => point.Name).ToArray();
        for (int index = ModelParam.OutputParams.Count - 1; index >= 0; index--)
        {
            TransmitParam outputParam = ModelParam.OutputParams[index];
            if (string.IsNullOrWhiteSpace(outputParam.ParamName) ||
                !validOutputNames.Contains(outputParam.ParamName))
            {
                ModelParam.OutputParams.RemoveAt(index);
            }
        }

        foreach (var point in dataPoints)
        {
            ModelParam.OutputParamResource.Add(point.Name + $"[{point.Description}]", new TransmitParam
            {
                LinkGuid = Guid,
                Name = point.Name,
                Type = GetOutputDataType(point.Name),
                Resourece = ResoureceType.None,
                Value = OutputParamCollector.GetDataPointValues(ModelParam)[point.Name],
                Describe = point.Description,
                ResourcePath = point.MemberInfo.DeclaringType?.FullName + "." + point.Name
            });
        }
    }

    private void PrepareModelForDialog()
    {
        if (ModelParam.Serial == -999)
        {
            ModelParam.Serial = Serial;
        }

        ModelParamBase.ModuleName = ModelParam.Serial.ToString("D3");
        ModelParam.moduleInputParam ??= new ModuleParam();
        ModelParam.moduleInputParam.TransmitParams ??= [];
        ModelParam.moduleOutputParam ??= new ModuleParam();
        ModelParam.moduleOutputParam.TransmitParams ??= [];
    }

    private void RefreshInputParamsForDialog()
    {
        ModelParam.InputParams.Clear();
        foreach (TransmitParam item in ModelParam.moduleInputParam.TransmitParams.Values.OfType<TransmitParam>())
        {
            item.Resourece = ResoureceType.Inupt;
            ModelParam.InputParams.Add(item);
        }

        ModelParam.OutputParamNames = ModelParam.OutputParamResource.Select(item => item.Key).ToList();
    }

    private void LoadDialogSettingsFromModel()
    {
        _isLoadingDialogSettings = true;
        try
        {
            ScaleFactor = ModelParam.ScaleFactor;
            SmoothSigma = ModelParam.SmoothSigma;
            EdgeThreshold = ModelParam.EdgeThreshold;
            MeasureCenterColumn = ModelParam.MeasureCenterColumn;
            MeasureRoiWidth = ModelParam.MeasureRoiWidth;
            CropRatio = ModelParam.CropRatio;
            ResetBeforeNextRun = ModelParam.ResetBeforeNextRun;
            IsFastModeEnabled = ModelParam.IsFastModeEnabled;
            SaveSheetImages = ModelParam.SaveSheetImages;
            SaveConcatImage = ModelParam.SaveConcatImage;
            SaveRemainImage = ModelParam.SaveRemainImage;
            SaveDirectory = ModelParam.SaveDirectory;
        }
        finally
        {
            _isLoadingDialogSettings = false;
        }
    }

    private void ApplyDialogSettingsToModel()
    {
        ModelParam.ScaleFactor = ScaleFactor;
        ModelParam.SmoothSigma = SmoothSigma;
        ModelParam.EdgeThreshold = EdgeThreshold;
        ModelParam.MeasureCenterColumn = MeasureCenterColumn;
        ModelParam.MeasureRoiWidth = MeasureRoiWidth;
        ModelParam.CropRatio = CropRatio;
        ModelParam.ResetBeforeNextRun = ResetBeforeNextRun;
        ModelParam.IsFastModeEnabled = IsFastModeEnabled;
        ModelParam.SaveSheetImages = SaveSheetImages;
        ModelParam.SaveConcatImage = SaveConcatImage;
        ModelParam.SaveRemainImage = SaveRemainImage;
        ModelParam.SaveDirectory = SaveDirectory;
    }

    private void AddSelectedOutputParam()
    {
        if (string.IsNullOrWhiteSpace(SltOutputParamName))
        {
            return;
        }

        if (!ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object? resource) ||
            resource is not TransmitParam selectedParam)
        {
            return;
        }

        if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
        {
            MessageBox.Show("已包含重名参数，请重新选择。");
            return;
        }

        ModelParam.OutputParams.Add(new TransmitParam
        {
            LinkGuid = Guid,
            ParamName = selectedParam.Name,
            Serial = ModelParam.Serial,
            Name = SltOutputParamName,
            Type = GetOutputDataType(selectedParam.Name),
            Value = OutputParamCollector.GetDataPointValues(ModelParam)[selectedParam.Name],
            ResourcePath = selectedParam.ResourcePath,
        });
    }

    private void DeleteSelectedOutputParam()
    {
        if (CurrentOutputParam == null)
        {
            return;
        }

        ModelParam.OutputParams.Remove(CurrentOutputParam);
        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
        CurrentOutputParam = null;
    }

    private void RaiseCounterProperties()
    {
        RaisePropertyChanged(nameof(CounterSummaryText));
        RaisePropertyChanged(nameof(ModelParam));
    }

    private static DataType GetOutputDataType(string outputName)
    {
        return outputName == nameof(LineScanSheetCounterModel.CroppedImages)
            ? DataType.HObject
            : DataType._object;
    }
}
