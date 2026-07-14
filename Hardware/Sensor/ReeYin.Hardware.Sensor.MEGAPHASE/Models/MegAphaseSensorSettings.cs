using MPSizectorS_DotNet;
using ReeYin_V.Core.Services.Module;
using System;
namespace ReeYin.Hardware.Sensor.MEGAPHASE.Models
{
    public class MegAphaseSensorSettings : BindableBase
    {
        #region Properties
        private WorkingModeType _workingMode = WorkingModeType.Standard3D;
        /// <summary>
        /// 设备工作模式
        /// </summary>
        public WorkingModeType WorkingMode
        {
            get { return _workingMode; }
            set
            {
                if (_workingMode == value)
                    return;
                _workingMode = value;
                RaisePropertyChanged();
            }
        }

        private byte _holdState;
        /// <summary>
        /// 设备Hold状态
        /// </summary>
        public byte HoldState
        {
            get { return _holdState; }
            set
            {
                if (_holdState == value)
                    return;
                _holdState = value;
                RaisePropertyChanged();
            }
        }

        private byte _binningState;
        /// <summary>
        /// Binning使能状态
        /// </summary>
        public byte BinningState
        {
            get { return _binningState; }
            set
            {
                if (_binningState == value)
                    return;
                _binningState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(BinningEnabled));
            }
        }
        /// <summary>
        /// Binning启用
        /// </summary>
        public bool BinningEnabled
        {
            get { return _binningState != 0; }
            set { BinningState = (byte)(value ? 1 : 0); }
        }

        private byte _roiX0Ratio;
        /// <summary>
        /// ROI起点X比例
        /// </summary>
        public byte ROIX0Ratio
        {
            get { return _roiX0Ratio; }
            set
            {
                if (_roiX0Ratio == value)
                    return;
                _roiX0Ratio = value;
                RaisePropertyChanged();
            }
        }

        private byte _roiY0Ratio;
        /// <summary>
        /// ROI起点Y比例
        /// </summary>
        public byte ROIY0Ratio
        {
            get { return _roiY0Ratio; }
            set
            {
                if (_roiY0Ratio == value)
                    return;
                _roiY0Ratio = value;
                RaisePropertyChanged();
            }
        }

        private byte _roiWidthRatio;
        /// <summary>
        /// ROI宽度比例
        /// </summary>
        public byte ROIWidthRatio
        {
            get { return _roiWidthRatio; }
            set
            {
                if (_roiWidthRatio == value)
                    return;
                _roiWidthRatio = value;
                RaisePropertyChanged();
            }
        }

        private byte _roiHeightRatio;
        /// <summary>
        /// ROI高度比例
        /// </summary>
        public byte ROIHeightRatio
        {
            get { return _roiHeightRatio; }
            set
            {
                if (_roiHeightRatio == value)
                    return;
                _roiHeightRatio = value;
                RaisePropertyChanged();
            }
        }

        private TriggerSourceType _triggerSource = TriggerSourceType.SoftTriggerOnly;
        /// <summary>
        /// 触发源类型
        /// </summary>
        public TriggerSourceType TriggerSource
        {
            get { return _triggerSource; }
            set
            {
                if (_triggerSource == value)
                    return;
                _triggerSource = value;
                RaisePropertyChanged();
            }
        }

        private byte _projectionMode;
        /// <summary>
        /// 多头投影模式
        /// </summary>
        public byte ProjectionMode
        {
            get { return _projectionMode; }
            set
            {
                if (_projectionMode == value)
                    return;
                _projectionMode = value;
                RaisePropertyChanged();
                RaiseProjectionEnabledPropertiesChanged();
            }
        }

        /// <summary>
        /// 启用投影0
        /// </summary>
        public bool Projection0Enabled
        {
            get { return GetProjectionEnabled(0); }
            set { SetProjectionEnabled(0, value); }
        }

        /// <summary>
        /// 启用投影1
        /// </summary>
        public bool Projection1Enabled
        {
            get { return GetProjectionEnabled(1); }
            set { SetProjectionEnabled(1, value); }
        }

        /// <summary>
        /// 启用投影2
        /// </summary>
        public bool Projection2Enabled
        {
            get { return GetProjectionEnabled(2); }
            set { SetProjectionEnabled(2, value); }
        }

        /// <summary>
        /// 启用投影3
        /// </summary>
        public bool Projection3Enabled
        {
            get { return GetProjectionEnabled(3); }
            set { SetProjectionEnabled(3, value); }
        }

