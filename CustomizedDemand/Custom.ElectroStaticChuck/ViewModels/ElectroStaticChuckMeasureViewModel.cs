using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;
using Forms = System.Windows.Forms;

namespace Custom.ElectroStaticChuckMeasure.ViewModels
{
    [Serializable]
    public class ElectroStaticChuckMeasureViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        private const int MaxOperationLogEntries = 300;
        private readonly ElectroStaticChuckMeasureModel _fallbackModel = new();
        private string _sltOutputParamName = string.Empty;
        private TransmitParam? _currentOutputParam;
        private bool _isExecuting;

        #endregion

        #region Properties

        public string SltOutputParamName
        {
            get => _sltOutputParamName;
            set => SetProperty(ref _sltOutputParamName, value ?? string.Empty);
        }

        public new ElectroStaticChuckMeasureModel ModelParam
        {
            get => base.ModelParam as ElectroStaticChuckMeasureModel ?? _fallbackModel;
            set
            {
                if (base.ModelParam is ElectroStaticChuckMeasureModel current)
                {
                    current.PropertyChanged -= ModelParam_PropertyChanged;
                }

                base.ModelParam = value;

                if (value != null)
                {
                    value.PropertyChanged += ModelParam_PropertyChanged;
                }

                RaisePropertyChanged();
                RaiseSummaryProperties();
            }
        }

        public TransmitParam? CurrentOutputParam
        {
            get => _currentOutputParam;
            set => SetProperty(ref _currentOutputParam, value);
        }

        public ObservableCollection<string> OperationLogs { get; } = new();

        public IEnumerable<FineRegistrationMode> FineRegistrationModes { get; } =
            Enum.GetValues(typeof(FineRegistrationMode)).Cast<FineRegistrationMode>();

        public IEnumerable<ElectroStaticChuckMeasurementMode> MeasurementModes { get; } =
            Enum.GetValues(typeof(ElectroStaticChuckMeasurementMode)).Cast<ElectroStaticChuckMeasurementMode>();

        public string RunStatusText => ModelParam.Output == null ? "Not run" : ModelParam.Output.RunStatus.ToString();

        public string RunTimeText => ModelParam.Output == null ? "--" : $"{ModelParam.Output.RunTime:F0} ms";

        public string CalibrationStateText => ModelParam.CurrentCalibrationState?.Pitch.IsCalibrated == true
            ? "Pitch calibrated"
            : "Not calibrated";

        #endregion

        #region Constructor

        public ElectroStaticChuckMeasureViewModel()
        {
            BrowseCalibrationFrameDirectoryCommand = new DelegateCommand(() => BrowseDirectory("Select calibration frame directory", path =>
            {
                ModelParam.CalibrationFrameDirectory = path;
                AppendOperationLog($"Calibration frame directory: {path}");
            }));

            BrowseMeasurementFrameDirectoryCommand = new DelegateCommand(() => BrowseDirectory("Select measurement frame directory", path =>
            {
                ModelParam.MeasurementFrameDirectory = path;
                AppendOperationLog($"Measurement frame directory: {path}");
            }));

            BrowseOutputDirectoryCommand = new DelegateCommand(() => BrowseDirectory("Select output directory", path =>
            {
                ModelParam.OutputDirectory = path;
                AppendOperationLog($"Output directory: {path}");
            }));

            RunCalibrationCommand = new DelegateCommand(async () => await RunCalibrationAsync(), () => !_isExecuting);
            ExecuteMeasurementCommand = new DelegateCommand(async () => await ExecuteMeasurementAsync(), () => !_isExecuting);
            AppendOperationLog("ElectroStaticChuck module is ready.");
        }

        #endregion

        #region Methods

        public override void InitParam()
        {
            ModelParam = InitModelParam<ElectroStaticChuckMeasureModel>();
            ModelParam.Output ??= new ExecuteModuleOutput
            {
                RunStatus = NodeStatus.NotRun,
                RunTime = 0
            };
            RaiseSummaryProperties();
        }

        public override void OnDialogClosed()
        {
            if (base.ModelParam is ElectroStaticChuckMeasureModel model)
            {
                model.PropertyChanged -= ModelParam_PropertyChanged;
                model.IsDebug = false;
            }

            base.OnDialogClosed();
        }

