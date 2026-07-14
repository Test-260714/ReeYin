using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static Custom.KCJC.Models.KCJC0_Algorithm;

namespace Custom.KCJC.ViewModels
{
    public class JudgementChartViewModel : DialogViewModelBase
    {

        #region Fields

        #endregion

        #region Properties
        private HObject image = new HObject();
        public HObject Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public JudgementChartViewModel()
        {
            //订阅
            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Subscribe((obj) =>
            {
                try
                {
                    PrismProvider.Dispatcher.BeginInvoke(() =>
                    {
                        Mat? img = null;
                        try
                        {
                            if (obj.Gray == null || obj.Gray.Empty())
                            {
                                Console.WriteLine("Resize failed: img is null or empty.");
                                return;
                            }
                            // 事件数据里的 Gray 是共享 Mat，页面只使用自己的副本，避免释放后影响其它结果和显示。
                            img = obj.Gray.Clone();

                            KCJC0_MeasureParam measureParam = obj.GetMemoryPara("KCJC0_MeasureParam", new KCJC0_MeasureParam());
                            Mat displayMat = img;
                            Mat? rotated = null;

                            switch (measureParam.GrayImageRotateAngle)
                            {
                                case 90:
                                    rotated = new Mat();
                                    Cv2.Rotate(img, rotated, RotateFlags.Rotate90Clockwise);
                                    displayMat = rotated;
                                    break;
                                case -90:
                                    rotated = new Mat();
                                    Cv2.Rotate(img, rotated, RotateFlags.Rotate90Counterclockwise);
                                    displayMat = rotated;
                                    break;
                                case 180:
                                case -180:
                                    rotated = new Mat();
                                    Cv2.Rotate(img, rotated, RotateFlags.Rotate180);
                                    displayMat = rotated;
                                    break;
                            }

                            HObject halconImg = ImageHelper.ConvertMatToHObject(displayMat);
                            Image?.Dispose();
                            Image = halconImg;
                            rotated?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            img?.Dispose();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.StackTrace}");
                }

            }, ThreadOption.BackgroundThread);
        }
        #endregion

        #region Methods

        #endregion

    }
}
