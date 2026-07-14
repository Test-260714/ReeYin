using Custom.PhysicalScale.Models;
using HalconDotNet;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Custom.PhysicalScale.ViewModels
{
    public class PhysicalScaleSharpenPreviewViewModel : BindableBase
    {
        private const double MinPreviewZoom = 0.1;
        private const double MaxPreviewZoom = 40.0;
        private const double PreviewZoomStepFactor = 1.15;
        private const double PreviewZoomCompareTolerance = 0.0001;
        private static readonly double[] PreviewZoomPresetLevels = { 0.10, 0.25, 0.50, 0.75, 1.00 };
        private const int CircleFitCoverageBinCount = 360;
        private const int CircleFitOverlayPointLimit = 720;

        private sealed class CircleEdgeCandidate
        {
            public CircleEdgeCandidate(Point point, double radius, double angle)
            {
                Point = point;
                Radius = radius;
                Angle = angle;
            }

            public Point Point { get; }

            public double Radius { get; }

            public double Angle { get; }
        }

        private sealed class CircleFitAnalysisResult
        {
            public double CenterX { get; set; }

            public double CenterY { get; set; }

            public double Radius { get; set; }

            public int CandidatePointCount { get; set; }

            public int CoverageBinCount { get; set; }

            public double MinDeviation { get; set; }

            public double MaxDeviation { get; set; }

            public double MeanAbsDeviation { get; set; }

            public double RmsDeviation { get; set; }

            public double MinDiameterPx { get; set; }

            public double MaxDiameterPx { get; set; }

            public IReadOnlyList<Point> OverlayPoints { get; set; } = Array.Empty<Point>();
        }

        private BitmapSource _sourceImage;
        private BitmapSource _displayBitmap;
        private ImageSource _displayImage;
        private PhysicalScalePreviewProcessMode _selectedProcessMode = PhysicalScalePreviewProcessMode.Sharpen;
        private PhysicalScaleSharpenLevel _selectedSharpenLevel = PhysicalScaleSharpenLevel.Medium;
        private int _binaryThreshold = 128;
        private int _edgeThreshold = 42;
        private int _subPixelLowThreshold = 8;
        private int _subPixelHighThreshold = 24;
        private double _subPixelSigma = 1.2;
        private string _processingSummary = "当前显示：原图局部";
        private double _previewZoom = 8.0;
        private bool _isUpdating;
        private Geometry _horizontalProfileFillGeometry;
        private Geometry _horizontalProfileLineGeometry;
        private Geometry _verticalProfileFillGeometry;
        private Geometry _verticalProfileLineGeometry;
        private int _grayMinValue;
        private int _grayMaxValue;
        private double _grayAverageValue;
        private double _grayStdDevValue;
        private int _sampleColumn;
        private int _sampleRow;
        private bool _sampleGuideInitialized;
        private Int32Rect? _currentViewport;
        private PhysicalScaleTool _measurementTool = PhysicalScaleTool.Select;
        private double _nominalCircleRadiusPx;
        private double _scaleX = 1.0;
        private double _scaleY = 1.0;
        private CircleFitAnalysisResult _circleFitAnalysis;
        private Geometry _circleFitGeometry;
        private Geometry _circleFitEdgeGeometry;
        private double _circleFitCenterOffsetX;
        private double _circleFitCenterOffsetY;
        private double _circleFitRadiusOffsetPx;
        private string _circleFitStatusText = "当前量测不是圆，未启用边缘拟合。";
        private string _circleFitDiameterSummary = "--";
        private string _circleFitDiameterRangeSummary = "--";
        private string _circleFitDeviationSummary = "--";
        private string _circleFitConsistencySummary = "--";
        private string _circleFitSampleSummary = "--";

        public ImageSource DisplayImage
        {
            get => _displayImage;
            private set => SetProperty(ref _displayImage, value);
        }

        public Geometry CircleFitGeometry
        {
            get => _circleFitGeometry;
            private set => SetProperty(ref _circleFitGeometry, value);
        }

        public Geometry CircleFitEdgeGeometry
        {
            get => _circleFitEdgeGeometry;
            private set => SetProperty(ref _circleFitEdgeGeometry, value);
        }

        public Visibility CircleFitOverlayVisibility => CircleFitGeometry == null
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility CircleFitEdgeVisibility => CircleFitEdgeGeometry == null
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility CircleFitMetricsVisibility => _circleFitAnalysis == null
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility CircleFitEmptyVisibility => _circleFitAnalysis == null
            ? Visibility.Visible
            : Visibility.Collapsed;

        public string CircleFitStatusText
        {
            get => _circleFitStatusText;
            private set => SetProperty(ref _circleFitStatusText, value);
        }

        public string CircleFitDiameterSummary
        {
            get => _circleFitDiameterSummary;
            private set => SetProperty(ref _circleFitDiameterSummary, value);
        }

        public string CircleFitDiameterRangeSummary
        {
            get => _circleFitDiameterRangeSummary;
            private set => SetProperty(ref _circleFitDiameterRangeSummary, value);
        }

        public string CircleFitDeviationSummary
        {
            get => _circleFitDeviationSummary;
            private set => SetProperty(ref _circleFitDeviationSummary, value);
        }

        public string CircleFitConsistencySummary
        {
            get => _circleFitConsistencySummary;
            private set => SetProperty(ref _circleFitConsistencySummary, value);
        }

        public string CircleFitSampleSummary
        {
            get => _circleFitSampleSummary;
            private set => SetProperty(ref _circleFitSampleSummary, value);
        }

        public Visibility CircleFitCorrectionVisibility => _measurementTool == PhysicalScaleTool.Circle
            ? Visibility.Visible
            : Visibility.Collapsed;

        public double CircleFitCenterOffsetX
        {
            get => _circleFitCenterOffsetX;
            set => UpdateCircleFitCorrection(value, _circleFitCenterOffsetY, _circleFitRadiusOffsetPx);
        }

        public double CircleFitCenterOffsetY
        {
            get => _circleFitCenterOffsetY;
            set => UpdateCircleFitCorrection(_circleFitCenterOffsetX, value, _circleFitRadiusOffsetPx);
        }

        public double CircleFitRadiusOffsetPx
        {
            get => _circleFitRadiusOffsetPx;
            set => UpdateCircleFitCorrection(_circleFitCenterOffsetX, _circleFitCenterOffsetY, value);
        }

        public double CircleFitCenterOffsetXMin => -GetCircleFitCenterOffsetXLimit();

        public double CircleFitCenterOffsetXMax => GetCircleFitCenterOffsetXLimit();

        public double CircleFitCenterOffsetYMin => -GetCircleFitCenterOffsetYLimit();

        public double CircleFitCenterOffsetYMax => GetCircleFitCenterOffsetYLimit();

        public double CircleFitRadiusOffsetMin => -GetCircleFitRadiusOffsetLimit();

        public double CircleFitRadiusOffsetMax => GetCircleFitRadiusOffsetLimit();

        public string CircleFitCorrectionSummary =>
            $"X {CircleFitCenterOffsetX:+0.0;-0.0;0.0} px | Y {CircleFitCenterOffsetY:+0.0;-0.0;0.0} px | R {CircleFitRadiusOffsetPx:+0.0;-0.0;0.0} px";

        public Geometry HorizontalProfileFillGeometry
        {
            get => _horizontalProfileFillGeometry;
            private set => SetProperty(ref _horizontalProfileFillGeometry, value);
        }

        public Geometry HorizontalProfileLineGeometry
        {
            get => _horizontalProfileLineGeometry;
            private set => SetProperty(ref _horizontalProfileLineGeometry, value);
        }

        public Geometry VerticalProfileFillGeometry
        {
            get => _verticalProfileFillGeometry;
            private set => SetProperty(ref _verticalProfileFillGeometry, value);
        }

        public Geometry VerticalProfileLineGeometry
        {
            get => _verticalProfileLineGeometry;
            private set => SetProperty(ref _verticalProfileLineGeometry, value);
        }

        public int GrayMinValue
        {
            get => _grayMinValue;
            private set => SetProperty(ref _grayMinValue, value);
        }

        public int GrayMaxValue
        {
            get => _grayMaxValue;
            private set => SetProperty(ref _grayMaxValue, value);
        }

        public double GrayAverageValue
        {
            get => _grayAverageValue;
            private set => SetProperty(ref _grayAverageValue, value);
        }

        public double GrayStdDevValue
        {
            get => _grayStdDevValue;
            private set => SetProperty(ref _grayStdDevValue, value);
        }


        public string ReportTitle { get; private set; } = "局部量测报告";

        public string ReportMeta { get; private set; } = "-";

        public string ReportDetail { get; private set; } = "-";

        public string DefectName { get; private set; } = "-";

        public string ShapeName { get; private set; } = "-";

        public string ImageSummary { get; private set; } = "-";

        public string SharpenLevelText { get; private set; } = "-";

        public string PreviewPixelSummary => _sourceImage == null
            ? "-"
            : $"{_sourceImage.PixelWidth} x {_sourceImage.PixelHeight} px";

        public string GrayScaleLabel0 => "0";

        public string GrayScaleLabel25 => "64";

        public string GrayScaleLabel50 => "128";

        public string GrayScaleLabel75 => "192";

        public string GrayScaleLabel100 => "255";

        public Visibility GrayProfileVisibility => _displayBitmap == null
            || HorizontalProfileLineGeometry == null
            || VerticalProfileLineGeometry == null
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility SampleGuideVisibility => _displayBitmap == null
            ? Visibility.Collapsed
            : Visibility.Visible;

        public double PreviewZoom
        {
            get => _previewZoom;
            set
            {
                double clamped = Math.Clamp(value, MinPreviewZoom, MaxPreviewZoom);
                if (SetProperty(ref _previewZoom, clamped))
                {
                    RaisePreviewScaleChanged();
                }
            }
        }

        public string PreviewZoomText => $"{PreviewZoom:F2}x";

        public double ScaledPreviewWidth => _sourceImage?.PixelWidth * PreviewZoom ?? 0;

        public double ScaledPreviewHeight => _sourceImage?.PixelHeight * PreviewZoom ?? 0;

        public double SampleVerticalLineDisplayX => (_sampleColumn + 0.5) * PreviewZoom;

        public double SampleHorizontalLineDisplayY => (_sampleRow + 0.5) * PreviewZoom;

        public string ProcessingSummary
        {
            get => _processingSummary;
            private set => SetProperty(ref _processingSummary, value);
        }

        public PhysicalScalePreviewProcessMode SelectedProcessMode
        {
            get => _selectedProcessMode;
            set
            {
                if (SetProperty(ref _selectedProcessMode, value))
                {
                    RaiseProcessVisibilityChanged();
                    RefreshPreview();
                }
            }
        }

        public PhysicalScaleSharpenLevel SelectedSharpenLevel
        {
            get => _selectedSharpenLevel;
            set
            {
                if (SetProperty(ref _selectedSharpenLevel, value))
                {
                    RaisePropertyChanged(nameof(CurrentModeTag));
                    RefreshPreview();
                }
            }
        }

        public int BinaryThreshold
        {
            get => _binaryThreshold;
            set
            {
                int clamped = Math.Clamp(value, 0, 255);
                if (SetProperty(ref _binaryThreshold, clamped))
                    RefreshPreview();
            }
        }

        public int EdgeThreshold
        {
            get => _edgeThreshold;
            set
            {
                int clamped = Math.Clamp(value, 1, 255);
                if (SetProperty(ref _edgeThreshold, clamped))
                    RefreshPreview();
            }
        }

        public int SubPixelLowThreshold
        {
            get => _subPixelLowThreshold;
            set
            {
                int clamped = Math.Clamp(value, 1, 255);
                if (SetProperty(ref _subPixelLowThreshold, clamped))
                    RefreshPreview();
            }
        }

        public int SubPixelHighThreshold
        {
            get => _subPixelHighThreshold;
            set
            {
                int clamped = Math.Clamp(value, 1, 255);
                if (SetProperty(ref _subPixelHighThreshold, clamped))
                    RefreshPreview();
            }
        }

        public double SubPixelSigma
        {
            get => _subPixelSigma;
            set
            {
                double clamped = Math.Clamp(value, 0.5, 5.0);
                if (SetProperty(ref _subPixelSigma, clamped))
                    RefreshPreview();
            }
        }

        public string CurrentModeTag => SelectedProcessMode switch
        {
            PhysicalScalePreviewProcessMode.Original => "原图",
            PhysicalScalePreviewProcessMode.Sharpen => $"锐化 {GetSharpenLevelText(SelectedSharpenLevel)}",
            PhysicalScalePreviewProcessMode.Binary => "二值化",
            PhysicalScalePreviewProcessMode.Edge => "边缘",
            PhysicalScalePreviewProcessMode.SubPixel => "亚像素",
            _ => "原图"
        };

        public Visibility SharpenOptionsVisibility =>
            SelectedProcessMode == PhysicalScalePreviewProcessMode.Sharpen ? Visibility.Visible : Visibility.Collapsed;

        public Visibility BinaryOptionsVisibility =>
            SelectedProcessMode == PhysicalScalePreviewProcessMode.Binary ? Visibility.Visible : Visibility.Collapsed;

        public Visibility EdgeOptionsVisibility =>
            SelectedProcessMode == PhysicalScalePreviewProcessMode.Edge ? Visibility.Visible : Visibility.Collapsed;

        public Visibility SubPixelOptionsVisibility =>
            SelectedProcessMode == PhysicalScalePreviewProcessMode.SubPixel ? Visibility.Visible : Visibility.Collapsed;

        public DelegateCommand CloseCommand { get; }

        public DelegateCommand PreviewZoomInCommand { get; }

        public DelegateCommand PreviewZoomOutCommand { get; }

        public DelegateCommand SetPreviewActualSizeCommand { get; }

        public DelegateCommand SetPreviewDefaultZoomCommand { get; }

        public DelegateCommand ApplySampleGuideAsCircleFitCenterCommand { get; }

        public DelegateCommand ResetCircleFitCorrectionCommand { get; }

        public Action RequestClose { get; set; }

        public event Action PreviewViewportResetRequested;

        public PhysicalScaleSharpenPreviewViewModel()
        {
            CloseCommand = new DelegateCommand(() => RequestClose?.Invoke());
            PreviewZoomInCommand = new DelegateCommand(() => PreviewZoom = GetNextPreviewZoom(true));
            PreviewZoomOutCommand = new DelegateCommand(() => PreviewZoom = GetNextPreviewZoom(false));
            SetPreviewActualSizeCommand = new DelegateCommand(() =>
            {
                PreviewZoom = 1;
                RequestViewportReset();
            });
            SetPreviewDefaultZoomCommand = new DelegateCommand(() =>
            {
                PreviewZoom = GetDefaultPreviewZoom(_sourceImage);
                RequestViewportReset();
            });
            ApplySampleGuideAsCircleFitCenterCommand = new DelegateCommand(ApplySampleGuideAsCircleFitCenter);
            ResetCircleFitCorrectionCommand = new DelegateCommand(ResetCircleFitCorrection);
        }

        public void ApplyPayload(PhysicalScaleSharpenPreviewPayload payload)
        {
            if (payload == null)
                return;

            _isUpdating = true;
            _sourceImage = payload.PreviewImage;
            ReportTitle = string.IsNullOrWhiteSpace(payload.ReportTitle) ? "局部量测报告" : payload.ReportTitle;
            ReportMeta = string.IsNullOrWhiteSpace(payload.ReportMeta) ? "-" : payload.ReportMeta;
            ReportDetail = string.IsNullOrWhiteSpace(payload.ReportDetail) ? "-" : payload.ReportDetail;
            DefectName = string.IsNullOrWhiteSpace(payload.DefectName) ? "-" : payload.DefectName;
            ShapeName = string.IsNullOrWhiteSpace(payload.ShapeName) ? "-" : payload.ShapeName;
            ImageSummary = string.IsNullOrWhiteSpace(payload.ImageSummary) ? "-" : payload.ImageSummary;
            SharpenLevelText = string.IsNullOrWhiteSpace(payload.SharpenLevelText) ? "-" : payload.SharpenLevelText;
            _measurementTool = payload.MeasurementTool;
            _nominalCircleRadiusPx = payload.NominalCircleRadiusPx;
            _scaleX = payload.ScaleX > 0 ? payload.ScaleX : 1.0;
            _scaleY = payload.ScaleY > 0 ? payload.ScaleY : 1.0;
            _selectedSharpenLevel = payload.SharpenLevel;
            _selectedProcessMode = PhysicalScalePreviewProcessMode.Sharpen;
            _previewZoom = GetDefaultPreviewZoom(_sourceImage);
            _circleFitCenterOffsetX = 0;
            _circleFitCenterOffsetY = 0;
            _circleFitRadiusOffsetPx = 0;
            _circleFitAnalysis = null;
            CircleFitGeometry = null;
            CircleFitEdgeGeometry = null;
            CircleFitStatusText = _measurementTool == PhysicalScaleTool.Circle
                ? "正在提取圆边缘并执行拟合..."
                : "当前量测不是圆，未启用边缘拟合。";
            CircleFitDiameterSummary = "--";
            CircleFitDiameterRangeSummary = "--";
            CircleFitDeviationSummary = "--";
            CircleFitConsistencySummary = "--";
            CircleFitSampleSummary = "--";
            ResetSampleGuideToCenter(_sourceImage);
            _isUpdating = false;

            RaisePropertyChanged(nameof(ReportTitle));
            RaisePropertyChanged(nameof(ReportMeta));
            RaisePropertyChanged(nameof(ReportDetail));
            RaisePropertyChanged(nameof(DefectName));
            RaisePropertyChanged(nameof(ShapeName));
            RaisePropertyChanged(nameof(ImageSummary));
            RaisePropertyChanged(nameof(SharpenLevelText));
            RaisePropertyChanged(nameof(PreviewPixelSummary));
            RaisePropertyChanged(nameof(GrayProfileVisibility));
            RaiseCircleFitCorrectionChanged();
            RaiseCircleFitStateChanged();
            RaisePropertyChanged(nameof(SelectedSharpenLevel));
            RaisePropertyChanged(nameof(SelectedProcessMode));
            RaisePropertyChanged(nameof(CurrentModeTag));
            RaiseProcessVisibilityChanged();
            RaisePreviewScaleChanged();

            RefreshPreview();
        }

        public void RequestViewportReset()
        {
            PreviewViewportResetRequested?.Invoke();
        }

        public void UpdateViewport(double offsetX, double offsetY, double viewportWidth, double viewportHeight)
        {
            if (_displayBitmap == null)
                return;

            int x = Math.Clamp((int)Math.Floor(offsetX), 0, Math.Max(0, _displayBitmap.PixelWidth - 1));
            int y = Math.Clamp((int)Math.Floor(offsetY), 0, Math.Max(0, _displayBitmap.PixelHeight - 1));
            int width = Math.Clamp((int)Math.Ceiling(viewportWidth), 1, Math.Max(1, _displayBitmap.PixelWidth - x));
            int height = Math.Clamp((int)Math.Ceiling(viewportHeight), 1, Math.Max(1, _displayBitmap.PixelHeight - y));
            _currentViewport = new Int32Rect(x, y, width, height);
            UpdateGrayProfiles(_displayBitmap);
        }

        public void UpdateSampleGuideByDisplayPoint(double displayX, double displayY, bool updateVertical, bool updateHorizontal)
        {
            BitmapSource bitmap = _displayBitmap ?? _sourceImage;
            if (bitmap == null || PreviewZoom <= 0)
                return;

            bool changed = false;
            if (updateVertical)
            {
                int newColumn = Math.Clamp((int)Math.Round(displayX / PreviewZoom - 0.5), 0, Math.Max(0, bitmap.PixelWidth - 1));
                if (newColumn != _sampleColumn)
                {
                    _sampleColumn = newColumn;
                    changed = true;
                }
            }

            if (updateHorizontal)
            {
                int newRow = Math.Clamp((int)Math.Round(displayY / PreviewZoom - 0.5), 0, Math.Max(0, bitmap.PixelHeight - 1));
                if (newRow != _sampleRow)
                {
                    _sampleRow = newRow;
                    changed = true;
                }
            }

            if (!changed)
                return;

            RaiseSampleGuideChanged();
            UpdateGrayProfiles(bitmap);
        }

        public void ApplyCircleFitCenterFromDisplayPoint(double displayX, double displayY)
        {
            BitmapSource bitmap = _displayBitmap ?? _sourceImage;
            if (bitmap == null || PreviewZoom <= 0 || _measurementTool != PhysicalScaleTool.Circle)
                return;

            double imageX = Math.Clamp(displayX / PreviewZoom, 0, Math.Max(0, bitmap.PixelWidth - 1));
            double imageY = Math.Clamp(displayY / PreviewZoom, 0, Math.Max(0, bitmap.PixelHeight - 1));
            UpdateCircleFitCorrection(
                imageX - GetDefaultCircleFitCenterX(bitmap),
                imageY - GetDefaultCircleFitCenterY(bitmap),
                _circleFitRadiusOffsetPx);
        }

        private void ApplySampleGuideAsCircleFitCenter()
        {
            BitmapSource bitmap = _displayBitmap ?? _sourceImage;
            if (bitmap == null || _measurementTool != PhysicalScaleTool.Circle)
                return;

            UpdateCircleFitCorrection(
                (_sampleColumn + 0.5) - GetDefaultCircleFitCenterX(bitmap),
                (_sampleRow + 0.5) - GetDefaultCircleFitCenterY(bitmap),
                _circleFitRadiusOffsetPx);
        }

        private void ResetCircleFitCorrection()
        {
            UpdateCircleFitCorrection(0, 0, 0);
        }

        private void UpdateCircleFitCorrection(double centerOffsetX, double centerOffsetY, double radiusOffsetPx)
        {
            double clampedCenterX = Math.Clamp(centerOffsetX, CircleFitCenterOffsetXMin, CircleFitCenterOffsetXMax);
            double clampedCenterY = Math.Clamp(centerOffsetY, CircleFitCenterOffsetYMin, CircleFitCenterOffsetYMax);
            double clampedRadius = Math.Clamp(radiusOffsetPx, CircleFitRadiusOffsetMin, CircleFitRadiusOffsetMax);

            bool changed = false;
            if (SetProperty(ref _circleFitCenterOffsetX, clampedCenterX, nameof(CircleFitCenterOffsetX)))
                changed = true;
            if (SetProperty(ref _circleFitCenterOffsetY, clampedCenterY, nameof(CircleFitCenterOffsetY)))
                changed = true;
            if (SetProperty(ref _circleFitRadiusOffsetPx, clampedRadius, nameof(CircleFitRadiusOffsetPx)))
                changed = true;

            if (!changed)
                return;

            RaiseCircleFitCorrectionChanged();
            RefreshCircleFitAnalysis();
        }

        private void RaiseProcessVisibilityChanged()
        {
            RaisePropertyChanged(nameof(CurrentModeTag));
            RaisePropertyChanged(nameof(SharpenOptionsVisibility));
            RaisePropertyChanged(nameof(BinaryOptionsVisibility));
            RaisePropertyChanged(nameof(EdgeOptionsVisibility));
            RaisePropertyChanged(nameof(SubPixelOptionsVisibility));
        }

        private void RefreshPreview()
        {
            if (_isUpdating || _sourceImage == null)
                return;

            try
            {
                BitmapSource previewBitmap = SelectedProcessMode switch
                {
                    PhysicalScalePreviewProcessMode.Original => CreateOriginalBitmap(_sourceImage),
                    PhysicalScalePreviewProcessMode.Sharpen => CreateSharpenedBitmap(_sourceImage, SelectedSharpenLevel),
                    PhysicalScalePreviewProcessMode.Binary => CreateBinaryBitmap(_sourceImage, BinaryThreshold),
                    PhysicalScalePreviewProcessMode.Edge => CreateEdgeBitmap(_sourceImage, EdgeThreshold),
                    PhysicalScalePreviewProcessMode.SubPixel => CreateSubPixelBitmap(_sourceImage, SubPixelSigma, SubPixelLowThreshold, SubPixelHighThreshold),
                    _ => CreateOriginalBitmap(_sourceImage)
                };

                _displayBitmap = previewBitmap;
                DisplayImage = previewBitmap;
                EnsureSampleGuideInBounds(previewBitmap);
                NormalizeViewport(previewBitmap);
                UpdateGrayProfiles(previewBitmap);
                RefreshCircleFitAnalysis();
                ProcessingSummary = BuildProcessingSummary();
                RaisePropertyChanged(nameof(SampleGuideVisibility));
            }
            catch (Exception ex)
            {
                BitmapSource fallbackBitmap = CreateOriginalBitmap(_sourceImage);
                _displayBitmap = fallbackBitmap;
                DisplayImage = fallbackBitmap;
                EnsureSampleGuideInBounds(fallbackBitmap);
                NormalizeViewport(fallbackBitmap);
                UpdateGrayProfiles(fallbackBitmap);
                RefreshCircleFitAnalysis();
                ProcessingSummary = $"处理失败，已回退原图：{ex.Message}";
            }
        }

        public double GetNextPreviewZoom(bool zoomIn)
        {
            return zoomIn
                ? ResolveNextPreviewZoomIn(PreviewZoom)
                : ResolveNextPreviewZoomOut(PreviewZoom);
        }

        private void RefreshCircleFitAnalysis()
        {
            if (_sourceImage == null)
            {
                ClearCircleFitState("当前没有可分析的局部图像。");
                return;
            }

            if (_measurementTool != PhysicalScaleTool.Circle)
            {
                ClearCircleFitState("当前量测不是圆，未启用边缘拟合。");
                return;
            }

            if (_nominalCircleRadiusPx < 1.0)
            {
                ClearCircleFitState("当前圆测量半径过小，无法执行边缘拟合。");
                return;
            }

            BitmapSource bitmap = _displayBitmap ?? _sourceImage;
            double targetCenterX = GetDefaultCircleFitCenterX(bitmap) + CircleFitCenterOffsetX;
            double targetCenterY = GetDefaultCircleFitCenterY(bitmap) + CircleFitCenterOffsetY;
            double targetRadiusPx = Math.Max(1.0, _nominalCircleRadiusPx + CircleFitRadiusOffsetPx);

            if (!TryAnalyzeCircleEdge(
                    _sourceImage,
                    SubPixelSigma,
                    SubPixelLowThreshold,
                    SubPixelHighThreshold,
                    targetCenterX,
                    targetCenterY,
                    targetRadiusPx,
                    out CircleFitAnalysisResult analysis,
                    out string statusText))
            {
                ClearCircleFitState(statusText);
                return;
            }

            _circleFitAnalysis = analysis;
            CircleFitStatusText = statusText;

            double diameterPx = analysis.Radius * 2.0;
            double minDiameterPx = analysis.MinDiameterPx;
            double maxDiameterPx = analysis.MaxDiameterPx;
            double averageScale = (_scaleX + _scaleY) * 0.5;

            CircleFitDiameterSummary =
                $"{diameterPx:F2} px | X {diameterPx * _scaleX:F4} mm / Y {diameterPx * _scaleY:F4} mm";
            CircleFitDiameterRangeSummary =
                $"{minDiameterPx:F2} ~ {maxDiameterPx:F2} px | 波动 {(maxDiameterPx - minDiameterPx):F2} px";
            CircleFitDeviationSummary =
                $"内 {analysis.MinDeviation:+0.00;-0.00;0.00} px | 外 {analysis.MaxDeviation:+0.00;-0.00;0.00} px";
            CircleFitConsistencySummary =
                $"RMS {analysis.RmsDeviation:F2} px | 峰峰值 {(analysis.MaxDeviation - analysis.MinDeviation) * 2.0:F2} px | 平均偏差 {analysis.MeanAbsDeviation:F2} px";
            CircleFitSampleSummary =
                $"{analysis.CandidatePointCount} 点 | 覆盖 {analysis.CoverageBinCount}/{CircleFitCoverageBinCount} | 约 {(maxDiameterPx - minDiameterPx) * averageScale:F4} mm";

            UpdateCircleFitOverlayGeometry();
            RaiseCircleFitStateChanged();
        }

        private void ClearCircleFitState(string statusText)
        {
            _circleFitAnalysis = null;
            CircleFitStatusText = string.IsNullOrWhiteSpace(statusText) ? "当前没有可用的圆边缘分析结果。" : statusText;
            CircleFitDiameterSummary = "--";
            CircleFitDiameterRangeSummary = "--";
            CircleFitDeviationSummary = "--";
            CircleFitConsistencySummary = "--";
            CircleFitSampleSummary = "--";
            CircleFitGeometry = null;
            CircleFitEdgeGeometry = null;
            RaiseCircleFitStateChanged();
        }

        private void UpdateCircleFitOverlayGeometry()
        {
            if (_circleFitAnalysis == null || PreviewZoom <= 0)
            {
                CircleFitGeometry = null;
                CircleFitEdgeGeometry = null;
                RaiseCircleFitStateChanged();
                return;
            }

            var geometry = new EllipseGeometry(
                new Point(_circleFitAnalysis.CenterX * PreviewZoom, _circleFitAnalysis.CenterY * PreviewZoom),
                _circleFitAnalysis.Radius * PreviewZoom,
                _circleFitAnalysis.Radius * PreviewZoom);
            geometry.Freeze();
            CircleFitGeometry = geometry;
            CircleFitEdgeGeometry = CreateCircleEdgeOverlayGeometry(_circleFitAnalysis.OverlayPoints, PreviewZoom);
            RaiseCircleFitStateChanged();
        }

        private void RaiseCircleFitStateChanged()
        {
            RaisePropertyChanged(nameof(CircleFitEdgeVisibility));
            RaisePropertyChanged(nameof(CircleFitOverlayVisibility));
            RaisePropertyChanged(nameof(CircleFitMetricsVisibility));
            RaisePropertyChanged(nameof(CircleFitEmptyVisibility));
        }

        private void RaiseCircleFitCorrectionChanged()
        {
            RaisePropertyChanged(nameof(CircleFitCorrectionVisibility));
            RaisePropertyChanged(nameof(CircleFitCenterOffsetXMin));
            RaisePropertyChanged(nameof(CircleFitCenterOffsetXMax));
            RaisePropertyChanged(nameof(CircleFitCenterOffsetYMin));
            RaisePropertyChanged(nameof(CircleFitCenterOffsetYMax));
            RaisePropertyChanged(nameof(CircleFitRadiusOffsetMin));
            RaisePropertyChanged(nameof(CircleFitRadiusOffsetMax));
            RaisePropertyChanged(nameof(CircleFitCorrectionSummary));
        }

        private double GetCircleFitCenterOffsetXLimit()
        {
            return _sourceImage == null
                ? 48
                : Math.Max(12.0, _sourceImage.PixelWidth * 0.35);
        }

        private double GetCircleFitCenterOffsetYLimit()
        {
            return _sourceImage == null
                ? 48
                : Math.Max(12.0, _sourceImage.PixelHeight * 0.35);
        }

        private double GetCircleFitRadiusOffsetLimit()
        {
            if (_sourceImage == null)
                return Math.Max(12.0, _nominalCircleRadiusPx * 0.5);

            double imageLimit = Math.Min(_sourceImage.PixelWidth, _sourceImage.PixelHeight) * 0.25;
            double nominalLimit = Math.Max(12.0, _nominalCircleRadiusPx * 0.6);
            return Math.Max(12.0, Math.Min(imageLimit, nominalLimit));
        }

        private static double GetDefaultCircleFitCenterX(BitmapSource bitmap)
        {
            return bitmap == null ? 0 : (bitmap.PixelWidth - 1) * 0.5;
        }

        private static double GetDefaultCircleFitCenterY(BitmapSource bitmap)
        {
            return bitmap == null ? 0 : (bitmap.PixelHeight - 1) * 0.5;
        }

        private void RaisePreviewScaleChanged()
        {
            RaisePropertyChanged(nameof(PreviewZoom));
            RaisePropertyChanged(nameof(PreviewZoomText));
            RaisePropertyChanged(nameof(ScaledPreviewWidth));
            RaisePropertyChanged(nameof(ScaledPreviewHeight));
            UpdateCircleFitOverlayGeometry();
            RaiseSampleGuideChanged();
        }

        private string BuildProcessingSummary()
        {
            return SelectedProcessMode switch
            {
                PhysicalScalePreviewProcessMode.Original => "当前显示：原图局部",
                PhysicalScalePreviewProcessMode.Sharpen => $"当前显示：锐化增强，档位 {GetSharpenLevelText(SelectedSharpenLevel)}",
                PhysicalScalePreviewProcessMode.Binary => $"当前显示：二值化，阈值 {BinaryThreshold}",
                PhysicalScalePreviewProcessMode.Edge => $"当前显示：边缘增强，阈值 {EdgeThreshold}",
                PhysicalScalePreviewProcessMode.SubPixel => $"当前显示：亚像素边缘预览，sigma {SubPixelSigma:F2}，low {SubPixelLowThreshold}，high {SubPixelHighThreshold}",
                _ => "当前显示：原图局部"
            };
        }

        private static BitmapSource CreateOriginalBitmap(BitmapSource source)
        {
            var converted = new FormatConvertedBitmap();
            converted.BeginInit();
            converted.Source = source;
            converted.DestinationFormat = PixelFormats.Bgra32;
            converted.EndInit();
            converted.Freeze();
            return converted;
        }

        private static BitmapSource CreateSharpenedBitmap(BitmapSource source, PhysicalScaleSharpenLevel level)
        {
            var converted = EnsureBgra32(source);
            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] sourcePixels = new byte[height * stride];
            byte[] outputPixels = new byte[sourcePixels.Length];
            converted.CopyPixels(sourcePixels, stride, 0);
            Buffer.BlockCopy(sourcePixels, 0, outputPixels, 0, sourcePixels.Length);

            double strength = level switch
            {
                PhysicalScaleSharpenLevel.Weak => 0.45,
                PhysicalScaleSharpenLevel.Medium => 0.85,
                PhysicalScaleSharpenLevel.Strong => 1.25,
                _ => 0.85
            };

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int centerIndex = y * stride + x * 4;
                    int upIndex = (y - 1) * stride + x * 4;
                    int downIndex = (y + 1) * stride + x * 4;
                    int leftIndex = y * stride + (x - 1) * 4;
                    int rightIndex = y * stride + (x + 1) * 4;

                    for (int channel = 0; channel < 3; channel++)
                    {
                        double center = sourcePixels[centerIndex + channel];
                        double neighborSum =
                            sourcePixels[upIndex + channel] +
                            sourcePixels[downIndex + channel] +
                            sourcePixels[leftIndex + channel] +
                            sourcePixels[rightIndex + channel];
                        int sharpenedValue = (int)Math.Round(center * (1.0 + 4.0 * strength) - neighborSum * strength);
                        outputPixels[centerIndex + channel] = (byte)Math.Clamp(sharpenedValue, 0, 255);
                    }

                    outputPixels[centerIndex + 3] = sourcePixels[centerIndex + 3];
                }
            }

            return CreateBgraBitmap(outputPixels, width, height);
        }

        private static BitmapSource CreateBinaryBitmap(BitmapSource source, int threshold)
        {
            byte[] grayPixels = ExtractGrayPixels(source, out int width, out int height);
            byte[] output = new byte[grayPixels.Length];

            for (int i = 0; i < grayPixels.Length; i++)
                output[i] = grayPixels[i] >= threshold ? (byte)255 : (byte)0;

            return CreateGrayBitmap(output, width, height);
        }

        private static BitmapSource CreateEdgeBitmap(BitmapSource source, int threshold)
        {
            byte[] grayPixels = ExtractGrayPixels(source, out int width, out int height);
            byte[] bgra = CreateGrayBackground(grayPixels, width, height);

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = y * width + x;
                    int gx =
                        -grayPixels[(y - 1) * width + (x - 1)] - 2 * grayPixels[y * width + (x - 1)] - grayPixels[(y + 1) * width + (x - 1)] +
                        grayPixels[(y - 1) * width + (x + 1)] + 2 * grayPixels[y * width + (x + 1)] + grayPixels[(y + 1) * width + (x + 1)];
                    int gy =
                        -grayPixels[(y - 1) * width + (x - 1)] - 2 * grayPixels[(y - 1) * width + x] - grayPixels[(y - 1) * width + (x + 1)] +
                        grayPixels[(y + 1) * width + (x - 1)] + 2 * grayPixels[(y + 1) * width + x] + grayPixels[(y + 1) * width + (x + 1)];

                    int magnitude = (int)Math.Min(255, Math.Sqrt(gx * gx + gy * gy));
                    if (magnitude >= threshold)
                    {
                        int pixelIndex = idx * 4;
                        bgra[pixelIndex] = 36;
                        bgra[pixelIndex + 1] = 190;
                        bgra[pixelIndex + 2] = 255;
                    }
                }
            }

            return CreateBgraBitmap(bgra, width, height);
        }

        private static BitmapSource CreateSubPixelBitmap(BitmapSource source, double sigma, int low, int high)
        {
            byte[] grayPixels = ExtractGrayPixels(source, out int width, out int height);
            byte[] bgra = CreateGrayBackground(grayPixels, width, height);

            GCHandle handle = GCHandle.Alloc(grayPixels, GCHandleType.Pinned);
            try
            {
                HOperatorSet.GenImage1(out HObject image, "byte", width, height, handle.AddrOfPinnedObject());
                try
                {
                    HOperatorSet.EdgesSubPix(image, out HObject edges, "canny", sigma, low, high);
                    try
                    {
                        HOperatorSet.CountObj(edges, out HTuple count);
                        for (int i = 1; i <= count.I; i++)
                        {
                            HOperatorSet.SelectObj(edges, out HObject contour, i);
                            try
                            {
                                HOperatorSet.GetContourXld(contour, out HTuple rows, out HTuple cols);
                                DrawContourPoints(bgra, width, height, rows, cols);
                            }
                            finally
                            {
                                contour.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        edges.Dispose();
                    }
                }
                finally
                {
                    image.Dispose();
                }
            }
            finally
            {
                handle.Free();
            }

            return CreateBgraBitmap(bgra, width, height);
        }

        private static void DrawContourPoints(byte[] bgra, int width, int height, HTuple rows, HTuple cols)
        {
            int length = Math.Min(rows.Length, cols.Length);
            for (int i = 0; i < length; i++)
            {
                int y = (int)Math.Round(rows[i].D);
                int x = (int)Math.Round(cols[i].D);

                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        int drawX = x + offsetX;
                        int drawY = y + offsetY;
                        if (drawX < 0 || drawX >= width || drawY < 0 || drawY >= height)
                            continue;

                        int pixelIndex = (drawY * width + drawX) * 4;
                        bgra[pixelIndex] = 64;
                        bgra[pixelIndex + 1] = 96;
                        bgra[pixelIndex + 2] = 255;
                        bgra[pixelIndex + 3] = 255;
                    }
                }
            }
        }

        private static bool TryAnalyzeCircleEdge(
            BitmapSource source,
            double sigma,
            int low,
            int high,
            double expectedCenterX,
            double expectedCenterY,
            double nominalRadiusPx,
            out CircleFitAnalysisResult result,
            out string statusText)
        {
            result = null;
            statusText = string.Empty;

            if (source == null)
            {
                statusText = "当前没有可分析的圆边缘图像。";
                return false;
            }

            try
            {
                List<Point> edgePoints = ExtractSubPixelEdgePoints(source, sigma, low, high);
                if (edgePoints.Count < 24)
                {
                    statusText = "亚像素边缘点数量不足，无法拟合圆。";
                    return false;
                }

                double centerX = double.IsFinite(expectedCenterX)
                    ? Math.Clamp(expectedCenterX, 0, Math.Max(0, source.PixelWidth - 1))
                    : (source.PixelWidth - 1) * 0.5;
                double centerY = double.IsFinite(expectedCenterY)
                    ? Math.Clamp(expectedCenterY, 0, Math.Max(0, source.PixelHeight - 1))
                    : (source.PixelHeight - 1) * 0.5;
                double targetRadius = nominalRadiusPx > 1.0
                    ? nominalRadiusPx
                    : Math.Min(source.PixelWidth, source.PixelHeight) * 0.25;
                double bandHalfWidth = Math.Clamp(Math.Max(6.0, targetRadius * 0.35), 6.0, Math.Max(18.0, targetRadius * 0.8));

                List<CircleEdgeCandidate> candidates = BuildCircleEdgeCandidates(edgePoints, centerX, centerY, targetRadius, bandHalfWidth);
                if (candidates.Count < 32)
                {
                    bandHalfWidth = Math.Clamp(Math.Max(bandHalfWidth * 1.5, 10.0), 10.0, Math.Max(24.0, targetRadius));
                    candidates = BuildCircleEdgeCandidates(edgePoints, centerX, centerY, targetRadius, bandHalfWidth);
                }

                if (candidates.Count < 24)
                {
                    statusText = "目标圆边缘点不足，暂时无法给出稳定拟合结果。";
                    return false;
                }

                if (!TryFitCircleLeastSquares(candidates.Select(item => item.Point).ToList(), out double fittedCenterX, out double fittedCenterY, out double fittedRadius))
                {
                    statusText = "圆边缘拟合失败，请调整亚像素阈值后重试。";
                    return false;
                }

                bool[] coverage = new bool[CircleFitCoverageBinCount];
                double minDeviation = double.MaxValue;
                double maxDeviation = double.MinValue;
                double totalAbsDeviation = 0;
                double totalSquaredDeviation = 0;

                foreach (CircleEdgeCandidate candidate in candidates)
                {
                    double deviation = Distance(candidate.Point.X, candidate.Point.Y, fittedCenterX, fittedCenterY) - fittedRadius;
                    minDeviation = Math.Min(minDeviation, deviation);
                    maxDeviation = Math.Max(maxDeviation, deviation);
                    totalAbsDeviation += Math.Abs(deviation);
                    totalSquaredDeviation += deviation * deviation;

                    int binIndex = Math.Clamp((int)Math.Floor(candidate.Angle / (Math.PI * 2.0) * CircleFitCoverageBinCount), 0, CircleFitCoverageBinCount - 1);
                    coverage[binIndex] = true;
                }

                result = new CircleFitAnalysisResult
                {
                    CenterX = fittedCenterX,
                    CenterY = fittedCenterY,
                    Radius = fittedRadius,
                    CandidatePointCount = candidates.Count,
                    CoverageBinCount = coverage.Count(item => item),
                    MinDeviation = minDeviation,
                    MaxDeviation = maxDeviation,
                    MeanAbsDeviation = totalAbsDeviation / candidates.Count,
                    RmsDeviation = Math.Sqrt(totalSquaredDeviation / candidates.Count),
                    MinDiameterPx = Math.Max(0, (fittedRadius + minDeviation) * 2.0),
                    MaxDiameterPx = Math.Max(0, (fittedRadius + maxDeviation) * 2.0),
                    OverlayPoints = SelectOverlayPoints(candidates)
                };

                statusText = $"圆边缘拟合成功，采用 {candidates.Count} 个亚像素边缘点。";
                return true;
            }
            catch (Exception ex)
            {
                statusText = $"圆边缘拟合失败：{ex.Message}";
                return false;
            }
        }

        private static List<CircleEdgeCandidate> BuildCircleEdgeCandidates(
            IEnumerable<Point> edgePoints,
            double centerX,
            double centerY,
            double targetRadius,
            double bandHalfWidth)
        {
            var candidates = new List<CircleEdgeCandidate>();
            double minRadius = Math.Max(1.0, targetRadius - bandHalfWidth);
            double maxRadius = targetRadius + bandHalfWidth;

            foreach (Point point in edgePoints)
            {
                double dx = point.X - centerX;
                double dy = point.Y - centerY;
                double radius = Math.Sqrt(dx * dx + dy * dy);
                if (radius < minRadius || radius > maxRadius)
                    continue;

                double angle = Math.Atan2(dy, dx);
                if (angle < 0)
                    angle += Math.PI * 2.0;

                candidates.Add(new CircleEdgeCandidate(point, radius, angle));
            }

            return candidates;
        }

        private static IReadOnlyList<Point> SelectOverlayPoints(IReadOnlyList<CircleEdgeCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return Array.Empty<Point>();

            List<CircleEdgeCandidate> ordered = candidates
                .OrderBy(item => item.Angle)
                .ToList();
            int step = Math.Max(1, (int)Math.Ceiling(ordered.Count / (double)CircleFitOverlayPointLimit));
            var points = new List<Point>((ordered.Count + step - 1) / step);
            for (int index = 0; index < ordered.Count; index += step)
            {
                points.Add(ordered[index].Point);
            }

            Point lastPoint = ordered[ordered.Count - 1].Point;
            if (points.Count == 0 || !points[points.Count - 1].Equals(lastPoint))
            {
                points.Add(lastPoint);
            }

            return points;
        }

        private static List<Point> ExtractSubPixelEdgePoints(BitmapSource source, double sigma, int low, int high)
        {
            byte[] grayPixels = ExtractGrayPixels(source, out int width, out int height);
            var points = new List<Point>(Math.Max(256, width * height / 12));

            GCHandle handle = GCHandle.Alloc(grayPixels, GCHandleType.Pinned);
            try
            {
                HOperatorSet.GenImage1(out HObject image, "byte", width, height, handle.AddrOfPinnedObject());
                try
                {
                    HOperatorSet.EdgesSubPix(image, out HObject edges, "canny", sigma, low, high);
                    try
                    {
                        HOperatorSet.CountObj(edges, out HTuple count);
                        for (int i = 1; i <= count.I; i++)
                        {
                            HOperatorSet.SelectObj(edges, out HObject contour, i);
                            try
                            {
                                HOperatorSet.GetContourXld(contour, out HTuple rows, out HTuple cols);
                                int length = Math.Min(rows.Length, cols.Length);
                                for (int index = 0; index < length; index++)
                                {
                                    points.Add(new Point(cols[index].D, rows[index].D));
                                }
                            }
                            finally
                            {
                                contour.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        edges.Dispose();
                    }
                }
                finally
                {
                    image.Dispose();
                }
            }
            finally
            {
                handle.Free();
            }

            return points;
        }

        private static bool TryFitCircleLeastSquares(IReadOnlyList<Point> points, out double centerX, out double centerY, out double radius)
        {
            centerX = 0;
            centerY = 0;
            radius = 0;

            if (points == null || points.Count < 3)
                return false;

            double sumXX = 0;
            double sumXY = 0;
            double sumYY = 0;
            double sumX = 0;
            double sumY = 0;
            double sumXz = 0;
            double sumYz = 0;
            double sumZ = 0;

            foreach (Point point in points)
            {
                double x = point.X;
                double y = point.Y;
                double z = x * x + y * y;

                sumXX += x * x;
                sumXY += x * y;
                sumYY += y * y;
                sumX += x;
                sumY += y;
                sumXz += x * z;
                sumYz += y * z;
                sumZ += z;
            }

            double[,] matrix =
            {
                { sumXX, sumXY, sumX, -sumXz },
                { sumXY, sumYY, sumY, -sumYz },
                { sumX,  sumY,  points.Count, -sumZ }
            };

            if (!TrySolveLinearEquation3x3(matrix, out double a, out double b, out double c))
                return false;

            centerX = -a * 0.5;
            centerY = -b * 0.5;
            double radiusSquared = centerX * centerX + centerY * centerY - c;
            if (radiusSquared <= 0 || double.IsNaN(radiusSquared) || double.IsInfinity(radiusSquared))
                return false;

            radius = Math.Sqrt(radiusSquared);
            return !(double.IsNaN(radius) || double.IsInfinity(radius));
        }

        private static bool TrySolveLinearEquation3x3(double[,] matrix, out double x, out double y, out double z)
        {
            x = 0;
            y = 0;
            z = 0;

            for (int pivot = 0; pivot < 3; pivot++)
            {
                int maxRow = pivot;
                double maxValue = Math.Abs(matrix[pivot, pivot]);
                for (int row = pivot + 1; row < 3; row++)
                {
                    double candidate = Math.Abs(matrix[row, pivot]);
                    if (candidate > maxValue)
                    {
                        maxValue = candidate;
                        maxRow = row;
                    }
                }

                if (maxValue < 1e-10)
                    return false;

                if (maxRow != pivot)
                {
                    for (int column = pivot; column < 4; column++)
                    {
                        (matrix[pivot, column], matrix[maxRow, column]) = (matrix[maxRow, column], matrix[pivot, column]);
                    }
                }

                double pivotValue = matrix[pivot, pivot];
                for (int column = pivot; column < 4; column++)
                {
                    matrix[pivot, column] /= pivotValue;
                }

                for (int row = 0; row < 3; row++)
                {
                    if (row == pivot)
                        continue;

                    double factor = matrix[row, pivot];
                    for (int column = pivot; column < 4; column++)
                    {
                        matrix[row, column] -= factor * matrix[pivot, column];
                    }
                }
            }

            x = matrix[0, 3];
            y = matrix[1, 3];
            z = matrix[2, 3];
            return true;
        }

        private static double Distance(double x, double y, double centerX, double centerY)
        {
            double dx = x - centerX;
            double dy = y - centerY;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static Geometry CreateCircleEdgeOverlayGeometry(IReadOnlyList<Point> overlayPoints, double previewZoom)
        {
            if (overlayPoints == null || overlayPoints.Count == 0 || previewZoom <= 0)
                return null;

            double pointRadius = Math.Clamp(previewZoom * 0.18, 0.8, 1.8);
            var geometryGroup = new GeometryGroup();

            foreach (Point point in overlayPoints)
            {
                geometryGroup.Children.Add(new EllipseGeometry(
                    new Point(point.X * previewZoom, point.Y * previewZoom),
                    pointRadius,
                    pointRadius));
            }

            geometryGroup.Freeze();
            return geometryGroup;
        }

        private static byte[] ExtractGrayPixels(BitmapSource source, out int width, out int height)
        {
            var gray = new FormatConvertedBitmap();
            gray.BeginInit();
            gray.Source = source;
            gray.DestinationFormat = PixelFormats.Gray8;
            gray.EndInit();
            gray.Freeze();

            width = gray.PixelWidth;
            height = gray.PixelHeight;
            byte[] pixels = new byte[width * height];
            gray.CopyPixels(pixels, width, 0);
            return pixels;
        }

        private static byte[] CreateGrayBackground(byte[] grayPixels, int width, int height)
        {
            byte[] bgra = new byte[width * height * 4];
            for (int i = 0; i < grayPixels.Length; i++)
            {
                int pixelIndex = i * 4;
                byte gray = grayPixels[i];
                bgra[pixelIndex] = gray;
                bgra[pixelIndex + 1] = gray;
                bgra[pixelIndex + 2] = gray;
                bgra[pixelIndex + 3] = 255;
            }

            return bgra;
        }

        private static BitmapSource CreateGrayBitmap(byte[] pixels, int width, int height)
        {
            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixels, width);
            bitmap.Freeze();
            return bitmap;
        }

        private static BitmapSource CreateBgraBitmap(byte[] pixels, int width, int height)
        {
            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            bitmap.Freeze();
            return bitmap;
        }

        private static BitmapSource EnsureBgra32(BitmapSource source)
        {
            var converted = new FormatConvertedBitmap();
            converted.BeginInit();
            converted.Source = source;
            converted.DestinationFormat = PixelFormats.Bgra32;
            converted.EndInit();
            converted.Freeze();
            return converted;
        }

        private static string GetSharpenLevelText(PhysicalScaleSharpenLevel level)
        {
            return level switch
            {
                PhysicalScaleSharpenLevel.Weak => "弱",
                PhysicalScaleSharpenLevel.Medium => "中",
                PhysicalScaleSharpenLevel.Strong => "强",
                _ => "中"
            };
        }

        private static double GetDefaultPreviewZoom(BitmapSource source)
        {
            return 1.0;
        }

        private static double ResolveNextPreviewZoomIn(double currentZoom)
        {
            double normalized = Math.Clamp(currentZoom, MinPreviewZoom, MaxPreviewZoom);
            if (normalized < 1.0 - PreviewZoomCompareTolerance)
            {
                foreach (double preset in PreviewZoomPresetLevels)
                {
                    if (preset > normalized + PreviewZoomCompareTolerance)
                        return preset;
                }

                return 1.0;
            }

            return Math.Clamp(normalized * PreviewZoomStepFactor, MinPreviewZoom, MaxPreviewZoom);
        }

        private static double ResolveNextPreviewZoomOut(double currentZoom)
        {
            double normalized = Math.Clamp(currentZoom, MinPreviewZoom, MaxPreviewZoom);
            if (normalized <= 1.0 + PreviewZoomCompareTolerance)
            {
                for (int i = PreviewZoomPresetLevels.Length - 1; i >= 0; i--)
                {
                    double preset = PreviewZoomPresetLevels[i];
                    if (normalized > preset + PreviewZoomCompareTolerance)
                        return preset;

                    if (Math.Abs(normalized - preset) <= PreviewZoomCompareTolerance && i > 0)
                        return PreviewZoomPresetLevels[i - 1];
                }

                return MinPreviewZoom;
            }

            double stepped = normalized / PreviewZoomStepFactor;
            return stepped >= 1.0 ? stepped : 1.0;
        }

        private void UpdateGrayProfiles(BitmapSource bitmap)
        {
            if (bitmap == null)
            {
                _displayBitmap = null;
                ClearGrayProfiles();
                return;
            }

            byte[] grayPixels = ExtractGrayPixels(bitmap, out int width, out int height);
            if (grayPixels.Length == 0 || width <= 0 || height <= 0)
            {
                ClearGrayProfiles();
                return;
            }

            EnsureSampleGuideInBounds(bitmap);
            Int32Rect viewport = NormalizeViewport(bitmap);

            int viewportLeft = viewport.X;
            int viewportTop = viewport.Y;
            int viewportRight = Math.Min(width, viewport.X + viewport.Width);
            int viewportBottom = Math.Min(height, viewport.Y + viewport.Height);

            if (viewportRight <= viewportLeft || viewportBottom <= viewportTop)
            {
                ClearGrayProfiles();
                return;
            }

            double[] horizontalProfile = new double[Math.Max(1, viewportRight - viewportLeft)];
            double[] verticalProfile = new double[Math.Max(1, viewportBottom - viewportTop)];
            int grayMin = 255;
            int grayMax = 0;
            double total = 0;
            double totalSquares = 0;
            int sampleCount = 0;
            int sampleRow = Math.Clamp(_sampleRow, viewportTop, viewportBottom - 1);
            int sampleColumn = Math.Clamp(_sampleColumn, viewportLeft, viewportRight - 1);
            int rowStart = sampleRow * width;

            for (int x = viewportLeft; x < viewportRight; x++)
            {
                byte gray = grayPixels[rowStart + x];
                horizontalProfile[x - viewportLeft] = gray;
                grayMin = Math.Min(grayMin, gray);
                grayMax = Math.Max(grayMax, gray);
                total += gray;
                totalSquares += gray * gray;
                sampleCount++;
            }

            for (int y = viewportTop; y < viewportBottom; y++)
            {
                byte gray = grayPixels[y * width + sampleColumn];
                verticalProfile[y - viewportTop] = gray;
                if (y == sampleRow)
                    continue;

                grayMin = Math.Min(grayMin, gray);
                grayMax = Math.Max(grayMax, gray);
                total += gray;
                totalSquares += gray * gray;
                sampleCount++;
            }

            double average = sampleCount == 0 ? 0 : total / sampleCount;
            double variance = sampleCount == 0 ? 0 : Math.Max(0, totalSquares / sampleCount - average * average);

            GrayMinValue = grayMin;
            GrayMaxValue = grayMax;
            GrayAverageValue = average;
            GrayStdDevValue = Math.Sqrt(variance);
            HorizontalProfileFillGeometry = CreateHorizontalProfileFillGeometry(horizontalProfile);
            HorizontalProfileLineGeometry = CreateHorizontalProfileLineGeometry(horizontalProfile);
            VerticalProfileFillGeometry = CreateVerticalProfileFillGeometry(verticalProfile);
            VerticalProfileLineGeometry = CreateVerticalProfileLineGeometry(verticalProfile);
            RaisePropertyChanged(nameof(GrayProfileVisibility));
            RaiseSampleGuideChanged();
        }

        private Int32Rect NormalizeViewport(BitmapSource bitmap)
        {
            if (bitmap == null)
            {
                _currentViewport = null;
                return new Int32Rect(0, 0, 0, 0);
            }

            if (_currentViewport == null)
            {
                _currentViewport = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
                return _currentViewport.Value;
            }

            Int32Rect viewport = _currentViewport.Value;
            int x = Math.Clamp(viewport.X, 0, Math.Max(0, bitmap.PixelWidth - 1));
            int y = Math.Clamp(viewport.Y, 0, Math.Max(0, bitmap.PixelHeight - 1));
            int width = Math.Clamp(viewport.Width, 1, Math.Max(1, bitmap.PixelWidth - x));
            int height = Math.Clamp(viewport.Height, 1, Math.Max(1, bitmap.PixelHeight - y));
            _currentViewport = new Int32Rect(x, y, width, height);
            return _currentViewport.Value;
        }

        private void ClearGrayProfiles()
        {
            _displayBitmap = null;
            GrayMinValue = 0;
            GrayMaxValue = 0;
            GrayAverageValue = 0;
            GrayStdDevValue = 0;
            HorizontalProfileFillGeometry = null;
            HorizontalProfileLineGeometry = null;
            VerticalProfileFillGeometry = null;
            VerticalProfileLineGeometry = null;
            RaisePropertyChanged(nameof(GrayProfileVisibility));
            RaiseSampleGuideChanged();
        }

        private void ResetSampleGuideToCenter(BitmapSource bitmap)
        {
            if (bitmap == null)
            {
                _sampleColumn = 0;
                _sampleRow = 0;
                _sampleGuideInitialized = false;
                RaiseSampleGuideChanged();
                return;
            }

            _sampleColumn = Math.Clamp(bitmap.PixelWidth / 2, 0, Math.Max(0, bitmap.PixelWidth - 1));
            _sampleRow = Math.Clamp(bitmap.PixelHeight / 2, 0, Math.Max(0, bitmap.PixelHeight - 1));
            _sampleGuideInitialized = true;
            RaiseSampleGuideChanged();
        }

        private void EnsureSampleGuideInBounds(BitmapSource bitmap)
        {
            if (bitmap == null)
            {
                _sampleGuideInitialized = false;
                return;
            }

            if (!_sampleGuideInitialized)
            {
                ResetSampleGuideToCenter(bitmap);
                return;
            }

            int clampedColumn = Math.Clamp(_sampleColumn, 0, Math.Max(0, bitmap.PixelWidth - 1));
            int clampedRow = Math.Clamp(_sampleRow, 0, Math.Max(0, bitmap.PixelHeight - 1));
            if (clampedColumn == _sampleColumn && clampedRow == _sampleRow)
                return;

            _sampleColumn = clampedColumn;
            _sampleRow = clampedRow;
            RaiseSampleGuideChanged();
        }

        private void RaiseSampleGuideChanged()
        {
            RaisePropertyChanged(nameof(SampleVerticalLineDisplayX));
            RaisePropertyChanged(nameof(SampleHorizontalLineDisplayY));
            RaisePropertyChanged(nameof(SampleGuideVisibility));
        }

        private static Geometry CreateHorizontalProfileFillGeometry(double[] profile)
        {
            return CreateProfileGeometry(profile, vertical: false, filled: true);
        }

        private static Geometry CreateHorizontalProfileLineGeometry(double[] profile)
        {
            return CreateProfileGeometry(profile, vertical: false, filled: false);
        }

        private static Geometry CreateVerticalProfileFillGeometry(double[] profile)
        {
            return CreateProfileGeometry(profile, vertical: true, filled: true);
        }

        private static Geometry CreateVerticalProfileLineGeometry(double[] profile)
        {
            return CreateProfileGeometry(profile, vertical: true, filled: false);
        }

        private static Geometry CreateProfileGeometry(double[] profile, bool vertical, bool filled)
        {
            if (profile == null || profile.Length == 0)
                return null;

            int count = profile.Length;
            var geometry = new StreamGeometry();

            using (StreamGeometryContext context = geometry.Open())
            {
                if (vertical)
                {
                    context.BeginFigure(new Point(0, 0), filled, filled);

                    for (int i = 0; i < count; i++)
                    {
                        context.LineTo(CreateVerticalProfilePoint(profile[i], i, count), true, false);
                    }

                    if (filled)
                    {
                        context.LineTo(new Point(0, 1), true, false);
                    }
                }
                else
                {
                    context.BeginFigure(new Point(0, 1), filled, filled);

                    for (int i = 0; i < count; i++)
                    {
                        context.LineTo(CreateHorizontalProfilePoint(profile[i], i, count), true, false);
                    }

                    if (filled)
                    {
                        context.LineTo(new Point(1, 1), true, false);
                    }
                }
            }

            geometry.Freeze();
            return geometry;
        }

        private static Point CreateHorizontalProfilePoint(double grayValue, int index, int count)
        {
            double x = count <= 1 ? 0 : index / (double)(count - 1);
            double y = 1.0 - Math.Clamp(grayValue / 255.0, 0, 1);
            return new Point(x, y);
        }

        private static Point CreateVerticalProfilePoint(double grayValue, int index, int count)
        {
            double x = Math.Clamp(grayValue / 255.0, 0, 1);
            double y = count <= 1 ? 0 : index / (double)(count - 1);
            return new Point(x, y);
        }
    }
}
