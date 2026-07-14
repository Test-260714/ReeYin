using Custom.PhysicalScale.Models;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Ioc;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Custom.PhysicalScale.Views;

namespace Custom.PhysicalScale.ViewModels
{
    public partial class PhysicalScaleViewModel : DialogViewModelBase, IViewModuleParam
    {
        public const double MinZoomLevel = 0.1;
        public const double MaxZoomLevel = 64.0;

        public PhysicalScaleViewModel()
        {
            Title = "物理标尺";
            Icon = "\ue6a2";
            Model.PropertyChanged += Model_PropertyChanged;
            _exposureRefreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            _exposureRefreshTimer.Tick += ExposureRefreshTimer_Tick;
        }

        public PhysicalScaleModel Model { get; } = new PhysicalScaleModel();

        public event Action FitRequested;

        public string ToolDisplayName => GetToolDisplayName();

        public string ImageSummary => Model.ImagePixelWidth > 0 && Model.ImagePixelHeight > 0
            ? $"{Model.ImagePixelWidth} x {Model.ImagePixelHeight} px"
            : "未加载图像";

        public string DisplayImageSummary => Model.DisplayImagePixelWidth > 0 && Model.DisplayImagePixelHeight > 0
            ? $"{Model.DisplayImagePixelWidth} x {Model.DisplayImagePixelHeight} px"
            : "未显示图像";

        public string CalibrationSummary => $"X: {Model.ScaleX:F4} mm/px   Y: {Model.ScaleY:F4} mm/px";

        public string FieldWidthSummary => $"当前按整幅宽度 {Model.FieldWidthMm:F1} mm 换算";

        public string MeasurementCountText => $"{Model.Measurements.Count} 项";

        public string CursorSummary => _cursorSummary;

        public string SharpenLevelSummary => GetSharpenLevelText(Model.SelectedSharpenLevel);

        public Visibility BasicModeVisibility => Model.SelectedMode == PhysicalScaleMode.BasicMeasurement
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility MicroHoleModeVisibility => Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility CanvasVisibility => Model.HolePreviewVisible ? Visibility.Collapsed : Visibility.Visible;

        public string ModeSummary => Model.SelectedMode == PhysicalScaleMode.BasicMeasurement
            ? "基础量测"
            : "微孔判定";

        public string CenterTitle => GetCenterTitle();

        public string CenterSubTitle => GetCenterSubTitle();

        public string CenterBadgeText => Model.SelectedMode == PhysicalScaleMode.BasicMeasurement
            ? MeasurementCountText
            : HoleJudgeDisplayText;

        public string SelectedMeasurementDetail => Model.SelectedMeasurement == null
            ? "请选择一项量测结果查看明细"
            : Model.SelectedMeasurement.DetailText;

        public string SelectedMeasurementReportTitle => Model.SelectedMeasurement == null
            ? $"局部量测报告 | {Model.DefectName}"
            : $"局部量测报告 | {Model.DefectName} | {Model.SelectedMeasurement.DisplayName}";

        public string SelectedMeasurementReportMeta => Model.SelectedMeasurement == null
            ? "等待选择量测对象"
            : $"{Model.SelectedMeasurement.ShapeTypeName} | {DateTime.Now:yyyy-MM-dd HH:mm}";

        public string HolePreviewModeText => GetHolePreviewModeText();

        public string HoleRoiSummary => GetHoleRoiSummary();

        public string HoleActionGuide => GetHoleActionGuide();

        public string HoleJudgeDisplayText => GetHoleJudgeDisplayText();

        public string HoleJudgeReasonText => GetHoleJudgeReasonText();

        public string HoleDiameterSummary => Model.HoleOpenDiameterPx > 0
            ? $"{Model.HoleOpenDiameterPx:F2} px / {Model.HoleOpenDiameterMm:F4} mm"
            : "--";

        public string HoleOpeningRatioSummary => Model.HoleOpeningRatio > 0
            ? $"{Model.HoleOpeningRatio * 100:F2}%"
            : "--";

        public string HoleDefectAreaSummary => Model.HoleTotalDefectAreaPx > 0
            ? $"{Model.HoleTotalDefectAreaPx:F1} px² / {Model.HoleTotalDefectAreaMm2:F4} mm²"
            : "0";

        public string HoleCountSummary => Model.HoleDefectCount > 0
            ? $"{Model.HoleDefectCount} 处"
            : "0 处";

        public string HoleJudgeResultTitle => $"微孔判定报告 | {Model.DefectName}";

        public string HoleJudgeResultMeta => $"{HoleJudgeDisplayText} | {HoleJudgeReasonText} | {DateTime.Now:yyyy-MM-dd HH:mm}";

