using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Services.DataCollectRelated;
using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Helper.ImageOP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Custom.KBTScraper.Models.KBTGDJC_Algorithm;
using Custom.KBTScraper.Models;

namespace Custom.KBTScraper.ViewModels
{
    public class ChartViewModel : DialogViewModelBase
    {
        #region Properties
        private HObject _grayImage = new HObject();
        public HObject GrayImage
        {
            get { return _grayImage; }
            set { _grayImage = value; RaisePropertyChanged(); }
        }

        private HObject _heightImage = new HObject();
        public HObject HeightImage
        {
            get { return _heightImage; }
            set { _heightImage = value; RaisePropertyChanged(); }
        }

        private KBTGDJC_MeasureResult _result;
        public KBTGDJC_MeasureResult Result
        {
            get { return _result; }
            set { _result = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public ChartViewModel()
        {
            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Subscribe((obj) =>
            {
                try
                {
                    Mat img = obj.Gray;
                    if (img == null || img.Empty())
                    {
                        Console.WriteLine("图像为空");
                        return;
                    }

                    HObject halconImg = ImageHelper.ConvertMatToHObject(img);
                    GrayImage = halconImg;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"显示图像异常: {ex.Message}");
                }
            }, ThreadOption.BackgroundThread);

            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
            {
                if (obj.Item1 != "KBTGDJC_MeasureResult") return;
                Result = obj.Item2 as KBTGDJC_MeasureResult;
            }, ThreadOption.BackgroundThread);
        }
        #endregion
    }
}
