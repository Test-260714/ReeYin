using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace Custom.PhysicalScale.Models
{
    public enum PhysicalScaleTool
    {
        Select,
        Point,
        Line,
        Rectangle,
        Circle
    }

    public enum PhysicalScaleSharpenLevel
    {
        Weak,
        Medium,
        Strong
    }

    public enum PhysicalScalePreviewProcessMode
    {
        Original,
        Sharpen,
        Binary,
        Edge,
        SubPixel
    }

    public enum PhysicalScaleMode
    {
        BasicMeasurement,
        MicroHoleJudgement
    }

    public enum PhysicalScaleHolePolarity
    {
        DarkHoleOnBrightBackground,
        BrightHoleOnDarkBackground
    }

    public enum PhysicalScaleHolePreviewMode
    {
        Original,
        Binary,
        Edge,
        SubPixelBoundary
    }

    public class PhysicalScaleMeasurement : BindableBase
    {
        private int _index;
        private string _displayName = string.Empty;
        private string _shapeTypeName = string.Empty;
        private string _pixelSummary = string.Empty;
        private string _physicalSummary = string.Empty;
        private string _detailText = string.Empty;

        public Guid Id { get; set; } = Guid.NewGuid();

        public PhysicalScaleTool Tool { get; set; }

        public double StartX { get; set; }

        public double StartY { get; set; }

        public double EndX { get; set; }

        public double EndY { get; set; }

        public double RadiusPx { get; set; }

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string ShapeTypeName
        {
            get => _shapeTypeName;
            set => SetProperty(ref _shapeTypeName, value);
        }

        public string PixelSummary
        {
            get => _pixelSummary;
            set => SetProperty(ref _pixelSummary, value);
        }

        public string PhysicalSummary
        {
            get => _physicalSummary;
            set => SetProperty(ref _physicalSummary, value);
        }

        public string DetailText
        {
            get => _detailText;
            set => SetProperty(ref _detailText, value);
        }
    }

    public class PhysicalScaleModel : BindableBase
    {
        private string _imagePath = string.Empty;
        private string _defectName = "未命名缺陷";
        private BitmapSource _loadedImage;
        private BitmapSource _displayImageSource;
        private int _imagePixelWidth;
        private int _imagePixelHeight;
        private int _displayImagePixelWidth;
        private int _displayImagePixelHeight;
        private double _fieldWidthMm = 200.0;
        private double _scaleX = 1.0;
        private double _scaleY = 1.0;
        private double _zoom = 1.0;
        private double _displayExposureCompensation;
        private string _statusText = "请先加载一张原始图像";
        private PhysicalScaleMode _selectedMode = PhysicalScaleMode.BasicMeasurement;
        private PhysicalScaleTool _selectedTool = PhysicalScaleTool.Line;
        private PhysicalScaleSharpenLevel _selectedSharpenLevel = PhysicalScaleSharpenLevel.Medium;
        private PhysicalScaleMeasurement _selectedMeasurement;
        private PhysicalScaleMeasurement _holeRoiMeasurement;
        private PhysicalScaleHolePreviewMode _holePreviewMode = PhysicalScaleHolePreviewMode.Original;
        private PhysicalScaleHolePolarity _holePolarity = PhysicalScaleHolePolarity.DarkHoleOnBrightBackground;
        private int _holeThreshold = 128;
        private double _holeMinDefectAreaPx = 8.0;
        private bool _holePreviewVisible;
        private string _holeJudgementText = "待判定";
        private double _holeOpenAreaPx;
        private double _holeOpenAreaMm2;
        private double _holeOpenDiameterPx;
        private double _holeOpenDiameterMm;
        private double _holeOpeningRatio;
        private int _holeDefectCount;
        private double _holeMaxDefectAreaPx;
        private double _holeMaxDefectAreaMm2;
        private double _holeTotalDefectAreaPx;
        private double _holeTotalDefectAreaMm2;
        private string _holePreviewSummary = "当前显示：原图";
        private string _holeReportDetail = "请先框选一个孔，再执行判定";

        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        public string DefectName
        {
            get => _defectName;
            set => SetProperty(ref _defectName, value);
        }

        public BitmapSource LoadedImage
        {
            get => _loadedImage;
            set => SetProperty(ref _loadedImage, value);
        }

        public BitmapSource DisplayImageSource
        {
            get => _displayImageSource;
            set => SetProperty(ref _displayImageSource, value);
        }

        public int ImagePixelWidth
        {
            get => _imagePixelWidth;
            set => SetProperty(ref _imagePixelWidth, value);
        }

        public int ImagePixelHeight
        {
            get => _imagePixelHeight;
            set => SetProperty(ref _imagePixelHeight, value);
        }

        public int DisplayImagePixelWidth
        {
            get => _displayImagePixelWidth;
            set => SetProperty(ref _displayImagePixelWidth, value);
        }

        public int DisplayImagePixelHeight
        {
            get => _displayImagePixelHeight;
            set => SetProperty(ref _displayImagePixelHeight, value);
        }

        public double FieldWidthMm
        {
            get => _fieldWidthMm;
            set => SetProperty(ref _fieldWidthMm, value);
        }

        public double ScaleX
        {
            get => _scaleX;
            set => SetProperty(ref _scaleX, value);
        }

        public double ScaleY
        {
            get => _scaleY;
            set => SetProperty(ref _scaleY, value);
        }

        public double Zoom
        {
            get => _zoom;
            set => SetProperty(ref _zoom, value);
        }

        public double DisplayExposureCompensation
        {
            get => _displayExposureCompensation;
            set => SetProperty(ref _displayExposureCompensation, Math.Clamp(value, -2.0, 2.0));
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public PhysicalScaleMode SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value);
        }

        public PhysicalScaleTool SelectedTool
        {
            get => _selectedTool;
            set => SetProperty(ref _selectedTool, value);
        }

        public PhysicalScaleSharpenLevel SelectedSharpenLevel
        {
            get => _selectedSharpenLevel;
            set => SetProperty(ref _selectedSharpenLevel, value);
        }

        public PhysicalScaleMeasurement SelectedMeasurement
        {
            get => _selectedMeasurement;
            set => SetProperty(ref _selectedMeasurement, value);
        }

        public PhysicalScaleMeasurement HoleRoiMeasurement
        {
            get => _holeRoiMeasurement;
            set => SetProperty(ref _holeRoiMeasurement, value);
        }

        public PhysicalScaleHolePreviewMode HolePreviewMode
        {
            get => _holePreviewMode;
            set => SetProperty(ref _holePreviewMode, value);
        }

        public PhysicalScaleHolePolarity HolePolarity
        {
            get => _holePolarity;
            set => SetProperty(ref _holePolarity, value);
        }

        public int HoleThreshold
        {
            get => _holeThreshold;
            set => SetProperty(ref _holeThreshold, value);
        }

        public double HoleMinDefectAreaPx
        {
            get => _holeMinDefectAreaPx;
            set => SetProperty(ref _holeMinDefectAreaPx, value);
        }

        public bool HolePreviewVisible
        {
            get => _holePreviewVisible;
            set => SetProperty(ref _holePreviewVisible, value);
        }

        public string HoleJudgementText
        {
            get => _holeJudgementText;
            set => SetProperty(ref _holeJudgementText, value);
        }

        public double HoleOpenAreaPx
        {
            get => _holeOpenAreaPx;
            set => SetProperty(ref _holeOpenAreaPx, value);
        }

        public double HoleOpenAreaMm2
        {
            get => _holeOpenAreaMm2;
            set => SetProperty(ref _holeOpenAreaMm2, value);
        }

        public double HoleOpenDiameterPx
        {
            get => _holeOpenDiameterPx;
            set => SetProperty(ref _holeOpenDiameterPx, value);
        }

        public double HoleOpenDiameterMm
        {
            get => _holeOpenDiameterMm;
            set => SetProperty(ref _holeOpenDiameterMm, value);
        }

        public double HoleOpeningRatio
        {
            get => _holeOpeningRatio;
            set => SetProperty(ref _holeOpeningRatio, value);
        }

        public int HoleDefectCount
        {
            get => _holeDefectCount;
            set => SetProperty(ref _holeDefectCount, value);
        }

        public double HoleMaxDefectAreaPx
        {
            get => _holeMaxDefectAreaPx;
            set => SetProperty(ref _holeMaxDefectAreaPx, value);
        }

        public double HoleMaxDefectAreaMm2
        {
            get => _holeMaxDefectAreaMm2;
            set => SetProperty(ref _holeMaxDefectAreaMm2, value);
        }

        public double HoleTotalDefectAreaPx
        {
            get => _holeTotalDefectAreaPx;
            set => SetProperty(ref _holeTotalDefectAreaPx, value);
        }

        public double HoleTotalDefectAreaMm2
        {
            get => _holeTotalDefectAreaMm2;
            set => SetProperty(ref _holeTotalDefectAreaMm2, value);
        }

        public string HolePreviewSummary
        {
            get => _holePreviewSummary;
            set => SetProperty(ref _holePreviewSummary, value);
        }

        public string HoleReportDetail
        {
            get => _holeReportDetail;
            set => SetProperty(ref _holeReportDetail, value);
        }

        public ObservableCollection<PhysicalScaleMeasurement> Measurements { get; set; } = new ObservableCollection<PhysicalScaleMeasurement>();
    }

    public class PhysicalScaleSharpenPreviewPayload
    {
        public BitmapSource PreviewImage { get; set; }

        public string ReportTitle { get; set; } = string.Empty;

        public string ReportMeta { get; set; } = string.Empty;

        public string ReportDetail { get; set; } = string.Empty;

        public string DefectName { get; set; } = string.Empty;

        public string ShapeName { get; set; } = string.Empty;

        public string ImageSummary { get; set; } = string.Empty;

        public string SharpenLevelText { get; set; } = string.Empty;

        public PhysicalScaleSharpenLevel SharpenLevel { get; set; } = PhysicalScaleSharpenLevel.Medium;

        public PhysicalScaleTool MeasurementTool { get; set; } = PhysicalScaleTool.Select;

        public double NominalCircleRadiusPx { get; set; }

        public double ScaleX { get; set; } = 1.0;

        public double ScaleY { get; set; } = 1.0;
    }
}
