using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DialogResult = System.Windows.Forms.DialogResult;

namespace ReeYin.ChartShow.ViewModels
{
    public class HalconImageViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields
        int cnt = 0;

        #endregion

        #region Properties
        private HImage _disposeImage;

        public HImage DisposeImage
        {
            get { return _disposeImage; }
            set { _disposeImage = value; RaisePropertyChanged(); }
        }

        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        public TransmitParam InputImage
        {
            get
            {
                try
                {
                    if (_inputImage != null && _inputImage.Value != null)
                    {
                        var temp = (_inputImage.Value as HObject).Clone();

                        if (temp != null && temp.IsInitialized())
                        {
                            var list = new List<HImage>();

                            for (int i = 1; i <= temp.CountObj(); i++)
                            {
                                list.Add(new HImage(temp.SelectObj(i)));
                            }
                            cnt = 0;
                            DisposeImage = list[cnt].Clone();
                            temp.Dispose();
                        }
                    }
                    return _inputImage;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            set
            {
                _inputImage = value;
            }
        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "加载文件":
                    {
                        string FilePath;
                        using (var ofd = new OpenFileDialog())
                        {
                            ofd.Title = "请选择文件";
                            ofd.InitialDirectory = @"\";
                            ofd.Filter = "文件|*.*";
                            if (ofd.ShowDialog() != DialogResult.OK) return;
                            FilePath = ofd.FileName;
                        }
                        Mat img = Cv2.ImRead(FilePath, ImreadModes.Unchanged);
                        PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("PLC", new ImageResultsDisplay { HeightImage = img }));
                    }
                    break;
                case "下一张":
                    {
                        var temp = (_inputImage.Value as HObject).Clone();

                        var list = new List<HImage>();

                        for (int i = 1; i <= temp.CountObj(); i++)
                        {
                            list.Add(new HImage(temp.SelectObj(i)));
                        }
                        if(cnt >= temp.CountObj() || cnt < 0)
                        {
                            cnt = 0;
                        }
                        DisposeImage = list[cnt].Clone();
                        cnt++;
                    }
                    break;
                case "上一张":
                    {
                        var temp = (_inputImage.Value as HObject).Clone();

                        var list = new List<HImage>();

                        for (int i = 1; i <= temp.CountObj(); i++)
                        {
                            list.Add(new HImage(temp.SelectObj(i)));
                        }
                        if (cnt <= 0 || cnt >= temp.CountObj())
                        {
                            cnt = temp.CountObj() - 1;
                        }
                        DisposeImage = list[cnt].Clone();
                        cnt--;
                    }
                    break;
                default:
                    break;
            }
        });
        #endregion


    }
}
