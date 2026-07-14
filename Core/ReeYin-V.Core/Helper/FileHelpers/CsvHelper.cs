using Dm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReeYin_V.Core.Helper
{
    public static class CsvHelper
    {
        /// <summary>
        /// 将任意类的大集合按列存储为CSV文件（每个属性对应一列，纵向排列）
        /// </summary>
        /// <typeparam name="T">任意自定义类</typeparam>
        /// <param name="dataList">待存储的类集合（支持超1000个元素）</param>
        /// <param name="filePath">CSV文件保存路径</param>
        /// <param name="propertyNames">要导出的属性名列表（null则导出所有公共属性）</param>
        /// <param name="columnHeaders">列标题（需与propertyNames一一对应，null则用属性名）</param>
        /// <param name="valueFormatter">自定义值格式化委托（可选，处理特殊类型值）</param>
        /// <param name="encoding">文件编码（默认UTF-8）</param>
        /// <param name="bufferSize">写入缓冲区大小（默认8192字节）</param>
        /// <exception cref="ArgumentNullException">集合或路径为空</exception>
        /// <exception cref="IOException">IO写入失败</exception>
        /// <exception cref="ArgumentException">属性名/列标题不匹配</exception>
        public static void WriteGenericListToCsvColumn<T>(
            List<T> dataList,
            string filePath,
            List<string> propertyNames = null,
            List<string> columnHeaders = null,
            Func<PropertyInfo, T, string> valueFormatter = null,
            Encoding encoding = null,
            int bufferSize = 8192)
        {
            // 1. 基础参数校验
            if (dataList == null || dataList.Count == 0)
                throw new ArgumentNullException(nameof(dataList), "待存储的集合不能为空且至少包含一个元素");
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "CSV文件路径不能为空");
            if (bufferSize < 1024) bufferSize = 1024;
            encoding ??= Encoding.UTF8;

            // 2. 获取类的公共属性（过滤要导出的属性）
            Type type = typeof(T);
            PropertyInfo[] allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (allProperties.Length == 0)
                throw new InvalidOperationException($"类型 {type.Name} 没有公共实例属性");

            // 确定最终要导出的属性列表
            List<PropertyInfo> exportProperties = new List<PropertyInfo>();
            if (propertyNames == null || propertyNames.Count == 0)
            {
                exportProperties = allProperties.ToList();
            }
            else
            {
                foreach (string propName in propertyNames)
                {
                    PropertyInfo prop = allProperties.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                    if (prop == null)
                        throw new ArgumentException($"类型 {type.Name} 中未找到属性：{propName}");
                    exportProperties.Add(prop);
                }
            }

            // 校验列标题与属性数量匹配
            if (columnHeaders != null && columnHeaders.Count != exportProperties.Count)
                throw new ArgumentException("列标题数量必须与要导出的属性数量一致");

            // 3. 确保目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // 4. 按列组织数据（核心：先收集每列的所有值，再写入）
            // 列数据字典：Key=属性名，Value=该属性的所有值（按顺序）
            Dictionary<PropertyInfo, List<string>> columnDataDict = new Dictionary<PropertyInfo, List<string>>();
            foreach (PropertyInfo prop in exportProperties)
            {
                columnDataDict[prop] = new List<string>(dataList.Count);
            }

            // 遍历所有对象，提取每个属性的值并格式化
            foreach (T item in dataList)
            {
                if (item == null) continue; // 跳过空对象
                foreach (PropertyInfo prop in exportProperties)
                {
                    string valueStr = string.Empty;
                    try
                    {
                        // 使用自定义格式化器或默认格式化
                        if (valueFormatter != null)
                        {
                            valueStr = valueFormatter(prop, item);
                        }
                        else
                        {
                            object value = prop.GetValue(item);
                            valueStr = value == null ? "NULL" : FormatValue(value);
                        }
                    }
                    catch (Exception ex)
                    {
                        valueStr = $"[获取值失败：{ex.Message}]";
                    }
                    // 转义CSV特殊字符
                    columnDataDict[prop].Add(EscapeCsvValue(valueStr));
                }
            }

            // 5. 高性能写入CSV（按列写入：先写列标题，再逐列写入所有值）
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
            using (var writer = new StreamWriter(stream, encoding, bufferSize))
            {
                // 第一步：写入列标题
                StringBuilder headerBuilder = new StringBuilder();
                for (int i = 0; i < exportProperties.Count; i++)
                {
                    string header = columnHeaders?[i] ?? exportProperties[i].Name;
                    headerBuilder.Append(EscapeCsvValue(header));
                    if (i < exportProperties.Count - 1)
                        headerBuilder.Append(","); // 列分隔符
                }
                writer.WriteLine(headerBuilder.ToString());
                writer.Flush();

                // 第二步：按列写入数据（核心：列存储，每列的所有值纵向排列）
                // 先获取最大行数（所有列的行数一致，取第一个列的行数）
                int maxRowCount = columnDataDict.Values.First().Count;
                // 分片写入（每批1000行，避免内存溢出）
                int batchSize = 1000;
                int currentRow = 0;

                while (currentRow < maxRowCount)
                {
                    int remainingRows = maxRowCount - currentRow;
                    int currentBatchSize = Math.Min(batchSize, remainingRows);
                    StringBuilder batchBuilder = new StringBuilder();

                    // 遍历当前批次的每一行，拼接所有列的当前行值
                    for (int row = currentRow; row < currentRow + currentBatchSize; row++)
                    {
                        for (int col = 0; col < exportProperties.Count; col++)
                        {
                            PropertyInfo prop = exportProperties[col];
                            // 获取当前列的当前行值（行索引超出则补空）
                            string cellValue = row < columnDataDict[prop].Count ? columnDataDict[prop][row] : string.Empty;
                            batchBuilder.Append(cellValue);
                            if (col < exportProperties.Count - 1)
                                batchBuilder.Append(","); // 列分隔符
                        }
                        batchBuilder.AppendLine(); // 行结束
                    }

                    // 写入当前批次
                    writer.Write(batchBuilder.ToString());
                    writer.Flush();
                    currentRow += currentBatchSize;
                }
            }

            Console.WriteLine($"CSV文件写入完成！路径：{filePath}，总对象数：{dataList.Count}，列数：{exportProperties.Count}");
        }

        /// <summary>
        /// 默认值格式化（处理常见类型）
        /// </summary>
        private static string FormatValue(object value)
        {
            if (value == null) return "NULL";
            Type valueType = value.GetType();

            // 处理数值类型（避免科学计数法）
            if (value is int || value is long || value is short || value is byte)
                return value.ToString();
            if (value is double d)
                return double.IsNaN(d) || double.IsInfinity(d) ? d.ToString() : d.ToString("F6");
            if (value is float f)
                return float.IsNaN(f) || float.IsInfinity(f) ? f.ToString() : f.ToString("F6");
            if (value is decimal dec)
                return dec.ToString("F6");

            // 处理日期时间
            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");

            // 处理布尔值
            if (value is bool b)
                return b ? "True" : "False";

            // 其他类型直接转字符串
            return value.ToString();
        }

        /// <summary>
        /// 转义CSV特殊字符（逗号、双引号、换行、回车）
        /// </summary>
        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            // 包含特殊字符则包裹双引号，双引号替换为两个
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
