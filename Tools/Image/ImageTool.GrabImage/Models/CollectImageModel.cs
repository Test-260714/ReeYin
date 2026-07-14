using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OpenCvSharp;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.Camera.Models;
using ReeYin_V.Hardware.Camera.ViewModels;
using ReeYin_V.Share;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ImageTool.GrabImage
{
    [Serializable]
    public class CollectImageModel : ModelParamBase
    {
        #region Fields

        #endregion

        #region Properties
        public Guid Guid { get; set; } = Guid.NewGuid();

        [JsonIgnore]
        private ObservableCollection<CameraBase> _CameraModels;
        [JsonIgnore]
        public ObservableCollection<CameraBase> CameraModels
        {
            get { return _CameraModels; }
            set { _CameraModels = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HImage image = new HImage();
        [OutputParam("Image", "被处理的图像")]
        public HImage Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _filePath;
        /// <summary>
        /// 文件取图路径
        /// </summary>
        [OutputParam("FilePath", "文件取图路径")]
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _sltCamName ="";

        public string SltCamName
        {
            get { return _sltCamName; }
            set { _sltCamName = value; }
        }

        [JsonIgnore]
        private string _linkPath;
        /// <summary>
        /// 链接路径
        /// </summary>
        public string LinkPath
        {
            get { return _linkPath; }
            set { _linkPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _isLinkVisibility = Visibility.Hidden;
        /// <summary>
        /// 链接路径可见性
        /// </summary>
        public Visibility IsLinkVisibility
        {
            get { return _isLinkVisibility; }
            set { _isLinkVisibility = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前采集模式
        /// </summary>
        [JsonIgnore]
        private eTrigMode _AcquisitionMode = eTrigMode.软触发;
        public eTrigMode AcquisitionMode
        {
            get { return _AcquisitionMode; }
            set { _AcquisitionMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _sltTriggerPicIndex = 1;
        /// <summary>
        /// 选择取图方式（0：指定图像，1：文件取图，2：相机取图）
        /// </summary>
        public int SltTriggerPicIndex
        {
            get { return _sltTriggerPicIndex; }
            set { _sltTriggerPicIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private CameraBase _SelectedCameraModel = new CameraBase();
        /// <summary>
        /// 选中的相机
        /// </summary>
        [JsonIgnore]
        public CameraBase SelectedCameraModel
        {
            get { return _SelectedCameraModel; }
            set
            {
                _SelectedCameraModel = value;
                if (value != null)
                {
                    SltCamName = value.CameraNo;
                }
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private string _camSltRotate="Null";
        /// <summary>
        /// 选择相机旋转
        /// </summary>
        public string CamSltRotate
        {
            get { return _camSltRotate; }
            set { _camSltRotate = value; }
        }

        [JsonIgnore]
        private string _sltTriggerMode = "Null";
        /// <summary>
        /// 采集模式
        /// </summary>
        public string SltTriggerMode
        {
            get { return _sltTriggerMode; }
            set { _sltTriggerMode = value; }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore] 
        private ExecuteModuleOutput _output;
        [JsonIgnore]
        public ExecuteModuleOutput Output
        {
            get { return _output; }
            set { _output = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public CollectImageModel()
        {



            EnsureRuntimeState();
        }

        ~CollectImageModel()
        {
            TriggerModuleRun -= () =>
            {
                return ExecuteModule().Result;
            };
        }
        #endregion

        #region Override
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            EnsureRuntimeState();
        }

        public override bool OnceInit()
        {
            EnsureRuntimeState();

            if (IsOnceInit)
            {
                return true;
            }

            if (!base.OnceInit())
            {
                return false;
            }

            TriggerModuleRun ??= () => ExecuteModule().Result;

            IsOnceInit = true;
            return true;
        }

        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public override bool LoadKeyParam()
        {
            try
            {
                EnsureRuntimeState();

                //等待加载完成赋值
                SelectedCameraModel = CameraModels.SingleOrDefault(c => c.CameraNo == SltCamName);

                if (PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache.Keys.Contains(Serial.ToString()) && PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache[Serial.ToString()].Count != 0)
                {
                    //OutputParams = PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache[Serial.ToString()].DeepCopy();
                    var temp = new Dictionary<string, object>();
                    foreach (var item in PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache[Serial.ToString()])
                    {
                        if (item.ParamName == "Image" && item.Value != null)
                        {
                            Image = ((HImage)item.Value).CopyImage();
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 释放相关资源
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

        }
        #endregion

        #region Methods
        private void EnsureRuntimeState()
        {
            var camParam = PrismProvider.HardwareModuleManager.Modules[ConfigKey.CamConfig] as CameraSetModel ?? new CameraSetModel();
            CameraModels = camParam.CameraModels;
        }

        /// <summary>
        /// 执行模块
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                #region 参数检查

                #endregion

                try
                {
                    switch (SltTriggerPicIndex)
                    {
                        //指定图像
                        case 0:
                            if (File.Exists(FilePath))
                            {
                                //// 用 OpenCV 读取
                                //Mat img = Cv2.ImRead(FilePath, ImreadModes.Unchanged);

                                //// 缩放 50%
                                //Mat small = new Mat();
                                //Cv2.Resize(img, small, new OpenCvSharp.Size(0, 0), 0.5, 0.5);

                                //// 保存临时文件
                                //string temp = System.IO.Path.GetTempFileName() + ".png";
                                //Cv2.ImWrite(temp, small);

                                //// 读取 HALCON 图像
                                //HImage halconImg = new HImage();
                                //halconImg.ReadImage(temp);
                                //Image = halconImg;

                                var img = new HImage();
                                img.ReadImage(FilePath);
                                Image = img.CopyImage();
                                //img.Dispose();
                                //Image.ReadImage(FilePath);
                                break;
                            }
                            else
                            {
                                MessageBox.Show("图片路径不存在，请重新选择！");
                                FilePath = "";
                            }

                            break;
                        //文件取图
                        case 1:
                            //if (ImageNameModels == null || ImageNameModels.Count == 0)
                            //{
                            //    break;
                            //}
                            //if (IsCyclicRead)
                            //{
                            //    SelectedIndex++;
                            //    SelectedIndex = SelectedIndex >= ImageNameModels.Count ? 0 : SelectedIndex;
                            //}
                            //if (File.Exists(ImageNameModels[SelectedIndex].ImagePath))
                            //{
                            //    DispImage.ReadImage(ImageNameModels[SelectedIndex].ImagePath);
                            //}

                            break;
                        //相机取图
                        case 2:
                            if (SelectedCameraModel != null && SelectedCameraModel.Connected)
                            {
                                //SelectedCameraModel.ExposeTime = int.Parse(ExposureTime.Value.ToString());
                                //SelectedCameraModel.Gain = int.Parse(Gain.Value.ToString());
                                //SelectedCameraModel.SetExposureTime(SelectedCameraModel.ExposeTime);
                                //SelectedCameraModel.SetGain(SelectedCameraModel.Gain);
                                //SelectedCameraModel.SetSpecifiedParam("Double", "ExposureTime",SelectedCameraModel.ExposeTime);
                                //SelectedCameraModel.SetGain(SelectedCameraModel.Gain);
                                //if (AcquisitionMode == eTrigMode.下降沿)
                                //{
                                //    SelectedCameraModel.TrigMode = eTrigMode.下降沿;
                                //    SelectedCameraModel.SetTriggerMode(eTrigMode.下降沿);
                                //}
                                //else if (AcquisitionMode == eTrigMode.上升沿)
                                //{
                                //    SelectedCameraModel.TrigMode = eTrigMode.上升沿;
                                //    SelectedCameraModel.SetTriggerMode(eTrigMode.上升沿);
                                //}
                                //else
                                //{
                                //    SelectedCameraModel.TrigMode = eTrigMode.软触发;
                                //    SelectedCameraModel.SetTriggerMode(eTrigMode.软触发);
                                //}


                                SelectedCameraModel.EventWait.Reset();
                                SelectedCameraModel.CaptureImage(false);

                                SelectedCameraModel.EventWait.WaitOne(3000);

                                if (SelectedCameraModel.Image != null && SelectedCameraModel.Image.IsInitialized())
                                {
                                    Image = SelectedCameraModel.Image;

                                }

                                //ChangeModuleRunStatus(eRunStatus.OK);
                            }
                            else
                            {
                                MessageBox.Show("相机未连接！");
                                //Logger.AddLog(ModuleParam.ModuleName + ":" + SelectedCameraModel.CameraNo + "相机未连接！", eMsgType.Warn);
                                //ChangeModuleRunStatus(eRunStatus.NG);
                            }
                            break;
                        // 链接路径取图
                        case 3:
                            {

                            }break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    return NodeStatus.Error;

                }

                #region 输出
                // 输出赋值到输出参数
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                }

                var start = DateTime.Now;

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                Console.WriteLine($"{DateTime.Now.Subtract(start).TotalMilliseconds}");
                #endregion

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}:采集模块耗时:{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }



        #endregion

    }



}
