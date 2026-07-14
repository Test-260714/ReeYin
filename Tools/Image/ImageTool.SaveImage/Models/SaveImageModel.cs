using HalconDotNet;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTool.SaveImage.Models
{
    [Serializable]
    public class SaveImageModel : ModelParamBase
    {
        #region Fields

        #endregion

        #region Properties

        [JsonIgnore]
        private string _savePath;
        public string SavePath
        {
            get { return _savePath; }
            set { _savePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ImageType _imageType;
        public ImageType ImageType
        {
            get { return _imageType; }
            set { _imageType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _saveDays = 1;
        public int SaveDays
        {
            get { return _saveDays; }
            set { _saveDays = value; RaisePropertyChanged(); }
        }

        private TransmitParam _inputImage;
        /// <summary>
        /// 输入图像
        /// </summary>
        public TransmitParam InputImage
        {
            get { return _inputImage; }
            set { _inputImage = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        #endregion

        #region Constructor
        public SaveImageModel()
        {
            TriggerModuleRun += () =>
            {
                return ExecuteModule().Result;
            };
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
                if (InputImage != null)
                    if (!HalconImageConverter.SaveImage((HObject)InputImage.Value, SavePath + "\\" + "Test_" + DateTime.Now.ToString("yyyyMMddHHmmssfff")))
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：存图模块执行失败！！！");
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：存图模块执行完成！！！");
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


        #endregion
    }
}
