using Custom.KBTBox.Helper;
using Custom.KBTBox.Models;
using HalconDotNet;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Logger;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using static Custom.KBTBox.KBTDispensing_Algorithm;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using Point = OpenCvSharp.Point;

namespace Custom.KBTBox.ViewModels
{
    public class GrayShowViewModel : DialogViewModelBase
    {
        #region Fields
        PLCBase CurPLC;
        #endregion

        #region Properties
        private KBTDispensing_MeasureResult _result;

        public KBTDispensing_MeasureResult Result
        {
            get { return _result; }
            set { _result = value; RaisePropertyChanged(); }
        }

        private HObject image = new HObject();
        public HObject Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
        }

        private Mat _myOpenCVMat = new Mat();
        public Mat MyOpenCVMat
        {
            get { return _myOpenCVMat; }
            set { _myOpenCVMat = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public GrayShowViewModel()
        {
            var models = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();

            if (models.Models.Count > 0)
                CurPLC = models.Models[0];

            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
            {
                if (obj.Item1 != "KBTDispensing_MeasureResult") return;
                Result = obj.Item2 as KBTDispensing_MeasureResult;

                CSVHelper.SaveAllData(Result);
                Save(Result);

                //进行数据判定
                try
                {
                    var sensorModel = PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches
                        .Values.OfType<SensorDataCollectionModel>().FirstOrDefault();
                    if (sensorModel?.OtherConfig?.IsEnableJudgment == true && sensorModel?.JudgmentConfig != null)
                    {
                        JudgeResult(Result, sensorModel.JudgmentConfig);
                    }
                }
                catch (Exception ex)
                {
                    Logs.LogError($"数据判定异常: {ex.Message}");
                }

            }, ThreadOption.BackgroundThread);


            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Subscribe((obj) =>
            {
                try
                {
                    // 缩放 50%
                    Mat img = obj.Gray;
                    Console.WriteLine("显示图片1");

                    if (img == null || img.Empty())
                    {
                        Console.WriteLine("Resize failed: img is null or empty.");
                    }

                    // 缩放 50%
                    //Mat small = new Mat();
                    //Cv2.Resize(img, small, new OpenCvSharp.Size(0, 0), 0.5, 0.5);
                    
                    Console.WriteLine("显示图片2");
                    // 保存临时图
                    //string temp = System.IO.Path.GetTempFileName() + ".png";

                    // 绘制参数
                    //string chineseText = "你好，世界！";
                    //System.Drawing.Point textPos = new System.Drawing.Point(50, 50); // 绘制起始位置 (X, Y)
                    //int size = 60; // 字体大小
                    //System.Drawing.Color color = System.Drawing.Color.Red; // 红色
                    //Cv2.Rectangle(small, new Point(1000, 1000), new Point(2000, 2000), Scalar.Red, 20);
                    //Mat imageWithText = DrawChineseTextOnImage(small, chineseText, textPos, size, color);

                    //Cv2.ImWrite(temp, small);
                    //MyOpenCVMat = small;
                    //Console.WriteLine("显示图片3");
                    // 再让 HALCON 读取
                    HObject halconImg = ImageHelper.ConvertMatToHObject(img);
                    //HOperatorSet.ReadImage(out halconImg, temp);
                    Image = halconImg;
                    Console.WriteLine("显示图片4");
                    var CustomAlgo = PrismProvider.Container.Resolve(typeof(KBTDispensing_Algorithm)) as KBTDispensing_Algorithm;
                    CustomAlgo.Dispose();
                    Logs.LogInfo("资源已释放");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"{ex.StackTrace}");
                }

            }, ThreadOption.BackgroundThread);


        }
        #endregion

