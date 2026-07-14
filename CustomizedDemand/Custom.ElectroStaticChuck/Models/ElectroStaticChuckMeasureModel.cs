using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Custom.ElectroStaticChuckMeasure
{
    public class ElectroStaticChuckMeasureModel : ModelParamBase
    {
        #region Fields

        private string _calibrationFrameDirectory = string.Empty;
        private string _measurementFrameDirectory = string.Empty;
        private string _outputDirectory = string.Empty;
        private double _sensorIntervalX = 2.9;
        private double _sensorIntervalY = 5.0;
        private double _sensorIntervalZ = 1.0;
        private double _sensorMinDepth = -5000;
        private double _sensorMaxDepth = 5000;
        private double _sensorInvalidValue = 888888;
        private bool _sensorIsFlip;
        private bool _usePitchCorrection;
        private bool _useYawCorrection;
        private double _coarseSearchWindowX = 50.0;
        private double _coarseSearchWindowY = 500.0;
        private double _coarseSearchSpeedFactor = 1.0;
        private double _coarseHeightWeight = 0.05;
        private double _coarseGrayWeight = 0.05;
        private double _coarseMaskWeight;
        private FineRegistrationMode _fineRegistrationMode = FineRegistrationMode.CoarseAndFine;
        private bool _noRegistrationUseZCorrection;
        private int _noRegistrationZSampleStepPixel = 4;
        private int _noRegistrationMinZSampleCount = 200;
        private double _icpLeafSizeMultiplier = 1.0;
        private int _icpMaxIterations = 1;
        private double _icpMaxCorrespondenceDistanceMultiplier = 3.0;
        private double _icpMinMaxCorrespondenceDistance = 1.0;
        private double _icpTransformationEpsilon = 0.000001;
        private double _icpFitnessEpsilon = 0.000001;
        private double _icpMinInlierRatio = 0.05;
        private bool _optimizeX = true;
        private bool _optimizeY = true;
        private bool _optimizeZ = true;
        private bool _optimizeYawDeg = true;
        private double _convexStandardDiameter = 820;
        private double _convexStandardHeight = 30;
        private int _streamingTileCoreWidthPixel = 2048;
        private int _streamingTileCoreHeightPixel = 2048;
        private int _streamingTileHaloPixel;
        private bool _drawLabels = true;
        private bool _drawOverlay = true;
        private int _imageDownsampleFactor = 1;
        private int _stitchingPointCloudStride = 1;
        private bool _enableStitching = true;
        private ElectroStaticChuckMeasurementMode _measurementMode = ElectroStaticChuckMeasurementMode.StreamingTiles;
        private bool _savePreviewImages = true;
        private bool _savePointCloud = true;
        private bool _renderOverlay = true;
        private bool _exportResults = true;
        private bool _calibrationSuccess;
        private string _calibrationMessage = "Not calibrated.";
        private bool _measurementSuccess;
        private string _measurementMessage = "Not measured.";
        private double _convexsFlatness = -1.0;
        private double _overallFlatness = -1.0;
        private string _exportOutputPathsText = string.Empty;
        private string _lastErrorStage = string.Empty;

        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; } = null!;

        #endregion

        #region Properties

        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new();

        [JsonIgnore]
        public ElectroStaticChuckRunResult? LastRunResult { get; private set; }

        [JsonIgnore]
        public CalibrationState? CurrentCalibrationState { get; internal set; }

        [RecipeParam("标定帧目录", "ElectroStaticChuckCalibrationPipeline 使用的原始 frame directory")]
        public string CalibrationFrameDirectory
        {
            get => _calibrationFrameDirectory;
            set => SetProperty(ref _calibrationFrameDirectory, value ?? string.Empty);
        }

        [RecipeParam("测量帧目录", "ElectroStaticChuckMeasurementPipeline 使用的原始 frame directory")]
        public string MeasurementFrameDirectory
        {
            get => _measurementFrameDirectory;
            set => SetProperty(ref _measurementFrameDirectory, value ?? string.Empty);
        }

        [RecipeParam("输出目录", "导出预览图、点云和测量结果的目录")]
        public string OutputDirectory
        {
            get => _outputDirectory;
            set => SetProperty(ref _outputDirectory, value ?? string.Empty);
        }

        [RecipeParam("Sensor IntervalX", "ALGO SensorParameters.IntervalX")]
        public double SensorIntervalX { get => _sensorIntervalX; set => SetProperty(ref _sensorIntervalX, value); }

        [RecipeParam("Sensor IntervalY", "ALGO SensorParameters.IntervalY")]
        public double SensorIntervalY { get => _sensorIntervalY; set => SetProperty(ref _sensorIntervalY, value); }

        [RecipeParam("Sensor IntervalZ", "ALGO SensorParameters.IntervalZ")]
        public double SensorIntervalZ { get => _sensorIntervalZ; set => SetProperty(ref _sensorIntervalZ, value); }

        [RecipeParam("Sensor MinDepth", "ALGO SensorParameters.MinDepth")]
        public double SensorMinDepth { get => _sensorMinDepth; set => SetProperty(ref _sensorMinDepth, value); }

        [RecipeParam("Sensor MaxDepth", "ALGO SensorParameters.MaxDepth")]
        public double SensorMaxDepth { get => _sensorMaxDepth; set => SetProperty(ref _sensorMaxDepth, value); }

        [RecipeParam("Sensor InvalidValue", "ALGO SensorParameters.InvalidValue")]
        public double SensorInvalidValue { get => _sensorInvalidValue; set => SetProperty(ref _sensorInvalidValue, value); }

        [RecipeParam("Sensor IsFlip", "ALGO SensorParameters.IsFlip")]
        public bool SensorIsFlip { get => _sensorIsFlip; set => SetProperty(ref _sensorIsFlip, value); }

        [RecipeParam("UsePitchCorrection", "测量启用 pitch correction 时必须先执行 RunCalibration")]
        public bool UsePitchCorrection { get => _usePitchCorrection; set => SetProperty(ref _usePitchCorrection, value); }

        [RecipeParam("UseYawCorrection", "当前 ALGO measurement path 尚不支持 yaw correction")]
        public bool UseYawCorrection { get => _useYawCorrection; set => SetProperty(ref _useYawCorrection, value); }

        [RecipeParam("Coarse SearchWindowX", "ALGO CoarseRegistrationParameters.SearchWindowX")]
        public double CoarseSearchWindowX { get => _coarseSearchWindowX; set => SetProperty(ref _coarseSearchWindowX, value); }

        [RecipeParam("Coarse SearchWindowY", "ALGO CoarseRegistrationParameters.SearchWindowY")]
        public double CoarseSearchWindowY { get => _coarseSearchWindowY; set => SetProperty(ref _coarseSearchWindowY, value); }

        [RecipeParam("Coarse SearchSpeedFactor", "ALGO CoarseRegistrationParameters.SearchSpeedFactor")]
        public double CoarseSearchSpeedFactor { get => _coarseSearchSpeedFactor; set => SetProperty(ref _coarseSearchSpeedFactor, value); }

        [RecipeParam("Coarse HeightWeight", "ALGO CoarseRegistrationParameters.HeightWeight")]
        public double CoarseHeightWeight { get => _coarseHeightWeight; set => SetProperty(ref _coarseHeightWeight, value); }

        [RecipeParam("Coarse GrayWeight", "ALGO CoarseRegistrationParameters.GrayWeight")]
        public double CoarseGrayWeight { get => _coarseGrayWeight; set => SetProperty(ref _coarseGrayWeight, value); }

        [RecipeParam("Coarse MaskWeight", "ALGO CoarseRegistrationParameters.MaskWeight")]
        public double CoarseMaskWeight { get => _coarseMaskWeight; set => SetProperty(ref _coarseMaskWeight, value); }

        [RecipeParam("Fine Mode", "ALGO FineRegistrationParameters.Mode")]
        public FineRegistrationMode FineRegistrationMode { get => _fineRegistrationMode; set => SetProperty(ref _fineRegistrationMode, value); }

        [RecipeParam("NoRegistration UseZCorrection", "ALGO FineRegistrationParameters.NoRegistrationUseZCorrection")]
        public bool NoRegistrationUseZCorrection { get => _noRegistrationUseZCorrection; set => SetProperty(ref _noRegistrationUseZCorrection, value); }

        [RecipeParam("NoRegistration ZSampleStepPixel", "ALGO FineRegistrationParameters.NoRegistrationZSampleStepPixel")]
        public int NoRegistrationZSampleStepPixel { get => _noRegistrationZSampleStepPixel; set => SetProperty(ref _noRegistrationZSampleStepPixel, value); }

        [RecipeParam("NoRegistration MinZSampleCount", "ALGO FineRegistrationParameters.NoRegistrationMinZSampleCount")]
        public int NoRegistrationMinZSampleCount { get => _noRegistrationMinZSampleCount; set => SetProperty(ref _noRegistrationMinZSampleCount, value); }

        [RecipeParam("ICP LeafSizeMultiplier", "ALGO FineRegistrationParameters.IcpLeafSizeMultiplier")]
        public double IcpLeafSizeMultiplier { get => _icpLeafSizeMultiplier; set => SetProperty(ref _icpLeafSizeMultiplier, value); }

        [RecipeParam("ICP MaxIterations", "ALGO FineRegistrationParameters.MaxIterations")]
        public int IcpMaxIterations { get => _icpMaxIterations; set => SetProperty(ref _icpMaxIterations, value); }

        [RecipeParam("ICP MaxCorrespondenceDistanceMultiplier", "ALGO FineRegistrationParameters.MaxCorrespondenceDistanceMultiplier")]
        public double IcpMaxCorrespondenceDistanceMultiplier { get => _icpMaxCorrespondenceDistanceMultiplier; set => SetProperty(ref _icpMaxCorrespondenceDistanceMultiplier, value); }

        [RecipeParam("ICP MinMaxCorrespondenceDistance", "ALGO FineRegistrationParameters.MinMaxCorrespondenceDistance")]
        public double IcpMinMaxCorrespondenceDistance { get => _icpMinMaxCorrespondenceDistance; set => SetProperty(ref _icpMinMaxCorrespondenceDistance, value); }

        [RecipeParam("ICP TransformationEpsilon", "ALGO FineRegistrationParameters.TransformationEpsilon")]
        public double IcpTransformationEpsilon { get => _icpTransformationEpsilon; set => SetProperty(ref _icpTransformationEpsilon, value); }

        [RecipeParam("ICP FitnessEpsilon", "ALGO FineRegistrationParameters.FitnessEpsilon")]
        public double IcpFitnessEpsilon { get => _icpFitnessEpsilon; set => SetProperty(ref _icpFitnessEpsilon, value); }

        [RecipeParam("ICP MinInlierRatio", "ALGO FineRegistrationParameters.MinInlierRatio")]
        public double IcpMinInlierRatio { get => _icpMinInlierRatio; set => SetProperty(ref _icpMinInlierRatio, value); }

        [RecipeParam("OptimizeX", "ALGO FineRegistrationParameters.OptimizeX")]
        public bool OptimizeX { get => _optimizeX; set => SetProperty(ref _optimizeX, value); }

        [RecipeParam("OptimizeY", "ALGO FineRegistrationParameters.OptimizeY")]
        public bool OptimizeY { get => _optimizeY; set => SetProperty(ref _optimizeY, value); }

        [RecipeParam("OptimizeZ", "ALGO FineRegistrationParameters.OptimizeZ")]
        public bool OptimizeZ { get => _optimizeZ; set => SetProperty(ref _optimizeZ, value); }

        [RecipeParam("OptimizeYawDeg", "ALGO FineRegistrationParameters.OptimizeYawDeg")]
        public bool OptimizeYawDeg { get => _optimizeYawDeg; set => SetProperty(ref _optimizeYawDeg, value); }

        [RecipeParam("ConvexStandardDiameter", "ALGO MeasurementParameters.ConvexStandardDiameter")]
        public double ConvexStandardDiameter { get => _convexStandardDiameter; set => SetProperty(ref _convexStandardDiameter, value); }

        [RecipeParam("ConvexStandardHeight", "ALGO MeasurementParameters.ConvexStandardHeight")]
        public double ConvexStandardHeight { get => _convexStandardHeight; set => SetProperty(ref _convexStandardHeight, value); }

        [RecipeParam("StreamingTileCoreWidthPixel", "ALGO MeasurementParameters.StreamingTileCoreWidthPixel")]
        public int StreamingTileCoreWidthPixel { get => _streamingTileCoreWidthPixel; set => SetProperty(ref _streamingTileCoreWidthPixel, value); }

        [RecipeParam("StreamingTileCoreHeightPixel", "ALGO MeasurementParameters.StreamingTileCoreHeightPixel")]
        public int StreamingTileCoreHeightPixel { get => _streamingTileCoreHeightPixel; set => SetProperty(ref _streamingTileCoreHeightPixel, value); }

        [RecipeParam("StreamingTileHaloPixel", "ALGO MeasurementParameters.StreamingTileHaloPixel")]
        public int StreamingTileHaloPixel { get => _streamingTileHaloPixel; set => SetProperty(ref _streamingTileHaloPixel, value); }

        [RecipeParam("DrawLabels", "ALGO RenderingParameters.DrawLabels")]
        public bool DrawLabels { get => _drawLabels; set => SetProperty(ref _drawLabels, value); }

        [RecipeParam("DrawOverlay", "ALGO RenderingParameters.DrawOverlay")]
        public bool DrawOverlay { get => _drawOverlay; set => SetProperty(ref _drawOverlay, value); }

        [RecipeParam("ImageDownsampleFactor", "ALGO ExportParameters.ImageDownsampleFactor")]
        public int ImageDownsampleFactor { get => _imageDownsampleFactor; set => SetProperty(ref _imageDownsampleFactor, value); }

        [RecipeParam("StitchingPointCloudStride", "ALGO ExportParameters.StitchingPointCloudStride")]
        public int StitchingPointCloudStride { get => _stitchingPointCloudStride; set => SetProperty(ref _stitchingPointCloudStride, value); }

        [RecipeParam("EnableStitching", "ALGO ElectroStaticChuckRunOptions.EnableStitching")]
        public bool EnableStitching { get => _enableStitching; set => SetProperty(ref _enableStitching, value); }

        [RecipeParam("MeasurementMode", "ALGO ElectroStaticChuckRunOptions.MeasurementMode")]
        public ElectroStaticChuckMeasurementMode MeasurementMode { get => _measurementMode; set => SetProperty(ref _measurementMode, value); }

        [RecipeParam("SavePreviewImages", "ALGO ElectroStaticChuckRunOptions.SavePreviewImages")]
        public bool SavePreviewImages { get => _savePreviewImages; set => SetProperty(ref _savePreviewImages, value); }

        [RecipeParam("SavePointCloud", "ALGO ElectroStaticChuckRunOptions.SavePointCloud")]
        public bool SavePointCloud { get => _savePointCloud; set => SetProperty(ref _savePointCloud, value); }

        [RecipeParam("RenderOverlay", "ALGO ElectroStaticChuckRunOptions.RenderOverlay")]
        public bool RenderOverlay { get => _renderOverlay; set => SetProperty(ref _renderOverlay, value); }

        [RecipeParam("ExportResults", "ALGO ElectroStaticChuckRunOptions.ExportResults")]
        public bool ExportResults { get => _exportResults; set => SetProperty(ref _exportResults, value); }

        [OutputParam("CalibrationSuccess", "标定流程是否成功")]
        public bool CalibrationSuccess { get => _calibrationSuccess; private set => SetProperty(ref _calibrationSuccess, value); }

        [OutputParam("CalibrationMessage", "标定流程消息")]
        public string CalibrationMessage { get => _calibrationMessage; private set => SetProperty(ref _calibrationMessage, value ?? string.Empty); }

        [OutputParam("MeasurementSuccess", "测量流程是否成功")]
        public bool MeasurementSuccess { get => _measurementSuccess; private set => SetProperty(ref _measurementSuccess, value); }

        [OutputParam("MeasurementMessage", "测量流程消息")]
        public string MeasurementMessage { get => _measurementMessage; private set => SetProperty(ref _measurementMessage, value ?? string.Empty); }

        [OutputParam("ConvexsFlatness", "凸点区域平面度")]
        public double ConvexsFlatness { get => _convexsFlatness; private set => SetProperty(ref _convexsFlatness, value); }

        [OutputParam("OverallFlatness", "整体平面度")]
        public double OverallFlatness { get => _overallFlatness; private set => SetProperty(ref _overallFlatness, value); }

        [OutputParam("ExportOutputPathsText", "导出文件路径，使用换行分隔")]
        public string ExportOutputPathsText { get => _exportOutputPathsText; private set => SetProperty(ref _exportOutputPathsText, value ?? string.Empty); }

        [OutputParam("LastErrorStage", "最后一次 ALGO 错误阶段")]
        public string LastErrorStage { get => _lastErrorStage; private set => SetProperty(ref _lastErrorStage, value ?? string.Empty); }

        #endregion

        #region Constructor

        public ElectroStaticChuckMeasureModel()
        {
        }

        #endregion

        #region Methods

        public override bool OnceInit()
        {
            if (IsOnceInit)
                return true;

            if (!base.OnceInit())
                return false;

            TriggerModuleRun = () => ExecuteModule().GetAwaiter().GetResult();
            IsOnceInit = true;
            return true;
        }

        internal bool TryBuildCalibrationRequest(out ElectroStaticChuckCalibrationRequest? request, out string message)
        {
            request = null;
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(CalibrationFrameDirectory))
            {
                message = $"{nameof(CalibrationFrameDirectory)} must not be empty before RunCalibration.";
                return false;
            }

            if (ExportResults && string.IsNullOrWhiteSpace(OutputDirectory))
            {
                message = $"{nameof(OutputDirectory)} must not be empty when ExportResults is enabled.";
                return false;
            }

            request = new ElectroStaticChuckCalibrationRequest
            {
                Input = ElectroStaticChuckInput.FromFrameDirectory(CalibrationFrameDirectory),
                Parameters = BuildAlgoParameters(),
                Options = BuildRunOptions(),
                OutputDirectory = string.IsNullOrWhiteSpace(OutputDirectory) ? null : OutputDirectory
            };
            return true;
        }

        internal bool TryBuildMeasurementRequest(out ElectroStaticChuckMeasurementRequest? request, out string message)
        {
            request = null;
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(MeasurementFrameDirectory))
            {
                message = $"{nameof(MeasurementFrameDirectory)} must not be empty before ExecuteModule.";
                return false;
            }

            if (UsePitchCorrection && CurrentCalibrationState?.Pitch.IsCalibrated != true)
            {
                message = "RunCalibration must complete successfully before measurement with UsePitchCorrection enabled.";
                return false;
            }

            if (UseYawCorrection)
            {
                message = "UseYawCorrection is not supported by the current ElectroStaticChuckMeasurementPipeline.";
                return false;
            }

            if (ExportResults && string.IsNullOrWhiteSpace(OutputDirectory))
            {
                message = $"{nameof(OutputDirectory)} must not be empty when ExportResults is enabled.";
                return false;
            }

            request = new ElectroStaticChuckMeasurementRequest
            {
                Input = ElectroStaticChuckInput.FromFrameDirectory(MeasurementFrameDirectory),
                Calibration = CurrentCalibrationState,
                Parameters = BuildAlgoParameters(),
                Options = BuildRunOptions(),
                OutputDirectory = string.IsNullOrWhiteSpace(OutputDirectory) ? null : OutputDirectory
            };
            return true;
        }

        internal ElectroStaticChuckParameters BuildAlgoParameters()
        {
            return new ElectroStaticChuckParameters
            {
                Sensor = new SensorParameters
                {
                    IntervalX = SensorIntervalX,
                    IntervalY = SensorIntervalY,
                    IntervalZ = SensorIntervalZ,
                    MinDepth = SensorMinDepth,
                    MaxDepth = SensorMaxDepth,
                    InvalidValue = SensorInvalidValue,
                    IsFlip = SensorIsFlip
                },
                Calibration = new CalibrationParameters
                {
                    UsePitchCorrection = UsePitchCorrection,
                    UseYawCorrection = UseYawCorrection
                },
                Registration = new RegistrationParameters
                {
                    Coarse = new CoarseRegistrationParameters
                    {
                        SearchWindowX = CoarseSearchWindowX,
                        SearchWindowY = CoarseSearchWindowY,
                        SearchSpeedFactor = CoarseSearchSpeedFactor,
                        HeightWeight = CoarseHeightWeight,
                        GrayWeight = CoarseGrayWeight,
                        MaskWeight = CoarseMaskWeight
                    },
                    Fine = new FineRegistrationParameters
                    {
                        Mode = FineRegistrationMode,
                        NoRegistrationUseZCorrection = NoRegistrationUseZCorrection,
                        NoRegistrationZSampleStepPixel = NoRegistrationZSampleStepPixel,
                        NoRegistrationMinZSampleCount = NoRegistrationMinZSampleCount,
                        IcpLeafSizeMultiplier = IcpLeafSizeMultiplier,
                        MaxIterations = IcpMaxIterations,
                        MaxCorrespondenceDistanceMultiplier = IcpMaxCorrespondenceDistanceMultiplier,
                        MinMaxCorrespondenceDistance = IcpMinMaxCorrespondenceDistance,
                        TransformationEpsilon = IcpTransformationEpsilon,
                        FitnessEpsilon = IcpFitnessEpsilon,
                        MinInlierRatio = IcpMinInlierRatio,
                        OptimizeX = OptimizeX,
                        OptimizeY = OptimizeY,
                        OptimizeZ = OptimizeZ,
                        OptimizeYawDeg = OptimizeYawDeg
                    }
                },
                Measurement = new MeasurementParameters
                {
                    ConvexStandardDiameter = ConvexStandardDiameter,
                    ConvexStandardHeight = ConvexStandardHeight,
                    StreamingTileCoreWidthPixel = StreamingTileCoreWidthPixel,
                    StreamingTileCoreHeightPixel = StreamingTileCoreHeightPixel,
                    StreamingTileHaloPixel = StreamingTileHaloPixel
                },
                Rendering = new RenderingParameters
                {
                    DrawLabels = DrawLabels,
                    DrawOverlay = DrawOverlay
                },
                Export = new ExportParameters
                {
                    ImageDownsampleFactor = ImageDownsampleFactor,
                    StitchingPointCloudStride = StitchingPointCloudStride
                }
            };
        }

        internal ElectroStaticChuckRunOptions BuildRunOptions()
        {
            return new ElectroStaticChuckRunOptions
            {
                EnableStitching = EnableStitching,
                MeasurementMode = MeasurementMode,
                SavePreviewImages = SavePreviewImages,
                SavePointCloud = SavePointCloud,
                RenderOverlay = RenderOverlay,
                ExportResults = ExportResults
            };
        }

        public Task<ExecuteModuleOutput> RunCalibration()
        {
            var (status, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!TryBuildCalibrationRequest(out ElectroStaticChuckCalibrationRequest? request, out string message))
                    {
                        LastErrorStage = ElectroStaticChuckStage.Calibration.ToString();
                        SetCalibrationStatus(false, message);
                        return NodeStatus.Error;
                    }

                    ElectroStaticChuckCalibrationPipeline pipeline = ElectroStaticChuckCalibrationPipeline.CreateDefault();
                    ElectroStaticChuckCalibrationResult result = pipeline.Run(request!);
                    CurrentCalibrationState = result.Calibration;
                    LastErrorStage = string.Empty;
                    SetCalibrationStatus(true, "Calibration completed.");
                    return NodeStatus.Success;
                }
                catch (ElectroStaticChuckException ex)
                {
                    LastErrorStage = ex.Stage.ToString();
                    SetCalibrationStatus(false, ex.Message);
                    return NodeStatus.Error;
                }
                catch (Exception ex)
                {
                    LastErrorStage = ElectroStaticChuckStage.Calibration.ToString();
                    SetCalibrationStatus(false, ex.Message);
                    return NodeStatus.Error;
                }
            });

            return Task.FromResult(FinishRun(status, time));
        }

        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (status, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!TryBuildMeasurementRequest(out ElectroStaticChuckMeasurementRequest? request, out string message))
                    {
                        LastErrorStage = ElectroStaticChuckStage.Measurement.ToString();
                        SetMeasurementStatus(false, message);
                        return NodeStatus.Error;
                    }

                    ResetLastRunResult();
                    ElectroStaticChuckMeasurementPipeline pipeline = ElectroStaticChuckMeasurementPipeline.CreateDefault();
                    LastRunResult = pipeline.Run(request!);
                    ApplyMeasurementResult(LastRunResult);
                    LastErrorStage = string.Empty;
                    SetMeasurementStatus(true, "Measurement completed.");
                    return NodeStatus.Success;
                }
                catch (ElectroStaticChuckException ex)
                {
                    LastErrorStage = ex.Stage.ToString();
                    SetMeasurementStatus(false, ex.Message);
                    return NodeStatus.Error;
                }
                catch (Exception ex)
                {
                    LastErrorStage = ElectroStaticChuckStage.Measurement.ToString();
                    SetMeasurementStatus(false, ex.Message);
                    return NodeStatus.Error;
                }
            });

            return await Task.FromResult(FinishRun(status, time));
        }

        private void ResetLastRunResult()
        {
            LastRunResult?.Dispose();
            LastRunResult = null;
        }

        private void SetCalibrationStatus(bool success, string message)
        {
            CalibrationSuccess = success;
            CalibrationMessage = message;
            RaisePropertyChanged(nameof(CurrentCalibrationState));
        }

        private void SetMeasurementStatus(bool success, string message)
        {
            MeasurementSuccess = success;
            MeasurementMessage = message;
        }

        private void ApplyMeasurementResult(ElectroStaticChuckRunResult result)
        {
            ConvexsFlatness = result.Measurement?.ConvexsFlatness ?? -1.0;
            OverallFlatness = result.Measurement?.OverallFlatness ?? -1.0;
            ExportOutputPathsText = result.Export == null
                ? string.Empty
                : string.Join(Environment.NewLine, result.Export.OutputPaths);
        }

        private void RefreshSelectedOutputParams()
        {
            if (OutputParams == null || OutputParams.Count == 0)
                return;

            Dictionary<string, object> values = OutputParamCollector.GetDataPointValues(this);
            foreach (TransmitParam item in OutputParams)
            {
                if (!string.IsNullOrWhiteSpace(item.ParamName) && values.TryGetValue(item.ParamName, out object? value))
                {
                    item.Value = value;
                }
                else if (values.TryGetValue(item.Name, out value))
                {
                    item.Value = value;
                }
            }

            if (PrismProvider.ProjectManager?.SltCurSolutionItem != null)
            {
                UpdateParam();
            }
        }

        private ExecuteModuleOutput FinishRun(NodeStatus status, double time)
        {
            RefreshSelectedOutputParams();
            return Output = new ExecuteModuleOutput
            {
                RunStatus = status,
                RunTime = time
            };
        }

        public void InitImg()
        {
            ShowHRoi();
        }

        public void ShowHRoi()
        {
            mWindowH?.ClearROI();
        }

        public void ShowHRoi(HRoi ROI)
        {
            try
            {
                int index = mHRoi.FindIndex(e => e.roiType == ROI.roiType && e.ModuleName == ROI.ModuleName);
                if (ROI.fors == true)
                {
                    mHRoi.Add(ROI);
                    return;
                }

                if (index > -1)
                    mHRoi[index] = ROI;
                else
                    mHRoi.Add(ROI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public override void Dispose()
        {
            ResetLastRunResult();
            base.Dispose();
        }

        #endregion
    }

}
