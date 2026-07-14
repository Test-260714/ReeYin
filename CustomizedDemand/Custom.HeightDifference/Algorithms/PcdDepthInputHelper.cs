using HalconDotNet;
using ReeYin_V.Core.Helper;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ReeYin.Customized.Algo.Algorithms
{
    internal sealed class PcdDepthImageLoadResult
    {
        /// <summary>
        /// 保存 PCD 转换后的 HALCON 高度图和临时图像路径。
        /// </summary>
        public PcdDepthImageLoadResult(HObject image, string tempImagePath)
        {
            Image = image;
            TempImagePath = tempImagePath;
        }

        /// <summary>
        /// 由 PCD 深度数据转换得到的 HALCON 高度图对象，调用方负责按生命周期释放。
        /// </summary>
        public HObject Image { get; }

        /// <summary>
        /// PCD 转换为临时 TIFF 高度图后的文件路径。
        /// </summary>
        public string TempImagePath { get; }
    }

    internal static class PcdDepthInputHelper
    {
        /// <summary>
        /// 判断输入路径是否为 PCD 点云文件。
        /// </summary>
        public static bool IsPcdFile(string? path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path)
                    && string.Equals(Path.GetExtension(path), ".pcd", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 根据输入图像路径查找同名或相邻的 PCD 点云文件。
        /// </summary>
        public static string TryResolvePointCloudPath(string? inputPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(inputPath))
                {
                    return string.Empty;
                }

                string fullPath = Path.GetFullPath(inputPath);
                if (IsPcdFile(fullPath))
                {
                    return File.Exists(fullPath) ? fullPath : string.Empty;
                }

                if (!File.Exists(fullPath))
                {
                    return string.Empty;
                }

                foreach (string candidate in EnumeratePointCloudCandidates(fullPath))
                {
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 读取 PCD 深度字段并转换成 HALCON real 高度图。
        /// </summary>
        public static PcdDepthImageLoadResult LoadPcdAsHeightImage(
            string pcdPath,
            double invalidGrayCenter)
        {
            if (string.IsNullOrWhiteSpace(pcdPath) || !File.Exists(pcdPath))
            {
                throw new FileNotFoundException("找不到 PCD 文件。", pcdPath);
            }

            double invalidValue = ResolveInvalidValue(invalidGrayCenter);
            using FileStream stream = File.OpenRead(pcdPath);
            PcdHeader header = ReadHeader(stream);
            PcdField depthField = GetDepthField(header, "z");

            double[] depthValues = header.Data switch
            {
                "ascii" => ReadAsciiDepth(stream, header, depthField),
                "binary" => ReadBinaryDepth(stream, header, depthField),
                "binary_compressed" => ReadBinaryCompressedDepth(stream, header, depthField),
                _ => throw new InvalidOperationException($"不支持的 PCD 数据格式：{header.Data}")
            };

            float[] imageData = ConvertDepthToRealImage(depthValues, invalidValue);
            string tempImagePath = BuildTempImagePath(pcdPath);
            WriteRealImageToTiff(imageData, header.Width, header.Height, tempImagePath);

            HObject? image = HalconImageConverter.ReadImage(tempImagePath);
            if (image == null || !image.IsInitialized())
            {
                image?.Dispose();
                throw new InvalidOperationException("PCD 转换后的临时高度图读取失败。");
            }

            return new PcdDepthImageLoadResult(image, tempImagePath);
        }

        /// <summary>
        /// 枚举与输入高度图可能匹配的点云文件路径。
        /// </summary>
        private static IEnumerable<string> EnumeratePointCloudCandidates(string inputImagePath)
        {
            string directory = Path.GetDirectoryName(inputImagePath) ?? string.Empty;
            string fileStem = Path.GetFileNameWithoutExtension(inputImagePath);
            HashSet<string> candidates = new(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                candidates.Add(Path.Combine(directory, $"{fileStem}.pcd"));

                DirectoryInfo directoryInfo = new(directory);
                if (directoryInfo.Name.Equals("tif_output", StringComparison.OrdinalIgnoreCase)
                    || directoryInfo.Name.Equals("tiff_output", StringComparison.OrdinalIgnoreCase))
                {
                    if (directoryInfo.Parent != null)
                    {
                        candidates.Add(Path.Combine(directoryInfo.Parent.FullName, $"{fileStem}.pcd"));
                    }
                }
            }

            foreach (string candidate in candidates)
            {
                yield return candidate;
            }
        }

        /// <summary>
        /// 把无效灰度中心转换为写入临时图像时使用的整数值。
        /// </summary>
        private static double ResolveInvalidValue(double invalidGrayCenter)
        {
            if (double.IsNaN(invalidGrayCenter) || double.IsInfinity(invalidGrayCenter))
            {
                throw new InvalidOperationException("无效灰度中心值非法，无法用于 PCD 转换。");
            }

            if (invalidGrayCenter < -float.MaxValue || invalidGrayCenter > float.MaxValue)
            {
                throw new InvalidOperationException("无效灰度中心值超出 Single 范围，无法用于 PCD 转换。");
            }

            return invalidGrayCenter;
        }

        /// <summary>
        /// 为 PCD 转换结果生成临时 TIFF 文件路径。
        /// </summary>
        private static string BuildTempImagePath(string pcdPath)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "ReeYin", "HeightDifferenceMeasure", "PcdInput");
            Directory.CreateDirectory(tempDirectory);

            string fileName = Path.GetFileNameWithoutExtension(pcdPath);
            return Path.Combine(tempDirectory, $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.tif");
        }

        /// <summary>
        /// 把 real 高度数组写入 HALCON TIFF 图像文件。
        /// </summary>
        private static void WriteRealImageToTiff(float[] imageData, int width, int height, string outputPath)
        {
            GCHandle handle = default;
            HObject image = null!;

            try
            {
                handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                HOperatorSet.GenImage1(out image, "real", width, height, handle.AddrOfPinnedObject());
                HOperatorSet.WriteImage(image, "tiff", 0, outputPath);
            }
            finally
            {
                image?.Dispose();
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        /// <summary>
        /// 把 PCD 深度值按原始 Z 数值写入图像高度值。
        /// </summary>
        private static float[] ConvertDepthToRealImage(
            IReadOnlyList<double> depthValues,
            double invalidValue)
        {
            float[] output = new float[depthValues.Count];

            for (int index = 0; index < depthValues.Count; index++)
            {
                output[index] = PcdDepthValueConverter.ToHeightImageRawValue(depthValues[index], invalidValue);
            }

            return output;
        }

        /// <summary>
        /// 读取并解析 PCD 文件头的字段、尺寸和数据编码。
        /// </summary>
        private static PcdHeader ReadHeader(Stream stream)
        {
            Dictionary<string, string[]> metadata = new(StringComparer.OrdinalIgnoreCase);

            while (true)
            {
                string? line = ReadAsciiLine(stream);
                if (line == null)
                {
                    throw new InvalidOperationException("读取 PCD 头信息时遇到文件结尾。");
                }

                string decoded = line.Trim();
                if (string.IsNullOrWhiteSpace(decoded) || decoded.StartsWith('#'))
                {
                    continue;
                }

                string[] parts = decoded
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                string key = parts[0].ToUpperInvariant();
                string[] values = parts.Skip(1).ToArray();
                metadata[key] = values;

                if (key == "DATA")
                {
                    break;
                }
            }

            string[] requiredKeys = ["FIELDS", "SIZE", "TYPE", "WIDTH", "HEIGHT", "DATA"];
            string[] missingKeys = requiredKeys.Where(key => !metadata.ContainsKey(key)).ToArray();
            if (missingKeys.Length > 0)
            {
                throw new InvalidOperationException($"PCD 头信息缺少必要字段：{string.Join(", ", missingKeys)}");
            }

            string[] fieldNames = metadata["FIELDS"];
            int[] sizes = metadata["SIZE"].Select(int.Parse).ToArray();
            char[] typeChars = metadata["TYPE"].Select(item => item[0]).ToArray();
            int[] counts = (metadata.TryGetValue("COUNT", out string[]? countValues)
                    ? countValues
                    : Enumerable.Repeat("1", fieldNames.Length).ToArray())
                .Select(int.Parse)
                .ToArray();

            if (fieldNames.Length != sizes.Length
                || fieldNames.Length != typeChars.Length
                || fieldNames.Length != counts.Length)
            {
                throw new InvalidOperationException("PCD 头信息的 FIELDS/SIZE/TYPE/COUNT 长度不一致。");
            }

            int width = int.Parse(metadata["WIDTH"][0], CultureInfo.InvariantCulture);
            int height = int.Parse(metadata["HEIGHT"][0], CultureInfo.InvariantCulture);
            int points = metadata.TryGetValue("POINTS", out string[]? pointValues)
                ? int.Parse(pointValues[0], CultureInfo.InvariantCulture)
                : width * height;

            if (width * height != points)
            {
                throw new InvalidOperationException(
                    $"PCD 宽高与点数不一致：WIDTH*HEIGHT={width * height}, POINTS={points}");
            }

            List<PcdField> fields = new(fieldNames.Length);
            for (int index = 0; index < fieldNames.Length; index++)
            {
                fields.Add(new PcdField(fieldNames[index], sizes[index], typeChars[index], counts[index]));
            }

            return new PcdHeader(fields, width, height, points, metadata["DATA"][0].ToLowerInvariant());
        }

        /// <summary>
        /// 从 PCD 流中读取一行 ASCII 头信息或数据。
        /// </summary>
        private static string? ReadAsciiLine(Stream stream)
        {
            List<byte> bytes = new();

            while (true)
            {
                int value = stream.ReadByte();
                if (value < 0)
                {
                    break;
                }

                if (value == '\n')
                {
                    break;
                }

                bytes.Add((byte)value);
            }

            if (bytes.Count == 0 && stream.Position >= stream.Length)
            {
                return null;
            }

            if (bytes.Count > 0 && bytes[^1] == '\r')
            {
                bytes.RemoveAt(bytes.Count - 1);
            }

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        /// <summary>
        /// 从 PCD 字段列表中定位指定的深度字段。
        /// </summary>
        private static PcdField GetDepthField(PcdHeader header, string fieldName)
        {
            PcdField? field = header.Fields.FirstOrDefault(item => item.Name == fieldName);
            if (field == null)
            {
                throw new InvalidOperationException($"PCD 中未找到字段：{fieldName}");
            }

            if (field.Count != 1)
            {
                throw new InvalidOperationException($"PCD 字段 {fieldName} 的 COUNT 必须为 1。");
            }

            return field;
        }

        /// <summary>
        /// 读取 ASCII 编码 PCD 中的深度列。
        /// </summary>
        private static double[] ReadAsciiDepth(Stream stream, PcdHeader header, PcdField depthField)
        {
            int columnIndex = 0;
            foreach (PcdField field in header.Fields)
            {
                if (ReferenceEquals(field, depthField) || field.Name == depthField.Name)
                {
                    break;
                }

                columnIndex += field.Count;
            }

            double[] depth = new double[header.Points];
            using StreamReader reader = new(stream, Encoding.ASCII, false, 4096, true);
            int rowIndex = 0;

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                string[] values = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (columnIndex >= values.Length)
                {
                    throw new InvalidOperationException("PCD ASCII 数据列数不足，无法读取 Z 字段。");
                }

                if (rowIndex >= depth.Length)
                {
                    throw new InvalidOperationException("PCD ASCII 数据行数超过 POINTS。");
                }

                depth[rowIndex] = double.Parse(values[columnIndex], CultureInfo.InvariantCulture);
                rowIndex++;
            }

            if (rowIndex != depth.Length)
            {
                throw new InvalidOperationException(
                    $"PCD ASCII 数据行数与 POINTS 不匹配：{rowIndex} / {depth.Length}");
            }

            return depth;
        }

        /// <summary>
        /// 读取 binary 编码 PCD 中的深度字段。
        /// </summary>
        private static double[] ReadBinaryDepth(Stream stream, PcdHeader header, PcdField depthField)
        {
            int pointSize = header.Fields.Sum(field => field.ItemSize);
            int depthOffset = 0;

            foreach (PcdField field in header.Fields)
            {
                if (field.Name == depthField.Name)
                {
                    break;
                }

                depthOffset += field.ItemSize;
            }

            byte[] raw = ReadExactly(stream, header.Points * pointSize);
            double[] depth = new double[header.Points];

            for (int index = 0; index < header.Points; index++)
            {
                int offset = index * pointSize + depthOffset;
                depth[index] = ReadNumericValue(raw.AsSpan(offset, depthField.Size), depthField);
            }

            return depth;
        }

        /// <summary>
        /// 解压 binary_compressed PCD 数据并读取深度字段。
        /// </summary>
        private static double[] ReadBinaryCompressedDepth(Stream stream, PcdHeader header, PcdField depthField)
        {
            byte[] sizePrefix = ReadExactly(stream, 8);
            int compressedSize = BinaryPrimitives.ReadInt32LittleEndian(sizePrefix.AsSpan(0, 4));
            int uncompressedSize = BinaryPrimitives.ReadInt32LittleEndian(sizePrefix.AsSpan(4, 4));
            byte[] compressed = ReadExactly(stream, compressedSize);
            byte[] raw = LzfDecompress(compressed, uncompressedSize);

            int cursor = 0;
            foreach (PcdField field in header.Fields)
            {
                int blockSize = header.Points * field.ItemSize;
                if (cursor + blockSize > raw.Length)
                {
                    throw new InvalidOperationException("PCD binary_compressed 数据长度不足。");
                }

                if (field.Name == depthField.Name)
                {
                    double[] depth = new double[header.Points];
                    for (int index = 0; index < header.Points; index++)
                    {
                        int offset = cursor + index * field.ItemSize;
                        depth[index] = ReadNumericValue(raw.AsSpan(offset, field.Size), field);
                    }

                    return depth;
                }

                cursor += blockSize;
            }

            throw new InvalidOperationException("PCD 压缩数据中未找到 Z 字段。");
        }

        /// <summary>
        /// 从流中读取指定字节数，长度不足时抛出异常。
        /// </summary>
        private static byte[] ReadExactly(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;

            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    throw new InvalidOperationException("读取 PCD 数据体时遇到文件结尾。");
                }

                offset += read;
            }

            return buffer;
        }

        /// <summary>
        /// 按 PCD 字段类型把字节片段转换为数值。
        /// </summary>
        private static double ReadNumericValue(ReadOnlySpan<byte> data, PcdField field)
        {
            return field.TypeChar switch
            {
                'F' when field.Size == 4 => BitConverter.ToSingle(data),
                'F' when field.Size == 8 => BitConverter.ToDouble(data),
                'I' when field.Size == 1 => unchecked((sbyte)data[0]),
                'I' when field.Size == 2 => BinaryPrimitives.ReadInt16LittleEndian(data),
                'I' when field.Size == 4 => BinaryPrimitives.ReadInt32LittleEndian(data),
                'I' when field.Size == 8 => BinaryPrimitives.ReadInt64LittleEndian(data),
                'U' when field.Size == 1 => data[0],
                'U' when field.Size == 2 => BinaryPrimitives.ReadUInt16LittleEndian(data),
                'U' when field.Size == 4 => BinaryPrimitives.ReadUInt32LittleEndian(data),
                'U' when field.Size == 8 => BinaryPrimitives.ReadUInt64LittleEndian(data),
                _ => throw new InvalidOperationException(
                    $"不支持的 PCD 字段类型：{field.TypeChar}{field.Size}")
            };
        }

        /// <summary>
        /// 解压 PCD binary_compressed 使用的 LZF 数据块。
        /// </summary>
        private static byte[] LzfDecompress(byte[] data, int expectedLength)
        {
            int index = 0;
            int outputIndex = 0;
            byte[] output = new byte[expectedLength];

            while (index < data.Length)
            {
                int ctrl = data[index++];
                if (ctrl < 32)
                {
                    int literalLength = ctrl + 1;
                    int literalEnd = index + literalLength;
                    int outputEnd = outputIndex + literalLength;

                    if (literalEnd > data.Length || outputEnd > expectedLength)
                    {
                        throw new InvalidOperationException("PCD LZF 解压失败：literal 块越界。");
                    }

                    Buffer.BlockCopy(data, index, output, outputIndex, literalLength);
                    index = literalEnd;
                    outputIndex = outputEnd;
                    continue;
                }

                int referenceLength = ctrl >> 5;
                int referenceOffset = outputIndex - ((ctrl & 0x1F) << 8) - 1;

                if (referenceLength == 7)
                {
                    if (index >= data.Length)
                    {
                        throw new InvalidOperationException("PCD LZF 解压失败：缺少扩展长度字节。");
                    }

                    referenceLength += data[index++];
                }

                if (index >= data.Length)
                {
                    throw new InvalidOperationException("PCD LZF 解压失败：缺少回溯偏移字节。");
                }

                referenceOffset -= data[index++];
                referenceLength += 2;

                if (referenceOffset < 0 || outputIndex + referenceLength > expectedLength)
                {
                    throw new InvalidOperationException("PCD LZF 解压失败：回溯引用越界。");
                }

                for (int count = 0; count < referenceLength; count++)
                {
                    output[outputIndex] = output[referenceOffset];
                    outputIndex++;
                    referenceOffset++;
                }
            }

            if (outputIndex != expectedLength)
            {
                throw new InvalidOperationException(
                    $"PCD LZF 解压长度不匹配：{outputIndex} / {expectedLength}");
            }

            return output;
        }

        private sealed class PcdField
        {
            /// <summary>
            /// 创建 PCD 字段描述，记录字段名、字节数、类型和分量数。
            /// </summary>
            public PcdField(string name, int size, char typeChar, int count)
            {
                Name = name;
                Size = size;
                TypeChar = typeChar;
                Count = count;
            }

            /// <summary>
            /// PCD 字段名称，对应文件头 FIELDS 中的字段。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// PCD 字段单个分量的字节数。
            /// </summary>
            public int Size { get; }

            /// <summary>
            /// PCD 字段的数据类型标记，例如 F、I 或 U。
            /// </summary>
            public char TypeChar { get; }

            /// <summary>
            /// PCD 字段包含的分量数量。
            /// </summary>
            public int Count { get; }

            /// <summary>
            /// PCD 字段在单个点记录中占用的总字节数。
            /// </summary>
            public int ItemSize => Size * Count;
        }

        private sealed class PcdHeader
        {
            /// <summary>
            /// 创建 PCD 文件头描述，保存字段、尺寸、点数和数据编码方式。
            /// </summary>
            public PcdHeader(IReadOnlyList<PcdField> fields, int width, int height, int points, string data)
            {
                Fields = fields;
                Width = width;
                Height = height;
                Points = points;
                Data = data;
            }

            /// <summary>
            /// PCD 文件头解析出的字段定义列表。
            /// </summary>
            public IReadOnlyList<PcdField> Fields { get; }

            /// <summary>
            /// PCD 点云宽度或有序点云的列数。
            /// </summary>
            public int Width { get; }

            /// <summary>
            /// PCD 点云高度或有序点云的行数。
            /// </summary>
            public int Height { get; }

            /// <summary>
            /// PCD 文件声明的点数量。
            /// </summary>
            public int Points { get; }

            /// <summary>
            /// PCD 数据区编码方式，支持 ascii、binary 或 binary_compressed。
            /// </summary>
            public string Data { get; }
        }
    }

    internal static class PcdDepthValueConverter
    {
        /// <summary>
        /// PCD 的 Z 字段按原始数值写入高度图；单位换算在测量阶段统一完成。
        /// </summary>
        public static float ToHeightImageRawValue(double rawValue, double invalidValue)
        {
            if (!double.IsFinite(rawValue))
            {
                return ToFiniteFloat(invalidValue);
            }

            return ToFiniteFloat(rawValue);
        }

        private static float ToFiniteFloat(double value)
        {
            if (!double.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
            {
                throw new OverflowException("PCD 深度值超出 Single 范围。");
            }

            return (float)value;
        }
    }
}