        private string _cursorSummary = "游标：--";
        private BitmapSource _effectiveLoadedDisplayImage;
        private readonly DispatcherTimer _exposureRefreshTimer;

        public string ExposureSummary => $"{Model.DisplayExposureCompensation:+0.0;-0.0;0.0} EV";

        public DelegateCommand LoadImageCommand => new DelegateCommand(LoadImage);

        public DelegateCommand ClearMeasurementsCommand => new DelegateCommand(ClearMeasurements);

        public DelegateCommand DeleteSelectedCommand => new DelegateCommand(DeleteSelectedMeasurement);

        public DelegateCommand ExportCsvCommand => new DelegateCommand(ExportCsv);

        public DelegateCommand FitImageCommand => new DelegateCommand(() => FitRequested?.Invoke());

        public DelegateCommand ApplyFieldWidthCommand => new DelegateCommand(ApplyScaleFromFieldWidth);

        public DelegateCommand RefineSelectedCommand => new DelegateCommand(RefineSelectedMeasurement);

        public DelegateCommand<string> SetModeCommand => new DelegateCommand<string>(SetMode);

        public DelegateCommand PrepareHoleRoiCommand => new DelegateCommand(PrepareHoleRoi);

        public DelegateCommand ExecuteHoleJudgeCommand => new DelegateCommand(ExecuteMicroHoleJudgement);

        public DelegateCommand ResetHoleJudgeCommand => new DelegateCommand(ResetMicroHolePreview);

        public DelegateCommand ZoomInCommand => new DelegateCommand(() => Model.Zoom = Math.Min(MaxZoomLevel, Model.Zoom + GetZoomStep(Model.Zoom)));

        public DelegateCommand ZoomOutCommand => new DelegateCommand(() => Model.Zoom = Math.Max(MinZoomLevel, Model.Zoom - GetZoomStep(Model.Zoom)));

        public DelegateCommand ResetExposureCommand => new DelegateCommand(ResetExposure);