        private byte _mergeParam0;
        /// <summary>
        /// 多头合并参数0
        /// </summary>
        public byte MergeParam0
        {
            get { return _mergeParam0; }
            set
            {
                if (_mergeParam0 == value)
                    return;
                _mergeParam0 = value;
                RaisePropertyChanged();
            }
        }

        private byte _mergeParam1;
        /// <summary>
        /// 多头合并参数1
        /// </summary>
        public byte MergeParam1
        {
            get { return _mergeParam1; }
            set
            {
                if (_mergeParam1 == value)
                    return;
                _mergeParam1 = value;
                RaisePropertyChanged();
            }
        }

        private byte _mergeParam2;
        /// <summary>
        /// 多头合并参数2
        /// </summary>
        public byte MergeParam2
        {
            get { return _mergeParam2; }
            set
            {
                if (_mergeParam2 == value)
                    return;
                _mergeParam2 = value;
                RaisePropertyChanged();
            }
        }

        private byte _mergeParam3;
        /// <summary>
        /// 多头合并参数3
        /// </summary>
        public byte MergeParam3
        {
            get { return _mergeParam3; }
            set
            {
                if (_mergeParam3 == value)
                    return;
                _mergeParam3 = value;
                RaisePropertyChanged();
            }
        }

        private byte _mergeThreshold;
        /// <summary>
        /// 多头合并阈值
        /// </summary>
        public byte MergeThreshold
        {
            get { return _mergeThreshold; }
            set
            {
                if (_mergeThreshold == value)
                    return;
                _mergeThreshold = value;
                RaisePropertyChanged();
            }
        }

        private byte _mergeNumber;
        /// <summary>
        /// 多头合并数量
        /// </summary>
        public byte MergeNumber
        {
            get { return _mergeNumber; }
            set
            {
                if (_mergeNumber == value)
                    return;
                _mergeNumber = value;
                RaisePropertyChanged();
            }
        }

