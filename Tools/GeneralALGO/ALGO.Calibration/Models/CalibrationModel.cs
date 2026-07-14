using Dm;
using HalconDotNet;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using ReeYin_V.Core.Calibration;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
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
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using static ReeYin_V.Core.Calibration.CameraCalibrationSdk;


namespace ALGO.Calibration.Models
{
    [Serializable]
    public class CalibrationModel : ModelParamBase
    {
        #region Fields
        public CameraCalibrationSdk cameraCalib = new CameraCalibrationSdk();
        #endregion

        #region Properties

        [JsonIgnore]
        private HImage image = null;
        [OutputParam("Image", "被处理的图像")]
        [JsonIgnore]
        public HImage Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ImageType _imageType;
        public ImageType ImageType
        {
            get { return _imageType; }
            set { _imageType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        [JsonIgnore]
        public TransmitParam InputImage
        {
            get
            {
                if (_inputImage != null && _inputImage.Value != null)
                {
                    var temp = ((HObject)_inputImage.Value);
                    var DisposeImage = temp.Clone();
                    mWindowH.HobjectToHimage(DisposeImage);
                    //InitImg();
                }

                return _inputImage;
            }
            set
            {
                _inputImage = value;
                RaisePropertyChanged();
            }
        }



        [JsonIgnore]
        private ExecuteModuleOutput _output;
        [JsonIgnore]
        public ExecuteModuleOutput Output
        {
            get { return _output; }
            set { _output = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private int _sltTriggerIndex = 0;
        /// <summary>
        /// 执行模式（0：标定，1：使用标定结果 ）
        /// </summary>
        public int SltTriggerIndex
        {
            get { return _sltTriggerIndex; }
            set { _sltTriggerIndex = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private string _cameraId = null;
        /// <summary>
        /// 相机ID
        /// </summary>
        public string CameraId
        {
            get { return _cameraId; }
            set { _cameraId = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _calibrationImageDir = null;
        /// <summary>
        /// 待标定的图片目录
        /// </summary>
        public string CalibrationImageDir
        {
            get { return _calibrationImageDir; }
            set { _calibrationImageDir = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private string _calibFileOutputDir = null;
        /// <summary>
        /// 标定结果保存目录
        /// </summary>
        public string CalibFileOutputDir
        {
            get { return _calibFileOutputDir; }
            set { _calibFileOutputDir = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _calibrationFilePath = string.Empty;
        public string CalibrationFilePath
        {
            get { return _calibrationFilePath; }
            set { _calibrationFilePath = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private ObservableCollection<ImageNameModel> _calibImageNameList = new ObservableCollection<ImageNameModel>();
        /// <summary>
        /// 待标定的图片名称列表
        /// </summary>
        public ObservableCollection<ImageNameModel> CalibImageNameList
        {
            get { return _calibImageNameList; }
            set { _calibImageNameList = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private eCalibrationBoardType _calibBoardType = eCalibrationBoardType.像素比;
        /// <summary>
        /// 标定板类型
        /// </summary>
        public eCalibrationBoardType CalibBoardType
        {
            get { return _calibBoardType; }
            set { _calibBoardType = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private eCalibUsageModel _calibUsageModel = eCalibUsageModel.像素转世界坐标;
        /// <summary>
        /// 使用标定结果的模式（像素转世界坐标，"世界坐标转像素"，"图像校正" ）
        /// </summary>
        public eCalibUsageModel CalibUsageModel
        {
            get { return _calibUsageModel; }
            set { _calibUsageModel = value; RaisePropertyChanged(); }
        }


        // 棋盘格 / Charuco 公共
        [JsonIgnore]
        private int _patternCols = -1;
        /// <summary>
        /// 棋盘格列数
        /// </summary>
        public int PatternCols 
        {
            get { return _patternCols; }
            set { _patternCols = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _patternRows = -1;
        /// <summary>
        /// 棋盘格行数
        /// </summary>
        public int PatternRows
        {
            get { return _patternRows; }
            set { _patternRows = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _squareSize = -1;
        /// <summary>
        /// 棋盘格尺寸
        /// </summary>
        public double SquareSize
        {
            get { return _squareSize; }
            set { _squareSize = value; RaisePropertyChanged(); }
        }

        // Charuco 追加
        [JsonIgnore]
        private double _heightCompensation;
        public double HeightCompensation
        {
            get { return _heightCompensation; }
            set { _heightCompensation = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _markerSize = -1;
        /// <summary>
        /// 二维码尺寸
        /// </summary>
        public double MarkerSize
        {
            get { return _markerSize; }
            set { _markerSize = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _squareSizePixel = -1;
        public double SquareSizePixel
        {
            get { return _squareSizePixel; }
            set { _squareSizePixel = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _markerSizePixel = -1;
        public double MarkerSizePixel
        {
            get { return _markerSizePixel; }
            set { _markerSizePixel = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        //private PredefinedDictionaryName _dictionaryId = PredefinedDictionaryName.Dict4X4_50;
        private PredefinedDictionaryName _dictionaryId = (PredefinedDictionaryName)(-1);
        /// <summary>
        /// 二维码字典
        /// </summary>
        public PredefinedDictionaryName DictionaryId
        {
            get { return _dictionaryId; }
            set { _dictionaryId = value; RaisePropertyChanged(); }
        }

        // 像素比标定
        [JsonIgnore]
        private ROILine line1 = null;
        /// <summary>
        /// 直线1
        /// </summary>
        public ROILine Line1
        {
            get { return line1; }
            set { line1 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ROILine line2 = null;
        /// <summary>
        /// 直线2
        /// </summary>
        public ROILine Line2
        {
            get { return line2; }
            set { line2 = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private double _realDistance;
        /// <summary>
        /// 直线1与直线2的物理距离
        /// </summary>
        public double RealDistance
        {
            get { return _realDistance; }
            set { _realDistance = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _inputLine1 = new TransmitParam();
        /// <summary>
        /// 直线1信息
        /// </summary>
        public TransmitParam InputLine1
        {
            get
            {
                if (_inputLine1 != null && _inputLine1.Value != null)
                {
                    Line tmpLine = (Line)_inputLine1.Value;
                    Line1 = new ROILine(tmpLine.RowBegin, tmpLine.ColumnBegin, tmpLine.RowEnd, tmpLine.ColumnEnd);
                }
                return _inputLine1;
            }
            set
            {
                _inputLine1 = value;
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        private TransmitParam _inputLine2 = new TransmitParam();
        /// <summary>
        /// 直线2信息
        /// </summary>
        public TransmitParam InputLine2
        {
            get
            {
                if (_inputLine2 != null && _inputLine2.Value != null)
                {
                    Line tmpLine = (Line)_inputLine2.Value;
                    Line2 = new ROILine(tmpLine.RowBegin, tmpLine.ColumnBegin, tmpLine.RowEnd, tmpLine.ColumnEnd);
                }
                return _inputLine2;
            }
            set
            {
                _inputLine2 = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private PointSet _points = null;
        /// <summary>
        /// 待转换点集
        /// </summary>
        public PointSet Points
        {
            get { return _points; }
            set { _points = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private TransmitParam _inputPointSet;
        /// <summary>
        /// 输入待转换点集
        /// </summary>
        public TransmitParam InputPointSet
        {
            get
            {
                if (_inputPointSet != null)
                {
                    Points = (PointSet)_inputPointSet.Value;
                }
                return _inputPointSet;
            }
            set
            {
                _inputPointSet = value;
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        private PointSet _outputPointSet = new PointSet();
        /// <summary>
        /// 已转换点集
        /// </summary>
        [OutputParam("OutputPointSet", "转换后的点集")]
        public PointSet OutputPointSet
        {
            get { return _outputPointSet; }
            set { SetProperty(ref _outputPointSet, value); }
        }

        [JsonIgnore]
        private CameraParams _cameraParams = new CameraParams();
        public CameraParams CameraParams
        {
            get { return _cameraParams; }
            set { SetProperty(ref _cameraParams, value); }
        }


        [JsonIgnore]
        private double _intervalX = -1;
        public double IntervalX
        {
            get { return _intervalX; }
            set { SetProperty(ref _intervalX, value); }
        }


        [JsonIgnore]
        private double _intervalY = -1;
        public double IntervalY
        {
            get { return _intervalY; }
            set { SetProperty(ref _intervalY, value); }
        }


        [JsonIgnore]
        private double _error = -1;
        public double Error
        {
            get { return _error; }
            set { SetProperty(ref _error, value); }
        }


        [JsonIgnore]
        private ObservableCollection<double> _intrinsicMatrix;
        public ObservableCollection<double> IntrinsicMatrix
        {
            get { return _intrinsicMatrix; }
            set { SetProperty(ref _intrinsicMatrix, value); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _distortionCoefficients;
        public ObservableCollection<double> DistortionCoefficients
        {
            get { return _distortionCoefficients; }
            set { SetProperty(ref _distortionCoefficients, value); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _rotationVector;
        public ObservableCollection<double> RotationVector
        {
            get { return _rotationVector; }
            set { SetProperty(ref _rotationVector, value); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _translationVector;
        public ObservableCollection<double> TranslationVector
        {
            get { return _translationVector; }
            set { SetProperty(ref _translationVector, value); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _extrinsicMatrix;
        public ObservableCollection<double> ExtrinsicMatrix
        {
            get { return _extrinsicMatrix; }
            set { SetProperty(ref _extrinsicMatrix, value); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _homographyMatrix;
        public ObservableCollection<double> HomographyMatrix
        {
            get { return _homographyMatrix; }
            set { SetProperty(ref _homographyMatrix, value); }
        }


        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        #endregion

        #region Constructor
        public CalibrationModel()
        {
            TriggerModuleRun += () =>
            {
                return ExecuteModule().Result;
            };

            PrismProvider.EventAggregator.GetEvent<NodifyRemoveNodeEvent>().Subscribe((order) =>
            {
                if (order != Serial.ToString()) return;
                Dispose();
            }, ThreadOption.UIThread);
        }


        #endregion

        #region Override
        public override void Dispose()
        {
            base.Dispose();

            cameraCalib.Dispose();
        }
        #endregion

        #region Methods
        public void ResetCalibrationSdk()
        {
            cameraCalib?.Dispose();
            cameraCalib = new CameraCalibrationSdk();
        }

        public void LoadCalibrationFile(string filePath)
        {
            ResetCalibrationSdk();
            cameraCalib.loadCalibrationFile(filePath);
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

        public string ResolveCalibrationOutputFile()
        {
            if (string.IsNullOrWhiteSpace(CalibFileOutputDir))
            {
                throw new InvalidOperationException("标定文件导出目录为空。");
            }

            return CalibrationFilePathResolver.ResolveSingleCameraOutputFile(
                CalibFileOutputDir,
                CameraId,
                CalibBoardType);
        }

        public void SaveCalibrationResults(string outputFile)
        {
            cameraCalib.setMeasurementPlaneParams(new MeasurementPlaneParams
            {
                heightCompensation = HeightCompensation
            });
            cameraCalib.saveCalibrationResults(outputFile);
            CalibrationFilePath = outputFile;
            CalibFileOutputDir = Path.GetDirectoryName(outputFile) ?? CalibFileOutputDir;
        }

        private void RefreshCalibrationResultsFromSdk()
        {
            CalibrationBoardParams calibBoardParams = new CalibrationBoardParams();
            cameraCalib.getCalibrationBoardParams(ref calibBoardParams);

            CalibBoardType = (eCalibrationBoardType)calibBoardParams.type;
            PatternCols = calibBoardParams.PatternCols;
            PatternRows = calibBoardParams.PatternRows;
            SquareSize = calibBoardParams.squareSize;
            DictionaryId = (PredefinedDictionaryName)calibBoardParams.dictionaryId;
            MarkerSize = calibBoardParams.markerSize;
            SquareSizePixel = calibBoardParams.squareSizePixel;
            MarkerSizePixel = calibBoardParams.markerSizePixel;

            MeasurementPlaneParams measurementPlaneParams = new MeasurementPlaneParams();
            cameraCalib.getMeasurementPlaneParams(ref measurementPlaneParams);
            HeightCompensation = measurementPlaneParams.heightCompensation;

            CameraParams cameraParams = new CameraParams();
            cameraCalib.getCameraParams(ref cameraParams);
            CameraParams = cameraParams;

            CameraId = cameraParams.cameraId;
            Error = cameraParams.error;
            IntervalX = cameraParams.intervalX;
            IntervalY = cameraParams.intervalY;

            UpdateMatrixDisplayData();
        }

        private bool HasNativeCalibrationState()
        {
            try
            {
                CameraParams cameraParams = new CameraParams();
                cameraCalib.getCameraParams(ref cameraParams);
                return !string.IsNullOrWhiteSpace(cameraParams.cameraId);
            }
            catch
            {
                return false;
            }
        }

        private bool HasDisplayCalibrationCache()
        {
            return !string.IsNullOrWhiteSpace(CameraParams.cameraId)
                || Error >= 0
                || (IntrinsicMatrix?.Count ?? 0) > 0
                || (HomographyMatrix?.Count ?? 0) > 0;
        }

        private string CreateDisplayCalibrationSnapshot()
        {
            return JsonConvert.SerializeObject(new
            {
                BoardType = CalibBoardType,
                PatternCols,
                PatternRows,
                SquareSize,
                DictionaryId,
                MarkerSize,
                SquareSizePixel,
                MarkerSizePixel,
                CameraId,
                Error,
                IntervalX,
                IntervalY,
                Intrinsic = IntrinsicMatrix?.ToArray() ?? Array.Empty<double>(),
                Distortion = DistortionCoefficients?.ToArray() ?? Array.Empty<double>(),
                Rotation = RotationVector?.ToArray() ?? Array.Empty<double>(),
                Translation = TranslationVector?.ToArray() ?? Array.Empty<double>(),
                Extrinsic = ExtrinsicMatrix?.ToArray() ?? Array.Empty<double>(),
                Homography = HomographyMatrix?.ToArray() ?? Array.Empty<double>()
            });
        }

        private static HImage ResolveCorrectionInputImage(object? inputValue)
        {
            if (inputValue is HImage inputHImage && inputHImage.IsInitialized())
            {
                return new HImage(inputHImage);
            }

            if (inputValue is HObject inputHObject && inputHObject.IsInitialized())
            {
                return new HImage(inputHObject);
            }

            if (inputValue == null)
            {
                throw new InvalidOperationException("图像校正缺少有效的输入图像。");
            }

            throw new InvalidOperationException($"图像校正输入图像类型不受支持: {inputValue.GetType().FullName}");
        }

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                switch (SltTriggerIndex)
                {
                    // 标定流程
                    case 0:
                        try
                        {
                            ResetCalibrationSdk();
                            CalibrationFilePath = string.Empty;

                            // 设置标定板参数
                            CalibrationBoardParams calibBoardParams = new CalibrationBoardParams();
                            calibBoardParams.type = (CalibrationBoardType)CalibBoardType;

                            switch(CalibBoardType)
                            {
                                case eCalibrationBoardType.像素比:
                                    if (Line1 != null && Line2 != null)
                                    {
                                        double tmpDist;
                                        bool status = DistanceLL(Line1, Line2, out tmpDist);
                                        if (status)
                                        {
                                            calibBoardParams.distanceReal = RealDistance;
                                            calibBoardParams.distancePixel = tmpDist;
                                        }
                                        else
                                        {
                                            calibBoardParams.distanceReal = -1;
                                            calibBoardParams.distancePixel = -1;
                                        }
                                    }
                                    else
                                    {
                                        calibBoardParams.distanceReal = -1;
                                        calibBoardParams.distancePixel = -1;
                                    }
                                    calibBoardParams.dictionaryId = -1;
                                    break;
                                case eCalibrationBoardType.棋盘格:
                                    calibBoardParams.PatternCols = PatternCols;
                                    calibBoardParams.PatternRows = PatternRows;
                                    calibBoardParams.squareSize = (float)(SquareSize);
                                    calibBoardParams.dictionaryId = -1;
                                    break;
                                case eCalibrationBoardType.Charuco:
                                    calibBoardParams.PatternCols = PatternCols;
                                    calibBoardParams.PatternRows = PatternRows;
                                    calibBoardParams.squareSize = (float)(SquareSize);
                                    calibBoardParams.dictionaryId = (int)DictionaryId;
                                    calibBoardParams.markerSize = (float)(MarkerSize);
                                    calibBoardParams.squareSizePixel = SquareSizePixel;
                                    calibBoardParams.markerSizePixel = MarkerSizePixel;
                                    break;
                                default:
                                    break;
                            }
                            cameraCalib.setCalibrationBoardParams(calibBoardParams);
                            cameraCalib.setMeasurementPlaneParams(new MeasurementPlaneParams
                            {
                                heightCompensation = HeightCompensation
                            });
                            cameraCalib.addCamera(CameraId);
                            
                            foreach (var item in CalibImageNameList)
                            {
                                if(item.IsSelected)
                                {
                                    cameraCalib.addCalibrationImagePath(CameraId, item.ImagePath);
                                }
                            }

                            // 执行标定
                            cameraCalib.calibrate();

                            CameraParams cameraParams = new CameraParams();
                            cameraCalib.getCameraParams(ref cameraParams);
                            CameraParams = cameraParams;

                            CameraId = cameraParams.cameraId;
                            Error = cameraParams.error;
                            IntervalX = cameraParams.intervalX;
                            IntervalY = cameraParams.intervalY;

                            UpdateMatrixDisplayData();

                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"标定过程异常: {ex.Message}");
                            return NodeStatus.Error;
                        }

                        break;
                    
                    // 使用标定流程
                    case 1:

                        try
                        {
                            EnsureCalibrationRuntimeLoaded();
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"加载标定文件失败: {ex.Message}");
                            return NodeStatus.Error;
                        }

                        switch(CalibUsageModel)
                        {
                            case eCalibUsageModel.像素转世界坐标:
                                try
                                {
                                    double[] X, Y;
                                    if(Points != null && cameraCalib != null)
                                    {
                                        int pointNum = Points.Length;
                                        X = new double[pointNum];
                                        Y = new double[pointNum];
                                        
                                        for(int i = 0; i < pointNum; i++)
                                        {
                                            cameraCalib.pixelToWorld(CameraId, Points.Columns[i], Points.Rows[i], out double x, out double y, out double z);
                                            X[i] = x;
                                            Y[i] = y;
                                        }
                                        OutputPointSet = new PointSet(X, Y);
                                    }
                                    else
                                    {
                                        OutputPointSet = new PointSet();

                                        return NodeStatus.Error;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Windows.MessageBox.Show($"像素转坐标世界坐标过程异常: {ex.Message}");
                                    return NodeStatus.Error;
                                }

                                break;

                            case eCalibUsageModel.世界坐标转像素:
                                try
                                {
                                    double[] X, Y;
                                    if (Points != null && cameraCalib != null)
                                    {
                                        int pointNum = Points.Length;
                                        X = new double[pointNum];
                                        Y = new double[pointNum];

                                        for (int i = 0; i < pointNum; i++)
                                        {
                                            cameraCalib.worldToPixel(CameraId, Points.Columns[i], Points.Rows[i], HeightCompensation, out double x, out double y);
                                            X[i] = x;
                                            Y[i] = y;
                                        }
                                        OutputPointSet = new PointSet(X, Y);
                                    }
                                    else
                                    {
                                        OutputPointSet = new PointSet();

                                        return NodeStatus.Error;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Windows.MessageBox.Show($"世界坐标转像素坐标过程异常: {ex.Message}");
                                    return NodeStatus.Error;
                                }

                                break;

                            case eCalibUsageModel.图像校正:
                                try
                                {
                                    using HImage correctionImage = ResolveCorrectionInputImage(_inputImage?.Value);

                                    // 获取HImage的信息
                                    string type;
                                    int inW, inH;
                                    int cvDepth;
                                    int inC = correctionImage.CountChannels();
                                    IntPtr inImageData;
                                    if (inC == 1)
                                    {
                                        inImageData = correctionImage.GetImagePointer1(out type, out inW, out inH);
                                        cvDepth = GetCvTypeFromHalconType(type);
                                    }
                                    else if (inC == 3)
                                    {
                                        IntPtr ptrRed = IntPtr.Zero;
                                        IntPtr ptrGreen = IntPtr.Zero;
                                        IntPtr ptrBlue = IntPtr.Zero;

                                        correctionImage.GetImagePointer3(out ptrRed, out ptrGreen, out ptrBlue, out type, out inW, out inH);

                                        cvDepth = GetCvTypeFromHalconType(type);

                                        //分别生成3张图片
                                        Mat matRed = new Mat();
                                        Mat matGreen = new Mat();
                                        Mat matBlue = new Mat();

                                        matRed = Mat.FromPixelData(inH, inW, MatType.MakeType(cvDepth, 1), ptrRed);
                                        matGreen = Mat.FromPixelData(inH, inW, MatType.MakeType(cvDepth, 1), ptrGreen);
                                        matBlue = Mat.FromPixelData(inH, inW, MatType.MakeType(cvDepth, 1), ptrBlue);

                                        //合成
                                        Mat dst = new Mat();
                                        Mat[] multi = new Mat[] { matBlue, matGreen, matRed };
                                        Cv2.Merge(multi, dst);

                                        inImageData = dst.Data;

                                        //释放
                                        matBlue.Dispose();
                                        matGreen.Dispose();
                                        matRed.Dispose();
                                    }
                                    else
                                    {
                                        throw new Exception("不支持的通道数");
                                    }

                                    // 确定OpenCV类型
                                    int inType = 0;

                                    // 计算OpenCV类型编码 CV_MAKETYPE(depth, channels) = (depth) + ((channels-1) << 3)
                                    inType = cvDepth + ((inC - 1) << 3);

                                    IntPtr outImageData;
                                    int outW, outH, outC, outType;

                                    cameraCalib.imageCorrection(CameraId, inImageData, inW, inH, inC, inType,
                                                                out outImageData, out outW, out outH, out outC, out outType);

                                    // 将返回的数据转换为HImage
                                    string halconType = GetHalconTypeFromCvType(outType);

                                    if (outC == 1)
                                    {
                                        Image = new HImage(halconType, outW, outH, outImageData);
                                    }
                                    else if (outC == 3)
                                    {
                                        string pixelType = halconType;
                                        int bitsPerChannel = 8; // 默认值

                                        // 根据halconType确定位深度
                                        switch (halconType)
                                        {
                                            case "byte":
                                                bitsPerChannel = 8;
                                                break;
                                            case "int1":
                                                bitsPerChannel = 8;
                                                break;
                                            case "uint2":
                                                bitsPerChannel = 16;
                                                break;
                                            case "int2":
                                                bitsPerChannel = 16;
                                                break;
                                            case "int4":
                                                bitsPerChannel = 32;
                                                break;
                                            case "real":
                                                bitsPerChannel = 32;
                                                break;
                                            case "long":
                                                bitsPerChannel = 64;
                                                break;
                                            default:
                                                throw new NotSupportedException($"Unsupported HALCON type: {halconType}");
                                        }

                                        // 计算每行的字节数
                                        int elementSize = Marshal.SizeOf(GetClrTypeFromHalconType(halconType));
                                        int lineWidth = outW * outC * elementSize;

                                        // 使用GenImageInterleaved创建图像
                                        HObject outputImage;
                                        HOperatorSet.GenImageInterleaved(out outputImage, outImageData, "bgr", outW, outH, -1, 
                                                                         halconType, outW, outH, 0, 0, bitsPerChannel, 0);
                                        
                                        Image = new HImage(outputImage);
                                        outputImage.Dispose();
                                    }
                                    else
                                    {
                                        // 处理其他通道数的情况
                                        throw new NotSupportedException($"Unsupported number of channels: {outC}");
                                    }

                                    cameraCalib.freePtr(outImageData);
                                }
                                catch (Exception ex)
                                {
                                    System.Windows.MessageBox.Show($"图像校正过程异常: {ex.Message}");
                                    return NodeStatus.Error;
                                }
                                break;

                            default:
                                break;
                        }
                        break;

                    default:
                        break;
                }

                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this.DeepClone())[item.ParamName];
                }

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：存图模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }


        public void UpdateMatrixDisplayData()
        {
            // 更新内参矩阵显示 (3x3)
            if (CameraParams.intrinsic != null && CameraParams.intrinsic.Length >= 9)
            {
                IntrinsicMatrix = new ObservableCollection<double>(CameraParams.intrinsic);
            }

            // 更新畸变系数显示
            if (CameraParams.distortion != null)
            {
                DistortionCoefficients = new ObservableCollection<double>(CameraParams.distortion);
            }

            // 更新旋转向量显示
            if (CameraParams.rvec != null)
            {
                RotationVector = new ObservableCollection<double>(CameraParams.rvec);
            }

            // 更新平移向量显示
            if (CameraParams.tvec != null)
            {
                TranslationVector = new ObservableCollection<double>(CameraParams.tvec);
            }

            // 更新外参矩阵显示 (4x4)
            if (CameraParams.extrinsic != null && CameraParams.extrinsic.Length >= 16)
            {
                ExtrinsicMatrix = new ObservableCollection<double>(CameraParams.extrinsic);
            }

            // 更新单应性矩阵显示 (3x3)
            if (CameraParams.homographyMatrix != null && CameraParams.homographyMatrix.Length >= 9)
            {
                HomographyMatrix = new ObservableCollection<double>(CameraParams.homographyMatrix);
            }
        }


        private static string GetHalconTypeFromCvType(int cvType)
        {
            int depth = cvType & 7; // 提取深度信息
            int channels = (cvType >> 3) + 1; // 提取通道数

            switch (depth)
            {
                case 0: return "byte";    // CV_8U
                case 1: return "int1";    // CV_8S
                case 2: return "uint2";   // CV_16U
                case 3: return "int2";    // CV_16S
                case 4: return "int4";    // CV_32S
                case 5: return "real";    // CV_32F
                case 6: return "long";    // CV_64F
                default: throw new NotSupportedException($"Unsupported CV type: {cvType}");
            }
        }


        private static Type GetClrTypeFromHalconType(string halconType)
        {
            switch (halconType)
            {
                case "byte": return typeof(byte);
                case "int1": return typeof(sbyte);
                case "uint2": return typeof(ushort);
                case "int2": return typeof(short);
                case "int4": return typeof(int);
                case "real": return typeof(float);
                case "long": return typeof(double);
                default: throw new NotSupportedException($"Unsupported HALCON type: {halconType}");
            }
        }


        private static int GetCvTypeFromHalconType(string halconType)
        {
            int cvDepth = 0;
            // HALCON类型到OpenCV类型的映射
            switch (halconType)
            {
                case "byte": // 8位无符号整数
                    cvDepth = 0; // CV_8U
                    break;
                case "int1": // 8位有符号整数
                    cvDepth = 1; // CV_8S
                    break;
                case "uint2": // 16位无符号整数
                    cvDepth = 2; // CV_16U
                    break;
                case "int2": // 16位有符号整数
                    cvDepth = 3; // CV_16S
                    break;
                case "int4": // 32位有符号整数
                    cvDepth = 4; // CV_32S
                    break;
                case "real": // 32位浮点数
                    cvDepth = 5; // CV_32F
                    break;
                case "long": // 64位整数
                    cvDepth = 6; // CV_64F (虽然不完全匹配，但这是最接近的)
                    break;
                case "complex": // 复数(实部和虚部均为32位浮点数)
                                // 复数类型需要特殊处理，这里暂时不支持
                    throw new NotSupportedException("Complex image type is not supported");
                default:
                    throw new NotSupportedException($"Unsupported image type: {halconType}");
            }
            return cvDepth;
        }


        /// <summary>
        /// 线线距离
        /// </summary>
        public bool DistanceLL(ROILine lineA, ROILine lineB, out double distance)
        {
            try
            {
                double distance1 = 0;
                double distance2 = 0;
                double distance3 = 0;
                double distance4 = 0;

                distance1 = HMisc.DistancePl(lineA.StartY, lineA.StartX, lineB.StartY, lineB.StartX, lineB.EndY, lineB.EndX);
                distance2 = HMisc.DistancePl(lineA.EndY, lineA.EndX, lineB.StartY, lineB.StartX, lineB.EndY, lineB.EndX);
                distance3 = HMisc.DistancePl(lineB.StartY, lineB.StartX, lineA.StartY, lineA.StartX, lineA.EndY, lineA.EndX);
                distance4 = HMisc.DistancePl(lineB.EndY, lineB.EndX, lineA.StartY, lineA.StartX, lineA.EndY, lineA.EndX);

                distance = (distance1 + distance2 + distance3 + distance4) * 0.25;

                return true;
            }
            catch
            {
                distance = -1;

                return false;
            }
        }


        #endregion
    }
}
