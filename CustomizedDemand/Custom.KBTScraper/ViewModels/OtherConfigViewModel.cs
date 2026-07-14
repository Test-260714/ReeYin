using Custom.KBTScraper.Models;
using HandyControl.Controls;
using OpenCvSharp;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using ReeYin_V.UI;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using static Custom.KBTScraper.Models.KBTGDJC_Algorithm;

namespace Custom.KBTScraper.ViewModels
{
    public class OtherConfigViewModel : DialogViewModelBase, INavigationAware
    {
        #region Fields
        private KBTGDJC_Algorithm? _customAlgo;
        #endregion

        #region Properties
        private OtherConfigModel _model = new OtherConfigModel();

        private KBTGDJC_MeasureParam _algorithm = new KBTGDJC_MeasureParam();
        public KBTGDJC_MeasureParam Algorithm
        {
            get { return _algorithm; }
            set { _algorithm = value; RaisePropertyChanged(); }
        }

        public OtherConfigModel Model
        {
            get { return _model; }
            set { _model = value; RaisePropertyChanged(); }
        }

        private string _filePath = string.Empty;
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public OtherConfigViewModel()
        {
            _customAlgo = PrismProvider.Container.Resolve(typeof(KBTGDJC_Algorithm)) as KBTGDJC_Algorithm;
        }
        #endregion