        private void ModelParam_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ElectroStaticChuckMeasureModel.Output):
                case nameof(ElectroStaticChuckMeasureModel.CurrentCalibrationState):
                case nameof(ElectroStaticChuckMeasureModel.CalibrationSuccess):
                case nameof(ElectroStaticChuckMeasureModel.CalibrationMessage):
                case nameof(ElectroStaticChuckMeasureModel.MeasurementSuccess):
                case nameof(ElectroStaticChuckMeasureModel.MeasurementMessage):
                case nameof(ElectroStaticChuckMeasureModel.ConvexsFlatness):
                case nameof(ElectroStaticChuckMeasureModel.OverallFlatness):
                    RaiseSummaryProperties();
                    break;
            }
        }

        private static void BrowseDirectory(string description, Action<string> apply)
        {
            using Forms.FolderBrowserDialog dialog = new()
            {
                Description = description,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                apply(dialog.SelectedPath);
            }
        }

        private async Task RunCalibrationAsync()
        {
            if (_isExecuting)
                return;

            try
            {
                _isExecuting = true;
                RaiseCommandCanExecuteChanged();
                AppendOperationLog("Starting calibration.");
                await Task.Run(() => ModelParam.RunCalibration().GetAwaiter().GetResult());
                AppendOperationLog(ModelParam.CalibrationMessage);
                RaiseSummaryProperties();
            }
            finally
            {
                _isExecuting = false;
                RaiseCommandCanExecuteChanged();
            }
        }

        private async Task ExecuteMeasurementAsync()
        {
            if (_isExecuting)
                return;

            try
            {
                _isExecuting = true;
                RaiseCommandCanExecuteChanged();
                AppendOperationLog("Starting measurement.");
                await Task.Run(() => ModelParam.ExecuteModule().GetAwaiter().GetResult());
                AppendOperationLog(ModelParam.MeasurementMessage);
                RaiseSummaryProperties();
            }
            finally
            {
                _isExecuting = false;
                RaiseCommandCanExecuteChanged();
            }
        }

        private void RaiseCommandCanExecuteChanged()
        {
            RunCalibrationCommand.RaiseCanExecuteChanged();
            ExecuteMeasurementCommand.RaiseCanExecuteChanged();
        }

        private void RaiseSummaryProperties()
        {
            RaisePropertyChanged(nameof(RunStatusText));
            RaisePropertyChanged(nameof(RunTimeText));
            RaisePropertyChanged(nameof(CalibrationStateText));
        }

        private void AppendOperationLog(string message)
        {
            OperationLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            while (OperationLogs.Count > MaxOperationLogEntries)
            {
                OperationLogs.RemoveAt(0);
            }
        }

        private void ConfirmAndClose()
        {
            ModelParam.LoadKeyParam();

            var solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solution != null)
            {
                solution.GlobalParams.AddRange(
                    ModelParam.OutputParams.Where(item => item.IsGlobal &&
                    !solution.GlobalParams.Any(gp => gp.Guid == item.Guid)));

                ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                    item => item.Guid.ToString(),
                    item => (object)item);

                if (!solution.ImgControlPair.ContainsKey(ElectroStaticChuckMeasureModel.ModuleName))
                {
                    solution.ImgControlPair.Add(ElectroStaticChuckMeasureModel.ModuleName, ModelParam.mWindowH);
                }
                else
                {
                    solution.ImgControlPair[ElectroStaticChuckMeasureModel.ModuleName] = ModelParam.mWindowH;
                }
            }

            CloseDialog(ButtonResult.OK, new DialogParameters()
            {
                { "Param", ModelParam },
            });
        }

        #endregion

        #region Commands

        public DelegateCommand BrowseCalibrationFrameDirectoryCommand { get; }

        public DelegateCommand BrowseMeasurementFrameDirectoryCommand { get; }

        public DelegateCommand BrowseOutputDirectoryCommand { get; }

        public DelegateCommand RunCalibrationCommand { get; }

        public DelegateCommand ExecuteMeasurementCommand { get; }

        public DelegateCommand<string> GeneralCommand => new((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "执行":
                    _ = ExecuteMeasurementAsync();
                    break;
                case "确认":
                    ConfirmAndClose();
                    break;
            }
        });

        public DelegateCommand LoadCommand => new(() =>
        {
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand => new((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    AddOutputParam();
                    break;
                case "Delete":
                    DeleteOutputParam();
                    break;
            }
        });

        private void AddOutputParam()
        {
            if (string.IsNullOrWhiteSpace(SltOutputParamName) ||
                !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object? resource) ||
                resource is not TransmitParam curSltParam)
            {
                return;
            }

            if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
            {
                MessageBox.Show("已包含重名参数，请重新输入！");
                return;
            }

            Dictionary<string, object> dataPointValues = OutputParamCollector.GetDataPointValues(ModelParam);

            if (curSltParam.Resourece == ResoureceType.None)
            {
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    LinkGuid = Guid,
                    ParamName = curSltParam.Name,
                    Serial = ModelParam.Serial,
                    Name = SltOutputParamName,
                    Type = DataType._object,
                    Value = dataPointValues.TryGetValue(curSltParam.Name, out object? value) ? value : null,
                    ResourcePath = curSltParam.ResourcePath,
                });
            }
            else if (curSltParam.Resourece == ResoureceType.Inupt)
            {
                TransmitParam? inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                if (inputParam == null)
                    return;

                ModelParam.OutputParams.Add(new TransmitParam
                {
                    LinkGuid = Guid,
                    Name = SltOutputParamName,
                    Type = DataType._object,
                    Value = inputParam.Value,
                    ResourcePath = inputParam.ResourcePath,
                    Serial = inputParam.Serial
                });
            }
        }

        private void DeleteOutputParam()
        {
            if (CurrentOutputParam == null)
                return;

            ModelParam.OutputParams.Remove(CurrentOutputParam);
            PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams.Remove(CurrentOutputParam);
            CurrentOutputParam = null;
        }

        #endregion
    }
}
