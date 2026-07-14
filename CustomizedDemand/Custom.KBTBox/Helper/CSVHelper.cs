using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.WorkStatus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Custom.KBTBox.KBTDispensing_Algorithm;

namespace Custom.KBTBox.Helper
{
    public static class CSVHelper
    {
        public static string RootPath = @"D:\KBT_data";

        /// <summary>
        /// 保存所有CSV数据
        /// </summary>
        /// <param name="result">测量结果</param>
        public static void SaveAllData(KBTDispensing_MeasureResult result)
        {
            if (result == null || result.SideResults == null || result.SideResults.Count == 0)
                return;

            // 深度拷贝数据，避免原始数据被修改
            var resultCopy = DeepCopyResult(result);

            try
            {
                // 创建本次采集的文件夹（以时间戳命名）
                string folderName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string folderPath = Path.Combine(RootPath, folderName);

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // 1. 保存胶框数据
                SaveGlueFrameData(folderPath, resultCopy);

                // 2. 保存胶框汇总数据
                SaveGlueFrameSummary(folderPath, resultCopy);

                // 3. 保存缺陷数据
                SaveDefectData(folderPath, resultCopy);
                resultCopy.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存CSV数据异常: {ex.Message}");
                PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Error);
            }
        }

        /// <summary>
        /// 1. 保存胶框数据CSV：四条边等间距的胶宽和高
        /// 列名：序号, 采样点索引, 边1宽, 边1高, 边2宽, 边2高, 边3宽, 边3高, 边4宽, 边4高
        /// </summary>
        private static void SaveGlueFrameData(string folderPath, KBTDispensing_MeasureResult result)
        {
            string filePath = Path.Combine(folderPath, "胶框数据.csv");

            var sb = new StringBuilder();

            // 写入表头
            sb.AppendLine("序号,边1宽(μm),边1高(μm),边2宽(μm),边2高(μm),边3宽(μm),边3高(μm),边4宽(μm),边4高(μm)");

            // 获取最大采样点数量
            int maxCount = result.SideResults.Max(s => s.GlueWidthList?.Length ?? 0);

            for (int i = 0; i < maxCount; i++)
            {
                var row = new List<string>
                {
                    (i + 1).ToString() // 序号
                };

                // 四条边的胶宽和胶高
                for (int sideIdx = 0; sideIdx < 4; sideIdx++)
                {
                    if (sideIdx < result.SideResults.Count)
                    {
                        var side = result.SideResults[sideIdx];
                        string width = (side.GlueWidthList != null && i < side.GlueWidthList.Length)
                            ? side.GlueWidthList[i].ToString("F1")
                            : "";
                        string thickness = (side.GlueThicknessList != null && i < side.GlueThicknessList.Length)
                            ? side.GlueThicknessList[i].ToString("F1")
                            : "";
                        row.Add(width);
                        row.Add(thickness);
                    }
                    else
                    {
                        row.Add("");
                        row.Add("");
                    }
                }

                sb.AppendLine(string.Join(",", row));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 2. 保存胶框汇总数据CSV：每个胶框的统计值
        /// 列名：胶宽最大值, 胶宽最小值, 胶宽平均值, 胶高最大值, 胶高最小值, 胶高平均值, 胶平面度, 框平面度, 缺陷数量
        /// </summary>
        private static void SaveGlueFrameSummary(string folderPath, KBTDispensing_MeasureResult result)
        {
            string filePath = Path.Combine(folderPath, "胶框汇总数据.csv");

            var sb = new StringBuilder();

            // 写入表头
            sb.AppendLine("胶宽最大值(μm),胶宽最小值(μm),胶宽平均值(μm),胶高最大值(μm),胶高最小值(μm),胶高平均值(μm),胶平面度(μm),缺陷数量(个)");

            // 统计缺陷数量
            int defectCount = result.SideResults.Sum(s => s.Defects?.Count ?? 0);

            // 写入数据行
            var row = new List<string>
            {
                result.GlueWidthMax.ToString("F1"),
                result.GlueWidthMin.ToString("F1"),
                result.GlueWidthAvg.ToString("F1"),
                result.GlueThicknessMax.ToString("F1"),
                result.GlueThicknessMin.ToString("F1"),
                result.GlueThicknessAvg.ToString("F1"),
                result.GlueFlatness.ToString("F1"),

                defectCount.ToString()
            };

            sb.AppendLine(string.Join(",", row));

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 3. 保存缺陷数据CSV：四条边的缺陷信息
        /// 列名：序号, 边1面积, 边1直径, 边1深度, 边1胶路偏转角度, 边2面积, 边2直径, 边2深度, 边2胶路偏转角度, ...
        /// </summary>
        private static void SaveDefectData(string folderPath, KBTDispensing_MeasureResult result)
        {
            string filePath = Path.Combine(folderPath, "缺陷数据.csv");

            var sb = new StringBuilder();

            // 写入表头
            sb.AppendLine("序号,边1面积(μm²),边1直径(μm),边1深度(μm),边1胶路偏转角度(°),边2面积(μm²),边2直径(μm),边2深度(μm),边2胶路偏转角度(°),边3面积(μm²),边3直径(μm),边3深度(μm),边3胶路偏转角度(°),边4面积(μm²),边4直径(μm),边4深度(μm),边4胶路偏转角度(°)");

            // 获取最大缺陷数量
            int maxDefectCount = result.SideResults.Max(s => s.Defects?.Count ?? 0);

            if (maxDefectCount == 0)
            {
                // 没有缺陷，写入空行表示无缺陷
                sb.AppendLine("无缺陷数据");
            }
            else
            {
                for (int i = 0; i < maxDefectCount; i++)
                {
                    var row = new List<string>
                    {
                        (i + 1).ToString() // 序号
                    };

                    // 四条边的缺陷数据
                    for (int sideIdx = 0; sideIdx < 4; sideIdx++)
                    {
                        if (sideIdx < result.SideResults.Count)
                        {
                            var side = result.SideResults[sideIdx];
                            if (side.Defects != null && i < side.Defects.Count)
                            {
                                var defect = side.Defects[i];
                                row.Add(defect.AreaFeature.ToString("F1"));
                                row.Add(defect.DiameterFeature.ToString("F1"));
                                row.Add(defect.DepthFeature.ToString("F1"));
                                row.Add(side.GluePathTiltAngle.ToString("F2"));
                            }
                            else
                            {
                                row.Add("");
                                row.Add("");
                                row.Add("");
                                row.Add("");
                            }
                        }
                        else
                        {
                            row.Add("");
                            row.Add("");
                            row.Add("");
                            row.Add("");
                        }
                    }

                    sb.AppendLine(string.Join(",", row));
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 深度拷贝测量结果
        /// </summary>
        private static KBTDispensing_MeasureResult DeepCopyResult(KBTDispensing_MeasureResult source)
        {
            var copy = new KBTDispensing_MeasureResult
            {
                GlueWidthMax = source.GlueWidthMax,
                GlueWidthMin = source.GlueWidthMin,
                GlueWidthAvg = source.GlueWidthAvg,
                GlueThicknessMax = source.GlueThicknessMax,
                GlueThicknessMin = source.GlueThicknessMin,
                GlueThicknessAvg = source.GlueThicknessAvg,
                GlueFlatness = source.GlueFlatness,
                SideResults = new List<KBTDispensing_Algorithm.SideResult>()
            };

            // 深度拷贝每个边的结果
            foreach (var side in source.SideResults)
            {
                var sideCopy = new KBTDispensing_Algorithm.SideResult
                {
                    GlueWidthList = (double[])side.GlueWidthList?.Clone(),
                    GlueThicknessList = (double[])side.GlueThicknessList?.Clone(),
                    GlueWidthMax = side.GlueWidthMax,
                    GlueWidthMin = side.GlueWidthMin,
                    GlueWidthAvg = side.GlueWidthAvg,
                    GlueThicknessMax = side.GlueThicknessMax,
                    GlueThicknessMin = side.GlueThicknessMin,
                    GlueThicknessAvg = side.GlueThicknessAvg,
                    GlueFlatness = side.GlueFlatness,
                    GluePathTiltAngle = side.GluePathTiltAngle,
                    Defects = new List<KBTDispensing_Algorithm.DefectResult>()
                };

                // 深度拷贝缺陷列表
                if (side.Defects != null)
                {
                    foreach (var defect in side.Defects)
                    {
                        sideCopy.Defects.Add(new KBTDispensing_Algorithm.DefectResult
                        {
                            IsOk = defect.IsOk,
                            InstanceId = defect.InstanceId,
                            Left = defect.Left,
                            Top = defect.Top,
                            Right = defect.Right,
                            Bottom = defect.Bottom,
                            Confidence = defect.Confidence,
                            ClassName = defect.ClassName,
                            AreaFeature = defect.AreaFeature,
                            DiameterFeature = defect.DiameterFeature,
                            DepthFeature = defect.DepthFeature
                        });
                    }
                }

                copy.SideResults.Add(sideCopy);
            }

            return copy;
        }
    }
}
