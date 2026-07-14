using Custom.CalibrationPlateMeasure.Algorithms;
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Share;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace Custom.CalibrationPlateMeasure.Models
{
    // 标准片测量运行模型：保存参数、调用算法、缓存显示图像和测量结果。
    public sealed class CalibrationPlateMeasureModel : ModelParamBase
    {
        private const string DisplayContourColor = "cyan";

        private HObject? _displayImage;
        private HObject? _displayContours;
        private BitmapSource? _displayImageSource;
        private double[] _heightValues = [];
        private int _heightValueWidth;
        private int _heightValueHeight;
        private double _displayPixelSizeX;
        private double _displayPixelSizeY;
        private double _displayPixelSizeZ;
        private string? _runtimeModuleKey;
        private string? _windowControlKey;
        private bool _hasMeasuredSelectedRoi;
        private string _measuredRoiImagePath = string.Empty;
        private CalibrationPlateMeasurementMode _measuredRoiMode;
        private double _measuredRoiRow1;
        private double _measuredRoiColumn1;
        private double _measuredRoiRow2;
        private double _measuredRoiColumn2;
        private bool _isDisposed;

        public CalibrationPlateMeasureModel()
        {
            TriggerModuleRun = () => ExecuteModule().Result;
        }

        [JsonIgnore]
        public override VMHWindowControl mWindowH { get; set; } = null!;

        [JsonIgnore]
        public List<HRoi> mHRoi { get; } = [];

        [JsonIgnore]
        public string RuntimeModuleKey
        {
            get
            {
                if (Serial >= 0)
                {
                    return Serial.ToString("D3");
                }

                if (string.IsNullOrWhiteSpace(_runtimeModuleKey))
                {
                    _runtimeModuleKey = $"CalibrationPlateMeasure_{Guid.NewGuid():N}";
                }

                return _runtimeModuleKey;
            }
        }

        private string _imagePath = string.Empty;
        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        private CalibrationPlateMeasurementMode _measurementMode = CalibrationPlateMeasurementMode.Groove;
        [RecipeParam("MeasurementMode", "测量类型")]
        public CalibrationPlateMeasurementMode MeasurementMode
        {
            get => _measurementMode;
            set => SetProperty(ref _measurementMode, value);
        }

        private double _intervalX = 0.00375;
        [RecipeParam("IntervalX", "X 方向像素间距")]
        public double IntervalX
        {
            get => _intervalX;
            set => SetProperty(ref _intervalX, value);
        }

        private double _intervalY = 0.00381;
        [RecipeParam("IntervalY", "Y 方向像素间距")]
        public double IntervalY
        {
            get => _intervalY;
            set => SetProperty(ref _intervalY, value);
        }

        private double _intervalZ = 0.001;
        [RecipeParam("IntervalZ", "Z 换算系数")]
        public double IntervalZ
        {
            get => _intervalZ;
            set => SetProperty(ref _intervalZ, value);
        }

        private double _depthExpand = 10.0;
        [RecipeParam("DepthExpand", "深度区域外扩像素")]
        public double DepthExpand
        {
            get => _depthExpand;
            set => SetProperty(ref _depthExpand, value);
        }

        private bool _hasSelectedRoi;
        [RecipeParam("HasSelectedRoi", "是否已框选目标区域")]
        public bool HasSelectedRoi
        {
            get => _hasSelectedRoi;
            set => SetProperty(ref _hasSelectedRoi, value);
        }

        private double _selectedRoiRow1;
        [RecipeParam("SelectedRoiRow1", "框选区域上边界")]
        public double SelectedRoiRow1
        {
            get => _selectedRoiRow1;
            set => SetProperty(ref _selectedRoiRow1, value);
        }

        private double _selectedRoiColumn1;
        [RecipeParam("SelectedRoiColumn1", "框选区域左边界")]
        public double SelectedRoiColumn1
        {
            get => _selectedRoiColumn1;
            set => SetProperty(ref _selectedRoiColumn1, value);
        }

        private double _selectedRoiRow2;
        [RecipeParam("SelectedRoiRow2", "框选区域下边界")]
        public double SelectedRoiRow2
        {
            get => _selectedRoiRow2;
            set => SetProperty(ref _selectedRoiRow2, value);
        }

        private double _selectedRoiColumn2;
        [RecipeParam("SelectedRoiColumn2", "框选区域右边界")]
        public double SelectedRoiColumn2
        {
            get => _selectedRoiColumn2;
            set => SetProperty(ref _selectedRoiColumn2, value);
        }

        private double _circleMinContourLength = 5.0;
        [RecipeParam("CircleMinContourLength", "圆测量最小轮廓长度")]
        public double CircleMinContourLength
        {
            get => _circleMinContourLength;
            set => SetProperty(ref _circleMinContourLength, value);
        }

        private double _circleMinRadiusPx = 1.0;
        [RecipeParam("CircleMinRadiusPx", "圆测量最小半径")]
        public double CircleMinRadiusPx
        {
            get => _circleMinRadiusPx;
            set => SetProperty(ref _circleMinRadiusPx, value);
        }

        private double _circleDepthExpandPx;
        [RecipeParam("CircleDepthExpandPx", "圆测量深度区域外扩像素")]
        public double CircleDepthExpandPx
        {
            get => _circleDepthExpandPx;
            set => SetProperty(ref _circleDepthExpandPx, value);
        }

        [JsonIgnore]
        private string _statusText = "请选择 TIFF 高度图后执行测量。";
        [JsonIgnore]
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        [JsonIgnore]
        public ObservableCollection<CalibrationPlateMeasureItem> MeasureItems { get; } = [];

        [JsonIgnore]
        public BitmapSource? DisplayImageSource
        {
            get => _displayImageSource;
            private set => SetProperty(ref _displayImageSource, value);
        }

        private string _cursorCoordinateText = "X: --  Y: --  Z: --";
        [JsonIgnore]
        public string CursorCoordinateText
        {
            get => _cursorCoordinateText;
            private set => SetProperty(ref _cursorCoordinateText, value);
        }

        public override bool OnceInit()
        {
            bool result = base.OnceInit();
            _isDisposed = false;
            EnsureWindowControlInitialized();
            return result;
        }

        public override bool LoadKeyParam()
        {
            if (!base.LoadKeyParam())
            {
                return false;
            }

            EnsureWindowControlInitialized();
            return true;
        }

        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (status, runTime) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    List<CalibrationPlateMeasureItem> existingItems = MeasureItems.ToList();
                    LoadKeyParam();
                    if (MeasureItems.Count == 0 && existingItems.Count > 0)
                    {
                        foreach (CalibrationPlateMeasureItem item in existingItems)
                        {
                            MeasureItems.Add(item);
                        }
                    }

                    ValidateImagePath();
                    if (!HasSelectedRoi)
                    {
                        StatusText = "测量失败：请先在灰度图中框选目标区域。";
                        return NodeStatus.Error;
                    }

                    if (IsSelectedRoiAlreadyMeasured())
                    {
                        StatusText = "当前框选区域已完成测量，请重新框选目标区域后再执行。";
                        return NodeStatus.Success;
                    }

                    PrepareMeasurementRun();

                    HOperatorSet.ReadImage(out HObject inputImage, ImagePath);
                    using (inputImage)
                    using (CalibrationPlateMeasureResult result = Measure(inputImage))
                    {
                        ApplyResult(result);
                        if (result.Items.Count == 0)
                        {
                            StatusText = "测量失败：目标区域已提取，但测量计算失败。";
                            return NodeStatus.Error;
                        }

                        MarkSelectedRoiMeasured();
                    }

                    StatusText = $"{GetMeasurementModeName()}完成，目标数量：{MeasureItems.Count}";
                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    ResetRuntimeState($"测量失败：{ex.Message}");
                    return NodeStatus.Error;
                }
            });

            Output = new ExecuteModuleOutput
            {
                RunStatus = status,
                RunTime = runTime
            };

            RaisePropertyChanged(nameof(Output));
            return Task.FromResult(Output);
        }

        public void ResetRuntimeState(string statusText)
        {
            StatusText = statusText;
            MeasureItems.Clear();
            ClearDisplayData();
            ClearHalconWindow();
            ClearMeasuredSelectedRoi();
            DisposeDisplayObjects();
        }

        public void LoadInputPreview()
        {
            ValidateImagePath();
            HOperatorSet.ReadImage(out HObject inputImage, ImagePath);
            using (inputImage)
            using (HObject displayImage = CreatePreviewDisplayImage(inputImage, IntervalX, IntervalY, IntervalZ, out HObject heightImage, out double pixelSizeX, out double pixelSizeY, out double pixelSizeZ))
            using (heightImage)
            {
                DisposeDisplayObjects();
                HOperatorSet.CopyImage(displayImage, out HObject imageCopy);
                _displayImage = imageCopy;
                HOperatorSet.GenEmptyObj(out HObject emptyContours);
                _displayContours = emptyContours;
                MeasureItems.Clear();
                ClearMeasuredSelectedRoi();
                CacheHeightImageData(heightImage, pixelSizeX, pixelSizeY, pixelSizeZ);
                RefreshDisplayImageSource();
            }
        }

        public void SetSelectedRoi(double row1, double column1, double row2, double column2)
        {
            SelectedRoiRow1 = Math.Min(row1, row2);
            SelectedRoiColumn1 = Math.Min(column1, column2);
            SelectedRoiRow2 = Math.Max(row1, row2);
            SelectedRoiColumn2 = Math.Max(column1, column2);
            HasSelectedRoi = true;
            ClearMeasuredSelectedRoi();
            StatusText = $"已框选目标区域：Row {SelectedRoiRow1:F1}-{SelectedRoiRow2:F1}, Col {SelectedRoiColumn1:F1}-{SelectedRoiColumn2:F1}";
            RefreshDisplayImageSource();
        }

        public void ClearSelectedRoi()
        {
            HasSelectedRoi = false;
            SelectedRoiRow1 = 0;
            SelectedRoiColumn1 = 0;
            SelectedRoiRow2 = 0;
            SelectedRoiColumn2 = 0;
            ClearMeasuredSelectedRoi();
            RefreshDisplayImageSource();
        }

        public bool DeleteMeasureItem(CalibrationPlateMeasureItem? item)
        {
            if (item == null)
            {
                return false;
            }

            bool removed = MeasureItems.Remove(item);
            if (!removed)
            {
                CalibrationPlateMeasureItem? matchedItem = MeasureItems.FirstOrDefault(candidate => candidate.Index == item.Index);
                removed = matchedItem != null && MeasureItems.Remove(matchedItem);
            }

            if (!removed)
            {
                return false;
            }

            ReindexMeasureItems();
            RebuildDisplayContoursFromMeasureItems();
            RebuildDisplayRois();
            RefreshDisplayImageSource();
            ShowResultInHalconWindow();
            ClearMeasuredSelectedRoi();
            StatusText = $"已删除测量结果，剩余目标数量：{MeasureItems.Count}";
            return true;
        }

        private bool IsSelectedRoiAlreadyMeasured()
        {
            return _hasMeasuredSelectedRoi
                && string.Equals(_measuredRoiImagePath, ImagePath, StringComparison.OrdinalIgnoreCase)
                && _measuredRoiMode == MeasurementMode
                && AreSameCoordinate(_measuredRoiRow1, SelectedRoiRow1)
                && AreSameCoordinate(_measuredRoiColumn1, SelectedRoiColumn1)
                && AreSameCoordinate(_measuredRoiRow2, SelectedRoiRow2)
                && AreSameCoordinate(_measuredRoiColumn2, SelectedRoiColumn2);
        }

        private void MarkSelectedRoiMeasured()
        {
            _hasMeasuredSelectedRoi = true;
            _measuredRoiImagePath = ImagePath;
            _measuredRoiMode = MeasurementMode;
            _measuredRoiRow1 = SelectedRoiRow1;
            _measuredRoiColumn1 = SelectedRoiColumn1;
            _measuredRoiRow2 = SelectedRoiRow2;
            _measuredRoiColumn2 = SelectedRoiColumn2;
        }

        private void ClearMeasuredSelectedRoi()
        {
            _hasMeasuredSelectedRoi = false;
            _measuredRoiImagePath = string.Empty;
            _measuredRoiRow1 = 0;
            _measuredRoiColumn1 = 0;
            _measuredRoiRow2 = 0;
            _measuredRoiColumn2 = 0;
        }

        private static bool AreSameCoordinate(double left, double right)
        {
            return Math.Abs(left - right) < 0.000001;
        }

        private CalibrationPlateMeasureResult Measure(HObject inputImage)
        {
            // 鼠标框选区域是两种算法的共同入口，测量类型只决定后续边界拟合方式。
            CalibrationPlateMeasureRequest request = CreateRequest(inputImage);
            return MeasurementMode == CalibrationPlateMeasurementMode.Circle
                ? new CalibrationPlateCircleMeasureAlgorithm().Measure(request)
                : new CalibrationPlateMeasureAlgorithm().Measure(request);
        }

        private CalibrationPlateMeasureRequest CreateRequest(HObject inputImage)
        {
            return new CalibrationPlateMeasureRequest
            {
                InputImage = inputImage,
                IntervalX = IntervalX,
                IntervalY = IntervalY,
                IntervalZ = IntervalZ,
                DepthExpand = DepthExpand,
                HasSelectedRoi = HasSelectedRoi,
                SelectedRoiRow1 = SelectedRoiRow1,
                SelectedRoiColumn1 = SelectedRoiColumn1,
                SelectedRoiRow2 = SelectedRoiRow2,
                SelectedRoiColumn2 = SelectedRoiColumn2,
                CircleMinContourLength = CircleMinContourLength,
                CircleMinRadiusPx = CircleMinRadiusPx,
                CircleDepthExpandPx = CircleDepthExpandPx
            };
        }

        private static HObject CreatePreviewDisplayImage(
            HObject inputImage,
            double intervalX,
            double intervalY,
            double intervalZ,
            out HObject heightImage,
            out double pixelSizeX,
            out double pixelSizeY,
            out double pixelSizeZ)
        {
            var request = new CalibrationPlateMeasureRequest
            {
                InputImage = inputImage,
                IntervalX = intervalX,
                IntervalY = intervalY,
                IntervalZ = intervalZ
            };
            GetHdevScaleFactors(request, out double scaleX, out double scaleY, out pixelSizeX, out pixelSizeY, out pixelSizeZ);

            HObject validMask = new();
            HObject irregularRegion = new();
            HObject irregularRegion0 = new();
            HObject fullRectangle = new();
            HObject irregularMask = new();
            try
            {
                HOperatorSet.ZoomImageFactor(inputImage, out heightImage, scaleX, scaleY, "nearest_neighbor");
                HOperatorSet.Threshold(heightImage, out validMask, 0, 1000);
                HOperatorSet.GenEmptyObj(out irregularRegion);
                HOperatorSet.Threshold(heightImage, out irregularRegion0, 0, 0);
                HOperatorSet.ConcatObj(irregularRegion, irregularRegion0, out irregularRegion);
                HOperatorSet.GetImageSize(heightImage, out HTuple imgWidthTuple, out HTuple imgHeightTuple);
                HOperatorSet.GenRectangle1(out fullRectangle, 0, 0, imgHeightTuple.I, imgWidthTuple.I);
                HOperatorSet.Union1(irregularRegion, out irregularMask);
                HOperatorSet.Difference(fullRectangle, irregularMask, out fullRectangle);
                HOperatorSet.Intersection(validMask, fullRectangle, out validMask);
                HOperatorSet.ReduceDomain(heightImage, validMask, out heightImage);
                return CreateByteDisplayImage(heightImage, validMask);
            }
            finally
            {
                validMask.Dispose();
                irregularRegion.Dispose();
                irregularRegion0.Dispose();
                fullRectangle.Dispose();
                irregularMask.Dispose();
            }
        }

        private void ApplyResult(CalibrationPlateMeasureResult result)
        {
            foreach (CalibrationPlateMeasureItem item in result.Items)
            {
                item.Index = MeasureItems.Count + 1;
                MeasureItems.Add(item);
            }

            ReplaceDisplayObjects(result.DisplayImage, result.DisplayContours);
            CacheHeightImageData(result.HeightImage, result.DisplayPixelSizeX, result.DisplayPixelSizeY, result.DisplayPixelSizeZ);
            RefreshDisplayImageSource();
            ShowResultInHalconWindow();
        }

        private void ReplaceDisplayObjects(HObject displayImage, HObject displayContours)
        {
            DisposeDisplayObjects();
            HOperatorSet.CopyImage(displayImage, out HObject imageCopy);
            _displayImage = imageCopy;
            _displayContours = displayContours?.IsInitialized() == true
                ? displayContours.Clone()
                : null;
            RebuildDisplayContoursFromMeasureItems();
            RebuildDisplayRois();
        }

        private void PrepareMeasurementRun()
        {
            StatusText = $"正在执行{GetMeasurementModeName()}...";
        }

        private void ReindexMeasureItems()
        {
            List<CalibrationPlateMeasureItem> remainingItems = MeasureItems.ToList();
            MeasureItems.Clear();
            for (int i = 0; i < remainingItems.Count; i++)
            {
                remainingItems[i].Index = i + 1;
                MeasureItems.Add(remainingItems[i]);
            }
        }

        private void ClearDisplayData()
        {
            _heightValues = [];
            _heightValueWidth = 0;
            _heightValueHeight = 0;
            _displayPixelSizeX = 0;
            _displayPixelSizeY = 0;
            _displayPixelSizeZ = 0;
            DisplayImageSource = null;
            CursorCoordinateText = "X: --  Y: --  Z: --";
        }

        private void RefreshDisplayImageSource()
        {
            // WPF 图像用于交互显示，HALCON ROI 同时保留给运行模式的原始显示链路。
            DisplayImageSource = _displayImage == null || !_displayImage.IsInitialized()
                ? null
                : BuildDisplayImageSource(
                    _displayImage,
                    MeasureItems,
                    HasSelectedRoi,
                    SelectedRoiRow1,
                    SelectedRoiColumn1,
                    SelectedRoiRow2,
                    SelectedRoiColumn2);
        }

        private static BitmapSource? BuildDisplayImageSource(
            HObject displayImage,
            IEnumerable<CalibrationPlateMeasureItem> items,
            bool hasSelectedRoi,
            double selectedRoiRow1,
            double selectedRoiColumn1,
            double selectedRoiRow2,
            double selectedRoiColumn2)
        {
            if (displayImage == null || !displayImage.IsInitialized())
            {
                return null;
            }

            using Mat grayMat = ConvertHalconImageToGrayMat(displayImage);
            using Mat colorMat = new();
            Cv2.CvtColor(grayMat, colorMat, ColorConversionCodes.GRAY2BGR);

            Scalar lineColor = new(255, 255, 0);
            Scalar textColor = new(255, 255, 0);
            if (hasSelectedRoi)
            {
                DrawRectangle(colorMat, selectedRoiColumn1, selectedRoiRow1, selectedRoiColumn2, selectedRoiRow2, new Scalar(0, 255, 255));
            }

            foreach (CalibrationPlateMeasureItem item in items)
            {
                if (item.IsCircle)
                {
                    DrawCircle(colorMat, item.CircleCenterColumn, item.CircleCenterRow, item.CircleRadiusPx, lineColor);
                    DrawMeasureItemLabel(colorMat, item, textColor);
                    continue;
                }

                DrawLine(colorMat, item.UpperLeftColumn, item.UpperLeftRow, item.UpperRightColumn, item.UpperRightRow, lineColor);
                DrawLine(colorMat, item.UpperRightColumn, item.UpperRightRow, item.LowerRightColumn, item.LowerRightRow, lineColor);
                DrawLine(colorMat, item.LowerRightColumn, item.LowerRightRow, item.LowerLeftColumn, item.LowerLeftRow, lineColor);
                DrawLine(colorMat, item.LowerLeftColumn, item.LowerLeftRow, item.UpperLeftColumn, item.UpperLeftRow, lineColor);
                DrawLine(colorMat, item.DepthUpperLeftColumn, item.DepthUpperLeftRow, item.DepthUpperRightColumn, item.DepthUpperRightRow, lineColor);
                DrawLine(colorMat, item.DepthUpperRightColumn, item.DepthUpperRightRow, item.DepthLowerRightColumn, item.DepthLowerRightRow, lineColor);
                DrawLine(colorMat, item.DepthLowerRightColumn, item.DepthLowerRightRow, item.DepthLowerLeftColumn, item.DepthLowerLeftRow, lineColor);
                DrawLine(colorMat, item.DepthLowerLeftColumn, item.DepthLowerLeftRow, item.DepthUpperLeftColumn, item.DepthUpperLeftRow, lineColor);
                DrawMeasureItemLabel(colorMat, item, textColor);
            }

            BitmapSource bitmap = colorMat.ToBitmapSource();
            bitmap.Freeze();
            return bitmap;
        }

        private static Mat ConvertHalconImageToGrayMat(HObject image)
        {
            HOperatorSet.GetImagePointer1(image, out HTuple pointerTuple, out HTuple typeTuple, out HTuple widthTuple, out HTuple heightTuple);
            try
            {
                int width = widthTuple.I;
                int height = heightTuple.I;
                int pixelCount = checked(width * height);
                byte[] bytes = new byte[pixelCount];

                switch (typeTuple.S)
                {
                    case "byte":
                        Marshal.Copy(pointerTuple.IP, bytes, 0, pixelCount);
                        break;
                    default:
                        throw new NotSupportedException($"当前显示仅支持 byte 图像类型，实际类型: {typeTuple.S}。");
                }

                Mat mat = new(height, width, MatType.CV_8UC1);
                Marshal.Copy(bytes, 0, mat.Data, pixelCount);
                return mat;
            }
            finally
            {
                pointerTuple.Dispose();
                typeTuple.Dispose();
                widthTuple.Dispose();
                heightTuple.Dispose();
            }
        }

        private static HObject CreateByteDisplayImage(HObject sourceImage, HObject measureRegion)
        {
            HObject normalizedImage = new();
            try
            {
                HOperatorSet.MinMaxGray(measureRegion, sourceImage, 0, out HTuple minValueTuple, out HTuple maxValueTuple, out _);
                double minValue = minValueTuple.D;
                double maxValue = maxValueTuple.D;
                double range = Math.Max(maxValue - minValue, 1e-9);
                HOperatorSet.ScaleImage(sourceImage, out normalizedImage, 255.0 / range, -minValue * 255.0 / range);
                HOperatorSet.ConvertImageType(normalizedImage, out HObject displayImage, "byte");
                return displayImage;
            }
            finally
            {
                normalizedImage.Dispose();
            }
        }

        private static void GetHdevScaleFactors(
            CalibrationPlateMeasureRequest request,
            out double scaleX,
            out double scaleY,
            out double xp,
            out double yp,
            out double zp)
        {
            if (request.IntervalX < request.IntervalY)
            {
                scaleX = 1.0;
                scaleY = request.IntervalY / request.IntervalX;
            }
            else if (request.IntervalX > request.IntervalY)
            {
                scaleX = request.IntervalX / request.IntervalY;
                scaleY = 1.0;
            }
            else
            {
                scaleX = 1.0;
                scaleY = 1.0;
            }

            double intervalX = request.IntervalX / scaleX;
            double intervalY = request.IntervalY / scaleY;
            xp = (intervalX * request.AccelerationFactor) / scaleX;
            yp = (intervalY * request.AccelerationFactor) / scaleY;
            zp = request.IntervalZ;
        }

        private static void DrawLine(Mat image, double x1, double y1, double x2, double y2, Scalar color)
        {
            Cv2.Line(
                image,
                new OpenCvSharp.Point((int)Math.Round(x1), (int)Math.Round(y1)),
                new OpenCvSharp.Point((int)Math.Round(x2), (int)Math.Round(y2)),
                color,
                1,
                LineTypes.AntiAlias);
        }

        private static void DrawRectangle(Mat image, double x1, double y1, double x2, double y2, Scalar color)
        {
            DrawLine(image, x1, y1, x2, y1, color);
            DrawLine(image, x2, y1, x2, y2, color);
            DrawLine(image, x2, y2, x1, y2, color);
            DrawLine(image, x1, y2, x1, y1, color);
        }

        private static void DrawCircle(Mat image, double centerX, double centerY, double radius, Scalar color)
        {
            Cv2.Circle(
                image,
                new OpenCvSharp.Point((int)Math.Round(centerX), (int)Math.Round(centerY)),
                Math.Max(1, (int)Math.Round(radius)),
                color,
                1,
                LineTypes.AntiAlias);
        }

        private static void DrawMeasureItemLabel(Mat image, CalibrationPlateMeasureItem item, Scalar color)
        {
            string text = GetMeasureItemLabelText(item);
            OpenCvSharp.Point anchor = GetMeasureItemLabelAnchor(image, item, text);

            // OpenCV 默认字体不支持中文，灰度图叠加使用短英文单位标签保证现场显示稳定。
            Cv2.PutText(image, text, anchor, HersheyFonts.HersheySimplex, 0.42, new Scalar(0, 0, 0), 3, LineTypes.AntiAlias);
            Cv2.PutText(image, text, anchor, HersheyFonts.HersheySimplex, 0.42, color, 1, LineTypes.AntiAlias);
        }

        private static string GetMeasureItemLabelText(CalibrationPlateMeasureItem item)
        {
            return item.IsCircle
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "#{0} Dia:{1:F4} R:{2:F4} D:{3:F4}mm",
                    item.Index,
                    item.CircleDiameterMm,
                    item.CircleRadiusMm,
                    item.DepthMm)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "#{0} L:{1:F4} W:{2:F4} D:{3:F4}mm",
                    item.Index,
                    item.LengthMm,
                    item.WidthMm,
                    item.DepthMm);
        }

        private static OpenCvSharp.Point GetMeasureItemLabelAnchor(Mat image, CalibrationPlateMeasureItem item, string text)
        {
            GetMeasureItemBounds(item, out double minRow, out double minColumn, out double maxRow, out double maxColumn);
            Cv2.GetTextSize(text, HersheyFonts.HersheySimplex, 0.42, 1, out int baseline);

            int margin = 6;
            int textHeight = 12 + baseline;
            int textWidth = Math.Max(8, Cv2.GetTextSize(text, HersheyFonts.HersheySimplex, 0.42, 1, out _).Width);
            int x = ClampToImage((int)Math.Round(minColumn), 2, Math.Max(2, image.Width - textWidth - 2));

            int yAbove = (int)Math.Round(minRow) - margin;
            int yBelow = (int)Math.Round(maxRow) + textHeight + margin;
            int y = yAbove > textHeight
                ? yAbove
                : ClampToImage(yBelow, textHeight, Math.Max(textHeight, image.Height - 2));

            return new OpenCvSharp.Point(x, y);
        }

        private static void GetMeasureItemBounds(
            CalibrationPlateMeasureItem item,
            out double minRow,
            out double minColumn,
            out double maxRow,
            out double maxColumn)
        {
            if (item.IsCircle)
            {
                minRow = item.CircleCenterRow - item.CircleRadiusPx;
                maxRow = item.CircleCenterRow + item.CircleRadiusPx;
                minColumn = item.CircleCenterColumn - item.CircleRadiusPx;
                maxColumn = item.CircleCenterColumn + item.CircleRadiusPx;
                return;
            }

            minRow = Math.Min(Math.Min(item.UpperLeftRow, item.UpperRightRow), Math.Min(item.LowerLeftRow, item.LowerRightRow));
            maxRow = Math.Max(Math.Max(item.UpperLeftRow, item.UpperRightRow), Math.Max(item.LowerLeftRow, item.LowerRightRow));
            minColumn = Math.Min(Math.Min(item.UpperLeftColumn, item.UpperRightColumn), Math.Min(item.LowerLeftColumn, item.LowerRightColumn));
            maxColumn = Math.Max(Math.Max(item.UpperLeftColumn, item.UpperRightColumn), Math.Max(item.LowerLeftColumn, item.LowerRightColumn));
        }

        private static int ClampToImage(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        private string GetMeasurementModeName()
        {
            return MeasurementMode == CalibrationPlateMeasurementMode.Circle
                ? "圆测量"
                : "刻槽测量";
        }

        public void UpdateCursorCoordinateText(double imageColumn, double imageRow)
        {
            if (!TryGetDisplayCoordinate(imageColumn, imageRow, out double x, out double y, out double z, out double gray))
            {
                CursorCoordinateText = "X: --  Y: --  Z: --";
                return;
            }

            int column = (int)Math.Round(imageColumn);
            int row = (int)Math.Round(imageRow);
            CursorCoordinateText = $"X: {x:F4} mm  Y: {y:F4} mm  Z: {z:F4} mm  Gray: {gray:F3}  Col: {column}  Row: {row}";
        }

        public bool TryGetDisplayCoordinate(double imageColumn, double imageRow, out double x, out double y, out double z, out double gray)
        {
            x = 0;
            y = 0;
            z = 0;
            gray = 0;

            if (!double.IsFinite(imageColumn) || !double.IsFinite(imageRow))
            {
                return false;
            }

            int column = (int)Math.Round(imageColumn);
            int row = (int)Math.Round(imageRow);
            if (column < 0 || row < 0 || column >= _heightValueWidth || row >= _heightValueHeight)
            {
                return false;
            }

            int valueIndex = checked(row * _heightValueWidth + column);
            if (valueIndex < 0 || valueIndex >= _heightValues.Length)
            {
                return false;
            }

            gray = _heightValues[valueIndex];
            if (!double.IsFinite(gray))
            {
                return false;
            }

            x = column * _displayPixelSizeX;
            y = row * _displayPixelSizeY;
            z = gray * _displayPixelSizeZ;
            return true;
        }

        private void CacheHeightImageData(HObject heightImage, double pixelSizeX, double pixelSizeY, double pixelSizeZ)
        {
            if (heightImage == null || !heightImage.IsInitialized())
            {
                _heightValues = [];
                _heightValueWidth = 0;
                _heightValueHeight = 0;
                return;
            }

            HOperatorSet.GetImagePointer1(heightImage, out HTuple pointerTuple, out HTuple typeTuple, out HTuple widthTuple, out HTuple heightTuple);
            try
            {
                _heightValueWidth = widthTuple.I;
                _heightValueHeight = heightTuple.I;
                _displayPixelSizeX = pixelSizeX;
                _displayPixelSizeY = pixelSizeY;
                _displayPixelSizeZ = pixelSizeZ;
                _heightValues = CopyImageValues(pointerTuple.IP, checked(_heightValueWidth * _heightValueHeight), typeTuple.S);
            }
            finally
            {
                pointerTuple.Dispose();
                typeTuple.Dispose();
                widthTuple.Dispose();
                heightTuple.Dispose();
            }
        }

        private static double[] CopyImageValues(IntPtr pointer, int count, string pixelType)
        {
            double[] values = new double[count];
            switch (pixelType)
            {
                case "byte":
                    byte[] sourceByte = new byte[count];
                    Marshal.Copy(pointer, sourceByte, 0, count);
                    for (int i = 0; i < count; i++)
                    {
                        values[i] = sourceByte[i];
                    }

                    return values;

                case "int2":
                    short[] sourceInt16 = new short[count];
                    Marshal.Copy(pointer, sourceInt16, 0, count);
                    for (int i = 0; i < count; i++)
                    {
                        values[i] = sourceInt16[i];
                    }

                    return values;

                case "uint2":
                    byte[] sourceUInt16Bytes = new byte[count * sizeof(ushort)];
                    Marshal.Copy(pointer, sourceUInt16Bytes, 0, sourceUInt16Bytes.Length);
                    ushort[] sourceUInt16 = new ushort[count];
                    Buffer.BlockCopy(sourceUInt16Bytes, 0, sourceUInt16, 0, sourceUInt16Bytes.Length);
                    for (int i = 0; i < count; i++)
                    {
                        values[i] = sourceUInt16[i];
                    }

                    return values;

                case "int4":
                    int[] sourceInt32 = new int[count];
                    Marshal.Copy(pointer, sourceInt32, 0, count);
                    for (int i = 0; i < count; i++)
                    {
                        values[i] = sourceInt32[i];
                    }

                    return values;

                case "real":
                    float[] sourceFloat = new float[count];
                    Marshal.Copy(pointer, sourceFloat, 0, count);
                    for (int i = 0; i < count; i++)
                    {
                        values[i] = sourceFloat[i];
                    }

                    return values;

                default:
                    throw new NotSupportedException($"当前不支持 HALCON 像素类型 {pixelType}。");
            }
        }

        private void ShowResultInHalconWindow()
        {
            if (IsWindowControlDisposed(mWindowH))
            {
                EnsureWindowControlInitialized();
            }

            if (IsWindowControlDisposed(mWindowH) || _displayImage == null || !_displayImage.IsInitialized())
            {
                return;
            }

            try
            {
                mWindowH.HobjectToHimage(_displayImage);
                mWindowH.DispImageFitImage();
                mWindowH.showStatusBar();
                ShowHRoi();
            }
            catch (Exception ex)
            {
                StatusText = $"测量完成，但显示失败：{ex.Message}";
            }
        }

        private void ClearHalconWindow()
        {
            try
            {
                if (IsWindowControlDisposed(mWindowH) || !mWindowH.IsHandleCreated)
                {
                    return;
                }

                mWindowH?.ClearWindow();
            }
            catch
            {
            }
        }

        private void DisposeDisplayObjects()
        {
            ClearDisplayRois();

            try
            {
                _displayImage?.Dispose();
            }
            catch
            {
            }

            try
            {
                _displayContours?.Dispose();
            }
            catch
            {
            }

            _displayImage = null;
            _displayContours = null;
        }

        public void EnsureWindowControlInitialized()
        {
            string moduleKey = RuntimeModuleKey;
            if (string.IsNullOrWhiteSpace(moduleKey))
            {
                return;
            }

            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem?.ImgControlPair == null)
            {
                if (mWindowH == null || IsWindowControlDisposed(mWindowH))
                {
                    mWindowH = new VMHWindowControl();
                }
                return;
            }

            if (solutionItem.ImgControlPair.TryGetValue(moduleKey, out object? windowControl) &&
                windowControl is VMHWindowControl existingWindow)
            {
                if (IsWindowControlDisposed(existingWindow))
                {
                    solutionItem.ImgControlPair.Remove(moduleKey);
                    mWindowH = new VMHWindowControl();
                    solutionItem.ImgControlPair[moduleKey] = mWindowH;
                }
                else
                {
                    mWindowH = existingWindow;
                }

                _windowControlKey = moduleKey;
                return;
            }

            if (mWindowH == null || IsWindowControlDisposed(mWindowH))
            {
                mWindowH = new VMHWindowControl();
            }

            if (!string.IsNullOrWhiteSpace(_windowControlKey) &&
                _windowControlKey != moduleKey &&
                solutionItem.ImgControlPair.TryGetValue(_windowControlKey, out object? oldWindowControl) &&
                ReferenceEquals(oldWindowControl, mWindowH))
            {
                solutionItem.ImgControlPair.Remove(_windowControlKey);
            }

            solutionItem.ImgControlPair[moduleKey] = mWindowH;
            _windowControlKey = moduleKey;
        }

        private static bool IsWindowControlDisposed(VMHWindowControl? windowControl)
        {
            return windowControl == null
                || windowControl.IsDisposed
                || windowControl.getHWindowControl()?.IsDisposed == true;
        }

        public void ShowHRoi()
        {
            if (mWindowH == null)
            {
                return;
            }

            mWindowH.ClearROI();
            try
            {
                mWindowH.hControl?.HalconWindow?.SetLineWidth(1);
            }
            catch
            {
            }

            foreach (HRoi roi in mHRoi.Where(item => item.ModuleName == RuntimeModuleKey).ToList())
            {
                if (roi.roiType == HRoiType.文字显示 && roi is HText roiText)
                {
                    if (mWindowH.hControl == null)
                    {
                        continue;
                    }

                    ShowTool.SetFont(mWindowH.hControl.HalconWindow, roiText.size, "false", "false");
                    ShowTool.SetMsg(
                        mWindowH.hControl.HalconWindow,
                        roiText.text,
                        "image",
                        roiText.row,
                        roiText.col,
                        roiText.drawColor,
                        "false");
                    continue;
                }

                mWindowH.WindowH.DispHobject(roi.hobject, roi.drawColor, roi.IsFillDisp);
            }
        }

        public void ShowHRoi(HRoi roi)
        {
            try
            {
                int index = mHRoi.FindIndex(item => item.roiType == roi.roiType && item.ModuleName == roi.ModuleName);
                if (roi.fors)
                {
                    mHRoi.Add(roi);
                    return;
                }

                if (index > -1)
                {
                    DisposeRoiObject(mHRoi[index]);
                    mHRoi[index] = roi;
                }
                else
                {
                    mHRoi.Add(roi);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public override void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            string moduleKey = !string.IsNullOrWhiteSpace(_windowControlKey)
                ? _windowControlKey
                : RuntimeModuleKey;

            DisposeDisplayObjects();
            try
            {
                mWindowH?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放窗口控件失败：{ex.Message}");
            }
            finally
            {
                mWindowH = null!;
            }

            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem?.ImgControlPair != null && !string.IsNullOrWhiteSpace(moduleKey))
            {
                solutionItem.ImgControlPair.Remove(moduleKey);
            }

            _windowControlKey = null;
            _runtimeModuleKey = null;
            base.Dispose();
        }

        private void RebuildDisplayRois()
        {
            ClearDisplayRois();
            if (_displayContours == null || !_displayContours.IsInitialized())
            {
                return;
            }

            ShowHRoi(new HRoi(
                Serial,
                RuntimeModuleKey,
                string.Empty,
                HRoiType.检测结果,
                DisplayContourColor,
                _displayContours.Clone(),
                true,
                false));
        }

        private void RebuildDisplayContoursFromMeasureItems()
        {
            HObject rebuiltContours = CreateDisplayContoursFromMeasureItems(MeasureItems);
            try
            {
                _displayContours?.Dispose();
            }
            catch
            {
            }

            _displayContours = rebuiltContours;
        }

        private static HObject CreateDisplayContoursFromMeasureItems(IEnumerable<CalibrationPlateMeasureItem> items)
        {
            HOperatorSet.GenEmptyObj(out HObject contours);
            foreach (CalibrationPlateMeasureItem item in items)
            {
                if (item.IsCircle)
                {
                    HOperatorSet.GenCircleContourXld(
                        out HObject circleContour,
                        item.CircleCenterRow,
                        item.CircleCenterColumn,
                        Math.Max(1.0, item.CircleRadiusPx),
                        0,
                        Math.PI * 2.0,
                        "positive",
                        1.0);
                    AppendContourObject(ref contours, circleContour);
                    circleContour.Dispose();
                    continue;
                }

                HObject depthContour = CreatePolygonContour(
                    item.DepthUpperLeftRow,
                    item.DepthUpperLeftColumn,
                    item.DepthUpperRightRow,
                    item.DepthUpperRightColumn,
                    item.DepthLowerRightRow,
                    item.DepthLowerRightColumn,
                    item.DepthLowerLeftRow,
                    item.DepthLowerLeftColumn);
                AppendContourObject(ref contours, depthContour);
                depthContour.Dispose();

                HObject upperLine = CreateLineContour(item.UpperLeftRow, item.UpperLeftColumn, item.UpperRightRow, item.UpperRightColumn);
                HObject lowerLine = CreateLineContour(item.LowerLeftRow, item.LowerLeftColumn, item.LowerRightRow, item.LowerRightColumn);
                HObject leftLine = CreateLineContour(item.UpperLeftRow, item.UpperLeftColumn, item.LowerLeftRow, item.LowerLeftColumn);
                HObject rightLine = CreateLineContour(item.UpperRightRow, item.UpperRightColumn, item.LowerRightRow, item.LowerRightColumn);
                AppendContourObject(ref contours, upperLine);
                AppendContourObject(ref contours, lowerLine);
                AppendContourObject(ref contours, leftLine);
                AppendContourObject(ref contours, rightLine);
                upperLine.Dispose();
                lowerLine.Dispose();
                leftLine.Dispose();
                rightLine.Dispose();
            }

            return contours;
        }

        private static HObject CreateLineContour(double row1, double column1, double row2, double column2)
        {
            HOperatorSet.GenContourPolygonXld(
                out HObject contour,
                new HTuple(new[] { row1, row2 }),
                new HTuple(new[] { column1, column2 }));
            return contour;
        }

        private static HObject CreatePolygonContour(
            double row1,
            double column1,
            double row2,
            double column2,
            double row3,
            double column3,
            double row4,
            double column4)
        {
            HOperatorSet.GenContourPolygonXld(
                out HObject contour,
                new HTuple(new[] { row1, row2, row3, row4, row1 }),
                new HTuple(new[] { column1, column2, column3, column4, column1 }));
            return contour;
        }

        private static void AppendContourObject(ref HObject contours, HObject contour)
        {
            HOperatorSet.ConcatObj(contours, contour, out HObject combinedContours);
            contours.Dispose();
            contours = combinedContours;
        }

        private void ClearDisplayRois()
        {
            foreach (HRoi roi in mHRoi)
            {
                DisposeRoiObject(roi);
            }

            mHRoi.Clear();
        }

        private static void DisposeRoiObject(HRoi roi)
        {
            try
            {
                roi?.hobject?.Dispose();
            }
            catch
            {
            }
        }

        private void ValidateImagePath()
        {
            if (string.IsNullOrWhiteSpace(ImagePath))
            {
                throw new InvalidOperationException("请先选择 TIFF 高度图。");
            }

            if (!File.Exists(ImagePath))
            {
                throw new FileNotFoundException("图像文件不存在。", ImagePath);
            }
        }
    }
}