        public DelegateCommand<string> SetToolCommand => new DelegateCommand<string>(SetTool);

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (string.IsNullOrWhiteSpace(Model.StatusText))
            {
                Model.StatusText = "请先加载一张原始图像";
            }
        });

        public override void InitParam()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = "物理标尺";
            }

            if (string.IsNullOrWhiteSpace(Icon))
            {
                Icon = "\ue6a2";
            }
        }

        public void AddMeasurement(PhysicalScaleMeasurement measurement)
        {
            Model.Measurements.Add(measurement);
            ReindexMeasurements();
            Model.SelectedMeasurement = measurement;
            Model.StatusText = $"已新增 {measurement.ShapeTypeName}";
        }

        public void UpdateCursorPosition(double pixelX, double pixelY)
        {
            double physicalX = pixelX * Model.ScaleX;
            double physicalY = pixelY * Model.ScaleY;
            _cursorSummary = $"游标：X={pixelX:F2}px / {physicalX:F4}mm    Y={pixelY:F2}px / {physicalY:F4}mm";
            RaisePropertyChanged(nameof(CursorSummary));
        }

        public void ClearCursorPosition()
        {
            _cursorSummary = "游标：--";
            RaisePropertyChanged(nameof(CursorSummary));
        }

        public void RefreshAllMeasurements()
        {
            foreach (var measurement in Model.Measurements)
            {
                RefreshMeasurement(measurement);
            }

            RaisePropertyChanged(nameof(CalibrationSummary));
            RaisePropertyChanged(nameof(MeasurementCountText));
            RaisePropertyChanged(nameof(SelectedMeasurementDetail));
            RaisePropertyChanged(nameof(SelectedMeasurementReportTitle));
            RaisePropertyChanged(nameof(SelectedMeasurementReportMeta));
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PhysicalScaleModel.ScaleX) ||
                e.PropertyName == nameof(PhysicalScaleModel.ScaleY))
            {
                RefreshAllMeasurements();
                UpdateHolePhysicalValues();
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.FieldWidthMm))
            {
                RaisePropertyChanged(nameof(FieldWidthSummary));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.DefectName))
            {
                RaisePropertyChanged(nameof(SelectedMeasurementReportTitle));
                RaisePropertyChanged(nameof(HoleJudgeResultTitle));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.SelectedTool))
            {
                RaisePropertyChanged(nameof(ToolDisplayName));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.SelectedSharpenLevel))
            {
                RaisePropertyChanged(nameof(SharpenLevelSummary));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.SelectedMeasurement))
            {
                Model.StatusText = Model.SelectedMeasurement == null
                    ? "请选择或绘制一个量测图形"
                    : $"当前选中：{Model.SelectedMeasurement.DisplayName}";
                RaisePropertyChanged(nameof(SelectedMeasurementDetail));
                RaisePropertyChanged(nameof(SelectedMeasurementReportTitle));
                RaisePropertyChanged(nameof(SelectedMeasurementReportMeta));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.ImagePixelWidth) ||
                     e.PropertyName == nameof(PhysicalScaleModel.ImagePixelHeight))
            {
                RaisePropertyChanged(nameof(ImageSummary));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.DisplayImagePixelWidth) ||
                     e.PropertyName == nameof(PhysicalScaleModel.DisplayImagePixelHeight))
            {
                RaisePropertyChanged(nameof(DisplayImageSummary));
                RaisePropertyChanged(nameof(CenterSubTitle));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.DisplayExposureCompensation))
            {
                ScheduleExposureRefresh();
                RaisePropertyChanged(nameof(ExposureSummary));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.SelectedMode))
            {
                HandleModeChanged();
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.HolePreviewMode))
            {
                if (Model.HolePreviewVisible)
                {
                    UpdateHolePreviewDisplay();
                }

                RaisePropertyChanged(nameof(HolePreviewModeText));
                RaisePropertyChanged(nameof(HoleJudgeResultMeta));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.HoleRoiMeasurement))
            {
                RaiseHoleStateChanged();
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.HoleJudgementText) ||
                     e.PropertyName == nameof(PhysicalScaleModel.HoleReportDetail))
            {
                RaiseHoleStateChanged();
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.HolePreviewVisible))
            {
                RaisePropertyChanged(nameof(CanvasVisibility));
                RaisePropertyChanged(nameof(CenterTitle));
                RaisePropertyChanged(nameof(CenterSubTitle));
            }
            else if (e.PropertyName == nameof(PhysicalScaleModel.HoleThreshold) ||
                     e.PropertyName == nameof(PhysicalScaleModel.HolePolarity) ||
                     e.PropertyName == nameof(PhysicalScaleModel.HoleMinDefectAreaPx))
            {
                if (Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
                {
                    Model.StatusText = "判定参数已更新，请重新执行判定";
                }
            }
        }

        private void LoadImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图像文件|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|BMP|*.bmp|PNG|*.png|JPEG|*.jpg;*.jpeg|TIFF|*.tif;*.tiff",
                Title = "选择连续保存的原始图像"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadImageFromPath(dialog.FileName);
            }
        }

        public void LoadImageFromPath(string filePath)
        {
            var bitmap = LoadBitmapFromPath(filePath);

            Model.Measurements.Clear();
            Model.SelectedMeasurement = null;
            Model.HoleRoiMeasurement = null;
            Model.ImagePath = filePath;
            Model.LoadedImage = bitmap;
            Model.ImagePixelWidth = bitmap.PixelWidth;
            Model.ImagePixelHeight = bitmap.PixelHeight;
            Model.Zoom = 1.0;
            ResetHoleMetrics();
            _effectiveLoadedDisplayImage = null;
            RefreshLoadedDisplayImage();
            ApplyScaleFromFieldWidth(false);
            Model.StatusText = $"图像已加载，已按视野宽度 {Model.FieldWidthMm:F1} mm 自动换算坐标";

            RaisePropertyChanged(nameof(ImageSummary));
            RaisePropertyChanged(nameof(CalibrationSummary));
            RaisePropertyChanged(nameof(FieldWidthSummary));
            RaisePropertyChanged(nameof(MeasurementCountText));
            RaisePropertyChanged(nameof(SelectedMeasurementDetail));
            RaisePropertyChanged(nameof(SelectedMeasurementReportTitle));
            RaisePropertyChanged(nameof(SelectedMeasurementReportMeta));
            RaiseHoleStateChanged();
            FitRequested?.Invoke();
        }

        private void ApplyScaleFromFieldWidth()
        {
            if (ApplyScaleFromFieldWidth(false))
            {
                Model.StatusText = $"已按视野宽度 {Model.FieldWidthMm:F1} mm 更新坐标换算";
            }
        }

        private bool ApplyScaleFromFieldWidth(bool silent)
        {
            if (Model.ImagePixelWidth <= 0)
            {
                if (!silent)
                {
                    Model.StatusText = "请先加载图像，再按视野宽度换算坐标";
                }

                return false;
            }

            if (Model.FieldWidthMm <= 0)
            {
                if (!silent)
                {
                    Model.StatusText = "视野宽度必须大于 0";
                }

                return false;
            }

            double scale = Model.FieldWidthMm / Model.ImagePixelWidth;
            Model.ScaleX = scale;
            Model.ScaleY = scale;
            RaisePropertyChanged(nameof(CalibrationSummary));
            RaisePropertyChanged(nameof(FieldWidthSummary));
            return true;
        }

        private static double GetZoomStep(double currentZoom)
        {
            if (currentZoom < 2.0)
                return 0.1;
            if (currentZoom < 8.0)
                return 0.25;
            if (currentZoom < 20.0)
                return 0.5;

            return 1.0;
        }

        private void RefineSelectedMeasurement()
        {
            if (Model.SelectedMeasurement == null)
            {
                Model.StatusText = "请先选中一个图形，再执行局部锐化";
                return;
            }

            if (Model.LoadedImage == null)
            {
                Model.StatusText = "请先加载一张原始图像，再执行局部锐化";
                return;
            }

            try
            {
                BitmapSource originalBitmap = GetDisplaySourceForPreview();
                BitmapSource previewBitmap = CreatePreviewCropBitmap(originalBitmap, Model.SelectedMeasurement);
                if (previewBitmap == null)
                {
                    Model.StatusText = "局部预览范围无效";
                    return;
                }

                var payload = new PhysicalScaleSharpenPreviewPayload
                {
                    PreviewImage = previewBitmap,
                    ReportTitle = SelectedMeasurementReportTitle,
                    ReportMeta = $"{SelectedMeasurementReportMeta} | 锐化档位 {GetSharpenLevelText(Model.SelectedSharpenLevel)}",
                    ReportDetail = SelectedMeasurementDetail,
                    DefectName = Model.DefectName,
                    ShapeName = Model.SelectedMeasurement.ShapeTypeName,
                    ImageSummary = ImageSummary,
                    SharpenLevelText = GetSharpenLevelText(Model.SelectedSharpenLevel),
                    SharpenLevel = Model.SelectedSharpenLevel,
                    MeasurementTool = Model.SelectedMeasurement.Tool,
                    NominalCircleRadiusPx = Model.SelectedMeasurement.Tool == PhysicalScaleTool.Circle
                        ? Model.SelectedMeasurement.RadiusPx
                        : 0,
                    ScaleX = Model.ScaleX,
                    ScaleY = Model.ScaleY
                };

                var previewView = ContainerLocator.Container.Resolve<PhysicalScaleSharpenPreviewView>();
                var previewViewModel = new PhysicalScaleSharpenPreviewViewModel();
                previewViewModel.ApplyPayload(payload);
                previewView.DataContext = previewViewModel;
                previewViewModel.RequestViewportReset();

                string windowName = $"PhysicalScaleSharpenPreview_{Serial}";
                UserControlManager.CloseWindow(windowName);
                UserControlManager.OpenSingleInstanceWindow(previewView, windowName, window =>
                {
                    previewViewModel.RequestClose = window.Close;
                    window.Title = "局部锐化预览";
                    window.Width = 1620;
                    window.Height = 980;
                    window.MinWidth = 1440;
                    window.MinHeight = 860;
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    window.SizeToContent = SizeToContent.Manual;
                    window.ResizeMode = ResizeMode.CanResize;
                });

                Model.StatusText = $"已打开 {Model.SelectedMeasurement.DisplayName} 的局部分析预览";
            }
            catch (Exception ex)
            {
                Model.StatusText = $"局部锐化失败：{ex.Message}";
            }
        }

        private static BitmapImage LoadBitmapFromPath(string filePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private void ExposureRefreshTimer_Tick(object sender, EventArgs e)
        {
            _exposureRefreshTimer.Stop();
            RefreshLoadedDisplayImage();
        }

        private void ScheduleExposureRefresh(bool immediate = false)
        {
            _exposureRefreshTimer.Stop();

            if (immediate || Model.LoadedImage == null)
            {
                if (immediate)
                {
                    RefreshLoadedDisplayImage();
                }

                return;
            }

            _exposureRefreshTimer.Start();
        }

        private void ResetExposure()
        {
            Model.DisplayExposureCompensation = 0.0;
            ScheduleExposureRefresh(true);
        }

        private void RefreshLoadedDisplayImage()
        {
            _effectiveLoadedDisplayImage = Model.LoadedImage == null
                ? null
                : ApplyExposureCompensation(Model.LoadedImage, Model.DisplayExposureCompensation);

            if (Model.HolePreviewVisible)
            {
                UpdateHolePreviewDisplay();
                return;
            }

            RestoreLoadedImageDisplay();
        }

        private BitmapSource GetDisplaySourceForPreview()
        {
            return _effectiveLoadedDisplayImage ?? Model.LoadedImage;
        }

        private static BitmapSource ApplyExposureCompensation(BitmapSource source, double exposureCompensation)
        {
            if (source == null)
                return null;

            BitmapSource converted = EnsureBgra32(source);
            if (Math.Abs(exposureCompensation) < 0.0001)
                return converted;

            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            double multiplier = Math.Pow(2.0, exposureCompensation);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = AdjustExposureChannel(pixels[i], multiplier);
                pixels[i + 1] = AdjustExposureChannel(pixels[i + 1], multiplier);
                pixels[i + 2] = AdjustExposureChannel(pixels[i + 2], multiplier);
            }

            return CreateBgraBitmap(pixels, width, height);
        }

        private static byte[] ApplyExposureCompensation(byte[] grayPixels, double exposureCompensation)
        {
            if (grayPixels == null)
                return null;

            if (Math.Abs(exposureCompensation) < 0.0001)
                return grayPixels;

            double multiplier = Math.Pow(2.0, exposureCompensation);
            byte[] adjusted = new byte[grayPixels.Length];
            for (int i = 0; i < grayPixels.Length; i++)
            {
                adjusted[i] = AdjustExposureChannel(grayPixels[i], multiplier);
            }

            return adjusted;
        }

        private static byte AdjustExposureChannel(byte value, double multiplier)
        {
            return (byte)Math.Clamp((int)Math.Round(value * multiplier), 0, 255);
        }

        private static BitmapSource CreatePreviewCropBitmap(
            BitmapSource source,
            PhysicalScaleMeasurement measurement)
        {
            if (source == null || measurement == null)
                return null;

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            Int32Rect roi = ResolveEnhanceBounds(measurement, width, height);
            if (roi.Width < 3 || roi.Height < 3)
                return null;

            BitmapSource converted = EnsureBgra32(source);
            int sourceStride = width * 4;
            byte[] sourcePixels = new byte[height * sourceStride];
            converted.CopyPixels(sourcePixels, sourceStride, 0);

            int targetStride = roi.Width * 4;
            byte[] targetPixels = new byte[roi.Height * targetStride];

            for (int y = 0; y < roi.Height; y++)
            {
                int sourceY = Math.Clamp(roi.Y + y, 0, height - 1);
                int sourceRowStart = sourceY * sourceStride;
                int targetRowStart = y * targetStride;

                for (int x = 0; x < roi.Width; x++)
                {
                    int sourceX = Math.Clamp(roi.X + x, 0, width - 1);
                    int sourceIndex = sourceRowStart + sourceX * 4;
                    int targetIndex = targetRowStart + x * 4;
                    Buffer.BlockCopy(sourcePixels, sourceIndex, targetPixels, targetIndex, 4);
                }
            }

            return CreateBgraBitmap(targetPixels, roi.Width, roi.Height);
        }

        private static Int32Rect ResolveEnhanceBounds(PhysicalScaleMeasurement measurement, int imageWidth, int imageHeight)
        {
            double left;
            double top;
            double right;
            double bottom;
            double minimumWidth;
            double minimumHeight;

            switch (measurement.Tool)
            {
                case PhysicalScaleTool.Point:
                    const double pointPadding = 10.0;
                    left = measurement.StartX - pointPadding;
                    top = measurement.StartY - pointPadding;
                    right = measurement.StartX + pointPadding;
                    bottom = measurement.StartY + pointPadding;
                    minimumWidth = 36.0;
                    minimumHeight = 36.0;
                    break;
                case PhysicalScaleTool.Line:
                    double lineWidth = Math.Abs(measurement.EndX - measurement.StartX);
                    double lineHeight = Math.Abs(measurement.EndY - measurement.StartY);
                    double linePadding = Math.Clamp(Math.Max(lineWidth, lineHeight) * 1.2 + 6.0, 8.0, 18.0);
                    left = Math.Min(measurement.StartX, measurement.EndX) - linePadding;
                    top = Math.Min(measurement.StartY, measurement.EndY) - linePadding;
                    right = Math.Max(measurement.StartX, measurement.EndX) + linePadding;
                    bottom = Math.Max(measurement.StartY, measurement.EndY) + linePadding;
                    minimumWidth = 36.0;
                    minimumHeight = 36.0;
                    break;
                case PhysicalScaleTool.Rectangle:
                    double rectWidth = Math.Abs(measurement.EndX - measurement.StartX);
                    double rectHeight = Math.Abs(measurement.EndY - measurement.StartY);
                    double rectPadding = Math.Clamp(Math.Max(rectWidth, rectHeight) * 0.18 + 8.0, 8.0, 20.0);
                    left = Math.Min(measurement.StartX, measurement.EndX) - rectPadding;
                    top = Math.Min(measurement.StartY, measurement.EndY) - rectPadding;
                    right = Math.Max(measurement.StartX, measurement.EndX) + rectPadding;
                    bottom = Math.Max(measurement.StartY, measurement.EndY) + rectPadding;
                    minimumWidth = 48.0;
                    minimumHeight = 48.0;
                    break;
                case PhysicalScaleTool.Circle:
                    double baseRadius = Math.Max(measurement.RadiusPx, 4.0);
                    double circlePadding = Math.Clamp(baseRadius * 0.35 + 8.0, 8.0, 18.0);
                    double radius = baseRadius + circlePadding;
                    left = measurement.StartX - radius;
                    top = measurement.StartY - radius;
                    right = measurement.StartX + radius;
                    bottom = measurement.StartY + radius;
                    minimumWidth = 48.0;
                    minimumHeight = 48.0;
                    break;
                default:
                    left = 0;
                    top = 0;
                    right = imageWidth;
                    bottom = imageHeight;
                    minimumWidth = Math.Min(96.0, imageWidth);
                    minimumHeight = Math.Min(96.0, imageHeight);
                    break;
            }

            EnsureMinimumSpan(ref left, ref right, minimumWidth);
            EnsureMinimumSpan(ref top, ref bottom, minimumHeight);

            int roiWidth = Math.Min(imageWidth, Math.Max(3, (int)Math.Ceiling(right - left)));
            int roiHeight = Math.Min(imageHeight, Math.Max(3, (int)Math.Ceiling(bottom - top)));
            double centerX = (left + right) * 0.5;
            double centerY = (top + bottom) * 0.5;
            int x = (int)Math.Round(centerX - roiWidth * 0.5);
            int y = (int)Math.Round(centerY - roiHeight * 0.5);
            return new Int32Rect(x, y, roiWidth, roiHeight);
        }

        private static void EnsureMinimumSpan(ref double min, ref double max, double minimumSpan)
        {
            double span = max - min;
            if (span >= minimumSpan)
                return;

            double center = (min + max) * 0.5;
            double halfSpan = minimumSpan * 0.5;
            min = center - halfSpan;
            max = center + halfSpan;
        }

        private static double GetSharpenStrength(PhysicalScaleSharpenLevel level)
        {
            return level switch
            {
                PhysicalScaleSharpenLevel.Weak => 0.45,
                PhysicalScaleSharpenLevel.Medium => 0.85,
                PhysicalScaleSharpenLevel.Strong => 1.25,
                _ => 0.85
            };
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

        private void ClearMeasurements()
        {
            if (Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
            {
                Model.HoleRoiMeasurement = null;
                ResetHoleMetrics();
                RestoreLoadedImageDisplay();
                Model.StatusText = "已清空当前单孔圆框和判定结果";
                RaiseHoleStateChanged();
                return;
            }

            Model.Measurements.Clear();
            Model.SelectedMeasurement = null;
            Model.StatusText = "已清空所有量测图形";
            RaisePropertyChanged(nameof(MeasurementCountText));
            RaisePropertyChanged(nameof(SelectedMeasurementDetail));
            RaisePropertyChanged(nameof(SelectedMeasurementReportTitle));
            RaisePropertyChanged(nameof(SelectedMeasurementReportMeta));
        }

        private void DeleteSelectedMeasurement()
        {
            if (Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
            {
                if (Model.HoleRoiMeasurement == null)
                {
                    Model.StatusText = "没有可删除的单孔圆框";
                    return;
                }

                Model.HoleRoiMeasurement = null;
                ResetHoleMetrics();
                RestoreLoadedImageDisplay();
                Model.StatusText = "已删除当前单孔圆框";
                RaiseHoleStateChanged();
                return;
            }

            if (Model.SelectedMeasurement == null)
            {
                Model.StatusText = "没有选中的量测图形";
                return;
            }

            Model.Measurements.Remove(Model.SelectedMeasurement);
            Model.SelectedMeasurement = null;
            ReindexMeasurements();
            Model.StatusText = "已删除选中的量测图形";
            RaisePropertyChanged(nameof(SelectedMeasurementDetail));
            RaisePropertyChanged(nameof(SelectedMeasurementReportTitle));
            RaisePropertyChanged(nameof(SelectedMeasurementReportMeta));
        }

        private void ExportCsv()
        {
            if (Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
            {
                if (string.IsNullOrWhiteSpace(Model.HoleReportDetail))
                {
                    Model.StatusText = "没有可导出的微孔判定结果";
                    return;
                }

                var holeDialog = new SaveFileDialog
                {
                    Filter = "CSV 文件|*.csv",
                    FileName = $"MicroHole_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    Title = "导出微孔判定结果"
                };

                if (holeDialog.ShowDialog() != true)
                    return;

                var lines = new[]
                {
                    "字段,值",
                    $"缺陷名称,\"{Model.DefectName}\"",
                    $"判定结果(OK/NG),\"{HoleJudgeDisplayText}\"",
                    $"异常类型,\"{HoleJudgeReasonText}\"",
                    $"等效开口直径(px),\"{Model.HoleOpenDiameterPx:F4}\"",
                    $"等效开口直径(mm),\"{Model.HoleOpenDiameterMm:F4}\"",
                    $"有效开口率,\"{Model.HoleOpeningRatio * 100:F2}%\"",
                    $"缺陷数量,\"{Model.HoleDefectCount}\"",
                    $"最大缺陷面积(px²),\"{Model.HoleMaxDefectAreaPx:F4}\"",
                    $"最大缺陷面积(mm²),\"{Model.HoleMaxDefectAreaMm2:F4}\"",
                    $"缺陷总面积(px²),\"{Model.HoleTotalDefectAreaPx:F4}\"",
                    $"缺陷总面积(mm²),\"{Model.HoleTotalDefectAreaMm2:F4}\""
                };

                File.WriteAllLines(holeDialog.FileName, lines, Encoding.UTF8);
                Model.StatusText = $"微孔判定结果已导出：{Path.GetFileName(holeDialog.FileName)}";
                return;
            }

            if (Model.Measurements.Count == 0)
            {
                Model.StatusText = "没有可导出的量测结果";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV 文件|*.csv",
                FileName = $"PhysicalScale_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "导出量测结果"
            };

            if (dialog.ShowDialog() != true)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("编号,类型,像素结果,物理结果,明细");
            foreach (var measurement in Model.Measurements)
            {
                sb.Append('"').Append(measurement.DisplayName).Append("\",");
                sb.Append('"').Append(measurement.ShapeTypeName).Append("\",");
                sb.Append('"').Append(measurement.PixelSummary.Replace("\"", "\"\"")).Append("\",");
                sb.Append('"').Append(measurement.PhysicalSummary.Replace("\"", "\"\"")).Append("\",");
                sb.Append('"').Append(measurement.DetailText.Replace("\"", "\"\"").Replace(Environment.NewLine, " | ")).Append('"');
                sb.AppendLine();
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
            Model.StatusText = $"量测结果已导出：{Path.GetFileName(dialog.FileName)}";
        }

        private void SetTool(string toolName)
        {
            if (Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
            {
                Model.SelectedTool = toolName == "Select"
                    ? PhysicalScaleTool.Select
                    : PhysicalScaleTool.Circle;
                Model.StatusText = Model.SelectedTool == PhysicalScaleTool.Select
                    ? "当前工具：选择单孔圆框"
                    : "当前工具：框选单孔";
                return;
            }

            Model.SelectedTool = toolName switch
            {
                "Point" => PhysicalScaleTool.Point,
                "Line" => PhysicalScaleTool.Line,
                "Rectangle" => PhysicalScaleTool.Rectangle,
                "Circle" => PhysicalScaleTool.Circle,
                _ => PhysicalScaleTool.Select
            };

            Model.StatusText = $"当前工具：{ToolDisplayName}";
        }

        private void ReindexMeasurements()
        {
            int index = 1;
            foreach (var measurement in Model.Measurements)
            {
                measurement.Index = index++;
                RefreshMeasurement(measurement);
            }

            RaisePropertyChanged(nameof(MeasurementCountText));
        }

        private void RefreshMeasurement(PhysicalScaleMeasurement measurement)
        {
            measurement.DisplayName = $"M{measurement.Index:D2}";
            measurement.ShapeTypeName = GetShapeTypeName(measurement.Tool);

            switch (measurement.Tool)
            {
                case PhysicalScaleTool.Point:
                    UpdatePointMeasurement(measurement);
                    break;
                case PhysicalScaleTool.Line:
                    UpdateLineMeasurement(measurement);
                    break;
                case PhysicalScaleTool.Rectangle:
                    UpdateRectangleMeasurement(measurement);
                    break;
                case PhysicalScaleTool.Circle:
                    UpdateCircleMeasurement(measurement);
                    break;
            }

            if (ReferenceEquals(measurement, Model.SelectedMeasurement))
            {
                RaisePropertyChanged(nameof(SelectedMeasurementDetail));
                RaisePropertyChanged(nameof(SelectedMeasurementReportTitle));
                RaisePropertyChanged(nameof(SelectedMeasurementReportMeta));
            }
        }

        private void UpdatePointMeasurement(PhysicalScaleMeasurement measurement)
        {
            double physicalX = measurement.StartX * Model.ScaleX;
            double physicalY = measurement.StartY * Model.ScaleY;

            measurement.PixelSummary = $"X={measurement.StartX:F1} Y={measurement.StartY:F1}";
            measurement.PhysicalSummary = $"X={physicalX:F3} Y={physicalY:F3} mm";
            measurement.DetailText =
                $"图形：点{Environment.NewLine}" +
                $"像素坐标 X={measurement.StartX:F2}, Y={measurement.StartY:F2}{Environment.NewLine}" +
                $"物理坐标 X={physicalX:F4} mm, Y={physicalY:F4} mm";
        }

        private void UpdateLineMeasurement(PhysicalScaleMeasurement measurement)
        {
            double dx = measurement.EndX - measurement.StartX;
            double dy = measurement.EndY - measurement.StartY;
            double pixelLength = Math.Sqrt(dx * dx + dy * dy);
            double physicalLength = Math.Sqrt(
                Math.Pow(dx * Model.ScaleX, 2) +
                Math.Pow(dy * Model.ScaleY, 2));

            measurement.PixelSummary = $"L={pixelLength:F2} px";
            measurement.PhysicalSummary = $"L={physicalLength:F4} mm";
            measurement.DetailText =
                $"图形：线{Environment.NewLine}" +
                $"起点 ({measurement.StartX:F2}, {measurement.StartY:F2}){Environment.NewLine}" +
                $"终点 ({measurement.EndX:F2}, {measurement.EndY:F2}){Environment.NewLine}" +
                $"像素长度 {pixelLength:F4} px{Environment.NewLine}" +
                $"物理长度 {physicalLength:F4} mm";
        }

        private void UpdateRectangleMeasurement(PhysicalScaleMeasurement measurement)
        {
            double widthPx = Math.Abs(measurement.EndX - measurement.StartX);
            double heightPx = Math.Abs(measurement.EndY - measurement.StartY);
            double areaPx = widthPx * heightPx;
            double perimeterPx = 2 * (widthPx + heightPx);
            double widthMm = widthPx * Model.ScaleX;
            double heightMm = heightPx * Model.ScaleY;
            double areaMm = widthMm * heightMm;
            double perimeterMm = 2 * (widthMm + heightMm);

            measurement.PixelSummary = $"W={widthPx:F1} H={heightPx:F1} A={areaPx:F1}";
            measurement.PhysicalSummary = $"W={widthMm:F3} H={heightMm:F3} A={areaMm:F3}";
            measurement.DetailText =
                $"图形：矩形{Environment.NewLine}" +
                $"像素宽 {widthPx:F4} px{Environment.NewLine}" +
                $"像素高 {heightPx:F4} px{Environment.NewLine}" +
                $"像素面积 {areaPx:F4} px²{Environment.NewLine}" +
                $"像素周长 {perimeterPx:F4} px{Environment.NewLine}" +
                $"物理宽 {widthMm:F4} mm{Environment.NewLine}" +
                $"物理高 {heightMm:F4} mm{Environment.NewLine}" +
                $"物理面积 {areaMm:F4} mm²{Environment.NewLine}" +
                $"物理周长 {perimeterMm:F4} mm";
        }

        private void UpdateCircleMeasurement(PhysicalScaleMeasurement measurement)
        {
            double diameterPx = measurement.RadiusPx * 2;
            double areaPx = Math.PI * measurement.RadiusPx * measurement.RadiusPx;
            double diaX = diameterPx * Model.ScaleX;
            double diaY = diameterPx * Model.ScaleY;
            double areaMm = Math.PI * (measurement.RadiusPx * Model.ScaleX) * (measurement.RadiusPx * Model.ScaleY);

            measurement.PixelSummary = $"D={diameterPx:F2} px A={areaPx:F2}";
            measurement.PhysicalSummary = $"Dx={diaX:F3} Dy={diaY:F3}";
            measurement.DetailText =
                $"图形：圆{Environment.NewLine}" +
                $"中心 ({measurement.StartX:F2}, {measurement.StartY:F2}){Environment.NewLine}" +
                $"像素半径 {measurement.RadiusPx:F4} px{Environment.NewLine}" +
                $"像素直径 {diameterPx:F4} px{Environment.NewLine}" +
                $"像素面积 {areaPx:F4} px²{Environment.NewLine}" +
                $"物理直径 X {diaX:F4} mm{Environment.NewLine}" +
                $"物理直径 Y {diaY:F4} mm{Environment.NewLine}" +
                $"物理面积 {areaMm:F4} mm²";
        }

        private static string GetShapeTypeName(PhysicalScaleTool tool)
        {
            return tool switch
            {
                PhysicalScaleTool.Point => "点",
                PhysicalScaleTool.Line => "线",
                PhysicalScaleTool.Rectangle => "矩形",
                PhysicalScaleTool.Circle => "圆",
                _ => "选择"
            };
        }
    }
}