        #region Methods
        /// <summary>
        /// 根据判定配置对测量结果进行OK/NG判定
        /// </summary>
        private void JudgeResult(KBTDispensing_MeasureResult result, JudgmentConfigModel config)
        {
            if (result?.SideResults == null || config == null) return;

            string[] sideNames = { "边1", "边2", "边3", "边4" };
            bool allOk = true;
            bool angleNg = false;

            for (int i = 0; i < result.SideResults.Count; i++)
            {
                var side = result.SideResults[i];
                string sideName = i < sideNames.Length ? sideNames[i] : $"边{i + 1}";
                bool sideOk = true;

                // 胶面参数判定
                if (!config.GlueFlatness.Check(side.GlueFlatness))
                {
                    Logs.LogWarning($"[NG] {sideName} 胶平面度={side.GlueFlatness:F2} 超出范围[{config.GlueFlatness.Min}, {config.GlueFlatness.Max}]");
                    sideOk = false;
                }
                if (!config.GlueWidth.Check(side.GlueWidthMax))
                {
                    Logs.LogWarning($"[NG] {sideName} 胶宽最大值={side.GlueWidthMax:F2} 超出范围[{config.GlueWidth.Min}, {config.GlueWidth.Max}]");
                    sideOk = false;
                }
                if (!config.GlueWidth.Check(side.GlueWidthMin))
                {
                    Logs.LogWarning($"[NG] {sideName} 胶宽最小值={side.GlueWidthMin:F2} 超出范围[{config.GlueWidth.Min}, {config.GlueWidth.Max}]");
                    sideOk = false;
                }
                if (!config.GlueThickness.Check(side.GlueThicknessMax))
                {
                    Logs.LogWarning($"[NG] {sideName} 胶厚最大值={side.GlueThicknessMax:F2} 超出范围[{config.GlueThickness.Min}, {config.GlueThickness.Max}]");
                    sideOk = false;
                }
                if (!config.GlueThickness.Check(side.GlueThicknessMin))
                {
                    Logs.LogWarning($"[NG] {sideName} 胶厚最小值={side.GlueThicknessMin:F2} 超出范围[{config.GlueThickness.Min}, {config.GlueThickness.Max}]");
                    sideOk = false;
                }
                if (!config.GluePathTiltAngle.Check(Math.Abs(side.GluePathTiltAngle)))
                {
                    Logs.LogWarning($"[NG] {sideName} 胶路偏转角度={side.GluePathTiltAngle:F2}(绝对值={Math.Abs(side.GluePathTiltAngle):F2}) 超出范围[{config.GluePathTiltAngle.Min}, {config.GluePathTiltAngle.Max}]");
                    sideOk = false;
                    angleNg = true;
                }

                // 缺陷参数判定
                if (side.Defects != null)
                {
                    foreach (var defect in side.Defects)
                    {
                        bool defectOk = true;
                        defectOk &= config.DefectArea.Check(defect.AreaFeature);
                        defectOk &= config.DefectDiameter.Check(defect.DiameterFeature);
                        defectOk &= config.DefectDepth.Check(defect.DepthFeature);
                        defect.IsOk = defectOk;

                        if (!defectOk)
                        {
                            Logs.LogWarning($"[NG] {sideName} 缺陷{defect.InstanceId} 不合格 (面积={defect.AreaFeature:F2}, 直径={defect.DiameterFeature:F2}, 深度={defect.DepthFeature:F2})");
                            sideOk = false;
                        }
                    }
                }

                if (!sideOk) allOk = false;
                Logs.LogInfo($"判定: {sideName} => {(sideOk ? "OK" : "NG")}");
            }

            // 写 PLC 开始值(1003代表写1代表OK，写2代表NG，写3代表角度NG)
            int PLCResult = 2;
            if (allOk)
                PLCResult = 1;
            else if (angleNg)
                PLCResult = 3;

            var param = new PLCParaInfoModel
            {
                PLCAddress = "1003",
                ParaType = EnumParaInfoModelParaType.Ushort,
                ParaValue = (ushort)PLCResult
            };

            if (CurPLC.WritePLCPara(param))
                Logs.LogInfo($"地址1003，写入成功 {PLCResult}");
            else
                Logs.LogInfo($"地址1003，写入失败！！");

            Logs.LogInfo($"整体判定结果: {(allOk ? "OK" : "NG")}");
        }

        /// <summary>
        /// 保存位置1到位置8的胶厚值（mm）
        /// </summary>
        /// <param name="result"></param>
        private void Save(KBTDispensing_MeasureResult? result)
        {
            if (result?.SideResults == null || result.SideResults.Count == 0)
            {
                Logs.LogWarning("测量结果为空，未导出胶厚8点值 CSV。");
                return;
            }

            static string FormatThicknessValue(double value)
            {
                const double MicrometersPerMillimeter = 1000.0;
                return double.IsFinite(value) && value >= 0
                    ? (value / MicrometersPerMillimeter).ToString("F4")
                    : string.Empty;
            }

            try
            {
                string folderName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string folderPath = System.IO.Path.Combine(CSVHelper.RootPath, folderName);
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                string filePath = System.IO.Path.Combine(folderPath, "胶厚开头中间值.csv");
                var sb = new System.Text.StringBuilder();
                List<string> positionValues = new(8);

                for (int i = 0; i < 4; i++)
                {
                    var sideResult = i < result.SideResults.Count ? result.SideResults[i] : null;
                    double[] glueThicknessList = sideResult?.GlueThicknessList ?? Array.Empty<double>();

                    if (glueThicknessList.Length == 0)
                    {
                        positionValues.Add(string.Empty);
                        positionValues.Add(string.Empty);
                        continue;
                    }

                    int startIndex = 0;
                    int middleIndex = (glueThicknessList.Length - 1) / 2;
                    positionValues.Add(FormatThicknessValue(glueThicknessList[startIndex]));
                    positionValues.Add(FormatThicknessValue(glueThicknessList[middleIndex]));
                }

                if (positionValues.Count > 1)
                {
                    string firstValue = positionValues[0];
                    positionValues.RemoveAt(0);
                    positionValues.Add(firstValue);
                }

                sb.AppendLine("位置1(mm),位置2(mm),位置3(mm),位置4(mm),位置5(mm),位置6(mm),位置7(mm),位置8(mm)");
                sb.AppendLine(string.Join(",", positionValues));

                System.IO.File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
                Logs.LogInfo($"胶厚8点值 CSV 已保存：{filePath}");
            }
            catch (Exception ex)
            {
                Logs.LogError($"保存胶厚8点值 CSV 异常: {ex.Message}");
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
                        // 使用 OpenCV 读图
                        Mat img = Cv2.ImRead("C:\\Users\\19765\\Desktop\\轮廓仪数据采集\\_gray.tiff", ImreadModes.Unchanged);

                        // 缩放 50%
                        Mat small = new Mat();
                        Cv2.Resize(img, small, new OpenCvSharp.Size(0, 0), 0.5, 0.5);

                        // 保存临时图
                        string temp = System.IO.Path.GetTempFileName() + ".png";
                        Cv2.ImWrite(temp, small);

                        // 再让 HALCON 读取
                        HObject halconImg;
                        HOperatorSet.ReadImage(out halconImg, temp);
                        Image = halconImg;
                    }
                    break;

                default:
                    break;
            }
        });
        #endregion
    }
}
