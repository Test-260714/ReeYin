using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Share;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

using Custom.WaferFlatnessMeasure.Models;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    public class WaferFlatnessConfigViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields
        private ObservableCollection<Shape> _trajectoryShapes = new ObservableCollection<Shape>();
        private readonly HashSet<LocusInfo> _subscribedLocusInfos = new HashSet<LocusInfo>();
        private readonly HashSet<LocusInfo> _syncingPointLocusInfos = new HashSet<LocusInfo>();
        private ObservableCollection<LocusInfo>? _observedLocusInfos;
        private CreateTrajectoryModel? _observedCreateTrajectoryModel;
        private ObservableCollection<CustomRingDefinition>? _observedCustomRings;
        private Task _moveToSelectedLocusTask = Task.CompletedTask;
        #endregion

        #region Properties
        private string _sltOutputParamName;
        public string SltOutputParamName
        {
            get => _sltOutputParamName;
            set => SetProperty(ref _sltOutputParamName, value);
        }

        private LocusInfo? _selectedLocus;
        public LocusInfo? SelectedLocus
        {
            get => _selectedLocus;
            set
            {
                if (SetProperty(ref _selectedLocus, value))
                {
                    RefreshTrajectoryShapes();
                }
            }
        }

        private CalibrationWaferPosition? _selectedCalibrationWaferPosition;
        public CalibrationWaferPosition? SelectedCalibrationWaferPosition
        {
            get => _selectedCalibrationWaferPosition;
            set => SetProperty(ref _selectedCalibrationWaferPosition, value);
        }

        private SensorDataCollectionModel _calibModel;
        public SensorDataCollectionModel CalibModel
        {
            get => _calibModel;
            set => SetProperty(ref _calibModel, value);
        }

        public new SensorMotionControlModel ModelParam
        {
            get { return base.ModelParam as SensorMotionControlModel; }
            set { base.ModelParam = value; }
        }

        private TransmitParam _currentOutputParam;
        public TransmitParam CurrentOutputParam
        {
            get => _currentOutputParam;
            set => SetProperty(ref _currentOutputParam, value);
        }

        public ObservableCollection<Shape> TrajectoryShapes
        {
            get => _trajectoryShapes;
            set => SetProperty(ref _trajectoryShapes, value);
        }

        private CustomRingDefinition? _selectedCustomRing;
        public CustomRingDefinition? SelectedCustomRing
        {
            get => _selectedCustomRing;
            set
            {
                if (SetProperty(ref _selectedCustomRing, value))
                {
                    RefreshTrajectoryShapes();
                }
            }
        }

        public IReadOnlyList<LocusTypeOption> LocusTypeOptions { get; } = new[]
        {
            new LocusTypeOption(LocusInfo.LineType, "线段"),
            new LocusTypeOption(LocusInfo.PointType, "点位")
        };

        public IReadOnlyList<CircleTrajectoryGenerateMode> CircleTrajectoryGenerateModes { get; } =
            Enum.GetValues(typeof(CircleTrajectoryGenerateMode))
                .Cast<CircleTrajectoryGenerateMode>()
                .ToList();

        public IReadOnlyList<CalibrationWaferMeasurementMode> CalibrationWaferMeasurementModes { get; } =
            Enum.GetValues(typeof(CalibrationWaferMeasurementMode))
                .Cast<CalibrationWaferMeasurementMode>()
                .ToList();

        private bool _isGoogolControlCardVisible = true;
        public bool IsGoogolControlCardVisible
        {
            get => _isGoogolControlCardVisible;
            private set => SetProperty(ref _isGoogolControlCardVisible, value);
        }

        private bool _isAcsControlCardVisible;
        public bool IsAcsControlCardVisible
        {
            get => _isAcsControlCardVisible;
            private set => SetProperty(ref _isAcsControlCardVisible, value);
        }
        #endregion

        #region Override
        public override void InitParam()
        {
            ModelParam = InitModelParam<SensorMotionControlModel>();
            ModelParam.LoadKeyParam();
            RefreshControlCardParameterVisibility();
            CalibModel = ResolveCalibModel();
            SltOutputParamName ??= ModelParam.OutputParamNames.FirstOrDefault();
            AttachModelListeners();
            SelectedLocus ??= ModelParam.AllLocusInfo.FirstOrDefault();
            SelectedCalibrationWaferPosition ??= ModelParam.CalibrationWaferPositions.FirstOrDefault();
            SelectedCustomRing ??= ModelParam.CreateTrajectoryModel.CustomRingDefinitions.FirstOrDefault();
            RefreshTrajectoryShapes();
        }

        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
                ModelParam.PropertyChanged -= OnModelParamPropertyChanged;
            }

            AttachTrajectoryListeners(null);
            AttachCreateTrajectoryModelListeners(null);
        }
        #endregion

        #region Commands
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "执行":
                    PrismProvider.Dispatcher.BeginInvoke(() =>
                    {
                        Task.Run(() =>
                        {
                            ModelParam.ExecuteModule();
                        });
                        
                    });
                    break;

                case "执行标定":
                    var ControlCard = (PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] as ControlCardConfigModel).CardModels[0];
                    ModelParam.ExecuteCalibrationPointMoving(ControlCard);
                    break;
                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam }
                    });
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam }
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    if (string.IsNullOrWhiteSpace(SltOutputParamName) ||
                        !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object resourceObject) ||
                        resourceObject is not TransmitParam curSltParam)
                    {
                        break;
                    }

                    if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
                    {
                        MessageBox.Show("已包含重名参数，请重新输入！");
                        break;
                    }

                    ModelParam.OutputParams.Add(new TransmitParam
                    {
                        LinkGuid = Guid,
                        ParamName = curSltParam.Name,
                        Serial = ModelParam.Serial,
                        Name = SltOutputParamName,
                        ParentNode = Name,
                        Type = DataType._object,
                        Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                        ResourcePath = curSltParam.ResourcePath,
                    });
                    break;

                case "Delete":
                    if (CurrentOutputParam == null)
                    {
                        break;
                    }

                    ModelParam.OutputParams.Remove(CurrentOutputParam);
                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
                    CurrentOutputParam = null;
                    break;
            }
        });

        public DelegateCommand GenerateCircleTrajectoryCommand => new DelegateCommand(() =>
        {
            try
            {
                var createTrajectoryModel = ModelParam.CreateTrajectoryModel;
                var locusInfos = createTrajectoryModel.GenerateCircleLocusInfos(
                    param => ModelParam.GetTransmitParam(ModelParam.InputParams, param, false),
                    ModelParam.SensorInterval);

                if (createTrajectoryModel.IsOptimalPathEnabled &&
                    !createTrajectoryModel.IsCalibrationWaferMeasurementActive)
                {
                    locusInfos = WaferTrajectoryMotionHelper.SortLocusInfosByShortestPath(locusInfos);
                }

                ModelParam.AllLocusInfo = locusInfos.ToObservableCollection();
                SelectedLocus = ModelParam.AllLocusInfo.FirstOrDefault();
                RefreshTrajectoryShapes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "生成轨迹失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        });

        public DelegateCommand AddCustomRingCommand => new DelegateCommand(() =>
        {
            try
            {
                var trajectoryModel = ModelParam?.CreateTrajectoryModel;
                if (trajectoryModel == null)
                {
                    return;
                }

                var customRing = trajectoryModel.AddCustomRing(trajectoryModel.CustomRingRadiusInput);
                if (customRing != null)
                {
                    SelectedCustomRing = customRing;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "添加圆环失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        });

        public DelegateCommand RemoveCustomRingCommand => new DelegateCommand(() =>
        {
            var trajectoryModel = ModelParam?.CreateTrajectoryModel;
            var customRings = trajectoryModel?.CustomRingDefinitions;
            if (customRings == null || customRings.Count == 0)
            {
                return;
            }

            if (SelectedCustomRing == null)
            {
                MessageBox.Show("请先选中要删除的圆环。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int selectedIndex = customRings.IndexOf(SelectedCustomRing);
            if (selectedIndex < 0)
            {
                return;
            }

            if (trajectoryModel == null || !trajectoryModel.RemoveCustomRing(SelectedCustomRing))
            {
                return;
            }

            SelectedCustomRing = customRings.Count == 0
                ? null
                : customRings[Math.Min(selectedIndex, customRings.Count - 1)];
        });

        public DelegateCommand AddLocusCommand => new DelegateCommand(() =>
        {
            var locusInfos = EnsureLocusCollection();
            var newLocus = CreateNewLocus();
            int insertIndex = SelectedLocus == null ? locusInfos.Count : locusInfos.IndexOf(SelectedLocus) + 1;
            if (insertIndex < 0 || insertIndex > locusInfos.Count)
            {
                insertIndex = locusInfos.Count;
            }

            locusInfos.Insert(insertIndex, newLocus);
            SelectedLocus = newLocus;
        });

        public DelegateCommand RemoveLocusCommand => new DelegateCommand(() =>
        {
            var locusInfos = ModelParam?.AllLocusInfo;
            if (locusInfos == null || locusInfos.Count == 0 || SelectedLocus == null)
            {
                return;
            }

            int selectedIndex = locusInfos.IndexOf(SelectedLocus);
            if (selectedIndex < 0)
            {
                return;
            }

            locusInfos.RemoveAt(selectedIndex);
            SelectedLocus = locusInfos.Count == 0
                ? null
                : locusInfos[Math.Min(selectedIndex, locusInfos.Count - 1)];
        });

        public DelegateCommand ClearLocusCommand => new DelegateCommand(() =>
        {
            var locusInfos = ModelParam?.AllLocusInfo;
            if (locusInfos == null || locusInfos.Count == 0)
            {
                return;
            }

            if (MessageBox.Show("确定要清空当前轨迹列表吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            locusInfos.Clear();
            SelectedLocus = null;
        });

        public DelegateCommand AddCalibrationWaferPositionCommand => new DelegateCommand(() =>
        {
            var positions = EnsureCalibrationWaferPositionCollection();
            var newPosition = CreateNewCalibrationWaferPosition(positions.Count + 1);
            int insertIndex = SelectedCalibrationWaferPosition == null
                ? positions.Count
                : positions.IndexOf(SelectedCalibrationWaferPosition) + 1;

            if (insertIndex < 0 || insertIndex > positions.Count)
            {
                insertIndex = positions.Count;
            }

            positions.Insert(insertIndex, newPosition);
            SelectedCalibrationWaferPosition = newPosition;
        });

        public DelegateCommand RemoveCalibrationWaferPositionCommand => new DelegateCommand(() =>
        {
            var positions = ModelParam?.CalibrationWaferPositions;
            if (positions == null || positions.Count == 0 || SelectedCalibrationWaferPosition == null)
            {
                return;
            }

            int selectedIndex = positions.IndexOf(SelectedCalibrationWaferPosition);
            if (selectedIndex < 0)
            {
                return;
            }

            positions.RemoveAt(selectedIndex);
            SelectedCalibrationWaferPosition = positions.Count == 0
                ? null
                : positions[Math.Min(selectedIndex, positions.Count - 1)];
        });

        public DelegateCommand ClearCalibrationWaferPositionCommand => new DelegateCommand(() =>
        {
            var positions = ModelParam?.CalibrationWaferPositions;
            if (positions == null || positions.Count == 0)
            {
                return;
            }

            if (MessageBox.Show("确定要清空当前标定片位置列表吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            positions.Clear();
            SelectedCalibrationWaferPosition = null;
        });

        public DelegateCommand MoveToSelectedLocusCommand => new DelegateCommand(() =>
        {
            if (SelectedLocus == null)
            {
                MessageBox.Show("请先在轨迹表格中选中一条记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var controlCard = (PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] as ControlCardConfigModel)?.CardModels?.FirstOrDefault();
            if (controlCard == null)
            {
                MessageBox.Show("未找到可用的运动控制卡。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isPoint = string.Equals(SelectedLocus.Type, LocusInfo.PointType, StringComparison.OrdinalIgnoreCase);
            string coordinateText = isPoint
                ? $"({SelectedLocus.TargetX:F2}, {SelectedLocus.TargetY:F2})"
                : $"({SelectedLocus.OriginX:F2}, {SelectedLocus.OriginY:F2})";
            string message = isPoint
                ? $"确定要移动到选中点 {coordinateText} 吗？"
                : $"当前选中的是线段轨迹，将移动到起点 {coordinateText}。是否继续？";

            if (MessageBox.Show(message, "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            if (_moveToSelectedLocusTask != null && !_moveToSelectedLocusTask.IsCompleted)
            {
                MessageBox.Show("正在执行上一条移动指令，请稍后再试。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var locusToMove = SelectedLocus;
            _moveToSelectedLocusTask = Task.Run(() =>
            {
                if (!ModelParam.MoveToLocus(controlCard, locusToMove))
                {
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("移动到选中点失败，请检查日志或软限位配置。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
            });
        });
        #endregion

        #region Private Methods
        private void RefreshControlCardParameterVisibility()
        {
            var controlCard = ResolveCurrentControlCard();
            var isAcsControlCard = IsAcsControlCard(controlCard);

            IsAcsControlCardVisible = isAcsControlCard;
            IsGoogolControlCardVisible = IsGoogolControlCard(controlCard) || !isAcsControlCard;
        }

        private static object? ResolveCurrentControlCard()
        {
            try
            {
                var modules = PrismProvider.HardwareModuleManager?.Modules;
                if (modules == null || !modules.TryGetValue(ConfigKey.ControlCard, out var moduleConfig))
                {
                    return null;
                }

                var controlCardConfig = moduleConfig as ControlCardConfigModel;
                return controlCardConfig?.CurSltCard ?? controlCardConfig?.CardModels?.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsGoogolControlCard(object? controlCard)
        {
            var typeName = controlCard?.GetType().FullName ?? string.Empty;
            var vendorName = GetStringProperty(controlCard, nameof(ReeYin_V.Hardware.ControlCard.ControlCardBase.VenderName));

            return string.Equals(vendorName, "Googol", StringComparison.OrdinalIgnoreCase)
                || string.Equals(vendorName, "固高", StringComparison.OrdinalIgnoreCase)
                || typeName.Contains(".Googol.", StringComparison.OrdinalIgnoreCase)
                || typeName.Contains("GoogolControlCard", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAcsControlCard(object? controlCard)
        {
            var typeName = controlCard?.GetType().FullName ?? string.Empty;
            var vendorName = GetStringProperty(controlCard, nameof(ReeYin_V.Hardware.ControlCard.ControlCardBase.VenderName));

            return string.Equals(vendorName, "ACS", StringComparison.OrdinalIgnoreCase)
                || typeName.Contains(".ACS.", StringComparison.OrdinalIgnoreCase)
                || typeName.Contains("AcsControlCard", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetStringProperty(object? source, string propertyName)
        {
            return source?.GetType().GetProperty(propertyName)?.GetValue(source) as string ?? string.Empty;
        }

        private SensorDataCollectionModel ResolveCalibModel()
        {
            var cachedModel = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches?
                .Values
                .OfType<SensorDataCollectionModel>()
                .FirstOrDefault();

            cachedModel ??= new SensorDataCollectionModel();
            cachedModel.CalibParam ??= new FlatCalib_MeasureParam();
            cachedModel.RawPointCloudPlyPath ??= string.Empty;
            cachedModel.ResidualPointCloudPlyPath ??= string.Empty;

            return cachedModel;
        }

        private void AttachModelListeners()
        {
            if (ModelParam == null)
            {
                return;
            }

            ModelParam.PropertyChanged -= OnModelParamPropertyChanged;
            ModelParam.PropertyChanged += OnModelParamPropertyChanged;
            AttachTrajectoryListeners(ModelParam.AllLocusInfo);
            AttachCreateTrajectoryModelListeners(ModelParam.CreateTrajectoryModel);
        }

        private void OnModelParamPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SensorMotionControlModel.AllLocusInfo))
            {
                AttachTrajectoryListeners(ModelParam?.AllLocusInfo);
                if (SelectedLocus != null && !(ModelParam?.AllLocusInfo?.Contains(SelectedLocus) ?? false))
                {
                    SelectedLocus = ModelParam?.AllLocusInfo?.FirstOrDefault();
                }

                RefreshTrajectoryShapes();
                return;
            }

            if (e.PropertyName == nameof(SensorMotionControlModel.CreateTrajectoryModel))
            {
                AttachCreateTrajectoryModelListeners(ModelParam?.CreateTrajectoryModel);
                SelectedCustomRing = ModelParam?.CreateTrajectoryModel?.CustomRingDefinitions?.FirstOrDefault();
                RefreshTrajectoryShapes();
            }
        }

        private void AttachTrajectoryListeners(ObservableCollection<LocusInfo>? locusInfos)
        {
            if (!ReferenceEquals(_observedLocusInfos, locusInfos) && _observedLocusInfos != null)
            {
                _observedLocusInfos.CollectionChanged -= OnLocusCollectionChanged;
            }

            _observedLocusInfos = locusInfos;
            if (_observedLocusInfos != null)
            {
                _observedLocusInfos.CollectionChanged -= OnLocusCollectionChanged;
                _observedLocusInfos.CollectionChanged += OnLocusCollectionChanged;
            }

            RefreshLocusSubscriptions();
        }

        private void AttachCreateTrajectoryModelListeners(CreateTrajectoryModel? createTrajectoryModel)
        {
            if (!ReferenceEquals(_observedCreateTrajectoryModel, createTrajectoryModel) && _observedCreateTrajectoryModel != null)
            {
                _observedCreateTrajectoryModel.PropertyChanged -= OnCreateTrajectoryModelPropertyChanged;
            }

            _observedCreateTrajectoryModel = createTrajectoryModel;
            if (_observedCreateTrajectoryModel != null)
            {
                _observedCreateTrajectoryModel.PropertyChanged -= OnCreateTrajectoryModelPropertyChanged;
                _observedCreateTrajectoryModel.PropertyChanged += OnCreateTrajectoryModelPropertyChanged;
            }

            AttachCustomRingListeners(_observedCreateTrajectoryModel?.CustomRingDefinitions);
        }

        private void AttachCustomRingListeners(ObservableCollection<CustomRingDefinition>? customRings)
        {
            if (!ReferenceEquals(_observedCustomRings, customRings) && _observedCustomRings != null)
            {
                _observedCustomRings.CollectionChanged -= OnCustomRingCollectionChanged;
            }

            _observedCustomRings = customRings;
            if (_observedCustomRings != null)
            {
                _observedCustomRings.CollectionChanged -= OnCustomRingCollectionChanged;
                _observedCustomRings.CollectionChanged += OnCustomRingCollectionChanged;
            }
        }

        private void OnLocusCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshLocusSubscriptions();

            if (SelectedLocus != null && !(_observedLocusInfos?.Contains(SelectedLocus) ?? false))
            {
                SelectedLocus = _observedLocusInfos?.FirstOrDefault();
            }

            RefreshTrajectoryShapes();
        }

        private void OnCreateTrajectoryModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CreateTrajectoryModel.CustomRingDefinitions))
            {
                AttachCustomRingListeners(_observedCreateTrajectoryModel?.CustomRingDefinitions);
                if (SelectedCustomRing != null && !(_observedCreateTrajectoryModel?.CustomRingDefinitions?.Contains(SelectedCustomRing) ?? false))
                {
                    SelectedCustomRing = _observedCreateTrajectoryModel?.CustomRingDefinitions?.FirstOrDefault();
                    return;
                }
            }

            RefreshTrajectoryShapes();
        }

        private void OnCustomRingCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedCustomRing != null && !(_observedCustomRings?.Contains(SelectedCustomRing) ?? false))
            {
                SelectedCustomRing = _observedCustomRings?.FirstOrDefault();
                return;
            }

            RefreshTrajectoryShapes();
        }

        private void RefreshLocusSubscriptions()
        {
            foreach (var locusInfo in _subscribedLocusInfos)
            {
                locusInfo.PropertyChanged -= OnLocusInfoPropertyChanged;
            }

            _subscribedLocusInfos.Clear();
            if (_observedLocusInfos == null)
            {
                return;
            }

            foreach (var locusInfo in _observedLocusInfos)
            {
                locusInfo.PropertyChanged -= OnLocusInfoPropertyChanged;
                locusInfo.PropertyChanged += OnLocusInfoPropertyChanged;
                _subscribedLocusInfos.Add(locusInfo);
            }
        }

        private void OnLocusInfoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not LocusInfo locusInfo)
            {
                return;
            }

            SyncPointLocusCoordinatesIfNeeded(locusInfo, e.PropertyName);
            RefreshTrajectoryShapes();
        }

        private void SyncPointLocusCoordinatesIfNeeded(LocusInfo locusInfo, string? propertyName)
        {
            if (_syncingPointLocusInfos.Contains(locusInfo))
            {
                return;
            }

            if (!string.Equals(locusInfo.Type, LocusInfo.PointType, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                _syncingPointLocusInfos.Add(locusInfo);
                switch (propertyName)
                {
                    case nameof(LocusInfo.Type):
                    case nameof(LocusInfo.OriginX):
                        if (!AreClose(locusInfo.TargetX, locusInfo.OriginX))
                        {
                            locusInfo.TargetX = locusInfo.OriginX;
                        }
                        break;

                    case nameof(LocusInfo.TargetX):
                        if (!AreClose(locusInfo.OriginX, locusInfo.TargetX))
                        {
                            locusInfo.OriginX = locusInfo.TargetX;
                        }
                        break;

                    case nameof(LocusInfo.OriginY):
                        if (!AreClose(locusInfo.TargetY, locusInfo.OriginY))
                        {
                            locusInfo.TargetY = locusInfo.OriginY;
                        }
                        break;

                    case nameof(LocusInfo.TargetY):
                        if (!AreClose(locusInfo.OriginY, locusInfo.TargetY))
                        {
                            locusInfo.OriginY = locusInfo.TargetY;
                        }
                        break;

                    default:
                        break;
                }
            }
            finally
            {
                _syncingPointLocusInfos.Remove(locusInfo);
            }
        }

        private ObservableCollection<LocusInfo> EnsureLocusCollection()
        {
            if (ModelParam.AllLocusInfo == null)
            {
                ModelParam.AllLocusInfo = new ObservableCollection<LocusInfo>();
            }

            return ModelParam.AllLocusInfo;
        }

        private ObservableCollection<CalibrationWaferPosition> EnsureCalibrationWaferPositionCollection()
        {
            if (ModelParam.CalibrationWaferPositions == null)
            {
                ModelParam.CalibrationWaferPositions = new ObservableCollection<CalibrationWaferPosition>();
            }

            return ModelParam.CalibrationWaferPositions;
        }

        private CalibrationWaferPosition CreateNewCalibrationWaferPosition(int index)
        {
            return new CalibrationWaferPosition
            {
                Name = $"标定片{index}",
                X = ModelParam?.CreateTrajectoryModel?.CircleCenterX ?? 0,
                Y = ModelParam?.CreateTrajectoryModel?.CircleCenterY ?? 0
            };
        }

        private LocusInfo CreateNewLocus()
        {
            if (SelectedLocus != null)
            {
                return new LocusInfo
                {
                    Type = SelectedLocus.Type,
                    OriginX = SelectedLocus.OriginX,
                    OriginY = SelectedLocus.OriginY,
                    TargetX = SelectedLocus.TargetX,
                    TargetY = SelectedLocus.TargetY
                };
            }

            bool isPoint = ModelParam?.CreateTrajectoryModel?.IsPointGenerationMode == true;
            double baseX = ModelParam?.CreateTrajectoryModel?.CircleCenterX ?? 0;
            double baseY = ModelParam?.CreateTrajectoryModel?.CircleCenterY ?? 0;
            double defaultLength = Math.Max(ModelParam?.SensorInterval ?? 0, 1);

            if (isPoint)
            {
                return new LocusInfo
                {
                    Type = LocusInfo.PointType,
                    OriginX = baseX,
                    OriginY = baseY,
                    TargetX = baseX,
                    TargetY = baseY
                };
            }

            return new LocusInfo
            {
                Type = LocusInfo.LineType,
                OriginX = baseX - defaultLength / 2,
                OriginY = baseY,
                TargetX = baseX + defaultLength / 2,
                TargetY = baseY
            };
        }

        private static bool AreClose(double left, double right)
        {
            return Math.Abs(left - right) < 1e-9;
        }

        private void RefreshTrajectoryShapes()
        {
            TrajectoryShapes.Clear();

            var locusInfos = ModelParam?.AllLocusInfo;
            var model = ModelParam?.CreateTrajectoryModel;
            if (model == null)
                return;

            bool isRectangleHorizontalLineMode = model.CircleGenerateMode == CircleTrajectoryGenerateMode.等间距水平线;

            // 计算坐标范围
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            bool hasBounds = false;

            if (locusInfos != null)
            {
                foreach (var locus in locusInfos)
                {
                    minX = Math.Min(minX, Math.Min(locus.OriginX, locus.TargetX));
                    maxX = Math.Max(maxX, Math.Max(locus.OriginX, locus.TargetX));
                    minY = Math.Min(minY, Math.Min(locus.OriginY, locus.TargetY));
                    maxY = Math.Max(maxY, Math.Max(locus.OriginY, locus.TargetY));
                    hasBounds = true;
                }
            }

            if (isRectangleHorizontalLineMode &&
                double.IsFinite(model.RectangleStartX) &&
                double.IsFinite(model.RectangleStartY) &&
                double.IsFinite(model.RectangleEndX) &&
                double.IsFinite(model.RectangleEndY))
            {
                minX = Math.Min(minX, Math.Min(model.RectangleStartX, model.RectangleEndX));
                maxX = Math.Max(maxX, Math.Max(model.RectangleStartX, model.RectangleEndX));
                minY = Math.Min(minY, Math.Min(model.RectangleStartY, model.RectangleEndY));
                maxY = Math.Max(maxY, Math.Max(model.RectangleStartY, model.RectangleEndY));
                hasBounds = true;
            }
            else if (double.IsFinite(model.CircleCenterX) &&
                double.IsFinite(model.CircleCenterY) &&
                double.IsFinite(model.CircleRadius) &&
                model.CircleRadius > 0)
            {
                minX = Math.Min(minX, model.CircleCenterX - model.CircleRadius);
                maxX = Math.Max(maxX, model.CircleCenterX + model.CircleRadius);
                minY = Math.Min(minY, model.CircleCenterY - model.CircleRadius);
                maxY = Math.Max(maxY, model.CircleCenterY + model.CircleRadius);
                hasBounds = true;
            }

            if (!hasBounds)
            {
                return;
            }

            double rangeX = maxX - minX;
            double rangeY = maxY - minY;

            // 添加边距。轨迹预览以轨迹包围盒左下角作为固定起点，不再居中平移。
            double padding = 20;
            double canvasWidth = 380 - 2 * padding;
            double canvasHeight = 380 - 2 * padding;

            // 计算缩放因子
            double scaleX = rangeX > 0 ? canvasWidth / rangeX : canvasWidth;
            double scaleY = rangeY > 0 ? canvasHeight / rangeY : canvasHeight;
            double scale = Math.Min(scaleX, scaleY) * 0.96;
            if (!double.IsFinite(scale) || scale <= 0)
            {
                scale = 1;
            }

            double originX = minX;
            double originY = minY;

            double MapX(double x) => padding + ((x - originX) * scale);
            double MapY(double y) => padding + canvasHeight - ((y - originY) * scale);

            if (isRectangleHorizontalLineMode &&
                double.IsFinite(model.RectangleStartX) &&
                double.IsFinite(model.RectangleStartY) &&
                double.IsFinite(model.RectangleEndX) &&
                double.IsFinite(model.RectangleEndY))
            {
                double rectangleMinX = Math.Min(model.RectangleStartX, model.RectangleEndX);
                double rectangleMaxX = Math.Max(model.RectangleStartX, model.RectangleEndX);
                double rectangleMinY = Math.Min(model.RectangleStartY, model.RectangleEndY);
                double rectangleMaxY = Math.Max(model.RectangleStartY, model.RectangleEndY);
                var boundary = new Rectangle
                {
                    Width = Math.Max((rectangleMaxX - rectangleMinX) * scale, 0),
                    Height = Math.Max((rectangleMaxY - rectangleMinY) * scale, 0),
                    Stroke = new SolidColorBrush(Color.FromRgb(0xB8, 0xC2, 0xCC)),
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection { 5, 3 },
                    Fill = Brushes.Transparent,
                    Margin = new Thickness(
                        MapX(rectangleMinX),
                        MapY(rectangleMaxY),
                        0,
                        0)
                };
                TrajectoryShapes.Add(boundary);
            }
            else if (double.IsFinite(model.CircleRadius) && model.CircleRadius > 0)
            {
                double diameter = model.CircleRadius * 2 * scale;
                var boundary = new Ellipse
                {
                    Width = diameter,
                    Height = diameter,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xB8, 0xC2, 0xCC)),
                    StrokeThickness = 1.5,
                    Fill = Brushes.Transparent,
                    Margin = new Thickness(
                        MapX(model.CircleCenterX - model.CircleRadius),
                        MapY(model.CircleCenterY + model.CircleRadius),
                        0,
                        0)
                };
                TrajectoryShapes.Add(boundary);
            }

            if (model.CircleGenerateMode == CircleTrajectoryGenerateMode.圆环点 &&
                double.IsFinite(model.InscribedCircleRadius) &&
                model.InscribedCircleRadius > 0 &&
                model.InscribedCircleRadius <= model.CircleRadius)
            {
                double innerDiameter = model.InscribedCircleRadius * 2 * scale;
                var innerBoundary = new Ellipse
                {
                    Width = innerDiameter,
                    Height = innerDiameter,
                    Stroke = new SolidColorBrush(Color.FromRgb(0x90, 0xA4, 0xAE)),
                    StrokeThickness = 1.2,
                    StrokeDashArray = new DoubleCollection { 4, 3 },
                    Fill = Brushes.Transparent,
                    Margin = new Thickness(
                        MapX(model.CircleCenterX - model.InscribedCircleRadius),
                        MapY(model.CircleCenterY + model.InscribedCircleRadius),
                        0,
                        0)
                };
                TrajectoryShapes.Add(innerBoundary);
            }

            if (model.CircleGenerateMode == CircleTrajectoryGenerateMode.自定义圆环 &&
                double.IsFinite(model.CircleCenterX) &&
                double.IsFinite(model.CircleCenterY))
            {
                foreach (var customRing in model.CustomRingDefinitions)
                {
                    if (customRing == null ||
                        !double.IsFinite(customRing.Radius) ||
                        customRing.Radius <= 0 ||
                        customRing.Radius > model.CircleRadius)
                    {
                        continue;
                    }

                    bool isSelectedRing = ReferenceEquals(customRing, SelectedCustomRing);
                    double ringDiameter = customRing.Radius * 2 * scale;
                    var ringBoundary = new Ellipse
                    {
                        Width = ringDiameter,
                        Height = ringDiameter,
                        Stroke = isSelectedRing
                            ? new SolidColorBrush(Color.FromRgb(0xF4, 0x51, 0x1E))
                            : new SolidColorBrush(Color.FromRgb(0x60, 0x7D, 0x8B)),
                        StrokeThickness = isSelectedRing ? 2.2 : 1.2,
                        StrokeDashArray = new DoubleCollection { 5, 3 },
                        Fill = Brushes.Transparent,
                        Margin = new Thickness(
                            MapX(model.CircleCenterX - customRing.Radius),
                            MapY(model.CircleCenterY + customRing.Radius),
                            0,
                            0)
                    };
                    TrajectoryShapes.Add(ringBoundary);
                }
            }

            // 绘制轨迹
            var colors = new[] { Colors.Blue, Colors.Red, Colors.Green, Colors.Orange, Colors.Purple };
            var pointBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x88, 0xE5));
            int colorIndex = 0;

            if (locusInfos != null)
            {
                foreach (var locus in locusInfos)
                {
                    double x1 = MapX(locus.OriginX);
                    double y1 = MapY(locus.OriginY);
                    bool isPoint = string.Equals(locus.Type, LocusInfo.PointType, StringComparison.OrdinalIgnoreCase);
                    bool isSelected = ReferenceEquals(locus, SelectedLocus);

                    if (isPoint)
                    {
                        var pointEllipse = new Ellipse
                        {
                            Width = isSelected ? 11 : 7,
                            Height = isSelected ? 11 : 7,
                            Fill = isSelected ? Brushes.OrangeRed : pointBrush,
                            Stroke = Brushes.White,
                            StrokeThickness = isSelected ? 1.4 : 0.8,
                            Margin = new Thickness(
                                x1 - (isSelected ? 5.5 : 3.5),
                                y1 - (isSelected ? 5.5 : 3.5),
                                0,
                                0)
                        };
                        TrajectoryShapes.Add(pointEllipse);
                    }
                    else
                    {
                        double x2 = MapX(locus.TargetX);
                        double y2 = MapY(locus.TargetY);
                        var brush = new SolidColorBrush(isSelected ? Colors.OrangeRed : colors[colorIndex % colors.Length]);

                        var line = new Line
                        {
                            X1 = x1,
                            Y1 = y1,
                            X2 = x2,
                            Y2 = y2,
                            Stroke = brush,
                            StrokeThickness = isSelected ? 3.5 : 2
                        };
                        TrajectoryShapes.Add(line);

                        var startEllipse = new Ellipse
                        {
                            Width = isSelected ? 9 : 6,
                            Height = isSelected ? 9 : 6,
                            Fill = brush,
                            Margin = new Thickness(
                                x1 - (isSelected ? 4.5 : 3),
                                y1 - (isSelected ? 4.5 : 3),
                                0,
                                0)
                        };
                        TrajectoryShapes.Add(startEllipse);
                    }

                    colorIndex++;
                }
            }

            if (isRectangleHorizontalLineMode ||
                !double.IsFinite(model.CircleCenterX) ||
                !double.IsFinite(model.CircleCenterY))
            {
                return;
            }

            // 绘制圆心
            double centerX = MapX(model.CircleCenterX);
            double centerY = MapY(model.CircleCenterY);

            var centerEllipse = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Colors.Black),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1,
                Margin = new Thickness(centerX - 4, centerY - 4, 0, 0)
            };
            TrajectoryShapes.Add(centerEllipse);
        }
        #endregion

        public sealed class LocusTypeOption
        {
            public LocusTypeOption(string value, string displayName)
            {
                Value = value;
                DisplayName = displayName;
            }

            public string Value { get; }

            public string DisplayName { get; }
        }
    }
}
