using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Series3D;
using Arction.Wpf.ChartingMVVM.Views.View3D;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReeYin_V.UI.UserControls.PointCloudDisplay
{
    public sealed class PointCloudDisplayViewModel : BindableBase, IDisposable
    {
        private readonly PointCloudDisplayModel _model = new();
        private IReadOnlyList<PointCloudPointData> _loadedPoints = Array.Empty<PointCloudPointData>();
        private IReadOnlyList<PointCloudPointData> _displayPoints = Array.Empty<PointCloudPointData>();
        private PointCloudBounds _loadedBounds = new();
        private PointCloudBounds _bounds = new();
        private bool _hasIntensity;
        private int _sourcePointCount;
        private string _sourceName = "No data loaded";
        private PointCloudPlaneProjectionResult _planeProjection = new();

        private PointLineSeries3DCollection _seriesCollection = new();
        private string _statusText = "Select a point cloud file or load point data from code.";
        private string _displayModeText = "Display mode: Original point cloud";
        private string _rangeText = string.Empty;
        private string _sourceDescription = "No data loaded";
        private string _chartTitle = "Point Cloud Display";
        private bool _isBusy;
        private PointCloudColorSource _colorSource = PointCloudColorSource.ZAxis;
        private PointCloudPaletteType _paletteType = PointCloudPaletteType.Classic;
        private double _pointSize = 1.5d;
        private PointShape3D _pointShape = PointShape3D.Sphere;
        private bool _connectPoints = true;
        private bool _planeSmoothEnabled;
        private double _lineWidth = 1.1d;
        private ProjectionType _projectionType = ProjectionType.Perspective;
        private OrientationModes _cameraOrientationMode = OrientationModes.ZXY_Extrinsic;
        private double _rotationX = 20d;
        private double _rotationY = -25d;
        private double _rotationZ;
        private double _viewDistance = 180d;
        private double _axisXMin = -50d;
        private double _axisXMax = 50d;
        private double _axisYMin = -50d;
        private double _axisYMax = 50d;
        private double _axisZMin = -50d;
        private double _axisZMax = 50d;
        private double _dimensionWidth = 100d;
        private double _dimensionHeight = 100d;
        private double _dimensionDepth = 100d;

        public PointCloudDisplayViewModel()
        {
            Lights3D = View3D.CreateDefaultLights();
        }

        public Light3DCollection Lights3D { get; }

        public PointLineSeries3DCollection SeriesCollection
        {
            get => _seriesCollection;
            private set => SetProperty(ref _seriesCollection, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string RangeText
        {
            get => _rangeText;
            private set => SetProperty(ref _rangeText, value);
        }

        public string DisplayModeText
        {
            get => _displayModeText;
            private set => SetProperty(ref _displayModeText, value);
        }

        public string SourceDescription
        {
            get => _sourceDescription;
            private set => SetProperty(ref _sourceDescription, value);
        }

        public string ChartTitle
        {
            get => _chartTitle;
            private set => SetProperty(ref _chartTitle, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public PointCloudColorSource ColorSource
        {
            get => _colorSource;
            private set => SetProperty(ref _colorSource, value);
        }

        public PointCloudPaletteType PaletteType
        {
            get => _paletteType;
            private set => SetProperty(ref _paletteType, value);
        }

        public double PointSize
        {
            get => _pointSize;
            private set => SetProperty(ref _pointSize, value);
        }

        public PointShape3D PointShape
        {
            get => _pointShape;
            private set => SetProperty(ref _pointShape, value);
        }

        public bool ConnectPoints
        {
            get => _connectPoints;
            private set => SetProperty(ref _connectPoints, value);
        }

        public bool PlaneSmoothEnabled
        {
            get => _planeSmoothEnabled;
            private set => SetProperty(ref _planeSmoothEnabled, value);
        }

        public double LineWidth
        {
            get => _lineWidth;
            private set => SetProperty(ref _lineWidth, value);
        }

        public ProjectionType ProjectionType
        {
            get => _projectionType;
            set => SetProperty(ref _projectionType, value);
        }

        public OrientationModes CameraOrientationMode
        {
            get => _cameraOrientationMode;
            private set => SetProperty(ref _cameraOrientationMode, value);
        }

        public double RotationX
        {
            get => _rotationX;
            set => SetProperty(ref _rotationX, value);
        }

        public double RotationY
        {
            get => _rotationY;
            set => SetProperty(ref _rotationY, value);
        }

        public double RotationZ
        {
            get => _rotationZ;
            set => SetProperty(ref _rotationZ, value);
        }

        public double ViewDistance
        {
            get => _viewDistance;
            set => SetProperty(ref _viewDistance, value);
        }

        public double AxisXMin
        {
            get => _axisXMin;
            private set => SetProperty(ref _axisXMin, value);
        }

        public double AxisXMax
        {
            get => _axisXMax;
            private set => SetProperty(ref _axisXMax, value);
        }

        public double AxisYMin
        {
            get => _axisYMin;
            private set => SetProperty(ref _axisYMin, value);
        }

        public double AxisYMax
        {
            get => _axisYMax;
            private set => SetProperty(ref _axisYMax, value);
        }

        public double AxisZMin
        {
            get => _axisZMin;
            private set => SetProperty(ref _axisZMin, value);
        }

        public double AxisZMax
        {
            get => _axisZMax;
            private set => SetProperty(ref _axisZMax, value);
        }

        public double DimensionWidth
        {
            get => _dimensionWidth;
            private set => SetProperty(ref _dimensionWidth, value);
        }

        public double DimensionHeight
        {
            get => _dimensionHeight;
            private set => SetProperty(ref _dimensionHeight, value);
        }

        public double DimensionDepth
        {
            get => _dimensionDepth;
            private set => SetProperty(ref _dimensionDepth, value);
        }

        public async Task<bool> LoadFromFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                StatusText = "File path is empty.";
                return false;
            }

            IsBusy = true;
            StatusText = $"Loading point cloud: {Path.GetFileName(filePath)}";

            try
            {
                PointCloudLoadResult result = await Task.Run(() => _model.LoadFromFile(filePath));
                ApplyLoadResult(result);
                return true;
            }
            catch (Exception ex)
            {
                StatusText = $"Point cloud load failed: {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void LoadPoints(IEnumerable<PointCloudPointData> points, string sourceName = "Memory Data")
        {
            PointCloudLoadResult result = _model.LoadFromPoints(points, sourceName);
            ApplyLoadResult(result);
        }

        public void LoadPoints(
            double[] xValues,
            double[] yValues,
            double[] zValues,
            double[]? intensityValues = null,
            string sourceName = "Memory Data")
        {
            PointCloudLoadResult result = _model.LoadFromArrays(xValues, yValues, zValues, intensityValues, sourceName);
            ApplyLoadResult(result);
        }

        public void ApplyDisplaySettings(
            PointCloudColorSource colorSource,
            PointCloudPaletteType paletteType,
            PointShape3D pointShape,
            double pointSize,
            ProjectionType projectionType,
            bool connectPoints,
            double lineWidth,
            bool planeSmooth)
        {
            ColorSource = colorSource;
            PaletteType = paletteType;
            PointShape = pointShape;
            PointSize = Math.Max(0.2d, pointSize);
            ConnectPoints = connectPoints;
            LineWidth = Math.Max(0.2d, lineWidth);
            ProjectionType = projectionType;

            bool planeSmoothChanged = PlaneSmoothEnabled != planeSmooth;
            PlaneSmoothEnabled = planeSmooth;

            if (planeSmoothChanged)
            {
                RefreshDisplayData(resetView: false);
                return;
            }

            RefreshSeries();
        }

        public void ResetView()
        {
            RotationX = 20d;
            RotationY = -25d;
            RotationZ = 0d;
            ProjectionType = Arction.Wpf.ChartingMVVM.ProjectionType.Perspective;

            double width = Math.Max(_bounds.MaxX - _bounds.MinX, 1d);
            double height = Math.Max(_bounds.MaxY - _bounds.MinY, 1d);
            double depth = Math.Max(_bounds.MaxZ - _bounds.MinZ, 1d);
            double maxSpan = Math.Max(width, Math.Max(height, depth));
            ViewDistance = Math.Max(maxSpan * 2.2d, 20d);
        }

        public void SetTopView()
        {
            ProjectionType = Arction.Wpf.ChartingMVVM.ProjectionType.Orthographic;
            RotationX = 90d;
            RotationY = 0d;
            RotationZ = 0d;
        }

        private void ApplyLoadResult(PointCloudLoadResult result)
        {
            _loadedPoints = result.Points;
            _loadedBounds = result.Bounds;
            _hasIntensity = result.HasIntensity;
            _sourcePointCount = result.SourcePointCount;
            _sourceName = result.SourceName;

            ChartTitle = $"Point Cloud - {result.SourceName}";
            RefreshDisplayData(resetView: true);
        }

        private void RefreshDisplayData(bool resetView)
        {
            if (_loadedPoints.Count == 0)
            {
                _displayPoints = Array.Empty<PointCloudPointData>();
                _planeProjection = new PointCloudPlaneProjectionResult();
                _bounds = new PointCloudBounds();
                DisplayModeText = "Display mode: Original point cloud";
                RangeText = string.Empty;
                UpdateAxes();

                if (resetView)
                {
                    ResetView();
                }

                RefreshSeries();
                return;
            }

            if (PlaneSmoothEnabled)
            {
                _planeProjection = _model.ProjectToBestFitPlane(_loadedPoints);
                _displayPoints = _planeProjection.Points;
                _bounds = _planeProjection.Bounds;
                DisplayModeText =
                    $"Display mode: Plane smooth | Normal ({_planeProjection.NormalX:F3}, {_planeProjection.NormalY:F3}, {_planeProjection.NormalZ:F3}) | RMS {_planeProjection.RootMeanSquareDistance:F4}";
            }
            else
            {
                _planeProjection = new PointCloudPlaneProjectionResult();
                _displayPoints = _loadedPoints;
                _bounds = _loadedBounds;
                DisplayModeText = "Display mode: Original point cloud";
            }

            RangeText =
                $"X: {_bounds.MinX:F3} ~ {_bounds.MaxX:F3}    Y: {_bounds.MinY:F3} ~ {_bounds.MaxY:F3}    Z: {_bounds.MinZ:F3} ~ {_bounds.MaxZ:F3}";

            UpdateAxes();

            if (resetView)
            {
                ResetView();
            }

            RefreshSeries();
        }

        private void RefreshSeries()
        {
            if (_displayPoints.Count == 0)
            {
                SeriesCollection = new PointLineSeries3DCollection();
                StatusText = "No point data to display.";
                return;
            }

            PointCloudSeriesBuildResult buildResult = _model.BuildSeriesCollection(
                _displayPoints,
                ColorSource,
                PaletteType,
                _hasIntensity,
                PointShape,
                PointSize,
                ConnectPoints,
                LineWidth);

            SeriesCollection = buildResult.SeriesCollection;
            string displayMode = PlaneSmoothEnabled ? "Plane Smooth" : "Original";
            SourceDescription = ConnectPoints
                ? $"{_sourceName} | Source {_sourcePointCount:N0} | Display {_displayPoints.Count:N0} | Mode {displayMode} | Strips {buildResult.StripCount:N0}"
                : $"{_sourceName} | Source {_sourcePointCount:N0} | Display {_displayPoints.Count:N0} | Mode {displayMode}";

            string colorDescription = ColorSource switch
            {
                PointCloudColorSource.SingleColor => "Single color",
                PointCloudColorSource.XAxis => "Colored by X axis",
                PointCloudColorSource.YAxis => "Colored by Y axis",
                PointCloudColorSource.ZAxis => "Colored by Z axis",
                PointCloudColorSource.Intensity when _hasIntensity => "Colored by intensity",
                PointCloudColorSource.Intensity => "No intensity field, fallback to Z axis",
                _ => "Colored by Z axis"
            };

            if (ConnectPoints)
            {
                StatusText = buildResult.StripCount > 1
                    ? $"{colorDescription}. Displaying {_displayPoints.Count:N0} points in {buildResult.StripCount:N0} connected strips."
                    : $"{colorDescription}. Displaying {_displayPoints.Count:N0} points with sequential connection lines.";
            }
            else
            {
                StatusText = $"{colorDescription}. Displaying {_displayPoints.Count:N0} points.";
            }
        }

        private void UpdateAxes()
        {
            double paddingX = GetPadding(_bounds.MinX, _bounds.MaxX);
            double paddingY = GetPadding(_bounds.MinY, _bounds.MaxY);
            double paddingZ = GetPadding(_bounds.MinZ, _bounds.MaxZ);

            AxisXMin = _bounds.MinX - paddingX;
            AxisXMax = _bounds.MaxX + paddingX;
            AxisYMin = _bounds.MinY - paddingY;
            AxisYMax = _bounds.MaxY + paddingY;
            AxisZMin = _bounds.MinZ - paddingZ;
            AxisZMax = _bounds.MaxZ + paddingZ;

            DimensionWidth = Math.Max(_bounds.MaxX - _bounds.MinX, 1d);
            DimensionHeight = Math.Max(_bounds.MaxY - _bounds.MinY, 1d);
            DimensionDepth = Math.Max(_bounds.MaxZ - _bounds.MinZ, 1d);
        }

        private static double GetPadding(double min, double max)
        {
            double span = Math.Abs(max - min);
            if (span < double.Epsilon)
            {
                return 1d;
            }

            return Math.Max(span * 0.05d, 0.1d);
        }
        public void Dispose()
        {
            SeriesCollection = new PointLineSeries3DCollection();
        }
    }
}
