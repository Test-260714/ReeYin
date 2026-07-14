using Custom.KBTBox.Models;
using OpenCvSharp;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using ReeYin_V.UI.Controls.Loading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using static Custom.KBTBox.KBTDispensing_Algorithm;

namespace Custom.KBTBox.ViewModels
{
    public class OtherConfigViewModel : DialogViewModelBase, INavigationAware
    {
        #region Fields
        KBTDispensing_Algorithm KBTCustomAlgo;
        Drawing drawing = new Drawing();
        LoadingWindow loading = null;
        #endregion

        #region Properties
        private OtherConfigModel _model = new OtherConfigModel();

        private KBTDispensing_MeasureParam _algorithm = new KBTDispensing_MeasureParam();
        public KBTDispensing_MeasureParam Algorithm
        {
            get { return _algorithm; }
            set { _algorithm = value; RaisePropertyChanged(); }
        }

        public OtherConfigModel Model
        {
            get { return _model; }
            set { _model = value; RaisePropertyChanged(); }
        }

        private string _filePath;
        /// <summary>
        /// 从文件中加载图片显示
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Constructor
        public OtherConfigViewModel()
        {
            KBTCustomAlgo = PrismProvider.Container.Resolve(typeof(KBTDispensing_Algorithm)) as KBTDispensing_Algorithm;
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
                case "保存":
                    {
                        MessageBoxResult result = System.Windows.MessageBox.Show("确定要保存吗?", "操作确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes)
                            return;


                        PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Publish("保存");
                    }
                    break;
                case "打开":
                    {
                        try
                        {
                            //先释放之前的资源
                            KBTCustomAlgo.Dispose();
                            PrismProvider.Dispatcher.Invoke(() =>
                            {
                                using (var folderDialog = new FolderBrowserDialog())
                                {
                                    folderDialog.Description = "请选择文件夹";
                                    folderDialog.ShowNewFolderButton = true;

                                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                    {
                                        FilePath = folderDialog.SelectedPath;
                                    }
                                }
                            });
                            Logs.LogInfo($"文件路径为：{FilePath}");
                            string[] subdirectories = Directory.GetDirectories(FilePath);
                            //从文件中加载图片执行算法流程
                            loading = new LoadingWindow()
                            {
                                Topmost = true
                            };

                            Task.Run(() =>
                            {
                                PrismProvider.Dispatcher.BeginInvoke(() =>
                                {
                                    loading.Show();
                                });
                                //SltModel.StopCollect();
                                string[] folders = Directory.GetDirectories(FilePath);

                                for (int i = 0; i < folders.Length; i++)
                                {
                                    string folder = folders[i];

                                    string grayImageDir = folder + "/gray";
                                    string heightImageDir = folder + "/depth";

                                    string grayImagePath = grayImageDir + $"/1_tif.tif";
                                    string heightImageL0Path = heightImageDir + $"/1_tif_0.tif";
                                    string heightImageL1Path = heightImageDir + $"/1_tif_1.tif";

                                    Mat grayImage = Cv2.ImRead(grayImagePath, ImreadModes.Grayscale);
                                    Mat heightImage = Cv2.ImRead(heightImageL0Path, ImreadModes.Unchanged);
                                    Mat height1Image = Cv2.ImRead(heightImageL1Path, ImreadModes.Unchanged);
                                    List<float[]> grayDate = KBTCustomAlgo.ConvertMatToList(grayImage);
                                    List<float[]> heightDate = KBTCustomAlgo.ConvertMatToList(heightImage);
                                    List<float[]> height1Date = KBTCustomAlgo.ConvertMatToList(height1Image);

                                    Stopwatch stopwatch0 = Stopwatch.StartNew();

                                    KBTCustomAlgo.Process(grayDate, heightDate, height1Date, ConvertMeasureParam(Algorithm));

                                    stopwatch0.Stop();
                                    Console.WriteLine($"Process运行时间：{stopwatch0.Elapsed.TotalMilliseconds} 毫秒");
                                }

                                Stopwatch stopwatch1 = Stopwatch.StartNew();
                                KBTDispensing_MeasureResult _measureResult = KBTCustomAlgo.GetMeasureResult();
                                Mat gray, depth;

                                DateTime dateTime = DateTime.Now;
                                drawing.CvDrawResult(_measureResult, out gray, out depth);

                                Console.WriteLine("绘制图形耗时：" + DateTime.Now.Subtract(dateTime).TotalMilliseconds);
                                //推送检测结果
                                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KBTDispensing_MeasureResult", _measureResult));

                                stopwatch1.Stop();
                                Console.WriteLine($"GetMeasureResult运行时间：{stopwatch1.Elapsed.TotalMilliseconds} 毫秒");
                                ProcessedData processedData = new ProcessedData();
                                processedData.SetMemoryPara("MFDJC0_MeasureResult", _measureResult);
                                Stopwatch stopwatch2 = Stopwatch.StartNew();
                                processedData.Gray = gray.Clone();
                                stopwatch2.Stop();
                                Console.WriteLine($"CvDrawResult运行时间：{stopwatch2.Elapsed.TotalMilliseconds} 毫秒");

                                //通知数据更新
                                PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

                                PrismProvider.Dispatcher.BeginInvoke(() =>
                                {
                                    loading.Close();
                                });
                            });

                            KBTDispensing_MeasureResult measureResult = KBTCustomAlgo.GetMeasureResult();
                            ProcessedData processedData = new ProcessedData();
                            processedData.SetMemoryPara("MFDJC0_MeasureResult", measureResult);

                            //通知数据更新
                            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

                            KBTCustomAlgo.Dispose();
                        }
                        catch (Exception ex)
                        {
                            PrismProvider.Dispatcher.BeginInvoke(() =>
                            {
                                loading.Close();
                            });
                            HandyControl.Controls.MessageBox.Show(ex.StackTrace);
                        }
                    }
                    break;

                default:
                    break;
            }
        });

        private KBTDispensing_MeasureParam ConvertMeasureParam(KBTDispensing_MeasureParam param)
        {
            return new KBTDispensing_MeasureParam
            {
                ModelPath = Algorithm.ModelPath,
                IntervalX = Algorithm.IntervalX,
                IntervalY = Algorithm.IntervalY,
                IntervalZ = Algorithm.IntervalZ,
                MinDepth = Algorithm.MinDepth * 10,
                MaxDepth = Algorithm.MaxDepth * 10,
                GlueLowThresh = Algorithm.GlueLowThresh * 10,
                GlueUpThresh = Algorithm.GlueUpThresh * 10,
                FrameLowThresh = Algorithm.FrameLowThresh * 10,
                FrameUpThresh = Algorithm.FrameUpThresh * 10,
                SamplingInterval = Algorithm.SamplingInterval,
                SamplingViewInterval = Algorithm.SamplingViewInterval,
                RefractiveIndex = Algorithm.RefractiveIndex,
                IsSaveImage = Algorithm.IsSaveImage,
                SaveImagePath = Algorithm.SaveImagePath,
            };
        }


        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var parameters = navigationContext.Parameters;

            int Serail = -999;
            if (parameters.TryGetValue<int>("Serial", out var id))
                Serail = id;

            Console.WriteLine($"开始节点{Serail}：加载！");
            var temp = PrismProvider.ProjectManager.GetNodeParamCacheValue<SensorDataCollectionModel>($"{Serail.ToString("D3")}");
            Model = temp.OtherConfig;
            Algorithm = temp.MeasureParam;

            Console.WriteLine($"节点{Serail}：其他参数加载成功！");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;

        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }
        #endregion
    }
}
