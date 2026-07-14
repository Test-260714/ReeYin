#nullable enable

using Prism.Commands;
using Prism.Mvvm;
using ReeYin_V.UI.UserControls.DefectMap;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace ReeYin.ChartShow.ViewModels
{
    public sealed class DefectMapDemoViewModel : BindableBase, IDisposable
    {
        private readonly DispatcherTimer _simulationTimer;
        private readonly Random _random = new();
        private int _defectIndex;
        private double _materialWidth = 1200d;
        private double _materialLength = 8000d;
        private double _simulationStepLength = 120d;
        private double _currentLengthPosition;
        private bool _isSimulationRunning;
        private DefectMapLengthOrigin _lengthOrigin = DefectMapLengthOrigin.Top;
        private DefectMapItem? _selectedDefect;
        private string _selectedDefectText = "未选中缺陷。";
        private string _simulationStatus = "未开始走带。";

        public DefectMapDemoViewModel()
        {
            _simulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(280d)
            };
            _simulationTimer.Tick += OnSimulationTick;

            StartSimulationCommand = new DelegateCommand(StartSimulation, () => !IsSimulationRunning);
            StopSimulationCommand = new DelegateCommand(StopSimulation, () => IsSimulationRunning);
            ResetSimulationCommand = new DelegateCommand(ResetSimulation);
            GenerateDefectCommand = new DelegateCommand(() =>
            {
                AddRandomDefects(forceCreate: true);
                UpdateSummaryProperties();
            });
            DefectSelectedCommand = new DelegateCommand<DefectMapItem?>(OnDefectSelected);

            UpdateSummaryProperties();
        }

        public ObservableCollection<DefectMapItem> Defects { get; } = new();

        public ObservableCollection<DefectMapTypeStyle> DefectTypeStyles { get; } = new(
            DefectMapTypeStyle.CreateDefaultStyles().Select(style => style.Clone()));

        public DefectMapLengthOrigin[] LengthOriginOptions { get; } =
        {
            DefectMapLengthOrigin.Top,
            DefectMapLengthOrigin.Bottom
        };

        public double MaterialWidth
        {
            get => _materialWidth;
            set
            {
                double normalizedValue = NormalizePositive(value, _materialWidth);
                if (SetProperty(ref _materialWidth, normalizedValue))
                {
                    UpdateSummaryProperties();
                }
            }
        }

        public double MaterialLength
        {
            get => _materialLength;
            set
            {
                double normalizedValue = NormalizePositive(value, _materialLength);
                if (SetProperty(ref _materialLength, normalizedValue))
                {
                    CurrentLengthPosition = Math.Min(CurrentLengthPosition, _materialLength);
                    UpdateSummaryProperties();
                }
            }
        }

        public double SimulationStepLength
        {
            get => _simulationStepLength;
            set => SetProperty(ref _simulationStepLength, NormalizePositive(value, _simulationStepLength));
        }

        public DefectMapLengthOrigin LengthOrigin
        {
            get => _lengthOrigin;
            set => SetProperty(ref _lengthOrigin, value);
        }

        public double CurrentLengthPosition
        {
            get => _currentLengthPosition;
            private set
            {
                double normalizedValue = Math.Clamp(value, 0d, MaterialLength);
                if (SetProperty(ref _currentLengthPosition, normalizedValue))
                {
                    UpdateSummaryProperties();
                }
            }
        }

        public DefectMapItem? SelectedDefect
        {
            get => _selectedDefect;
            set
            {
                if (SetProperty(ref _selectedDefect, value))
                {
                    SelectedDefectText = value == null
                        ? "未选中缺陷。"
                        : $"{value.Name}，类型：{value.DefectType}，等级：{value.Severity}，宽度 {value.WidthPosition:F2}，长度 {value.LengthPosition:F2}";
                }
            }
        }

        public string SelectedDefectText
        {
            get => _selectedDefectText;
            private set => SetProperty(ref _selectedDefectText, value);
        }

        public string SimulationStatus
        {
            get => _simulationStatus;
            private set => SetProperty(ref _simulationStatus, value);
        }

        public bool IsSimulationRunning
        {
            get => _isSimulationRunning;
            private set
            {
                if (SetProperty(ref _isSimulationRunning, value))
                {
                    StartSimulationCommand.RaiseCanExecuteChanged();
                    StopSimulationCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public double ProgressPercent => MaterialLength <= 0d ? 0d : (CurrentLengthPosition / MaterialLength) * 100d;

        public string ProgressText => $"当前走带：{CurrentLengthPosition:F2} / {MaterialLength:F2}";

        public string DefectSummary
        {
            get
            {
                int minorCount = Defects.Count(item => item.Severity == DefectMapSeverity.Minor);
                int warningCount = Defects.Count(item => item.Severity == DefectMapSeverity.Warning);
                int criticalCount = Defects.Count(item => item.Severity == DefectMapSeverity.Critical);
                int enabledTypeCount = DefectTypeStyles.Count(style => style.IsEnabled);
                return $"Defects {Defects.Count}, Minor {minorCount}, Warning {warningCount}, Critical {criticalCount}, Types {enabledTypeCount}";
            }
        }

        public DelegateCommand StartSimulationCommand { get; }

        public DelegateCommand StopSimulationCommand { get; }

        public DelegateCommand ResetSimulationCommand { get; }

        public DelegateCommand GenerateDefectCommand { get; }

        public DelegateCommand<DefectMapItem?> DefectSelectedCommand { get; }

        private void StartSimulation()
        {
            if (CurrentLengthPosition >= MaterialLength)
            {
                ResetSimulation();
            }

            IsSimulationRunning = true;
            SimulationStatus = "走带中，随机生成缺陷。";
            _simulationTimer.Start();
        }

        private void StopSimulation()
        {
            _simulationTimer.Stop();
            IsSimulationRunning = false;
            SimulationStatus = CurrentLengthPosition >= MaterialLength ? "走带完成。" : "走带已停止。";
        }

        private void ResetSimulation()
        {
            _simulationTimer.Stop();
            IsSimulationRunning = false;
            Defects.Clear();
            SelectedDefect = null;
            CurrentLengthPosition = 0d;
            _defectIndex = 0;
            SimulationStatus = "已重置，等待开始走带。";
            UpdateSummaryProperties();
        }

        private void OnSimulationTick(object? sender, EventArgs e)
        {
            CurrentLengthPosition += SimulationStepLength;
            if (CurrentLengthPosition > MaterialLength)
            {
                CurrentLengthPosition = MaterialLength;
            }

            AddRandomDefects();
            UpdateSummaryProperties();

            if (CurrentLengthPosition >= MaterialLength)
            {
                StopSimulation();
            }
        }

        private void AddRandomDefects(bool forceCreate = false)
        {
            int defectCount = forceCreate ? 1 : _random.Next(0, 3);
            for (int index = 0; index < defectCount; index++)
            {
                DefectMapTypeStyle typeStyle = NextDefectTypeStyle();
                DefectMapSeverity severity = typeStyle.DefaultSeverity;
                double lengthJitter = _random.NextDouble() * Math.Min(SimulationStepLength, Math.Max(CurrentLengthPosition, 1d));
                double lengthPosition = Math.Clamp(CurrentLengthPosition - lengthJitter, 0d, MaterialLength);
                double widthPosition = _random.NextDouble() * MaterialWidth;
                _defectIndex++;

                Defects.Add(new DefectMapItem
                {
                    Id = $"SIM-{_defectIndex:D5}",
                    Name = $"{typeStyle.DisplayName} {_defectIndex:D3}",
                    DefectType = typeStyle.TypeKey,
                    Severity = severity,
                    WidthPosition = widthPosition,
                    LengthPosition = lengthPosition,
                    Description = $"Generated at web length {CurrentLengthPosition:F2}: {typeStyle.DisplayName}.",
                    DisplaySize = null
                });
            }
        }

        private DefectMapSeverity NextSeverity()
        {
            double value = _random.NextDouble();
            if (value > 0.84d)
            {
                return DefectMapSeverity.Critical;
            }

            return value > 0.55d ? DefectMapSeverity.Warning : DefectMapSeverity.Minor;
        }

        private DefectMapTypeStyle NextDefectTypeStyle()
        {
            DefectMapTypeStyle[] enabledStyles = DefectTypeStyles
                .Where(style => style.IsEnabled)
                .ToArray();

            if (enabledStyles.Length == 0)
            {
                return DefectTypeStyles.FirstOrDefault()
                    ?? DefectMapTypeStyle.CreateDefault("Unknown", "Unknown", "#22C55E", 10d, DefectMapMarkerShape.Circle, NextSeverity());
            }

            return enabledStyles[_random.Next(enabledStyles.Length)];
        }

        private void OnDefectSelected(DefectMapItem? defect)
        {
            SelectedDefect = defect;
        }

        private void UpdateSummaryProperties()
        {
            RaisePropertyChanged(nameof(ProgressPercent));
            RaisePropertyChanged(nameof(ProgressText));
            RaisePropertyChanged(nameof(DefectSummary));
        }

        private static double NormalizePositive(double value, double fallback)
        {
            return double.IsFinite(value) && value > 0d ? value : fallback;
        }

        public void Dispose()
        {
            _simulationTimer.Stop();
            IsSimulationRunning = false;
        }
    }
}
