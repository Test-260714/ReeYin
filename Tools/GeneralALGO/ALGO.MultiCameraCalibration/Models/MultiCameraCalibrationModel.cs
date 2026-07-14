using HalconDotNet;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using ReeYin_V.Core.Calibration;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ALGO.MultiCameraCalibration.Models
{
    [Serializable]
    public class MultiCameraCalibrationModel : ModelParamBase
    {
        [JsonIgnore]
        public MultiCameraCalibrationSdk MultiCameraCalib { get; private set; } = new MultiCameraCalibrationSdk();

        [JsonIgnore]
        private HImage _image;

        [OutputParam("Image", "被处理的图像")]
        [JsonIgnore]
        public HImage Image
        {
            get { return _image; }
            set { SetProperty(ref _image, value); }
        }

        [JsonIgnore]
        private ExecuteModuleOutput _output;

        [JsonIgnore]
        public new ExecuteModuleOutput Output
        {
            get { return _output; }
            set { SetProperty(ref _output, value); }
        }

        [JsonIgnore]
        private int _sltTriggerIndex;
        public int SltTriggerIndex
        {
            get { return _sltTriggerIndex; }
            set { SetProperty(ref _sltTriggerIndex, value); }
        }

        [JsonIgnore]
        private string _cameraId = string.Empty;
        public string CameraId
        {
            get { return _cameraId; }
            set { SetProperty(ref _cameraId, value); }
        }

        [JsonIgnore]
        private string _useCameraId = string.Empty;
        public string UseCameraId
        {
            get { return _useCameraId; }
            set { SetProperty(ref _useCameraId, value); }
        }

        [JsonIgnore]
        private string _calibrationImageDir = string.Empty;
        public string CalibrationImageDir
        {
            get { return _calibrationImageDir; }
            set { SetProperty(ref _calibrationImageDir, value); }
        }

        [JsonIgnore]
        private string _calibFileOutputDir = string.Empty;
        public string CalibFileOutputDir
        {
            get { return _calibFileOutputDir; }
            set { SetProperty(ref _calibFileOutputDir, value); }
        }

        [JsonIgnore]
        private string _calibrationFilePath = string.Empty;
        public string CalibrationFilePath
        {
            get { return _calibrationFilePath; }
            set { SetProperty(ref _calibrationFilePath, value); }
        }

        [JsonIgnore]
        private ObservableCollection<ImageNameModel> _calibImageNameList = new ObservableCollection<ImageNameModel>();
        public ObservableCollection<ImageNameModel> CalibImageNameList
        {
            get { return _calibImageNameList; }
            set { SetProperty(ref _calibImageNameList, value); }
        }

        [JsonIgnore]
        private ImageNameModel _selectedCalibrationImage;
        public ImageNameModel SelectedCalibrationImage
        {
            get { return _selectedCalibrationImage; }
            set { SetProperty(ref _selectedCalibrationImage, value); }
        }

        [JsonIgnore]
        private ObservableCollection<StitchInputImageModel> _stitchInputImages = new ObservableCollection<StitchInputImageModel>();
        public ObservableCollection<StitchInputImageModel> StitchInputImages
        {
            get { return _stitchInputImages; }
            set { SetProperty(ref _stitchInputImages, value); }
        }

        [JsonIgnore]
        private StitchInputImageModel _selectedStitchInputImage;
        public StitchInputImageModel SelectedStitchInputImage
        {
            get { return _selectedStitchInputImage; }
            set { SetProperty(ref _selectedStitchInputImage, value); }
        }

        [JsonIgnore]
        private eCalibrationBoardType _calibBoardType = eCalibrationBoardType.棋盘格;
        public eCalibrationBoardType CalibBoardType
        {
            get { return _calibBoardType; }
            set { SetProperty(ref _calibBoardType, value); }
        }

        [JsonIgnore]
        private MultiCameraUsageMode _calibUsageModel = MultiCameraUsageMode.像素转公共世界坐标;
        public MultiCameraUsageMode CalibUsageModel
        {
            get { return _calibUsageModel; }
            set { SetProperty(ref _calibUsageModel, value); }
        }

        [JsonIgnore]
        private int _patternCols = -1;
        public int PatternCols
        {
            get { return _patternCols; }
            set { SetProperty(ref _patternCols, value); }
        }

        [JsonIgnore]
        private int _patternRows = -1;
        public int PatternRows
        {
            get { return _patternRows; }
            set { SetProperty(ref _patternRows, value); }
        }

        [JsonIgnore]
        private double _squareSize = -1;
        public double SquareSize
        {
            get { return _squareSize; }
            set { SetProperty(ref _squareSize, value); }
        }

        [JsonIgnore]
        private double _markerSize = -1;
        public double MarkerSize
        {
            get { return _markerSize; }
            set { SetProperty(ref _markerSize, value); }
        }

        [JsonIgnore]
        private PredefinedDictionaryName _dictionaryId = (PredefinedDictionaryName)(-1);
        public PredefinedDictionaryName DictionaryId
        {
            get { return _dictionaryId; }
            set { SetProperty(ref _dictionaryId, value); }
        }

        [JsonIgnore]
        private ROILine _line1;
        public ROILine Line1
        {
            get { return _line1; }
            set { SetProperty(ref _line1, value); }
        }

        [JsonIgnore]
        private ROILine _line2;
        public ROILine Line2
        {
            get { return _line2; }
            set { SetProperty(ref _line2, value); }
        }

        [JsonIgnore]
        private double _realDistance;
        public double RealDistance
        {
            get { return _realDistance; }
            set { SetProperty(ref _realDistance, value); }
        }

        [JsonIgnore]
        private TransmitParam _inputLine1 = new TransmitParam();
        public TransmitParam InputLine1
        {
            get { return _inputLine1; }
            set { SetProperty(ref _inputLine1, value); }
        }

        [JsonIgnore]
        private TransmitParam _inputLine2 = new TransmitParam();
        public TransmitParam InputLine2
        {
            get { return _inputLine2; }
            set { SetProperty(ref _inputLine2, value); }
        }

        [JsonIgnore]
        private PointSet _points;
        public PointSet Points
        {
            get { return _points; }
            set { SetProperty(ref _points, value); }
        }

        [JsonIgnore]
        private TransmitParam _inputPointSet = new TransmitParam();
        public TransmitParam InputPointSet
        {
            get { return _inputPointSet; }
            set { SetProperty(ref _inputPointSet, value); }
        }

        [JsonIgnore]
        private PointSet _outputPointSet = new PointSet();

        [OutputParam("OutputPointSet", "转换后的点集")]
        public PointSet OutputPointSet
        {
            get { return _outputPointSet; }
            set { SetProperty(ref _outputPointSet, value); }
        }

        [JsonIgnore]
        private MultiCameraCalibrationSdk.MultiCameraCalibrationReport _calibrationReport;
        public MultiCameraCalibrationSdk.MultiCameraCalibrationReport CalibrationReport
        {
            get { return _calibrationReport; }
            set
            {
                if (SetProperty(ref _calibrationReport, value))
                {
                    RaiseCalibrationReportPropertiesChanged();
                }
            }
        }

        public int ReportCameraCount => CalibrationReport.cameraCount;

        public int ReportCaptureCount => CalibrationReport.captureCount;

        public int ReportObservationCount => CalibrationReport.observationCount;

        public int ReportResidualCount => CalibrationReport.residualCount;

        public double ReportInitialRmsError => CalibrationReport.initialRmsError;

        public double ReportFinalRmsError => CalibrationReport.finalRmsError;

        public double ReportMaxReprojectionError => CalibrationReport.maxReprojectionError;

        public int ReportConnectedComponentCount => CalibrationReport.connectedComponentCount;

        public string ReportConvergedText => CalibrationReport.converged == 0 ? "否" : "是";

        [JsonIgnore]
        private ObservableCollection<MultiCameraCameraResultModel> _cameraResults = new ObservableCollection<MultiCameraCameraResultModel>();
        public ObservableCollection<MultiCameraCameraResultModel> CameraResults
        {
            get { return _cameraResults; }
            set { SetProperty(ref _cameraResults, value); }
        }

        [JsonIgnore]
        private MultiCameraCameraResultModel _selectedCameraResult;
        public MultiCameraCameraResultModel SelectedCameraResult
        {
            get { return _selectedCameraResult; }
            set
            {
                if (SetProperty(ref _selectedCameraResult, value))
                {
                    UpdateMatrixDisplayData();
                }
            }
        }

        [JsonIgnore]
        private ObservableCollection<double> _intrinsicMatrix = new ObservableCollection<double>();
        public ObservableCollection<double> IntrinsicMatrix
        {
            get { return _intrinsicMatrix; }
            set { SetProperty(ref _intrinsicMatrix, value); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _distortionCoefficients = new ObservableCollection<double>();
        public ObservableCollection<double> DistortionCoefficients
        {
            get { return _distortionCoefficients; }
            set { SetProperty(ref _distortionCoefficients, value); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _rotationVector = new ObservableCollection<double>();
        public ObservableCollection<double> RotationVector
        {
            get { return _rotationVector; }
            set { SetProperty(ref _rotationVector, value); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _translationVector = new ObservableCollection<double>();
        public ObservableCollection<double> TranslationVector
        {
            get { return _translationVector; }
            set { SetProperty(ref _translationVector, value); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _extrinsicMatrix = new ObservableCollection<double>();
        public ObservableCollection<double> ExtrinsicMatrix
        {
            get { return _extrinsicMatrix; }
            set { SetProperty(ref _extrinsicMatrix, value); }
        }

        [JsonIgnore]
        private double _heightCompensation;
        public double HeightCompensation
        {
            get { return _heightCompensation; }
            set { SetProperty(ref _heightCompensation, value); }
        }

        [JsonIgnore]
        private MultiCameraCalibrationSdk.MultiCameraBlendMode _stitchBlendMode = MultiCameraCalibrationSdk.MultiCameraBlendMode.Overlay;
        public MultiCameraCalibrationSdk.MultiCameraBlendMode StitchBlendMode
        {
            get { return _stitchBlendMode; }
            set { SetProperty(ref _stitchBlendMode, value); }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        public MultiCameraCalibrationModel()
        {
            TriggerModuleRun += () => ExecuteModule().Result;

            PrismProvider.EventAggregator.GetEvent<NodifyRemoveNodeEvent>().Subscribe((order) =>
            {
                if (order != Serial.ToString()) return;
                Dispose();
            }, ThreadOption.UIThread);
        }

        public override void Dispose()
        {
            base.Dispose();
            MultiCameraCalib?.Dispose();
        }

        public void ResetCalibrationSdk()
        {
            MultiCameraCalib?.Dispose();
            MultiCameraCalib = new MultiCameraCalibrationSdk();
            RaisePropertyChanged(nameof(MultiCameraCalib));
        }

        public void LoadCalibrationFile(string filePath)
        {
            ResetCalibrationSdk();
            MultiCameraCalib.LoadCalibrationFile(filePath);
            CalibrationFilePath = filePath;
            CalibFileOutputDir = Path.GetDirectoryName(filePath) ?? CalibFileOutputDir;
            RefreshCalibrationResultsFromSdk();
        }

        public bool TryRestoreCalibrationFromFile(out bool displayCacheOverwritten, out string errorMessage)
        {
            displayCacheOverwritten = false;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(CalibrationFilePath))
            {
                errorMessage = "未记录标定文件路径。";
                return false;
            }

            if (!File.Exists(CalibrationFilePath))
            {
                errorMessage = $"标定文件不存在: {CalibrationFilePath}";
                return false;
            }

            try
            {
                bool hadDisplayCache = HasDisplayCalibrationCache();
                string beforeSnapshot = CreateDisplayCalibrationSnapshot();
                LoadCalibrationFile(CalibrationFilePath);
                string afterSnapshot = CreateDisplayCalibrationSnapshot();
                displayCacheOverwritten = hadDisplayCache
                    && !string.Equals(beforeSnapshot, afterSnapshot, StringComparison.Ordinal);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public void EnsureCalibrationRuntimeLoaded()
        {
            if (HasNativeCalibrationState())
            {
                return;
            }

            if (TryRestoreCalibrationFromFile(out _, out string errorMessage))
            {
                return;
            }

            throw new InvalidOperationException(
                $"当前标定结果只有 UI 缓存，原生标定状态无效。请重新导入或重新导出有效 YAML 标定文件。{errorMessage}");
        }

        public void RefreshCalibrationResultsFromSdk()
        {
            CalibrationReport = MultiCameraCalib.GetReport();
            HeightCompensation = MultiCameraCalib.GetMeasurementPlaneParams().heightCompensation;

            string previousUseCameraId = UseCameraId;
            IReadOnlyList<string> cameraIds = MultiCameraCalib.GetCameraIds();
            CameraResults = new ObservableCollection<MultiCameraCameraResultModel>(
                cameraIds.Select(cameraId => MultiCameraCameraResultModel.FromParams(MultiCameraCalib.GetCameraParams(cameraId))));

            SelectedCameraResult = CameraResults.FirstOrDefault(item => item.CameraId == previousUseCameraId)
                ?? CameraResults.FirstOrDefault();
            UseCameraId = SelectedCameraResult?.CameraId ?? string.Empty;
            SynchronizeStitchInputImagesWithCameraIds(cameraIds);
        }

        public void AddCalibrationImage(string cameraId, string imagePath)
        {
            if (string.IsNullOrWhiteSpace(cameraId))
            {
                throw new InvalidOperationException("添加标定图片前需要先输入相机 ID。");
            }

            int nextId = CalibImageNameList.Count == 0 ? 1 : CalibImageNameList.Max(item => item.ID) + 1;
            CalibImageNameList.Add(new ImageNameModel
            {
                ID = nextId,
                IsSelected = true,
                CameraId = cameraId,
                CaptureId = Path.GetFileNameWithoutExtension(imagePath),
                ImageName = Path.GetFileName(imagePath),
                ImagePath = imagePath
            });
        }

        public void AddStitchInputImage()
        {
            StitchInputImages.Add(new StitchInputImageModel());
        }

        public void RemoveSelectedStitchInputImage()
        {
            if (SelectedStitchInputImage == null)
            {
                return;
            }

            StitchInputImages.Remove(SelectedStitchInputImage);
            SelectedStitchInputImage = null;
        }

        private void SynchronizeStitchInputImagesWithCameraIds(IReadOnlyList<string> cameraIds)
        {
            if (cameraIds == null || cameraIds.Count == 0)
            {
                return;
            }

            string selectedCameraId = SelectedStitchInputImage?.CameraId ?? string.Empty;
            IReadOnlyList<StitchInputImageModel> synchronizedRows =
                MultiCameraStitchingInputSynchronizer.Synchronize(
                    cameraIds,
                    StitchInputImages,
                    item => item.CameraId,
                    (item, cameraId) => item.CameraId = cameraId,
                    () => new StitchInputImageModel());

            StitchInputImages.Clear();
            foreach (StitchInputImageModel row in synchronizedRows)
            {
                StitchInputImages.Add(row);
            }

            SelectedStitchInputImage = StitchInputImages.FirstOrDefault(
                item => string.Equals(item.CameraId, selectedCameraId, StringComparison.Ordinal))
                ?? StitchInputImages.FirstOrDefault();
        }

        private bool HasNativeCalibrationState()
        {
            try
            {
                return MultiCameraCalib.GetCameraIds().Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private bool HasDisplayCalibrationCache()
        {
            return CalibrationReport.cameraCount > 0 || (CameraResults?.Count ?? 0) > 0;
        }

        private string CreateDisplayCalibrationSnapshot()
        {
            return JsonConvert.SerializeObject(new
            {
                Report = CalibrationReport,
                Cameras = (CameraResults ?? new ObservableCollection<MultiCameraCameraResultModel>())
                    .OrderBy(camera => camera.CameraId, StringComparer.Ordinal)
                    .Select(camera => new
                    {
                        camera.CameraId,
                        camera.ImageWidth,
                        camera.ImageHeight,
                        camera.RmsError,
                        Intrinsic = camera.IntrinsicMatrix?.ToArray() ?? Array.Empty<double>(),
                        Distortion = camera.DistortionCoefficients?.ToArray() ?? Array.Empty<double>(),
                        Rotation = camera.RotationVector?.ToArray() ?? Array.Empty<double>(),
                        Translation = camera.TranslationVector?.ToArray() ?? Array.Empty<double>(),
                        Extrinsic = camera.ExtrinsicMatrix?.ToArray() ?? Array.Empty<double>()
                    })
                    .ToArray()
            });
        }

        private IReadOnlyList<string> ResolveCalibrationCameraIdsForFileName()
        {
            try
            {
                IReadOnlyList<string> nativeCameraIds = MultiCameraCalib.GetCameraIds();
                if (nativeCameraIds.Count > 0)
                {
                    return nativeCameraIds;
                }
            }
            catch
            {
            }

            if (CameraResults?.Count > 0)
            {
                return CameraResults
                    .Select(item => item.CameraId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }

            return CalibImageNameList
                .Where(item => item.IsSelected && !string.IsNullOrWhiteSpace(item.CameraId))
                .Select(item => item.CameraId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        public string ResolveCalibrationOutputFile()
        {
            if (string.IsNullOrWhiteSpace(CalibFileOutputDir))
            {
                throw new InvalidOperationException("标定文件导出目录为空。");
            }

            return CalibrationFilePathResolver.ResolveMultiCameraOutputFile(
                CalibFileOutputDir,
                ResolveCalibrationCameraIdsForFileName(),
                CalibBoardType);
        }

        public void SaveCalibrationResults(string outputFile)
        {
            SyncMeasurementPlaneParamsToSdk();
            MultiCameraCalib.SaveCalibrationResults(outputFile);
            CalibrationFilePath = outputFile;
            CalibFileOutputDir = Path.GetDirectoryName(outputFile) ?? CalibFileOutputDir;
        }

        private void SyncMeasurementPlaneParamsToSdk()
        {
            MultiCameraCalib.SetMeasurementPlaneParams(new MultiCameraCalibrationSdk.MeasurementPlaneParams
            {
                heightCompensation = HeightCompensation
            });
        }

        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    switch (SltTriggerIndex)
                    {
                        case 0:
                            ExecuteCalibration();
                            break;
                        case 1:
                            ExecuteCalibrationUsage();
                            break;
                    }

                    foreach (var item in OutputParams)
                    {
                        item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                    }

                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"多相机标定模块执行异常: {ex.Message}");
                    return NodeStatus.Error;
                }
            });

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: MultiCameraCalibration elapsed: {time} ms");
            return Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        private void ExecuteCalibration()
        {
            List<ImageNameModel> rows = CalibImageNameList.Where(item => item.IsSelected).ToList();
            ValidateCalibrationRows(rows);

            ResetCalibrationSdk();
            CalibrationFilePath = string.Empty;

            MultiCameraCalib.SetCalibrationBoardParams(CreateCalibrationBoardParams());
            SyncMeasurementPlaneParamsToSdk();
            MultiCameraCalibrationSdk.MultiCameraCalibrationOptions options = MultiCameraCalibrationSdk.CreateDefaultOptions();
            options.referenceCaptureId = MultiCameraReferenceCaptureSelector.SelectBestReferenceCapture(
                rows,
                row => row.CameraId,
                row => row.CaptureId);
            MultiCameraCalib.SetOptions(options);

            foreach (IGrouping<string, ImageNameModel> group in rows.GroupBy(item => item.CameraId))
            {
                (int width, int height) = GetCameraImageSize(group.Key, group.ToList());
                MultiCameraCalib.AddCamera(group.Key, width, height);
            }

            foreach (ImageNameModel row in rows)
            {
                MultiCameraCalib.AddObservationImagePath(row.CameraId, row.CaptureId, row.ImagePath);
            }

            MultiCameraCalib.Calibrate();
            RefreshCalibrationResultsFromSdk();
        }

        private void ExecuteCalibrationUsage()
        {
            EnsureCalibrationRuntimeLoaded();

            switch (CalibUsageModel)
            {
                case MultiCameraUsageMode.像素转公共世界坐标:
                    ExecutePixelToCommonWorld();
                    break;
                case MultiCameraUsageMode.公共世界坐标转像素:
                    ExecuteCommonWorldToPixel();
                    break;
                case MultiCameraUsageMode.多视角拼接:
                    ExecuteStitchImages();
                    break;
            }
        }

        private void ExecutePixelToCommonWorld()
        {
            PointSet input = ResolveInputPointSet();
            ValidateUseCameraId();

            double[] rows = new double[input.Length];
            double[] columns = new double[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                MultiCameraCalib.PixelToCommonWorld(
                    UseCameraId,
                    input.Columns[i],
                    input.Rows[i],
                    out double worldX,
                    out double worldY,
                    out _);

                rows[i] = worldY;
                columns[i] = worldX;
            }

            OutputPointSet = new PointSet(rows, columns);
        }

        private void ExecuteCommonWorldToPixel()
        {
            PointSet input = ResolveInputPointSet();
            ValidateUseCameraId();

            double[] rows = new double[input.Length];
            double[] columns = new double[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                MultiCameraCalib.CommonWorldToPixel(
                    UseCameraId,
                    input.Columns[i],
                    input.Rows[i],
                    HeightCompensation,
                    out double pixelX,
                    out double pixelY);

                rows[i] = pixelY;
                columns[i] = pixelX;
            }

            OutputPointSet = new PointSet(rows, columns);
        }

        private void ExecuteStitchImages()
        {
            List<StitchInputImageModel> enabledRows = StitchInputImages.Where(item => item.IsSelected).ToList();
            if (enabledRows.Count == 0)
            {
                throw new InvalidOperationException("多视角拼接至少需要一路启用图像。");
            }

            foreach (StitchInputImageModel row in enabledRows)
            {
                if (string.IsNullOrWhiteSpace(row.CameraId))
                {
                    throw new InvalidOperationException("多视角拼接输入存在空相机 ID。");
                }
            }

            var retained = new List<IDisposable>();
            try
            {
                var inputs = new List<MultiCameraCalibrationSdk.MultiCameraImageInput>();
                foreach (StitchInputImageModel row in enabledRows)
                {
                    if (string.IsNullOrWhiteSpace(row.CameraId))
                    {
                        throw new InvalidOperationException("多视角拼接输入存在空相机 ID。");
                    }

                    object imageValue = GetTransmitParam(InputParams, row.InputImage, false);
                    HImage inputImage = ResolveInputImage(imageValue);
                    inputs.Add(ConvertToMultiCameraInput(row.CameraId, inputImage, retained));
                }

                MultiCameraCalibrationSdk.MultiCameraImageOutput output =
                    MultiCameraCalib.StitchImages(inputs, StitchBlendMode);
                try
                {
                    Image = ConvertOutputToHImage(output);
                }
                finally
                {
                    MultiCameraCalibrationSdk.FreePtr(output.imageData);
                }
            }
            finally
            {
                foreach (IDisposable item in retained)
                {
                    item.Dispose();
                }
            }
        }

        private CameraCalibrationSdk.CalibrationBoardParams CreateCalibrationBoardParams()
        {
            var param = new CameraCalibrationSdk.CalibrationBoardParams
            {
                type = (CameraCalibrationSdk.CalibrationBoardType)(int)CalibBoardType,
                PatternCols = PatternCols,
                PatternRows = PatternRows,
                squareSize = SquareSize,
                dictionaryId = (int)DictionaryId,
                markerSize = MarkerSize,
                distanceReal = -1,
                distancePixel = -1
            };

            if ((int)CalibBoardType == 0 && Line1 != null && Line2 != null && DistanceLL(Line1, Line2, out double distance))
            {
                param.distanceReal = RealDistance;
                param.distancePixel = distance;
            }

            if ((int)CalibBoardType != 2)
            {
                param.dictionaryId = -1;
            }

            return param;
        }

        private static void ValidateCalibrationRows(List<ImageNameModel> rows)
        {
            if (rows.Count == 0)
            {
                throw new InvalidOperationException("请至少添加并启用一张标定图片。");
            }

            if (rows.Select(item => item.CameraId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().Count() < 2)
            {
                throw new InvalidOperationException("多相机联合标定至少需要两个不同的相机 ID。");
            }

            foreach (ImageNameModel row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.CameraId))
                {
                    throw new InvalidOperationException($"索引 {row.ID} 的相机 ID 为空。");
                }

                if (string.IsNullOrWhiteSpace(row.CaptureId))
                {
                    throw new InvalidOperationException($"索引 {row.ID} 的标定板位姿为空。");
                }

                if (string.IsNullOrWhiteSpace(row.ImagePath) || !File.Exists(row.ImagePath))
                {
                    throw new InvalidOperationException($"索引 {row.ID} 的图片文件不存在: {row.ImagePath}");
                }
            }
        }

        private static (int width, int height) GetCameraImageSize(string cameraId, List<ImageNameModel> rows)
        {
            int width = -1;
            int height = -1;

            foreach (ImageNameModel row in rows)
            {
                using Mat image = Cv2.ImRead(row.ImagePath, ImreadModes.Unchanged);
                if (image.Empty())
                {
                    throw new InvalidOperationException($"无法读取标定图片: {row.ImagePath}");
                }

                if (width < 0)
                {
                    width = image.Width;
                    height = image.Height;
                    continue;
                }

                if (width != image.Width || height != image.Height)
                {
                    throw new InvalidOperationException($"相机 {cameraId} 下存在尺寸不一致的标定图片。");
                }
            }

            return (width, height);
        }

        private PointSet ResolveInputPointSet()
        {
            object value = GetTransmitParam(InputParams, InputPointSet, false);
            if (value is not PointSet pointSet || pointSet.Length <= 0)
            {
                throw new InvalidOperationException("缺少有效的输入点集。");
            }

            Points = pointSet;
            return pointSet;
        }

        private void ValidateUseCameraId()
        {
            if (string.IsNullOrWhiteSpace(UseCameraId))
            {
                throw new InvalidOperationException("使用标定结果前需要指定相机 ID。");
            }
        }

        private static HImage ResolveInputImage(object inputValue)
        {
            if (inputValue is HImage inputHImage && inputHImage.IsInitialized())
            {
                try
                {
                    HOperatorSet.CopyImage(inputHImage, out HObject copy);
                    return new HImage(copy);
                }
                catch (HalconException ex)
                {
                    throw new InvalidOperationException(
                        $"多视角拼接输入图像已失效，请检查上游模块是否已释放图像。（{ex.Message}）", ex);
                }
            }

            if (inputValue is HObject inputHObject && inputHObject.IsInitialized())
            {
                try
                {
                    HOperatorSet.CopyImage(inputHObject, out HObject copy);
                    return new HImage(copy);
                }
                catch (HalconException ex)
                {
                    throw new InvalidOperationException(
                        $"多视角拼接输入图像已失效，请检查上游模块是否已释放图像。（{ex.Message}）", ex);
                }
            }

            if (inputValue == null)
            {
                throw new InvalidOperationException("多视角拼接缺少有效的输入图像。");
            }

            throw new InvalidOperationException($"多视角拼接输入图像类型不受支持: {inputValue.GetType().FullName}");
        }

        private static MultiCameraCalibrationSdk.MultiCameraImageInput ConvertToMultiCameraInput(
            string cameraId,
            HImage image,
            List<IDisposable> retained)
        {
            retained.Add(image);

            int channels = image.CountChannels();
            string halconType;
            int width;
            int height;
            IntPtr imageData;
            int cvDepth;

            if (channels == 1)
            {
                imageData = image.GetImagePointer1(out halconType, out width, out height);
                cvDepth = GetCvTypeFromHalconType(halconType);
            }
            else if (channels == 3)
            {
                image.GetImagePointer3(out IntPtr ptrRed, out IntPtr ptrGreen, out IntPtr ptrBlue, out halconType, out width, out height);
                cvDepth = GetCvTypeFromHalconType(halconType);

                Mat red = Mat.FromPixelData(height, width, MatType.MakeType(cvDepth, 1), ptrRed);
                Mat green = Mat.FromPixelData(height, width, MatType.MakeType(cvDepth, 1), ptrGreen);
                Mat blue = Mat.FromPixelData(height, width, MatType.MakeType(cvDepth, 1), ptrBlue);
                Mat merged = new Mat();
                Cv2.Merge(new[] { blue, green, red }, merged);

                retained.Add(red);
                retained.Add(green);
                retained.Add(blue);
                retained.Add(merged);
                imageData = merged.Data;
            }
            else
            {
                throw new NotSupportedException($"Unsupported number of channels: {channels}");
            }

            return new MultiCameraCalibrationSdk.MultiCameraImageInput
            {
                cameraId = cameraId,
                imageData = imageData,
                width = width,
                height = height,
                channels = channels,
                cvType = cvDepth + ((channels - 1) << 3)
            };
        }

        private static HImage ConvertOutputToHImage(MultiCameraCalibrationSdk.MultiCameraImageOutput output)
        {
            if (output.imageData == IntPtr.Zero)
            {
                throw new InvalidOperationException("多视角拼接返回空图像。");
            }

            string halconType = GetHalconTypeFromCvType(output.cvType);
            if (output.channels == 1)
            {
                return new HImage(halconType, output.width, output.height, output.imageData);
            }

            if (output.channels == 3)
            {
                int bitsPerChannel = GetBitsPerChannel(halconType);
                HOperatorSet.GenImageInterleaved(
                    out HObject outputImage,
                    output.imageData,
                    "bgr",
                    output.width,
                    output.height,
                    -1,
                    halconType,
                    output.width,
                    output.height,
                    0,
                    0,
                    bitsPerChannel,
                    0);

                HOperatorSet.CopyImage(outputImage, out HObject copy);
                outputImage.Dispose();
                return new HImage(copy);
            }

            throw new NotSupportedException($"Unsupported number of channels: {output.channels}");
        }

        public void UpdateMatrixDisplayData()
        {
            IntrinsicMatrix = SelectedCameraResult?.IntrinsicMatrix ?? new ObservableCollection<double>();
            DistortionCoefficients = SelectedCameraResult?.DistortionCoefficients ?? new ObservableCollection<double>();
            RotationVector = SelectedCameraResult?.RotationVector ?? new ObservableCollection<double>();
            TranslationVector = SelectedCameraResult?.TranslationVector ?? new ObservableCollection<double>();
            ExtrinsicMatrix = SelectedCameraResult?.ExtrinsicMatrix ?? new ObservableCollection<double>();
        }

        private void RaiseCalibrationReportPropertiesChanged()
        {
            RaisePropertyChanged(nameof(ReportCameraCount));
            RaisePropertyChanged(nameof(ReportCaptureCount));
            RaisePropertyChanged(nameof(ReportObservationCount));
            RaisePropertyChanged(nameof(ReportResidualCount));
            RaisePropertyChanged(nameof(ReportInitialRmsError));
            RaisePropertyChanged(nameof(ReportFinalRmsError));
            RaisePropertyChanged(nameof(ReportMaxReprojectionError));
            RaisePropertyChanged(nameof(ReportConnectedComponentCount));
            RaisePropertyChanged(nameof(ReportConvergedText));
        }

        private static string GetHalconTypeFromCvType(int cvType)
        {
            int depth = cvType & 7;
            return depth switch
            {
                0 => "byte",
                1 => "int1",
                2 => "uint2",
                3 => "int2",
                4 => "int4",
                5 => "real",
                6 => "long",
                _ => throw new NotSupportedException($"Unsupported CV type: {cvType}")
            };
        }

        private static int GetCvTypeFromHalconType(string halconType)
        {
            return halconType switch
            {
                "byte" => 0,
                "int1" => 1,
                "uint2" => 2,
                "int2" => 3,
                "int4" => 4,
                "real" => 5,
                "long" => 6,
                _ => throw new NotSupportedException($"Unsupported image type: {halconType}")
            };
        }

        private static int GetBitsPerChannel(string halconType)
        {
            return halconType switch
            {
                "byte" or "int1" => 8,
                "uint2" or "int2" => 16,
                "int4" or "real" => 32,
                "long" => 64,
                _ => throw new NotSupportedException($"Unsupported HALCON type: {halconType}")
            };
        }

        public bool DistanceLL(ROILine lineA, ROILine lineB, out double distance)
        {
            try
            {
                double distance1 = HMisc.DistancePl(lineA.StartY, lineA.StartX, lineB.StartY, lineB.StartX, lineB.EndY, lineB.EndX);
                double distance2 = HMisc.DistancePl(lineA.EndY, lineA.EndX, lineB.StartY, lineB.StartX, lineB.EndY, lineB.EndX);
                double distance3 = HMisc.DistancePl(lineB.StartY, lineB.StartX, lineA.StartY, lineA.StartX, lineA.EndY, lineA.EndX);
                double distance4 = HMisc.DistancePl(lineB.EndY, lineB.EndX, lineA.StartY, lineA.StartX, lineA.EndY, lineA.EndX);

                distance = (distance1 + distance2 + distance3 + distance4) * 0.25;
                return true;
            }
            catch
            {
                distance = -1;
                return false;
            }
        }
    }
}
