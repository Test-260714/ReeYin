using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Custom.WaferRoutePlan
{
    public class PixelToActualCoordinateModel : ModelParamBase
    {
        #region Fields

        #endregion

        #region Properties

        #endregion

        private double _captureXPos;
        public double CaptureXPos
        {
            get => _captureXPos;
            set => SetProperty(ref _captureXPos, value);
        }

        private double _captureYPos;
        public double CaptureYPos
        {
            get => _captureYPos;
            set => SetProperty(ref _captureYPos, value);
        }

        private double _imageWidth = 5120;
        public double ImageWidth
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
        }

        private double _imageHeight = 5120;
        public double ImageHeight
        {
            get => _imageHeight;
            set => SetProperty(ref _imageHeight, value);
        }

        [InputParam(nameof(CircleCenterI), "圆心像素 X 坐标", needDeepCopy: false)]
        private TransmitParam _circleCenterI = new TransmitParam();
        public TransmitParam CircleCenterI
        {
            get => _circleCenterI;
            set => SetProperty(ref _circleCenterI, value);
        }

        [InputParam(nameof(CircleCenterJ), "圆心像素 Y 坐标", needDeepCopy: false)]
        private TransmitParam _circleCenterJ = new TransmitParam();
        public TransmitParam CircleCenterJ
        {
            get => _circleCenterJ;
            set => SetProperty(ref _circleCenterJ, value);
        }

        private double _outputCompensationX;
        [RecipeParam("OutputCompensationX", "点光谱补偿X")]
        public double OutputCompensationX
        {
            get => _outputCompensationX;
            set => SetProperty(ref _outputCompensationX, value);
        }

        private double _outputCompensationY;
        [RecipeParam("OutputCompensationY", "点光谱补偿Y")]
        public double OutputCompensationY
        {
            get => _outputCompensationY;
            set => SetProperty(ref _outputCompensationY, value);
        }

        private string _calibrationFile = string.Empty;
        public string CalibrationFile
        {
            get => _calibrationFile;
            set => SetProperty(ref _calibrationFile, value);
        }

        private string _nPointCalibrationFile = string.Empty;
        public string NPointCalibrationFile
        {
            get => _nPointCalibrationFile;
            set => SetProperty(ref _nPointCalibrationFile, value);
        }

        private double _distancePoint1PixelI;
        public double DistancePoint1PixelI
        {
            get => _distancePoint1PixelI;
            set => SetProperty(ref _distancePoint1PixelI, value);
        }

        private double _distancePoint1PixelJ;
        public double DistancePoint1PixelJ
        {
            get => _distancePoint1PixelJ;
            set => SetProperty(ref _distancePoint1PixelJ, value);
        }

        private double _distancePoint2PixelI;
        public double DistancePoint2PixelI
        {
            get => _distancePoint2PixelI;
            set => SetProperty(ref _distancePoint2PixelI, value);
        }

        private double _distancePoint2PixelJ;
        public double DistancePoint2PixelJ
        {
            get => _distancePoint2PixelJ;
            set => SetProperty(ref _distancePoint2PixelJ, value);
        }

        private double _cameraCenterPosX;
        public double CameraCenterPosX
        {
            get => _cameraCenterPosX;
            set => SetProperty(ref _cameraCenterPosX, value);
        }

        private double _cameraCenterPosY;
        public double CameraCenterPosY
        {
            get => _cameraCenterPosY;
            set => SetProperty(ref _cameraCenterPosY, value);
        }

        private double _pointSpectrumPosX;
        public double PointSpectrumPosX
        {
            get => _pointSpectrumPosX;
            set => SetProperty(ref _pointSpectrumPosX, value);
        }

        private double _pointSpectrumPosY;
        public double PointSpectrumPosY
        {
            get => _pointSpectrumPosY;
            set => SetProperty(ref _pointSpectrumPosY, value);
        }

        private double _calibrationOffsetX;
        public double CalibrationOffsetX
        {
            get => _calibrationOffsetX;
            set => SetProperty(ref _calibrationOffsetX, value);
        }

        private double _calibrationOffsetY;
        public double CalibrationOffsetY
        {
            get => _calibrationOffsetY;
            set => SetProperty(ref _calibrationOffsetY, value);
        }

        [JsonIgnore]
        private double _imageCenterPixelI;
        [JsonIgnore]
        public double ImageCenterPixelI
        {
            get => _imageCenterPixelI;
            set => SetProperty(ref _imageCenterPixelI, value);
        }

        [JsonIgnore]
        private double _imageCenterPixelJ;
        [JsonIgnore]
        public double ImageCenterPixelJ
        {
            get => _imageCenterPixelJ;
            set => SetProperty(ref _imageCenterPixelJ, value);
        }

        [JsonIgnore]
        private double _actualOffsetX;
        [JsonIgnore]
        public double ActualOffsetX
        {
            get => _actualOffsetX;
            set => SetProperty(ref _actualOffsetX, value);
        }

        [JsonIgnore]
        private double _actualOffsetY;
        [JsonIgnore]
        public double ActualOffsetY
        {
            get => _actualOffsetY;
            set => SetProperty(ref _actualOffsetY, value);
        }

        [JsonIgnore]
        private double _circleCenterActualX;
        [JsonIgnore]
        public double CircleCenterActualX
        {
            get => _circleCenterActualX;
            set => SetProperty(ref _circleCenterActualX, value);
        }

        [JsonIgnore]
        private double _circleCenterActualY;
        [JsonIgnore]
        public double CircleCenterActualY
        {
            get => _circleCenterActualY;
            set => SetProperty(ref _circleCenterActualY, value);
        }

        [JsonIgnore]
        private double _distancePoint1ActualX;
        [JsonIgnore]
        public double DistancePoint1ActualX
        {
            get => _distancePoint1ActualX;
            set => SetProperty(ref _distancePoint1ActualX, value);
        }

        [JsonIgnore]
        private double _distancePoint1ActualY;
        [JsonIgnore]
        public double DistancePoint1ActualY
        {
            get => _distancePoint1ActualY;
            set => SetProperty(ref _distancePoint1ActualY, value);
        }

        [JsonIgnore]
        private double _distancePoint2ActualX;
        [JsonIgnore]
        public double DistancePoint2ActualX
        {
            get => _distancePoint2ActualX;
            set => SetProperty(ref _distancePoint2ActualX, value);
        }

        [JsonIgnore]
        private double _distancePoint2ActualY;
        [JsonIgnore]
        public double DistancePoint2ActualY
        {
            get => _distancePoint2ActualY;
            set => SetProperty(ref _distancePoint2ActualY, value);
        }

        [JsonIgnore]
        private double _distanceActualOffsetX;
        [JsonIgnore]
        public double DistanceActualOffsetX
        {
            get => _distanceActualOffsetX;
            set => SetProperty(ref _distanceActualOffsetX, value);
        }

        [JsonIgnore]
        private double _distanceActualOffsetY;
        [JsonIgnore]
        public double DistanceActualOffsetY
        {
            get => _distanceActualOffsetY;
            set => SetProperty(ref _distanceActualOffsetY, value);
        }

        [JsonIgnore]
        private double _distanceActualValue;
        [JsonIgnore]
        public double DistanceActualValue
        {
            get => _distanceActualValue;
            set => SetProperty(ref _distanceActualValue, value);
        }

        [JsonIgnore]
        private string _resultMessage = "待执行";
        [JsonIgnore]
        public string ResultMessage
        {
            get => _resultMessage;
            set => SetProperty(ref _resultMessage, value);
        }

        [JsonIgnore]
        private double[] _outCircleCenterActualXY = [0, 0];
        [JsonIgnore]
        [OutputParam("outCircleCenterActualXY", "圆心实际坐标")]
        public double[] OutCircleCenterActualXY
        {
            get => _outCircleCenterActualXY;
            set => SetProperty(ref _outCircleCenterActualXY, value);
        }

        [JsonIgnore]
        private double[] _outActualOffsetXY = [0, 0];
        [JsonIgnore]
        [OutputParam("outActualOffsetXY", "圆心相对拍照中心的实际偏移")]
        public double[] OutActualOffsetXY
        {
            get => _outActualOffsetXY;
            set => SetProperty(ref _outActualOffsetXY, value);
        }

        [JsonIgnore]
        private double[] _outImageCenterPixelIJ = [0, 0];
        [JsonIgnore]
        [OutputParam("outImageCenterPixelIJ", "图像中心像素坐标")]
        public double[] OutImageCenterPixelIJ
        {
            get => _outImageCenterPixelIJ;
            set => SetProperty(ref _outImageCenterPixelIJ, value);
        }

        [JsonIgnore]
        private double[] _outDistanceActualOffsetXY = [0, 0];
        [JsonIgnore]
        [OutputParam("outDistanceActualOffsetXY", "两像素点对应的实际位移")]
        public double[] OutDistanceActualOffsetXY
        {
            get => _outDistanceActualOffsetXY;
            set => SetProperty(ref _outDistanceActualOffsetXY, value);
        }

        [JsonIgnore]
        private double[] _outDistancePoint1ActualXY = [0, 0];
        [JsonIgnore]
        [OutputParam("outDistancePoint1ActualXY", "距离点 1 实际坐标")]
        public double[] OutDistancePoint1ActualXY
        {
            get => _outDistancePoint1ActualXY;
            set => SetProperty(ref _outDistancePoint1ActualXY, value);
        }

        [JsonIgnore]
        private double[] _outDistancePoint2ActualXY = [0, 0];
        [JsonIgnore]
        [OutputParam("outDistancePoint2ActualXY", "距离点 2 实际坐标")]
        public double[] OutDistancePoint2ActualXY
        {
            get => _outDistancePoint2ActualXY;
            set => SetProperty(ref _outDistancePoint2ActualXY, value);
        }

        [JsonIgnore]
        private double _outDistanceActualValue;
        [JsonIgnore]
        [OutputParam("outDistanceActualValue", "两像素点对应的实际距离")]
        public double OutDistanceActualValue
        {
            get => _outDistanceActualValue;
            set => SetProperty(ref _outDistanceActualValue, value);
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        public PixelToActualCoordinateModel()
        {
            _resultMessage = "待执行";
        }

        protected string GetRecipeParamSubjection()
        {
            return "PixelToActualCoordinate";
        }

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                {
                    return true;
                }

                if (!base.OnceInit())
                {
                    return false;
                }

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun = () =>
                    {
                        return ExecuteModule().Result;
                    };
                }

                IsOnceInit = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public override bool LoadKeyParam()
        {
            return base.LoadKeyParam();
        }

        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    LoadKeyParam();
                    ValidateInputs();
                    int[] imageSize = [Convert.ToInt32(ImageWidth), Convert.ToInt32(ImageHeight)];

                    double circleCenterI = GetRequiredCircleCenterValue(nameof(CircleCenterI), CircleCenterI, "X像素坐标");
                    double circleCenterJ = GetRequiredCircleCenterValue(nameof(CircleCenterJ), CircleCenterJ, "Y像素坐标");

                    PixelToActualCoordinate_Algorithm algorithm = new PixelToActualCoordinate_Algorithm(
                        CalibrationFile,
                        NPointCalibrationFile);

                    CircleCenterActualCoordinateResult coordinateResult = algorithm.CalculateCircleCenterActualCoordinate(
                        new CircleCenterActualCoordinateRequest
                        {
                            CaptureActualXY = [CaptureXPos, CaptureYPos],
                            ImageSize = imageSize,
                            CircleCenterPixelIJ = [circleCenterI, circleCenterJ]
                        });

                    PixelPointsActualDistanceResult distanceResult = algorithm.CalculateActualDistanceBetweenPixels(
                        new PixelPointsActualDistanceRequest
                        {
                            Point1PixelIJ = [DistancePoint1PixelI, DistancePoint1PixelJ],
                            Point2PixelIJ = [DistancePoint2PixelI, DistancePoint2PixelJ]
                        });

                    // 点光谱补偿 = 点光谱光点位置 - 相机中心位置，执行时加这个带符号补偿。
                    double[] pointSpectrumOffsetXY =
                    [
                        coordinateResult.ActualOffsetXY[0] + OutputCompensationX,
                        coordinateResult.ActualOffsetXY[1] + OutputCompensationY
                    ];
                    double[] pointSpectrumCircleCenterActualXY =
                    [
                        coordinateResult.CircleCenterActualXY[0] + OutputCompensationX,
                        coordinateResult.CircleCenterActualXY[1] + OutputCompensationY
                    ];

                    OutImageCenterPixelIJ = coordinateResult.ImageCenterPixelIJ;
                    OutActualOffsetXY = pointSpectrumOffsetXY;
                    OutCircleCenterActualXY = pointSpectrumCircleCenterActualXY;
                    OutDistancePoint1ActualXY = distanceResult.Point1ActualXY;
                    OutDistancePoint2ActualXY = distanceResult.Point2ActualXY;
                    OutDistanceActualOffsetXY = distanceResult.ActualOffsetXY;
                    OutDistanceActualValue = distanceResult.ActualDistance;

                    ImageCenterPixelI = coordinateResult.ImageCenterPixelIJ[0];
                    ImageCenterPixelJ = coordinateResult.ImageCenterPixelIJ[1];
                    ActualOffsetX = OutActualOffsetXY[0];
                    ActualOffsetY = OutActualOffsetXY[1];
                    CircleCenterActualX = OutCircleCenterActualXY[0];
                    CircleCenterActualY = OutCircleCenterActualXY[1];
                    DistancePoint1ActualX = distanceResult.Point1ActualXY[0];
                    DistancePoint1ActualY = distanceResult.Point1ActualXY[1];
                    DistancePoint2ActualX = distanceResult.Point2ActualXY[0];
                    DistancePoint2ActualY = distanceResult.Point2ActualXY[1];
                    DistanceActualOffsetX = distanceResult.ActualOffsetXY[0];
                    DistanceActualOffsetY = distanceResult.ActualOffsetXY[1];
                    DistanceActualValue = distanceResult.ActualDistance;

                    ResultMessage = algorithm.HasNPointCalibration
                        ? $"已使用相机标定和 N 点标定完成坐标与两点距离换算。点光谱补偿(光点-相机) X={OutputCompensationX:F4}, Y={OutputCompensationY:F4}，两点物理距离按标定实际平面直接计算。"
                        : $"已使用相机标定完成坐标与两点距离换算。点光谱补偿(光点-相机) X={OutputCompensationX:F4}, Y={OutputCompensationY:F4}，两点物理距离按标定实际平面直接计算。";
                }
                catch (Exception ex)
                {
                    ResultMessage = ex.Message;
                    return NodeStatus.Error;
                }

                foreach (TransmitParam item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                }

                return NodeStatus.Success;
            });

            Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time
            };

            return Task.FromResult(Output);
        }

        private void ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(CalibrationFile))
            {
                throw new InvalidOperationException("请选择相机标定文件。");
            }

            if (ImageWidth <= 0 || ImageHeight <= 0)
            {
                throw new InvalidOperationException("图像宽高必须大于 0。");
            }
        }

        private double GetRequiredCircleCenterValue(string inputParamName, TransmitParam param, string displayName)
        {
            object value = GetMarkedInputParamValue(inputParamName);
            if (TryConvertToDouble(value, out double result))
            {
                return result;
            }

            if (TryConvertToDouble(param?.Value, out result))
            {
                return result;
            }

            throw new InvalidOperationException($"{displayName}未连接或值无效。");
        }

        private static bool TryConvertToDouble(object value, out double result)
        {
            switch (value)
            {
                case null:
                    result = 0;
                    return false;
                case double doubleValue:
                    result = doubleValue;
                    return true;
                case float floatValue:
                    result = floatValue;
                    return true;
                case decimal decimalValue:
                    result = (double)decimalValue;
                    return true;
                case byte byteValue:
                    result = byteValue;
                    return true;
                case sbyte sbyteValue:
                    result = sbyteValue;
                    return true;
                case short shortValue:
                    result = shortValue;
                    return true;
                case ushort ushortValue:
                    result = ushortValue;
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case uint uintValue:
                    result = uintValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case ulong ulongValue:
                    result = ulongValue;
                    return true;
                case string stringValue:
                    return double.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result)
                        || double.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result);
                default:
                    try
                    {
                        result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch
                    {
                        result = 0;
                        return false;
                    }
            }
        }

        public void CalculateCalibrationOffset()
        {
            // 点光谱目标 = 相机中心目标 + (点光谱光点标定位置 - 相机中心标定位置)。
            CalibrationOffsetX = PointSpectrumPosX - CameraCenterPosX;
            CalibrationOffsetY = PointSpectrumPosY - CameraCenterPosY;
            OutputCompensationX = CalibrationOffsetX;
            OutputCompensationY = CalibrationOffsetY;
        }
    }
}
