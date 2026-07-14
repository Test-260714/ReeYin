using Custom.KBTScraper.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Custom.KBTScraper.Helper
{
    /// <summary>
    /// 缺陷数据保存辅助类
    /// </summary>
    public static class DefectDataHelper
    {
        /// <summary>
        /// 保存缺陷数据到CSV文件（单个保存模式，用于走带分段处理）
        /// 每次处理创建一个新文件夹（格式：yyyyMMdd_HHmmss），在其中保存CSV文件
        /// </summary>
        /// <param name="defects">缺陷数据列表</param>
        /// <param name="basePath">基础存储路径</param>
        /// <param name="fileName">文件名（可选，默认使用时间戳）</param>
        /// <returns>是否保存成功</returns>
        public static bool SaveDefectData(List<DefectResult> defects, string basePath, string fileName = null, string productNum = "", string batchNum = "")
        {
            return SaveDefectData(defects, basePath, fileName, createFolder: true, productNum: productNum, batchNum: batchNum);
        }

        /// <summary>
        /// 保存缺陷数据到CSV文件（批量保存模式，用于文件夹读取处理）
        /// 在指定路径直接保存CSV文件，不创建子文件夹
        /// </summary>
        /// <param name="defects">缺陷数据列表</param>
        /// <param name="basePath">基础存储路径</param>
        /// <param name="fileName">文件名（可选，默认使用时间戳）</param>
        /// <returns>是否保存成功</returns>

        /// <summary>
        /// 追加缺陷数据到CSV文件（用于批量处理时追加数据）
        /// </summary>
        /// <param name="defects">缺陷数据列表</param>
        /// <param name="filePath">CSV文件完整路径</param>
        /// <param name="isFirstBatch">是否是第一批数据（需要写入表头）</param>
        /// <returns>是否保存成功</returns>
        private static bool SaveDefectData(List<DefectResult> defects, string basePath, string fileName, bool createFolder, string productNum, string batchNum)
        {
            try
            {
                if (defects == null || defects.Count == 0)
                {
                    return false;
                }

                // 如果基础路径为空，使用默认路径
                if (string.IsNullOrEmpty(basePath))
                {
                    basePath = @"D:KBTScraper_results\缺陷信息";
                }

                string filePath;
                
                if (createFolder)
                {
                    // 创建时间戳文件夹（格式：yyyyMMdd_HHmmss）
                    string folderName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string folderPath = Path.Combine(basePath, folderName);

                    // 确保目录存在
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    // 如果没有指定文件名，使用默认文件名
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = $"defects_{DateTime.Now:yyyyMMdd_HHmmss_fff}.csv";
                    }
                    else if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName += ".csv";
                    }

                    filePath = Path.Combine(folderPath, fileName);
                }
                else
                {
                    // 不创建文件夹，直接在基础路径下保存
                    // 确保目录存在
                    if (!Directory.Exists(basePath))
                    {
                        Directory.CreateDirectory(basePath);
                    }

                    // 如果没有指定文件名，使用默认文件名
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = $"defects_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    }
                    else if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName += ".csv";
                    }

                    filePath = Path.Combine(basePath, fileName);
                }

                // 创建CSV内容
                StringBuilder csvContent = new StringBuilder();

                // 写入CSV表头（中文）
                csvContent.AppendLine("实例ID,型号,批号,类别ID,类别名称,是否合格,置信度,深度特征(μm),直径特征(μm),面积(mm²),缺陷位置");

                // 写入缺陷数据
                foreach (var defect in defects)
                {
                    // 获取类别名称
                    string className = defect.Categories != null && defect.Categories.ContainsKey(defect.ClassId)
                        ? defect.Categories[defect.ClassId]
                        : "Unknown";

                    // 格式化数据，处理负无穷大值
                    string confidence = FormatValue(defect.Confidence);
                    string depthFeature = FormatValue(defect.DepthFeature);
                    string diameterFeature = FormatValue(defect.DiameterFeature);
                    string areaFeature = FormatValue(defect.AreaFeature);
                    string position = FormatValue(defect.Position);

                    // 写入一行数据
                    csvContent.AppendLine($"{defect.InstanceId}," +
                                        $"{productNum}," +
                                        $"{batchNum}," +
                                        $"{defect.ClassId}," +
                                        $"{className}," +
                                        $"{defect.IsOk}," +
                                        $"{confidence}," +
                                        $"{depthFeature}," +
                                        $"{diameterFeature}," +
                                        $"{areaFeature}," +
                                        $"{position}");
                }

                // 写入文件（使用UTF-8编码，带BOM，确保Excel能正确打开中文）
                File.WriteAllText(filePath, csvContent.ToString(), new UTF8Encoding(true));

                return true;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                System.Console.WriteLine($"保存缺陷数据失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 格式化数值，处理负无穷大和无效值
        /// </summary>
        private static string FormatValue(double value)
        {
            if (double.IsNegativeInfinity(value) || double.IsNaN(value))
            {
                return "";
            }
            return value.ToString("F4");
        }
    }
}