        #region Commands
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "保存":
                    {
                        MessageBoxResult result = MessageView.Ins.MessageBoxShow("确定要保存吗?", eMsgType.Info, MessageBoxButton.YesNo);
                        if (result != MessageBoxResult.Yes)
                            return;
                        PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Publish("保存");
                    }
                    break;
                case "打开":
                    {
                        Algorithm.ProductNum = Model.ProductNum;
                        Algorithm.BatchNum = Model.BatchNum;

                        // 选择包含多个子文件夹的根目录（每个子文件夹包含 gray/depth 目录）
                        string selectedPath = string.Empty;
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            using (var folderDialog = new FolderBrowserDialog())
                            {
                                folderDialog.Description = "请选择包含图片子文件夹的根目录";
                                folderDialog.ShowNewFolderButton = false;

                                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    selectedPath = folderDialog.SelectedPath;
                                }
                            }
                        });

                        if (string.IsNullOrEmpty(selectedPath) || !Directory.Exists(selectedPath))
                        {
                            Logs.LogInfo("未选择文件夹或文件夹不存在，取消操作");
                            return;
                        }

                        FilePath = selectedPath;
                        Logs.LogInfo($"选择文件夹：{FilePath}");

                        // 后台处理
                        Task.Run(() =>
                        {
                            Stopwatch totalStopwatch = Stopwatch.StartNew();

                            try
                            {
                                string[] folders = Directory.GetDirectories(FilePath);
                                Logs.LogInfo($"找到 {folders.Length} 个子文件夹");

                                if (folders.Length == 0)
                                {
                                    PrismProvider.Dispatcher.BeginInvoke(() =>
                                    {
                                        Growl.ErrorGlobal("指定的文件夹中没有子文件夹！");
                                    });
                                    return;
                                }

                                int imageCounter = 1;
                                int processedCount = 0;

                                for (int i = 0; i < folders.Length; i++)
                                {
                                    string folder = folders[i];
                                    string grayImageDir = Path.Combine(folder, "gray");
                                    string heightImageDir = Path.Combine(folder, "depth");

                                    // 检查目录是否存在
                                    if (!Directory.Exists(grayImageDir) || !Directory.Exists(heightImageDir))
                                    {
                                        Logs.LogInfo($"跳过文件夹 {folder}：缺少 gray 或 depth 目录");
                                        continue;
                                    }

                                    // 获取图像文件
                                    string[] grayImagePaths = Directory.GetFiles(grayImageDir, "*.tif");
                                    string[] heightImagePaths = Directory.GetFiles(heightImageDir, "*.tif");

                                    if (grayImagePaths.Length == 0 || heightImagePaths.Length == 0)
                                    {
                                        Logs.LogInfo($"跳过文件夹 {folder}：缺少图像文件");
                                        continue;
                                    }

                                    // 读取图像
                                    using Mat grayImage = Cv2.ImRead(grayImagePaths[0], ImreadModes.Grayscale);
                                    using Mat heightImage = Cv2.ImRead(heightImagePaths[0], ImreadModes.Unchanged);

                                    if (grayImage.Empty() || heightImage.Empty())
                                    {
                                        Logs.LogInfo($"无法读取图像文件：{grayImagePaths[0]} 或 {heightImagePaths[0]}");
                                        continue;
                                    }

                                    // 图像转换
                                    List<float[]> grayData = Common_Algorithm.ConvertMatToList(grayImage);
                                    List<float[]> heightData = Common_Algorithm.ConvertMatToList(heightImage);

                                    // 调用算法
                                    Algorithm.IsScanEnd = false;
                                    Stopwatch processWatch = Stopwatch.StartNew();
                                    _customAlgo?.Process(grayData, heightData, Algorithm);
                                    processWatch.Stop();
                                    double algoTime = processWatch.Elapsed.TotalMilliseconds;

                                    // 获取并绘制结果
                                    Stopwatch drawWatch = Stopwatch.StartNew();
                                    KBTGDJC_MeasureResult measureResult = _customAlgo?.GetDefectsResult() ?? new KBTGDJC_MeasureResult();
                                    // 文件模式：坐标固定为0
                                    _customAlgo?.CvDrawResult(measureResult, 0, 0);
                                    drawWatch.Stop();
                                    double drawTime = drawWatch.Elapsed.TotalMilliseconds;

                                    Mat resultImage = measureResult.GrayImage;

                                    processedCount++;

                                    // 先展示结果（优先响应）
                                    Stopwatch showWatch = Stopwatch.StartNew();
                                    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KBTGDJC_MeasureResult", measureResult));
                                    ProcessedData processedData = new ProcessedData();
                                    processedData.SetMemoryPara("KBTGDJC_MeasureResult", measureResult);
                                    if (resultImage != null && !resultImage.Empty())
                                    {
                                        processedData.Gray = resultImage.Clone();
                                        PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
                                    }
                                    showWatch.Stop();
                                    double showTime = showWatch.Elapsed.TotalMilliseconds;

                                    int defectCount = measureResult.Defects?.Count ?? 0;
                                    Logs.LogInfo($"文件夹 {i + 1}/{folders.Length} 算法: {algoTime:F2}ms, 绘制: {drawTime:F2}ms, 展示: {showTime:F2}ms, 缺陷: {defectCount}");

                                    // 异步保存结果图片（不阻塞主流程）
                                    if (Model.IsSaveImage && resultImage != null && !resultImage.Empty())
                                    {
                                        Mat imageToSave = resultImage.Clone();
                                        int imgIndex = imageCounter;
                                        imageCounter++;
                                        Task.Run(() =>
                                        {
                                            try
                                            {
                                                string dstDir = string.IsNullOrEmpty(Model.SavePath) ? @"D:\KBTScraper_results" : Model.SavePath;
                                                Directory.CreateDirectory(dstDir);
                                                string outputPath = Path.Combine(dstDir, $"images{imgIndex}.png");
                                                Cv2.ImWrite(outputPath, imageToSave);
                                                Logs.LogInfo($"异步保存：结果图片已保存 {outputPath}");
                                                imageToSave.Dispose();
                                            }
                                            catch (Exception ex)
                                            {
                                                Logs.LogError($"异步保存结果图片失败: {ex.Message}");
                                            }
                                        });
                                    }
                                    else
                                    {
                                        imageCounter++;
                                    }
                                }

                                Logs.LogInfo($"处理完成，共处理 {processedCount}/{folders.Length} 个文件夹");
                            }
                            catch (Exception ex)
                            {
                                Logs.LogError($"文件取图处理异常：{ex.Message}\n{ex.StackTrace}");
                                PrismProvider.Dispatcher.BeginInvoke(() =>
                                {
                                    Growl.ErrorGlobal($"处理过程中发生错误：{ex.Message}");
                                });
                            }
                            finally
                            {
                                totalStopwatch.Stop();
                                Logs.LogInfo($"文件目录模式总耗时：{totalStopwatch.Elapsed.TotalMilliseconds:F2} 毫秒");
                            }
                        });
                    }
                    break;

                default:
                    break;
            }
        });
        #endregion

        #region Navigation
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 直接从算法单例获取参数，实现多页面共享
            if (_customAlgo != null)
            {
                Algorithm = _customAlgo.MParam;
                Logs.LogInfo($"OtherConfigViewModel: 从算法单例获取参数，MinDefectDepth = {Algorithm.MinDefectDepth}");
            }
            else
            {
                Algorithm = new KBTGDJC_MeasureParam();
                Logs.LogInfo("OtherConfigViewModel: 算法单例为空，使用默认参数");
            }

            // 尝试获取 OtherConfig
            var parameters = navigationContext.Parameters;
            int serial = -999;
            if (parameters.TryGetValue<int>("Serial", out var id))
                serial = id;

            string key = $"{serial}";
            if (PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.ContainsKey(key))
            {
                var temp = PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches[key] as SensorDataCollectionModel;
                if (temp != null)
                {
                    Model = temp.OtherConfig;
                }
            }
            else
            {
                // 尝试从所有缓存中找到第一个 SensorDataCollectionModel
                foreach (var kvp in PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches)
                {
                    if (kvp.Value is SensorDataCollectionModel model)
                    {
                        Model = model.OtherConfig;
                        break;
                    }
                }
            }

            Algorithm.ProductNum = Model.ProductNum;
            Algorithm.BatchNum = Model.BatchNum;
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
