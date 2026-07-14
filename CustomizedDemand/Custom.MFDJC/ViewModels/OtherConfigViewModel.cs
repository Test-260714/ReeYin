using Custom.MFDJC.Models;
using Newtonsoft.Json;
using OpenCvSharp;
using ReeYin_V.Core;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Custom.MFDJC.ViewModels
{
    public class OtherConfigViewModel : DialogViewModelBase, INavigationAware
    {
        #region Fields
        ElectroStaticChuck_Algorithm MFDCustomAlgo;
        #endregion

        #region Properties
        private OtherConfigModel _model =new OtherConfigModel();

        public ElectroStaticChuck_MeasureParam Algorithm = new ElectroStaticChuck_MeasureParam();

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
            MFDCustomAlgo = PrismProvider.Container.Resolve(typeof(ElectroStaticChuck_Algorithm)) as ElectroStaticChuck_Algorithm;
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


                        if(!FileHelper.ContainsFolder(FilePath,"0") || !FileHelper.ContainsFolder(FilePath, "0"))
                        {
                            MessageBox.Show("文件夹格式不正确，请检查！");
                            return;
                        }
                        string[] subdirectories = Directory.GetDirectories(FilePath);

                        foreach (string subdirectory in subdirectories)
                        {
                            string grayImageDir = subdirectory + "/gray";
                            string heightImageDir = subdirectory + "/depth";

                            string grayImagePath = FileHelper.GetImagePaths(grayImageDir)[0];
                            string heightImagePath = FileHelper.GetImagePaths(heightImageDir)[0];

                            Mat grayImage = Cv2.ImRead(grayImagePath, ImreadModes.Grayscale);
                            Mat heightImage = Cv2.ImRead(heightImagePath, ImreadModes.Unchanged);
                            List<float[]> grayData = MFDCustomAlgo.ConvertMatToList(grayImage);
                            List<float[]> heightData = MFDCustomAlgo.ConvertMatToList(heightImage);

                            string imageName = Path.GetFileNameWithoutExtension(grayImagePath);
                            string[] parts = imageName.Split('_');
                            double[] values = parts.Select(part => double.Parse(part)).ToArray();
                            if (values.Count() == 6)
                            {
                                Algorithm.IntervalX = values[0];
                                Algorithm.IntervalY = values[1];
                                Algorithm.IntervalZ = 0.1;
                                Algorithm.MinDepth = values[2];
                                Algorithm.MaxDepth = values[3];
                                Algorithm.IsFlip = false;
                                Algorithm.IsScanEnd = false;
                                Algorithm.OffsetX = values[4];
                                Algorithm.OffsetY = values[5];
                            }
                            else if (values.Count() == 7)
                            {
                                Algorithm.IntervalX = values[0];
                                Algorithm.IntervalY = values[1];
                                Algorithm.IntervalZ = values[2];
                                Algorithm.MinDepth = values[3];
                                Algorithm.MaxDepth = values[4];
                                Algorithm.IsFlip = false;
                                Algorithm.IsScanEnd = false;
                                Algorithm.OffsetX = values[5];
                                Algorithm.OffsetY = values[6];
                            }

                            MFDCustomAlgo.Process(grayData, heightData, Algorithm);

                            if (grayImage.Data != IntPtr.Zero)
                                grayImage.Dispose();
                            if (heightImage.Data != IntPtr.Zero)
                                heightImage.Dispose();
                        }

                        MFDCustomAlgo.GetFeature();

                        ElectroStaticChuck_MeasureResult measureResult = MFDCustomAlgo.GetMeasureResult();
                        ProcessedData processedData = new ProcessedData();
                        processedData.SetMemoryPara("ElectroStaticChuck_MeasureResult", measureResult);

                        MFDCustomAlgo.CvDrawResult(measureResult, true);

                        //通知数据更新
                        PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

                        MFDCustomAlgo.Dispose();
                    }
                    break;
  
                default:
                    break;
            }
        });

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var parameters = navigationContext.Parameters;

            int Serail = -999;
            if (parameters.TryGetValue<int>("Serial", out var id))
                Serail = id;

            //var temp = PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches[$"{Serail}"] as SensorDataCollectionModel;
            //Model = temp.OtherConfig;
            //Algorithm = temp.Algorithm;
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