        private ExposureBasicSettingStructType _exposureBasicSetting;
        /// <summary>
        /// 曝光模式
        /// </summary>
        public ExposureModeType ExposureMode
        {
            get { return _exposureBasicSetting.ExposureMode; }
            set
            {
                if (_exposureBasicSetting.ExposureMode == value)
                    return;
                _exposureBasicSetting.ExposureMode = value;
                RaisePropertyChanged();
                RaiseExposureModePropertiesChanged();
            }
        }
        /// <summary>
        /// 手动模式
        /// </summary>
        public bool ExposureModeManualSelected
        {
            get { return _exposureBasicSetting.ExposureMode == ExposureModeType.Manual; }
            set { SetExposureMode(ExposureModeType.Manual, value); }
        }
        /// <summary>
        /// 手动多次平均模式
        /// </summary>
        public bool ExposureModeManualRepeatSelected
        {
            get { return _exposureBasicSetting.ExposureMode == ExposureModeType.ManualRepeat; }
            set { SetExposureMode(ExposureModeType.ManualRepeat, value); }
        }
        /// <summary>
        /// 指定次数自动曝光
        /// </summary>
        public bool ExposureModeAutoNHDRSelected
        {
            get { return _exposureBasicSetting.ExposureMode == ExposureModeType.AutoNHDR; }
            set { SetExposureMode(ExposureModeType.AutoNHDR, value); }
        }
        /// <summary>
        /// 指定质量自动曝光
        /// </summary>
        public bool ExposureModeAutoPHDRSelected
        {
            get { return _exposureBasicSetting.ExposureMode == ExposureModeType.AutoPHDR; }
            set { SetExposureMode(ExposureModeType.AutoPHDR, value); }
        }
        /// <summary>
        /// 曝光次数
        /// </summary>
        public byte ExposureNumber
        {
            get { return _exposureBasicSetting.ExposureNumber; }
            set
            {
                if (_exposureBasicSetting.ExposureNumber == value)
                    return;
                _exposureBasicSetting.ExposureNumber = value;
                RaisePropertyChanged();
                RaiseExposureNumberPropertiesChanged();
            }
        }
        /// <summary>
        /// 1次曝光
        /// </summary>
        public bool ExposureNumberSingleSelected
        {
            get { return _exposureBasicSetting.ExposureNumber == 1; }
            set { SetExposureNumber(1, value); }
        }
        /// <summary>
        /// 2次曝光
        /// </summary>
        public bool ExposureNumberDoubleSelected
        {
            get { return _exposureBasicSetting.ExposureNumber == 2; }
            set { SetExposureNumber(2, value); }
        }
        /// <summary>
        /// 3次曝光
        /// </summary>
        public bool ExposureNumberTripleSelected
        {
            get { return _exposureBasicSetting.ExposureNumber == 3; }
            set { SetExposureNumber(3, value); }
        }
        /// <summary>
        /// 自动HDR优先级
        /// </summary>
        public byte AutoHDRPriority
        {
            get { return _exposureBasicSetting.AutoHDRPriority; }
            set
            {
                if (_exposureBasicSetting.AutoHDRPriority == value)
                    return;
                _exposureBasicSetting.AutoHDRPriority = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 自动PHDR质量
        /// </summary>
        public byte AutoPHDRQuality
        {
            get { return _exposureBasicSetting.AutoPHDRQuality; }
            set
            {
                if (_exposureBasicSetting.AutoPHDRQuality == value)
                    return;
                _exposureBasicSetting.AutoPHDRQuality = value;
                RaisePropertyChanged();
            }
        }

        private byte _userGain;
        /// <summary>
        /// 用户增益
        /// </summary>
        public byte UserGain
        {
            get { return _userGain; }
            set
            {
                if (_userGain == value)
                    return;
                _userGain = value;
                RaisePropertyChanged();
            }
        }

        private float _exposureIntensity3D1st;
        /// <summary>
        /// 第一路3D曝光强度
        /// </summary>
        public float ExposureIntensity3D_1st
        {
            get { return _exposureIntensity3D1st; }
            set
            {
                if (_exposureIntensity3D1st == value)
                    return;
                _exposureIntensity3D1st = value;
                RaisePropertyChanged();
            }
        }

        private float _exposureIntensity3D2nd;
        /// <summary>
        /// 第二路3D曝光强度
        /// </summary>
        public float ExposureIntensity3D_2nd
        {
            get { return _exposureIntensity3D2nd; }
            set
            {
                if (_exposureIntensity3D2nd == value)
                    return;
                _exposureIntensity3D2nd = value;
                RaisePropertyChanged();
            }
        }

        private float _exposureIntensity3D3rd;
        /// <summary>
        /// 第三路3D曝光强度
        /// </summary>
        public float ExposureIntensity3D_3rd
        {
            get { return _exposureIntensity3D3rd; }
            set
            {
                if (_exposureIntensity3D3rd == value)
                    return;
                _exposureIntensity3D3rd = value;
                RaisePropertyChanged();
            }
        }

        private float _exposureIntensity2D;
        /// <summary>
        /// 2D曝光强度
        /// </summary>
        public float ExposureIntensity2D
        {
            get { return _exposureIntensity2D; }
            set
            {
                if (_exposureIntensity2D == value)
                    return;
                _exposureIntensity2D = value;
                RaisePropertyChanged();
            }
        }

        private byte _multiHeadExpoVarMode;
        /// <summary>
        /// 多头曝光差异模式
        /// </summary>
        public byte MultiHeadExpoVarMode
        {
            get { return _multiHeadExpoVarMode; }
            set
            {
                if (_multiHeadExpoVarMode == value)
                    return;
                _multiHeadExpoVarMode = value;
                RaisePropertyChanged();
                RaiseMultiHeadExpoVarModePropertiesChanged();
            }
        }
        /// <summary>
        /// 多头曝光差异关闭
        /// </summary>
        public bool MultiHeadExpoVarModeCloseSelected
        {
            get { return _multiHeadExpoVarMode == 0; }
            set { SetMultiHeadExpoVarMode(0, value); }
        }
        /// <summary>
        /// 多头曝光差异01
        /// </summary>
        public bool MultiHeadExpoVarMode01Selected
        {
            get { return _multiHeadExpoVarMode == 1; }
            set { SetMultiHeadExpoVarMode(1, value); }
        }
        /// <summary>
        /// 多头曝光差异23
        /// </summary>
        public bool MultiHeadExpoVarMode23Selected
        {
            get { return _multiHeadExpoVarMode == 2; }
            set { SetMultiHeadExpoVarMode(2, value); }
        }
        /// <summary>
        /// 多头曝光差异02
        /// </summary>
        public bool MultiHeadExpoVarMode02Selected
        {
            get { return _multiHeadExpoVarMode == 3; }
            set { SetMultiHeadExpoVarMode(3, value); }
        }
        /// <summary>
        /// 多头曝光差异13
        /// </summary>
        public bool MultiHeadExpoVarMode13Selected
        {
            get { return _multiHeadExpoVarMode == 4; }
            set { SetMultiHeadExpoVarMode(4, value); }
        }

        private byte _multiHeadExpoVarRatio;
        /// <summary>
        /// 多头曝光差异比例
        /// </summary>
        public byte MultiHeadExpoVarRatio
        {
            get { return _multiHeadExpoVarRatio; }
            set
            {
                if (_multiHeadExpoVarRatio == value)
                    return;
                _multiHeadExpoVarRatio = value;
                RaisePropertyChanged();
            }
        }

        private byte _overExposureFilterThreshold;
        /// <summary>
        /// 过曝过滤阈值
        /// </summary>
        public byte OverExposureFilterThreshold
        {
            get { return _overExposureFilterThreshold; }
            set
            {
                if (_overExposureFilterThreshold == value)
                    return;
                _overExposureFilterThreshold = value;
                RaisePropertyChanged();
            }
        }

        private byte _validPointThreshold0;
        /// <summary>
        /// 有效点阈值0
        /// </summary>
        public byte ValidPointThreshold0
        {
            get { return _validPointThreshold0; }
            set
            {
                if (_validPointThreshold0 == value)
                    return;
                _validPointThreshold0 = value;
                RaisePropertyChanged();
            }
        }

        private byte _validPointThreshold1;
        /// <summary>
        /// 有效点阈值1
        /// </summary>
        public byte ValidPointThreshold1
        {
            get { return _validPointThreshold1; }
            set
            {
                if (_validPointThreshold1 == value)
                    return;
                _validPointThreshold1 = value;
                RaisePropertyChanged();
            }
        }

        private byte _burrFilterThreshold0;
        /// <summary>
        /// 毛刺过滤阈值0
        /// </summary>
        public byte BurrFilterThreshold0
        {
            get { return _burrFilterThreshold0; }
            set
            {
                if (_burrFilterThreshold0 == value)
                    return;
                _burrFilterThreshold0 = value;
                RaisePropertyChanged();
            }
        }

        private byte _burrFilterThreshold1;
        /// <summary>
        /// 毛刺过滤阈值1
        /// </summary>
        public byte BurrFilterThreshold1
        {
            get { return _burrFilterThreshold1; }
            set
            {
                if (_burrFilterThreshold1 == value)
                    return;
                _burrFilterThreshold1 = value;
                RaisePropertyChanged();
            }
        }

        private byte _preProcessLoopNum;
        /// <summary>
        /// 预处理循环次数
        /// </summary>
        public byte PreProcessLoopNum
        {
            get { return _preProcessLoopNum; }
            set
            {
                if (_preProcessLoopNum == value)
                    return;
                _preProcessLoopNum = value;
                RaisePropertyChanged();
            }
        }

        private byte _preProcessThreshold;
        /// <summary>
        /// 预处理阈值
        /// </summary>
        public byte PreProcessThreshold
        {
            get { return _preProcessThreshold; }
            set
            {
                if (_preProcessThreshold == value)
                    return;
                _preProcessThreshold = value;
                RaisePropertyChanged();
            }
        }

        private DataOutModeType _dataOutMode;
        /// <summary>
        /// 数据输出模式
        /// </summary>
        public DataOutModeType DataOutMode
        {
            get { return _dataOutMode; }
            set
            {
                if (_dataOutMode == value)
                    return;
                _dataOutMode = value;
                RaisePropertyChanged();
                RaiseDataOutModePropertiesChanged();
            }
        }
        /// <summary>
        /// 浮点点云输出
        /// </summary>
        public bool DataOutModeFloatPointCloudSelected
        {
            get { return _dataOutMode == DataOutModeType.FloatPointCloud; }
            set { SetDataOutMode(DataOutModeType.FloatPointCloud, value); }
        }
        /// <summary>
        /// 定点点云输出
        /// </summary>
        public bool DataOutModeFixPointCloudSelected
        {
            get { return _dataOutMode == DataOutModeType.FixPointCloud; }
            set { SetDataOutMode(DataOutModeType.FixPointCloud, value); }
        }
        /// <summary>
        /// 定点ZMap输出
        /// </summary>
        public bool DataOutModeFixZMapSelected
        {
            get { return _dataOutMode == DataOutModeType.FixZMap; }
            set { SetDataOutMode(DataOutModeType.FixZMap, value); }
        }
        /// <summary>
        /// 简化ZMap输出
        /// </summary>
        public bool DataOutModeFixZMapSimpleSelected
        {
            get { return _dataOutMode == DataOutModeType.FixZMapSimple; }
            set { SetDataOutMode(DataOutModeType.FixZMapSimple, value); }
        }

        private byte _userRTMatrixEnableState;
        /// <summary>
        /// 用户RT矩阵启用状态
        /// </summary>
        public byte UserRTMatrixEnableState
        {
            get { return _userRTMatrixEnableState; }
            set
            {
                if (_userRTMatrixEnableState == value)
                    return;
                _userRTMatrixEnableState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(UserRTMatrixEnabled));
            }
        }
        /// <summary>
        /// 用户RT矩阵启用
        /// </summary>
        public bool UserRTMatrixEnabled
        {
            get { return _userRTMatrixEnableState != 0; }
            set { UserRTMatrixEnableState = (byte)(value ? 1 : 0); }
        }

        private byte _rangeCheckEnableState;
        /// <summary>
        /// 范围检查启用状态
        /// </summary>
        public byte RangeCheckEnableState
        {
            get { return _rangeCheckEnableState; }
            set
            {
                if (_rangeCheckEnableState == value)
                    return;
                _rangeCheckEnableState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RangeCheckEnabled));
            }
        }
        /// <summary>
        /// 范围检查启用
        /// </summary>
        public bool RangeCheckEnabled
        {
            get { return _rangeCheckEnableState != 0; }
            set { RangeCheckEnableState = (byte)(value ? 1 : 0); }
        }

        private PointScaleSettingStructType _pointScaleSetting;
        /// <summary>
        /// 点比例X起点
        /// </summary>
        public float PointScaleX0Pos
        {
            get { return _pointScaleSetting.X0Pos; }
            set
            {
                if (_pointScaleSetting.X0Pos == value)
                    return;
                _pointScaleSetting.X0Pos = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 点比例X增量
        /// </summary>
        public float PointScaleXIncrement
        {
            get { return _pointScaleSetting.XIncrement; }
            set
            {
                if (_pointScaleSetting.XIncrement == value)
                    return;
                _pointScaleSetting.XIncrement = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 点比例Y起点
        /// </summary>
        public float PointScaleY0Pos
        {
            get { return _pointScaleSetting.Y0Pos; }
            set
            {
                if (_pointScaleSetting.Y0Pos == value)
                    return;
                _pointScaleSetting.Y0Pos = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 点比例Y增量
        /// </summary>
        public float PointScaleYIncrement
        {
            get { return _pointScaleSetting.YIncrement; }
            set
            {
                if (_pointScaleSetting.YIncrement == value)
                    return;
                _pointScaleSetting.YIncrement = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 点比例Z起点
        /// </summary>
        public float PointScaleZ0Pos
        {
            get { return _pointScaleSetting.Z0Pos; }
            set
            {
                if (_pointScaleSetting.Z0Pos == value)
                    return;
                _pointScaleSetting.Z0Pos = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 点比例Z增量
        /// </summary>
        public float PointScaleZIncrement
        {
            get { return _pointScaleSetting.ZIncrement; }
            set
            {
                if (_pointScaleSetting.ZIncrement == value)
                    return;
                _pointScaleSetting.ZIncrement = value;
                RaisePropertyChanged();
            }
        }

        private RangeCheckSettingStructType _rangeCheckSetting;
        /// <summary>
        /// 范围检查X最小值
        /// </summary>
        public float RangeUXmin
        {
            get { return _rangeCheckSetting.UXmin; }
            set
            {
                if (_rangeCheckSetting.UXmin == value)
                    return;
                _rangeCheckSetting.UXmin = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 范围检查X最大值
        /// </summary>
        public float RangeUXmax
        {
            get { return _rangeCheckSetting.UXmax; }
            set
            {
                if (_rangeCheckSetting.UXmax == value)
                    return;
                _rangeCheckSetting.UXmax = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 范围检查Y最小值
        /// </summary>
        public float RangeUYmin
        {
            get { return _rangeCheckSetting.UYmin; }
            set
            {
                if (_rangeCheckSetting.UYmin == value)
                    return;
                _rangeCheckSetting.UYmin = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 范围检查Y最大值
        /// </summary>
        public float RangeUYmax
        {
            get { return _rangeCheckSetting.UYmax; }
            set
            {
                if (_rangeCheckSetting.UYmax == value)
                    return;
                _rangeCheckSetting.UYmax = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 范围检查Z最小值
        /// </summary>
        public float RangeUZmin
        {
            get { return _rangeCheckSetting.UZmin; }
            set
            {
                if (_rangeCheckSetting.UZmin == value)
                    return;
                _rangeCheckSetting.UZmin = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 范围检查Z最大值
        /// </summary>
        public float RangeUZmax
        {
            get { return _rangeCheckSetting.UZmax; }
            set
            {
                if (_rangeCheckSetting.UZmax == value)
                    return;
                _rangeCheckSetting.UZmax = value;
                RaisePropertyChanged();
            }
        }

        private RTMatrixStructType _userRTMatrix;
        /// <summary>
        /// 用户RT矩阵R00
        /// </summary>
        public float UserRTMatrixR00
        {
            get { return _userRTMatrix.R00; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixR00), value, v => _userRTMatrix.R00 = v); }
        }
        /// <summary>
        /// 用户RT矩阵R01
        /// </summary>
        public float UserRTMatrixR01
        {
            get { return _userRTMatrix.R01; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixR01), value, v => _userRTMatrix.R01 = v); }
        }
        /// <summary>
        /// 用户RT矩阵R02
        /// </summary>
        public float UserRTMatrixR02
        {
            get { return _userRTMatrix.R02; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixR02), value, v => _userRTMatrix.R02 = v); }
        }
        /// <summary>
        /// 用户RT矩阵R10
        /// </summary>
        public float UserRTMatrixR10
        {
            get { return _userRTMatrix.R10; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixR10), value, v => _userRTMatrix.R10 = v); }
        }
        /// <summary>
        /// 用户RT矩阵R11
        /// </summary>
        public float UserRTMatrixR11
        {
            get { return _userRTMatrix.R11; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixR11), value, v => _userRTMatrix.R11 = v); }
        }
        /// <summary>
        /// 用户RT矩阵R12
        /// </summary>
        public float UserRTMatrixR12
        {
            get { return _userRTMatrix.R12; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixR12), value, v => _userRTMatrix.R12 = v); }
        }
        /// <summary>
        /// 用户RT矩阵R20
        /// </summary>
        public float UserRTMatrixR20
        {
            get { return _userRTMatrix.R20; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixR20), value, v => _userRTMatrix.R20 = v); }
        }
        /// <summary>
        /// 用户RT矩阵R21
        /// </summary>
        public float UserRTMatrixR21
        {
            get { return _userRTMatrix.R21; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixR21), value, v => _userRTMatrix.R21 = v); }
        }
        /// <summary>
        /// 用户RT矩阵R22
        /// </summary>
        public float UserRTMatrixR22
        {
            get { return _userRTMatrix.R22; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixR22), value, v => _userRTMatrix.R22 = v); }
        }
        /// <summary>
        /// 用户RT矩阵T0
        /// </summary>
        public float UserRTMatrixT0
        {
            get { return _userRTMatrix.T0; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixT0), value, v => _userRTMatrix.T0 = v); }
        }
        /// <summary>
        /// 用户RT矩阵T1
        /// </summary>
        public float UserRTMatrixT1
        {
            get { return _userRTMatrix.T1; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixT1), value, v => _userRTMatrix.T1 = v); }
        }
        /// <summary>
        /// 用户RT矩阵T2
        /// </summary>
        public float UserRTMatrixT2
        {
            get { return _userRTMatrix.T2; }
            set { SetUserRTMatrixValue(nameof(UserRTMatrixT2), value, v => _userRTMatrix.T2 = v); }
        }

        private bool _softwarePreprocessEnabled;
        /// <summary>
        /// 软件预处理总开关
        /// </summary>
        public bool SoftwarePreprocessEnabled
        {
            get { return _softwarePreprocessEnabled; }
            set
            {
                if (_softwarePreprocessEnabled == value)
                    return;
                _softwarePreprocessEnabled = value;
                RaisePropertyChanged();
            }
        }

        private SoftwarePreprocessModeType _removeBurrsMode = SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off;
        /// <summary>
        /// 去飞点模式
        /// </summary>
        public SoftwarePreprocessModeType RemoveBurrsMode
        {
            get { return _removeBurrsMode; }
            set
            {
                if (_removeBurrsMode == value)
                    return;
                _removeBurrsMode = value;
                RaisePropertyChanged();
            }
        }

        private byte _removeBurrsWinSize = 5;
        /// <summary>
        /// 去飞点窗口1
        /// </summary>
        public byte RemoveBurrsWinSize
        {
            get { return _removeBurrsWinSize; }
            set
            {
                if (_removeBurrsWinSize == value)
                    return;
                _removeBurrsWinSize = value;
                RaisePropertyChanged();
            }
        }

        private byte _removeBurrsWinSize2 = 2;
        /// <summary>
        /// 去飞点窗口2
        /// </summary>
        public byte RemoveBurrsWinSize2
        {
            get { return _removeBurrsWinSize2; }
            set
            {
                if (_removeBurrsWinSize2 == value)
                    return;
                _removeBurrsWinSize2 = value;
                RaisePropertyChanged();
            }
        }

        private byte _removeBurrsSlopeLevel = 50;
        /// <summary>
        /// 去飞点斜率等级
        /// </summary>
        public byte RemoveBurrsSlopeLevel
        {
            get { return _removeBurrsSlopeLevel; }
            set
            {
                if (_removeBurrsSlopeLevel == value)
                    return;
                _removeBurrsSlopeLevel = value;
                RaisePropertyChanged();
            }
        }

        private byte _removeBurrsNeighborCloseLevel = 50;
        /// <summary>
        /// 去飞点邻近度等级
        /// </summary>
        public byte RemoveBurrsNeighborCloseLevel
        {
            get { return _removeBurrsNeighborCloseLevel; }
            set
            {
                if (_removeBurrsNeighborCloseLevel == value)
                    return;
                _removeBurrsNeighborCloseLevel = value;
                RaisePropertyChanged();
            }
        }

        private byte _removeBurrsNeighborNumLevel = 50;
        /// <summary>
        /// 去飞点邻点数等级
        /// </summary>
        public byte RemoveBurrsNeighborNumLevel
        {
            get { return _removeBurrsNeighborNumLevel; }
            set
            {
                if (_removeBurrsNeighborNumLevel == value)
                    return;
                _removeBurrsNeighborNumLevel = value;
                RaisePropertyChanged();
            }
        }

        private byte _removeBurrsEdgeSuppressLevel = 50;
        /// <summary>
        /// 去飞点边缘抑制等级
        /// </summary>
        public byte RemoveBurrsEdgeSuppressLevel
        {
            get { return _removeBurrsEdgeSuppressLevel; }
            set
            {
                if (_removeBurrsEdgeSuppressLevel == value)
                    return;
                _removeBurrsEdgeSuppressLevel = value;
                RaisePropertyChanged();
            }
        }

        private SoftwarePreprocessModeType _mendMode = SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off;
        /// <summary>
        /// 修补模式
        /// </summary>
        public SoftwarePreprocessModeType MendMode
        {
            get { return _mendMode; }
            set
            {
                if (_mendMode == value)
                    return;
                _mendMode = value;
                RaisePropertyChanged();
            }
        }

        private byte _mendWinSize = 5;
        /// <summary>
        /// 修补窗口1
        /// </summary>
        public byte MendWinSize
        {
            get { return _mendWinSize; }
            set
            {
                if (_mendWinSize == value)
                    return;
                _mendWinSize = value;
                RaisePropertyChanged();
            }
        }

        private byte _mendWinSize2 = 3;
        /// <summary>
        /// 修补窗口2
        /// </summary>
        public byte MendWinSize2
        {
            get { return _mendWinSize2; }
            set
            {
                if (_mendWinSize2 == value)
                    return;
                _mendWinSize2 = value;
                RaisePropertyChanged();
            }
        }

        private byte _mendMethod = 1;
        /// <summary>
        /// 修补方法
        /// </summary>
        public byte MendMethod
        {
            get { return _mendMethod; }
            set
            {
                if (_mendMethod == value)
                    return;
                _mendMethod = value;
                RaisePropertyChanged();
            }
        }

        private SoftwarePreprocessModeType _filtrateMode = SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off;
        /// <summary>
        /// 平滑模式
        /// </summary>
        public SoftwarePreprocessModeType FiltrateMode
        {
            get { return _filtrateMode; }
            set
            {
                if (_filtrateMode == value)
                    return;
                _filtrateMode = value;
                RaisePropertyChanged();
            }
        }

        private byte _filtrateWinSize = 5;
        /// <summary>
        /// 平滑窗口
        /// </summary>
        public byte FiltrateWinSize
        {
            get { return _filtrateWinSize; }
            set
            {
                if (_filtrateWinSize == value)
                    return;
                _filtrateWinSize = value;
                RaisePropertyChanged();
            }
        }

        private byte _filtrateNeighborCloseLevel = 50;
        /// <summary>
        /// 平滑邻近度等级
        /// </summary>
        public byte FiltrateNeighborCloseLevel
        {
            get { return _filtrateNeighborCloseLevel; }
            set
            {
                if (_filtrateNeighborCloseLevel == value)
                    return;
                _filtrateNeighborCloseLevel = value;
                RaisePropertyChanged();
            }
        }

        private byte _filtrateNeighborNumLevel = 50;
        /// <summary>
        /// 平滑邻点数等级
        /// </summary>
        public byte FiltrateNeighborNumLevel
        {
            get { return _filtrateNeighborNumLevel; }
            set
            {
                if (_filtrateNeighborNumLevel == value)
                    return;
                _filtrateNeighborNumLevel = value;
                RaisePropertyChanged();
            }
        }

        private bool _filtrateMendPointOnly;
        /// <summary>
        /// 只平滑修补点
        /// </summary>
        public bool FiltrateMendPointOnly
        {
            get { return _filtrateMendPointOnly; }
            set
            {
                if (_filtrateMendPointOnly == value)
                    return;
                _filtrateMendPointOnly = value;
                RaisePropertyChanged();
            }
        }

        #endregion
        #region Methods
        private bool GetProjectionEnabled(int index)
        {
            return (_projectionMode & (1 << index)) != 0;
        }

        private void SetExposureMode(ExposureModeType mode, bool isSelected)
        {
            if (!isSelected || _exposureBasicSetting.ExposureMode == mode)
                return;
            _exposureBasicSetting.ExposureMode = mode;
            RaisePropertyChanged(nameof(ExposureMode));
            RaiseExposureModePropertiesChanged();
        }

        private void RaiseExposureModePropertiesChanged()
        {
            RaisePropertyChanged(nameof(ExposureModeManualSelected));
            RaisePropertyChanged(nameof(ExposureModeManualRepeatSelected));
            RaisePropertyChanged(nameof(ExposureModeAutoNHDRSelected));
            RaisePropertyChanged(nameof(ExposureModeAutoPHDRSelected));
        }

        private void SetExposureNumber(byte number, bool isSelected)
        {
            if (!isSelected || _exposureBasicSetting.ExposureNumber == number)
                return;
            _exposureBasicSetting.ExposureNumber = number;
            RaisePropertyChanged(nameof(ExposureNumber));
            RaiseExposureNumberPropertiesChanged();
        }

        private void RaiseExposureNumberPropertiesChanged()
        {
            RaisePropertyChanged(nameof(ExposureNumberSingleSelected));
            RaisePropertyChanged(nameof(ExposureNumberDoubleSelected));
            RaisePropertyChanged(nameof(ExposureNumberTripleSelected));
        }

        private void SetProjectionEnabled(int index, bool isEnabled)
        {
            int bit = 1 << index;
            ProjectionMode = (byte)(isEnabled ? _projectionMode | bit : _projectionMode & ~bit);
        }

        private void RaiseProjectionEnabledPropertiesChanged()
        {
            RaisePropertyChanged(nameof(Projection0Enabled));
            RaisePropertyChanged(nameof(Projection1Enabled));
            RaisePropertyChanged(nameof(Projection2Enabled));
            RaisePropertyChanged(nameof(Projection3Enabled));
        }

        private void SetMultiHeadExpoVarMode(byte mode, bool isSelected)
        {
            if (!isSelected || _multiHeadExpoVarMode == mode)
                return;
            _multiHeadExpoVarMode = mode;
            RaisePropertyChanged(nameof(MultiHeadExpoVarMode));
            RaiseMultiHeadExpoVarModePropertiesChanged();
        }

        private void RaiseMultiHeadExpoVarModePropertiesChanged()
        {
            RaisePropertyChanged(nameof(MultiHeadExpoVarModeCloseSelected));
            RaisePropertyChanged(nameof(MultiHeadExpoVarMode01Selected));
            RaisePropertyChanged(nameof(MultiHeadExpoVarMode23Selected));
            RaisePropertyChanged(nameof(MultiHeadExpoVarMode02Selected));
            RaisePropertyChanged(nameof(MultiHeadExpoVarMode13Selected));
        }

        private void SetDataOutMode(DataOutModeType mode, bool isSelected)
        {
            if (!isSelected || _dataOutMode == mode)
                return;
            _dataOutMode = mode;
            RaisePropertyChanged(nameof(DataOutMode));
            RaiseDataOutModePropertiesChanged();
        }

        private void RaiseDataOutModePropertiesChanged()
        {
            RaisePropertyChanged(nameof(DataOutModeFloatPointCloudSelected));
            RaisePropertyChanged(nameof(DataOutModeFixPointCloudSelected));
            RaisePropertyChanged(nameof(DataOutModeFixZMapSelected));
            RaisePropertyChanged(nameof(DataOutModeFixZMapSimpleSelected));
        }

        public ExposureBasicSettingStructType GetExposureBasicSetting()
        {
            return _exposureBasicSetting;
        }
        public PointScaleSettingStructType GetPointScaleSetting()
        {
            return _pointScaleSetting;
        }
        public RangeCheckSettingStructType GetRangeCheckSetting()
        {
            return _rangeCheckSetting;
        }
        public RTMatrixStructType GetUserRTMatrix()
        {
            return _userRTMatrix;
        }

        private void SetUserRTMatrixValue(string propertyName, float value, Action<float> setValue)
        {
            setValue(value);
            RaisePropertyChanged(propertyName);
        }
        #endregion
    }
}
