using Custom.KBTScraper.Models;
using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.IOC;
using System;
using System.Linq;
using System.Windows;
using static Custom.KBTScraper.Models.KBTGDJC_Algorithm;

namespace Custom.KBTScraper.ViewModels
{
    public class GrayShowViewModel : DialogViewModelBase
    {
        #region Properties
        private KBTGDJC_MeasureResult _result;

        public KBTGDJC_MeasureResult Result
        {
            get { return _result; }
            set
            {
                // 先将当前统计移到上一段
                PrevMinDefectDepth = MinDefectDepth;
                PrevMaxDefectDepth = MaxDefectDepth;
                PrevAvgDefectDepth = AvgDefectDepth;
                PrevMinDefectRadius = MinDefectRadius;
                PrevMaxDefectRadius = MaxDefectRadius;
                PrevAvgDefectRadius = AvgDefectRadius;
                PrevDefectCount = DefectCount;

                // 更新当前结果
                _result = value;
                RaisePropertyChanged();
                UpdateDefectStatistics();
            }
        }

        // 缺陷深度统计
        private double _minDefectDepth = 0.0;
        public double MinDefectDepth
        {
            get { return _minDefectDepth; }
            set { _minDefectDepth = value; RaisePropertyChanged(); }
        }

        private double _maxDefectDepth = 0.0;
        public double MaxDefectDepth
        {
            get { return _maxDefectDepth; }
            set { _maxDefectDepth = value; RaisePropertyChanged(); }
        }

        private double _avgDefectDepth = 0.0;
        public double AvgDefectDepth
        {
            get { return _avgDefectDepth; }
            set { _avgDefectDepth = value; RaisePropertyChanged(); }
        }

        // 缺陷直径统计
        private double _minDefectRadius = 0.0;
        public double MinDefectRadius
        {
            get { return _minDefectRadius; }
            set { _minDefectRadius = value; RaisePropertyChanged(); }
        }

        private double _maxDefectRadius = 0.0;
        public double MaxDefectRadius
        {
            get { return _maxDefectRadius; }
            set { _maxDefectRadius = value; RaisePropertyChanged(); }
        }

        private double _avgDefectRadius = 0.0;
        public double AvgDefectRadius
        {
            get { return _avgDefectRadius; }
            set { _avgDefectRadius = value; RaisePropertyChanged(); }
        }

        // 缺陷数量
        private int _defectCount = 0;
        public int DefectCount
        {
            get { return _defectCount; }
            set { _defectCount = value; RaisePropertyChanged(); }
        }

        // 上一段缺陷深度统计
        private double _prevMinDefectDepth = 0.0;
        public double PrevMinDefectDepth
        {
            get { return _prevMinDefectDepth; }
            set { _prevMinDefectDepth = value; RaisePropertyChanged(); }
        }

        private double _prevMaxDefectDepth = 0.0;
        public double PrevMaxDefectDepth
        {
            get { return _prevMaxDefectDepth; }
            set { _prevMaxDefectDepth = value; RaisePropertyChanged(); }
        }

        private double _prevAvgDefectDepth = 0.0;
        public double PrevAvgDefectDepth
        {
            get { return _prevAvgDefectDepth; }
            set { _prevAvgDefectDepth = value; RaisePropertyChanged(); }
        }

        // 上一段缺陷直径统计
        private double _prevMinDefectRadius = 0.0;
        public double PrevMinDefectRadius
        {
            get { return _prevMinDefectRadius; }
            set { _prevMinDefectRadius = value; RaisePropertyChanged(); }
        }

        private double _prevMaxDefectRadius = 0.0;
        public double PrevMaxDefectRadius
        {
            get { return _prevMaxDefectRadius; }
            set { _prevMaxDefectRadius = value; RaisePropertyChanged(); }
        }

        private double _prevAvgDefectRadius = 0.0;
        public double PrevAvgDefectRadius
        {
            get { return _prevAvgDefectRadius; }
            set { _prevAvgDefectRadius = value; RaisePropertyChanged(); }
        }

