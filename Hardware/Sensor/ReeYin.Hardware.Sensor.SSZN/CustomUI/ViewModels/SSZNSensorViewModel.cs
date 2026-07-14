using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Logger;
using SR7Link;
using SImagePro;
using HalconDotNet;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using MessageBox = System.Windows.MessageBox;

namespace ReeYin.Hardware.Sensor.SSZN.CustomUI.ViewModels
{
    public class SSZNSensorViewModel : DialogViewModelBase
    {
        #region Fields
        private bool _isRefreshing;
        private int _profileWidth;
        private double _xPitch;
        private string _singleProfileStatus = "";
        private HalconDotNet.HObject _grayImage;
        #endregion

        #region Properties
        private SSZNSensorModel _modelParam;

        public SSZNSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        public int ProfileWidth
        {
            get { return _profileWidth; }
            set { _profileWidth = value; RaisePropertyChanged(); }
        }

        public double XPitch
        {
            get { return _xPitch; }
            set { _xPitch = value; RaisePropertyChanged(); }
        }

        public string SingleProfileStatus
        {
            get { return _singleProfileStatus; }
            set { _singleProfileStatus = value; RaisePropertyChanged(); }
        }

        public HalconDotNet.HObject GrayImage
        {
            get { return _grayImage; }
            set { _grayImage = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public SSZNSensorViewModel()
        {
            ModelParam = new SSZNSensorModel
            {
                Sensor = new SSZNSensor()
            };
        }
        #endregion

        #region Override
        public override void InitParam()
        {
            if (Param != null && (Param is SSZNSensor))
                ModelParam.Sensor = Param as SSZNSensor ?? new SSZNSensor();
            else
                ModelParam.Sensor = new SSZNSensor();

            LoadParamsOnOpen();
            RefreshProfileInfo();

            // 订阅采集完成事件
            if (ModelParam.Sensor != null)
            {
                ModelParam.Sensor.PropertyChanged += Sensor_PropertyChanged;
            }
        }

        private void Sensor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 当采集状态变为Complete时，更新灰度图
            if (e.PropertyName == nameof(SSZNSensor.State) && ModelParam.Sensor.State == ReeYin_V.Core.HardwareState.Complete)
            {
                UpdateGrayImage();
            }
        }
        #endregion

        #region Commands
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.Sensor },
                    });
                    break;
                case "3D显示":
                    Show3DDisplay();
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand<string> SensorCtrlCommand => new DelegateCommand<string>((order) =>
        {
            if (ModelParam?.Sensor == null)
                return;

            switch (order)
            {
                case "开始采集":
                    ModelParam.Sensor.StartCollect();
                    break;
                case "停止采集":
                    ModelParam.Sensor.StopCollect();
                    break;
                case "获取2.5D单条轮廓":
                    GetSingleProfile25D();
                    break;
                case "获取3D单条轮廓":
                    GetSingleProfile3D();
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand<string> SaveCommand => new DelegateCommand<string>((order) =>
        {
            if (ModelParam?.Sensor == null)
                return;

            switch (order)
            {
                case "保存TIFF32":
                    SaveTiff32();
                    break;
                case "保存TIFF16":
                    SaveTiff16();
                    break;
                case "保存32转16":
                    SaveTiff16From32();
                    break;
                case "保存PCD":
                    SavePCD();
                    break;
                case "保存PLY":
                    SavePLY();
                    break;
                case "保存灰度":
                    SaveGray();
                    break;
                case "保存ECD":
                    SaveECD();
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand<string> ValueChangedCommand => new DelegateCommand<string>((order) =>
        {
            if (_isRefreshing || ModelParam?.Sensor == null)
                return;

            var cfg = ModelParam.Sensor.Config;
            switch (order)
            {
                case "采集行数":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.BATCH_POINT, cfg.RowCollected);
                    break;
                case "采样周期":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.SAMPLED_CYCLE, cfg.SampledCycle);
                    break;
                case "批处理开关":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.BATCH_ON_OFF, (int)cfg.BatchOnOff);
                    break;
                case "编码器类型":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.ENCODER_TYPE, (int)cfg.EncoderType);
                    break;
                case "编码器输入模式":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.ENCODER_INPUTMODE, cfg.EncoderInputMode);
                    break;
                case "细化点数":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.REFINING_POINTS, cfg.RefiningPoints);
                    break;
                case "循环模式":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.CYCLICAL_PATTERN, (int)cfg.CyclicalPattern);
                    break;
                case "分段存储":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.SEGMENT_BUFER, cfg.SegmentBuffer ? 1 : 0);
                    break;
                case "批处理输出":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.BATCH_OUTPUT, (int)cfg.BatchOutput);
                    break;
                case "Z量程":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.Z_MEASURING_RANGE, (int)cfg.ZMeasuringRange);
                    break;
                case "感光灵敏度":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.SENSITIVITY, (int)cfg.Sensitivity);
                    break;
                case "曝光时间":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.EXP_TIME, (int)cfg.ExpTime);
                    break;
                case "光亮控制":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.LIGHT_CONTROL, (int)cfg.LightControl);
                    break;
                case "亮度上限":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.LIGHT_MAX, cfg.LightMax);
                    break;
                case "亮度下限":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.LIGHT_MIN, cfg.LightMin);
                    break;
                case "峰值灵敏度":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.PEAK_SENSITIVITY, (int)cfg.PeakSensitivity);
                    break;
                case "峰值选择":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.PEAK_SELECT, (int)cfg.PeakSelect);
                    break;
                case "X轴压缩":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.X_SAMPLING, (int)cfg.XSampling);
                    break;
                case "X轴中位数滤波":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.FILTER_X_MEDIAN, (int)cfg.FilterXMedian);
                    break;
                case "X轴平滑滤波":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.FILTER_X_SMOOTH, (int)cfg.FilterXSmooth);
                    break;
                case "Y轴中位数滤波":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.FILTER_Y_MEDIAN, (int)cfg.FilterYMedian);
                    break;
                case "Y轴平滑滤波":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.FILTER_Y_SMOOTH, (int)cfg.FilterYSmooth);
                    break;
                case "3D2.5D切换":
                    ModelParam.Sensor.TrySetParam(SR7IF_SETTING_ITEM.CHANGE_3D_25D, (int)cfg.Change3D25D);
                    break;
                default:
                    break;
            }
        });
        #endregion

        #region Methods
        private void LoadParamsOnOpen()
        {
            if (ModelParam?.Sensor == null)
                return;

            _isRefreshing = true;
            if (ModelParam.Sensor.EnsureConnectedForParams())
            {
                ModelParam.Sensor.RefreshParamsFromSensor();
            }
            _isRefreshing = false;
        }

        private void RefreshProfileInfo()
        {
            if (ModelParam?.Sensor?.iSR7APi == null)
                return;

            ProfileWidth = ModelParam.Sensor.iSR7APi.GetProfileDataWidth();
            XPitch = ModelParam.Sensor.iSR7APi.GetProfileData_XPitch();
        }

        private void GetSingleProfile25D()
        {
            if (ModelParam?.Sensor?.iSR7APi == null)
                return;

            int width = ModelParam.Sensor.iSR7APi.GetProfileDataWidth();
            if (width <= 0)
            {
                SingleProfileStatus = "获取轮廓宽度失败";
                return;
            }

            int[] heightData = new int[width];
            uint[] encoder = new uint[1];
            int ret = ModelParam.Sensor.iSR7APi.SR7IFGet25DSingleProfile(heightData, encoder);
            SingleProfileStatus = $"2.5D获取结果:{ret}";
        }

        private void GetSingleProfile3D()
        {
            if (ModelParam?.Sensor?.iSR7APi == null)
                return;

            int width = ModelParam.Sensor.iSR7APi.GetProfileDataWidth();
            if (width <= 0)
            {
                SingleProfileStatus = "获取轮廓宽度失败";
                return;
            }

            int[] heightData = new int[width];
            int ret = ModelParam.Sensor.iSR7APi.SR7IFGet3DSingleProfile(heightData);
            SingleProfileStatus = $"3D获取结果:{ret}";
        }

        private bool TryGetSaveFilePath(string typeName, string extension, out string fileNameA, out string fileNameB, out SPointCloudHead pcHead, bool tif16 = false)
        {
            fileNameA = string.Empty;
            fileNameB = string.Empty;
            pcHead = new SPointCloudHead(0, 0, 0, 0, 0);

            if (ModelParam?.Sensor?.iSR7APi == null)
            {
                MessageBox.Show("采集接口未初始化，请先连接并采集数据。", "保存失败");
                return false;
            }

            int width = ModelParam.Sensor.iSR7APi.GetProfileDataWidth();
            if (ModelParam.Sensor.iSR7APi.CameraBOnline)
                width /= 2;

            int height = ModelParam.Sensor.iSR7APi.BatchPoints;
            if (width <= 0 || height <= 0)
            {
                MessageBox.Show("未检测到有效采集数据，请先采集。", "保存失败");
                return false;
            }

            pcHead.width = (uint)width;
            pcHead.height = (uint)height;
            pcHead.xInterval = ModelParam.Sensor.iSR7APi.GetProfileData_XPitch();
            pcHead.yInterval = ModelParam.Sensor.iSR7APi.GetProfileData_XPitch();
            pcHead.zInterval = 1e-5;

            if (tif16)
            {
                typeName += $"_[{pcHead.xInterval},{pcHead.yInterval}]_tif16";
            }

            using (var folderDlg = new FolderBrowserDialog())
            {
                folderDlg.Description = "请选择保存路径";
                folderDlg.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (folderDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return false;
                }

                string folderPath = folderDlg.SelectedPath;
                DateTime now = DateTime.Now;
                fileNameA = Path.Combine(folderPath, $"{now:yyyyMMdd_HHmmssfff}_A_{typeName}.{extension}");
                fileNameB = Path.Combine(folderPath, $"{now:yyyyMMdd_HHmmssfff}_B_{typeName}.{extension}");
                return true;
            }
        }

        private bool EnsureCameraOnline()
        {
            if (ModelParam?.Sensor?.iSR7APi == null || !ModelParam.Sensor.IsConnected)
            {
                MessageBox.Show("传感器未连接，请先连接传感器。", "保存失败");
                return false;
            }

            if (!ModelParam.Sensor.iSR7APi.CameraAOnline)
            {
                MessageBox.Show("相机离线，请检查连接。", "保存失败");
                return false;
            }

            return true;
        }

        private void SaveECD()
        {
            if (!EnsureCameraOnline())
                return;

            if (!TryGetSaveFilePath("ecd", "ecd", out string fileA, out string fileB, out SPointCloudHead pcHead))
                return;

            var sensor = ModelParam.Sensor;
            if (sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 0 && sensor.ImgBuff32[0] != null)
            {
                int retA = SCV.WriteEcd(fileA.ToCharArray(), sensor.ImgBuff32[0], pcHead);
                Logs.LogInfo($"SSZN_SaveECD A Ret:{retA}");
            }

            if (sensor.iSR7APi.CameraBOnline && sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 1 && sensor.ImgBuff32[1] != null)
            {
                int retB = SCV.WriteEcd(fileB.ToCharArray(), sensor.ImgBuff32[1], pcHead);
                Logs.LogInfo($"SSZN_SaveECD B Ret:{retB}");
            }
        }

        private void SaveGray()
        {
            if (!EnsureCameraOnline())
                return;

            if (!TryGetSaveFilePath("gray", "bmp", out string fileA, out string fileB, out SPointCloudHead pcHead))
                return;

            var sensor = ModelParam.Sensor;
            if (sensor.GrayBuff != null && sensor.GrayBuff.Length > 0 && sensor.GrayBuff[0] != null)
            {
                int retA = SCV.SaveBmp(fileA.ToCharArray(), sensor.GrayBuff[0], (int)pcHead.width, (int)pcHead.height);
                Logs.LogInfo($"SSZN_SaveGray A Ret:{retA}");
            }

            if (sensor.iSR7APi.CameraBOnline && sensor.GrayBuff != null && sensor.GrayBuff.Length > 1 && sensor.GrayBuff[1] != null)
            {
                int retB = SCV.SaveBmp(fileB.ToCharArray(), sensor.GrayBuff[1], (int)pcHead.width, (int)pcHead.height);
                Logs.LogInfo($"SSZN_SaveGray B Ret:{retB}");
            }
        }

        private void SavePLY()
        {
            if (!EnsureCameraOnline())
                return;

            if (!TryGetSaveFilePath("ply", "ply", out string fileA, out string fileB, out SPointCloudHead pcHead))
                return;

            var sensor = ModelParam.Sensor;
            if (sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 0 && sensor.ImgBuff32[0] != null)
            {
                int retA = SCV.SavePly(fileA.ToCharArray(), sensor.ImgBuff32[0], pcHead);
                Logs.LogInfo($"SSZN_SavePLY A Ret:{retA}");
            }

            if (sensor.iSR7APi.CameraBOnline && sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 1 && sensor.ImgBuff32[1] != null)
            {
                int retB = SCV.SavePly(fileB.ToCharArray(), sensor.ImgBuff32[1], pcHead);
                Logs.LogInfo($"SSZN_SavePLY B Ret:{retB}");
            }
        }

        private void SavePCD()
        {
            if (!EnsureCameraOnline())
                return;

            if (!TryGetSaveFilePath("pcd", "pcd", out string fileA, out string fileB, out SPointCloudHead pcHead))
                return;

            var sensor = ModelParam.Sensor;
            if (sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 0 && sensor.ImgBuff32[0] != null)
            {
                int retA = SCV.SavePcd(fileA.ToCharArray(), sensor.ImgBuff32[0], pcHead);
                Logs.LogInfo($"SSZN_SavePCD A Ret:{retA}");
            }

            if (sensor.iSR7APi.CameraBOnline && sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 1 && sensor.ImgBuff32[1] != null)
            {
                int retB = SCV.SavePcd(fileB.ToCharArray(), sensor.ImgBuff32[1], pcHead);
                Logs.LogInfo($"SSZN_SavePCD B Ret:{retB}");
            }
        }

        private void SaveTiff32()
        {
            if (!EnsureCameraOnline())
                return;

            if (!TryGetSaveFilePath("tif32", "tif", out string fileA, out string fileB, out SPointCloudHead pcHead))
                return;

            var sensor = ModelParam.Sensor;
            if (sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 0 && sensor.ImgBuff32[0] != null)
            {
                int retA = SCV.Save32Tif(fileA.ToCharArray(), sensor.ImgBuff32[0], pcHead);
                Logs.LogInfo($"SSZN_SaveTiff32 A Ret:{retA}");
            }

            if (sensor.iSR7APi.CameraBOnline && sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 1 && sensor.ImgBuff32[1] != null)
            {
                int retB = SCV.Save32Tif(fileB.ToCharArray(), sensor.ImgBuff32[1], pcHead);
                Logs.LogInfo($"SSZN_SaveTiff32 B Ret:{retB}");
            }
        }

        private void SaveTiff16()
        {
            if (!EnsureCameraOnline())
                return;

            if (ModelParam.Sensor.profile16Bits != 1)
            {
                MessageBox.Show("当前不是16位采集数据，无法保存16位TIFF。", "保存失败");
                return;
            }

            float scale = 0;
            ModelParam.Sensor.iSR7APi.Get16BitScale(out scale);

            if (!TryGetSaveFilePath($"{scale}", "tif", out string fileA, out string fileB, out SPointCloudHead pcHead, true))
                return;

            var sensor = ModelParam.Sensor;
            if (sensor.ImgBuff16 != null && sensor.ImgBuff16.Length > 0 && sensor.ImgBuff16[0] != null)
            {
                int retA = SCV.Save16TifOfShort(fileA.ToCharArray(), sensor.ImgBuff16[0], pcHead, scale, 1);
                Logs.LogInfo($"SSZN_SaveTiff16 A Ret:{retA}");
            }

            if (sensor.iSR7APi.CameraBOnline && sensor.ImgBuff16 != null && sensor.ImgBuff16.Length > 1 && sensor.ImgBuff16[1] != null)
            {
                int retB = SCV.Save16TifOfShort(fileB.ToCharArray(), sensor.ImgBuff16[1], pcHead, scale, 1);
                Logs.LogInfo($"SSZN_SaveTiff16 B Ret:{retB}");
            }
        }

        private void SaveTiff16From32()
        {
            if (!EnsureCameraOnline())
                return;

            if (ModelParam.Sensor.profile16Bits != 0)
            {
                MessageBox.Show("当前不是32位采集数据，无法执行32转16保存。", "保存失败");
                return;
            }

            float scale = 0;
            ModelParam.Sensor.iSR7APi.Get16BitScale(out scale);

            if (!TryGetSaveFilePath($"{scale}", "tif", out string fileA, out string fileB, out SPointCloudHead pcHead, true))
                return;

            var sensor = ModelParam.Sensor;
            if (sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 0 && sensor.ImgBuff32[0] != null)
            {
                int retA = SCV.Save16TifOfScale(fileA.ToCharArray(), sensor.ImgBuff32[0], pcHead, scale);
                Logs.LogInfo($"SSZN_Save32ToTiff16 A Ret:{retA}");
            }

            if (sensor.iSR7APi.CameraBOnline && sensor.ImgBuff32 != null && sensor.ImgBuff32.Length > 1 && sensor.ImgBuff32[1] != null)
            {
                int retB = SCV.Save16TifOfScale(fileB.ToCharArray(), sensor.ImgBuff32[1], pcHead, scale);
                Logs.LogInfo($"SSZN_Save32ToTiff16 B Ret:{retB}");
            }
        }

        /// <summary>
        /// 更新灰度图显示
        /// </summary>
        private void UpdateGrayImage()
        {
            try
            {
                var sensor = ModelParam.Sensor;
                if (sensor == null || sensor.GrayBuff == null || sensor.GrayBuff.Length == 0 || sensor.GrayBuff[0] == null)
                {
                    Logs.LogWarning("SSZN_灰度数据为空，无法显示");
                    return;
                }

                // 获取图像尺寸
                int width = sensor.iSR7APi.GetProfileDataWidth();
                if (sensor.iSR7APi.CameraBOnline)
                    width /= 2;

                int height = sensor.iSR7APi.BatchPoints;

                if (width <= 0 || height <= 0)
                {
                    Logs.LogWarning($"SSZN_图像尺寸无效: {width}x{height}");
                    return;
                }

                // 释放旧图像
                GrayImage?.Dispose();

                // 将byte[]转换为Halcon图像
                HalconDotNet.HOperatorSet.GenImage1(
                    out HalconDotNet.HObject hImage,
                    "byte",
                    width,
                    height,
                    System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(sensor.GrayBuff[0], 0));

                GrayImage = hImage;
                Logs.LogInfo($"SSZN_灰度图更新成功: {width}x{height}");
            }
            catch (Exception ex)
            {
                Logs.LogError($"SSZN_更新灰度图失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示3D图像窗口
        /// </summary>
        private void Show3DDisplay()
        {
            SSZN3DDisplayViewModel? displayViewModel = null;
            try
            {
                // 检查是否有采集数据
                if (ModelParam?.Sensor == null || ModelParam.Sensor.ImgBuff32 == null ||
                    ModelParam.Sensor.ImgBuff32.Length == 0 || ModelParam.Sensor.ImgBuff32[0] == null)
                {
                    MessageBox.Show("没有可显示的采集数据，请先进行采集。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                displayViewModel = new SSZN3DDisplayViewModel();
                if (!displayViewModel.TryLoadHeightDisplayResult(ModelParam.Sensor, out string errorMessage))
                {
                    displayViewModel.Cleanup();
                    MessageBox.Show(errorMessage, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var displayView = new Views.SSZN3DDisplayView();
                displayView.DataContext = displayViewModel;

                // 窗口关闭时清理资源
                displayView.Closed += (s, e) =>
                {
                    displayViewModel.Cleanup();
                };

                // 显示窗口
                displayView.ShowDialog();
            }
            catch (Exception ex)
            {
                displayViewModel?.Cleanup();
                Logs.LogError($"SSZN_显示3D窗口失败: {ex.Message}");
                MessageBox.Show($"显示3D窗口失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        #endregion

    }
}
