using Dm;
using DryIoc;
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace ALGO.ImageOperation
{
    [Serializable]
    public class ImageOperationModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; }

        [OutputParam("Image", "被处理的图像")]
        public HImage Image { get; set; }

        public HImage DisposeImage1 { get; set; }
        public HImage DisposeImage2 { get; set; }
        #endregion

        #region Properties
        [JsonIgnore]
        private TransmitParam _inputImage1 = new TransmitParam();
        /// <summary>
        /// 输入图像1
        /// </summary>
        public TransmitParam InputImage1
        {
            get {
                if (_inputImage1 != null && _inputImage1.Value != null)
                {
                    var temp = ((HImage)_inputImage1.Value);
                    DisposeImage1 = temp.Clone();
                    mWindowH.HobjectToHimage(DisposeImage1);
                    InitImg();
                }

                return _inputImage1; 
            }
            set
            {
                _inputImage1 = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private TransmitParam _inputImage2 = new TransmitParam();
        /// <summary>
        /// 输入图像2
        /// </summary>
        public TransmitParam InputImage2
        {
            get
            {
                if (_inputImage2 != null && _inputImage2.Value != null)
                {
                    var temp = ((HImage)_inputImage2.Value);
                    DisposeImage2 = temp.Clone();
                    Image = temp.Clone();
                    mWindowH.HobjectToHimage(DisposeImage2);
                    InitImg();
                }

                return _inputImage2;
            }
            set
            {
                _inputImage2 = value;
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();


        [JsonIgnore]
        private double _multiplicationFactor = 1;
        /// <summary>
        /// 乘数因子
        /// </summary>
        [JsonIgnore]
        public double MultiplicationFactor
        { 
            get{return _multiplicationFactor; }
            set{ _multiplicationFactor = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private double _addendFactor = 0;
        /// <summary>
        /// 加数因子
        /// </summary>
        [JsonIgnore]
        public double AddendFactor
        {
            get { return _addendFactor; }
            set { _addendFactor = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private eOperatorsType _operatorsType = eOperatorsType.相加;
        /// <summary>
        /// 操作符类型
        /// </summary>
        public eOperatorsType OperatorsType
        {
            get { return _operatorsType; }
            set { _operatorsType = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Constructor
        public ImageOperationModel()
        {
            

            TriggerModuleRun += () =>
            {
                return ExecuteModule().Result;
            };
        }
        #endregion


        #region Override
        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public override bool LoadKeyParam()
        {
            try
            {
                if (_inputImage1.Value != null)
                    (_inputImage1.Value as HImage).Dispose();
                if (_inputImage2.Value != null)
                    (_inputImage2.Value as HImage).Dispose();

                _inputImage1.Value = (GetTransmitParam(InputParams, _inputImage1) as HImage)?.CopyImage();
                _inputImage2.Value = (GetTransmitParam(InputParams, _inputImage2) as HImage)?.CopyImage();

                DisposeImage1 = ((HImage)_inputImage1.Value)?.CopyImage();
                DisposeImage2 = ((HImage)_inputImage2.Value)?.CopyImage();

                Image = ((HImage)_inputImage2.Value)?.CopyImage();
                mWindowH.HobjectToHimage(Image);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常{ex.StackTrace}");
                return false;
            }
        }

        public override void Dispose()
        {
            PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache.Remove(Serial.ToString());

            mWindowH.Dispose();

            base.Dispose();
        }
        #endregion


        #region Methods
        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (DisposeImage1 == null || DisposeImage2 == null)
                        return NodeStatus.None;

                    mHRoi.Clear();


                    switch (OperatorsType)
                    {
                        case eOperatorsType.相加:
                            Image = DisposeImage1.AddImage(DisposeImage2, MultiplicationFactor, AddendFactor);
                            break;
                        case eOperatorsType.相减:
                            Image = DisposeImage1.SubImage(DisposeImage2, MultiplicationFactor, AddendFactor);
                            break;
                        case eOperatorsType.相乘:
                            Image = DisposeImage1.MultImage(DisposeImage2, MultiplicationFactor, AddendFactor);
                            break;
                        case eOperatorsType.相除:
                            Image = DisposeImage1.DivImage(DisposeImage2, MultiplicationFactor, AddendFactor);
                            break;
                        default:
                            break;
                    }

                    mWindowH.HobjectToHimage(Image);
                    InitImg();

                }
                catch (Exception ex)
                {
                    return NodeStatus.Error;
                }

                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                    Console.WriteLine(JsonHelper.Serialize(item.Value));
                }

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                
                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：图片运算模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }



        public void InitImg()
        {
            ShowHRoi();
        }


        public void ShowHRoi()
        {
            if (mWindowH != null)
            {
                mWindowH.ClearROI();
            }

            List<HRoi> roiList = mHRoi.Where(c => c.ModuleName == ModuleName).ToList();
            foreach (HRoi roi in roiList)
            {
                if (roi.roiType == HRoiType.文字显示)
                {
                    HText roiText = (HText)roi;
                    ShowTool.SetFont(
                        mWindowH.hControl.HalconWindow,
                        roiText.size,
                        "false",
                        "false"
                    );
                    ShowTool.SetMsg(
                        mWindowH.hControl.HalconWindow,
                        roiText.text,
                        "image",
                        roiText.row,
                        roiText.col,
                        roiText.drawColor,
                        "false"
                    );
                }
                else
                {
                    mWindowH.WindowH.DispHobject(roi.hobject, roi.drawColor, roi.IsFillDisp);
                }
            }
        }


        public void ShowHRoi(HRoi ROI)
        {
            try
            {
                int index = mHRoi.FindIndex(e => e.roiType == ROI.roiType && e.ModuleName == ROI.ModuleName);
                if (ROI.fors == true)
                {
                    mHRoi.Add(ROI);
                    return;
                }
                if (index > -1)
                    mHRoi[index] = ROI;
                else
                    mHRoi.Add(ROI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        #endregion

    }


    /// <summary>
    /// 运算符类型
    /// </summary>
    public enum eOperatorsType
    {
        相加 = 0,
        相减 = 1,
        相乘 = 2,
        相除 = 3,
    }


}