        // 上一段缺陷数量
        private int _prevDefectCount = 0;
        public int PrevDefectCount
        {
            get { return _prevDefectCount; }
            set { _prevDefectCount = value; RaisePropertyChanged(); }
        }

        private HObject image = new HObject();
        public HObject Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
        }

        // 上一段图像
        private HObject _previousImage = new HObject();
        public HObject PreviousImage
        {
            get { return _previousImage; }
            set { _previousImage = value; RaisePropertyChanged(); }
        }

        private Mat _myOpenCVMat = new Mat();
        public Mat MyOpenCVMat
        {
            get { return _myOpenCVMat; }
            set { _myOpenCVMat = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Methods
        private void UpdateDefectStatistics()
        {
            if (Result == null || Result.Defects == null || Result.Defects.Count == 0)
            {
                MinDefectDepth = 0.0;
                MaxDefectDepth = 0.0;
                AvgDefectDepth = 0.0;
                MinDefectRadius = 0.0;
                MaxDefectRadius = 0.0;
                AvgDefectRadius = 0.0;
                DefectCount = 0;
                return;
            }

            DefectCount = Result.Defects.Count;

            // 计算深度统计
            var depths = Result.Defects.Select(d => d.DepthFeature).ToList();
            MinDefectDepth = depths.Min();
            MaxDefectDepth = depths.Max();
            AvgDefectDepth = depths.Average();

            // 计算直径统计
            var radii = Result.Defects.Select(d => d.DiameterFeature).ToList();
            MinDefectRadius = radii.Min();
            MaxDefectRadius = radii.Max();
            AvgDefectRadius = radii.Average();
        }
        #endregion

        #region Constructor
        public GrayShowViewModel()
        {
            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
            {
                if (obj.Item1 != "KBTGDJC_MeasureResult") return;
                Result = obj.Item2 as KBTGDJC_MeasureResult;
            }, ThreadOption.BackgroundThread);

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

                    // 将当前图像移到上一段
                    if (Image != null && Image.IsInitialized())
                    {
                        PreviousImage = Image;
                    }

                    // 显示新的当前图像
                    HObject halconImg = ImageHelper.ConvertMatToHObject(img);
                    Image = halconImg;

                    var CustomAlgo = PrismProvider.Container.Resolve(typeof(KBTGDJC_Algorithm)) as KBTGDJC_Algorithm;
                    CustomAlgo?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"显示图像异常: {ex.Message}");
                }
            }, ThreadOption.BackgroundThread);

            // 订阅重置事件，清空显示
            PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Subscribe((msg) =>
            {
                if (msg == "ClearGrayShow")
                {
                    ClearDisplay();
                }
            }, ThreadOption.UIThread);
        }

        /// <summary>
        /// 清空显示
        /// </summary>
        private void ClearDisplay()
        {
            try
            {
                // 生成空白黑色图像用于清空显示
                HOperatorSet.GenImageConst(out HObject emptyImage, "byte", 100, 100);
                Image = emptyImage;
                PreviousImage = emptyImage;
            }
            catch
            {
                // 如果生成失败，设置为null
                Image = null;
                PreviousImage = null;
            }

            // 清空当前段统计
            MinDefectDepth = 0.0;
            MaxDefectDepth = 0.0;
            AvgDefectDepth = 0.0;
            MinDefectRadius = 0.0;
            MaxDefectRadius = 0.0;
            AvgDefectRadius = 0.0;
            DefectCount = 0;

            // 清空上一段统计
            PrevMinDefectDepth = 0.0;
            PrevMaxDefectDepth = 0.0;
            PrevAvgDefectDepth = 0.0;
            PrevMinDefectRadius = 0.0;
            PrevMaxDefectRadius = 0.0;
            PrevAvgDefectRadius = 0.0;
            PrevDefectCount = 0;
        }
        #endregion

        #region Commands
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "保存":
                    {
                        MessageBoxResult result = System.Windows.MessageBox.Show("确定要保存吗?", "操作确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes)
                            return;
                        PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Publish("保存");
                    }
                    break;

                default:
                    break;
            }
        });
        #endregion
    }
}
