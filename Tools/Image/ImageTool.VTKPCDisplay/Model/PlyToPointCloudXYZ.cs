using PointCloudSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageTool.VTKPCDisplay.Model
{
    /// <summary>
    /// 加载PLY转为PointCloudXYZ
    /// </summary>
    public static class PlyToPointCloudXYZ
    {

        public static PointCloudXYZ Load(string plyPath)
        {
            var cloud = new PointCloudXYZ();
            LoadInto(plyPath, cloud);
            return cloud;
        }

        public static Task LoadIntoAsync(string plyPath, PointCloudXYZ cloud, IProgress<int> progress, CancellationToken token)
        {
            return Task.Run(() =>
            {
                LoadIntoCore(plyPath, cloud, sampleStep: 1, maxPoints: 0, progress: progress, token: token);
            }, token);
        }

        public static Task LoadPreviewIntoAsync(string plyPath, PointCloudXYZ previewCloud, int maxPoints, CancellationToken token)
        {
            return Task.Run(() =>
            {
                LoadIntoCore(plyPath, previewCloud, sampleStep: 0, maxPoints: maxPoints, progress: null, token: token);
            }, token);
        }

        public static async Task LoadProgressiveAsync(
            string plyPath,
            PointCloudXYZ previewCloud,
            PointCloudXYZ fullCloud,
            int previewMaxPoints,
            Action onPreviewReady,
            Action onFullReady,
            IProgress<int> fullProgress,
            CancellationToken token,
            Action<Action> uiInvoke) // <- 新增
        {
            await LoadPreviewIntoAsync(plyPath, previewCloud, previewMaxPoints, token).ConfigureAwait(false);
            if (onPreviewReady != null) uiInvoke(onPreviewReady);

            await LoadIntoAsync(plyPath, fullCloud, fullProgress, token).ConfigureAwait(false);
            if (onFullReady != null) uiInvoke(onFullReady);
        }


        private static void LoadIntoCore(
    string plyPath,
    PointCloudXYZ cloud,
    int sampleStep,          // =1 全量；=0 表示自动计算
    int maxPoints,           // >0 表示做预览（限点）
    IProgress<int> progress,
    CancellationToken token)
        {
            if (cloud == null) throw new ArgumentNullException("cloud");
            if (string.IsNullOrWhiteSpace(plyPath)) throw new ArgumentNullException("plyPath");
            if (!File.Exists(plyPath)) throw new FileNotFoundException("PLY 文件不存在", plyPath);

            cloud.Clear();

            // 大 buffer + 顺序扫描（对大文件有帮助）
            using (var fs = new FileStream(plyPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4 << 20, FileOptions.SequentialScan))
            {
                var header = ParseHeader(fs);

                if (header.VertexCount <= 0)
                    throw new InvalidDataException("PLY header 中 vertex 数为 0 或未找到 element vertex");

                if (header.XIndex < 0 || header.YIndex < 0 || header.ZIndex < 0)
                    throw new InvalidDataException("PLY vertex 属性中未找到 x/y/z");

                // 自动计算抽样步长：让点数大约 <= maxPoints
                if (sampleStep <= 0)
                {
                    if (maxPoints > 0)
                        sampleStep = Math.Max(1, header.VertexCount / Math.Max(1, maxPoints));
                    else
                        sampleStep = 1;
                }

                if (header.Format == PlyFormat.Ascii)
                {
                    ReadVerticesAscii_FastToken(fs, header, cloud, sampleStep, progress, token);
                }
                else if (header.Format == PlyFormat.BinaryLittleEndian)
                {
                    ReadVerticesBinaryLittle_Sampled(fs, header, cloud, sampleStep, progress, token);
                }
                else if (header.Format == PlyFormat.BinaryBigEndian)
                {
                    throw new NotSupportedException("暂不支持 binary_big_endian");
                }
                else
                {
                    throw new NotSupportedException("未知 PLY 格式");
                }
            }
        }

        private static void ReadVerticesAscii_FastToken(
    FileStream fs, PlyHeader header, PointCloudXYZ cloud,
    int sampleStep,
    IProgress<int> progress,
    CancellationToken token)
        {
            using (var sr = new StreamReader(fs, Encoding.ASCII, false, 1 << 20, true))
            {
                var tr = new AsciiTokenReader(sr);
                var ci = CultureInfo.InvariantCulture;

                int propCount = header.VertexProperties.Count;

                const int reportStep = 20000;

                for (int i = 0; i < header.VertexCount; i++)
                {
                    if ((i & 0x3FFF) == 0) token.ThrowIfCancellationRequested();

                    bool take = (sampleStep <= 1) || (i % sampleStep == 0);

                    double x = 0, y = 0, z = 0;

                    for (int p = 0; p < propCount; p++)
                    {
                        string tok = tr.ReadToken();
                        if (tok == null) throw new EndOfStreamException("PLY 顶点数据不足");

                        if (!take) continue; // 不抽样的点：只消费 token，不做 Parse

                        if (p == header.XIndex) x = double.Parse(tok, ci);
                        else if (p == header.YIndex) y = double.Parse(tok, ci);
                        else if (p == header.ZIndex) z = double.Parse(tok, ci);
                    }

                    if (take)
                        cloud.Push(x, y, z);

                    if (progress != null && (i % reportStep) == 0)
                        progress.Report(i);
                }

                if (progress != null) progress.Report(header.VertexCount);
            }
        }

        private sealed class AsciiTokenReader
        {
            private readonly TextReader _reader;

            public AsciiTokenReader(TextReader reader)
            {
                _reader = reader;
            }

            public string ReadToken()
            {
                // 跳过空白
                int c = _reader.Read();
                while (c >= 0 && char.IsWhiteSpace((char)c)) c = _reader.Read();
                if (c < 0) return null;

                var sb = new StringBuilder(32);
                while (c >= 0 && !char.IsWhiteSpace((char)c))
                {
                    sb.Append((char)c);
                    c = _reader.Read();
                }
                return sb.ToString();
            }
        }

        private static void ReadVerticesBinaryLittle_Sampled(
    FileStream fs, PlyHeader header, PointCloudXYZ cloud,
    int sampleStep,
    IProgress<int> progress,
    CancellationToken token)
{
    using (var br = new BinaryReader(fs, Encoding.ASCII, true))
    {
        int propCount = header.VertexProperties.Count;
        int recordSize = GetVertexRecordSize(header);

        const int reportStep = 50000;

        for (int i = 0; i < header.VertexCount; i++)
        {
            if ((i & 0x3FFF) == 0) token.ThrowIfCancellationRequested();

            bool take = (sampleStep <= 1) || (i % sampleStep == 0);
            if (!take)
            {
                br.BaseStream.Seek(recordSize, SeekOrigin.Current);
                continue;
            }

            double x = 0, y = 0, z = 0;
            for (int p = 0; p < propCount; p++)
            {
                var prop = header.VertexProperties[p];
                double val = ReadScalarAsDoubleLittle(br, prop.Type);

                if (p == header.XIndex) x = val;
                else if (p == header.YIndex) y = val;
                else if (p == header.ZIndex) z = val;
            }

            cloud.Push(x, y, z);

            if (progress != null && (i % reportStep) == 0)
                progress.Report(i);
        }

        if (progress != null) progress.Report(header.VertexCount);
    }
}

private static int GetVertexRecordSize(PlyHeader header)
{
    int size = 0;
    for (int i = 0; i < header.VertexProperties.Count; i++)
        size += GetScalarSize(header.VertexProperties[i].Type);
    return size;
}

private static int GetScalarSize(PlyScalarType t)
{
    switch (t)
    {
        case PlyScalarType.Int8:
        case PlyScalarType.UInt8:
            return 1;
        case PlyScalarType.Int16:
        case PlyScalarType.UInt16:
            return 2;
        case PlyScalarType.Int32:
        case PlyScalarType.UInt32:
        case PlyScalarType.Float32:
            return 4;
        case PlyScalarType.Float64:
            return 8;
        default:
            throw new NotSupportedException();
    }
}

        public static void LoadInto(string plyPath, PointCloudXYZ cloud)
        {
            if (cloud == null) throw new ArgumentNullException(nameof(cloud));
            if (string.IsNullOrWhiteSpace(plyPath)) throw new ArgumentNullException(nameof(plyPath));
            if (!File.Exists(plyPath)) throw new FileNotFoundException("PLY 文件不存在", plyPath);

            cloud.Clear();

            using (var fs = new FileStream(plyPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var header = ParseHeader(fs);

                if (header.VertexCount <= 0)
                    throw new InvalidDataException("PLY header 中 vertex 数为 0 或未找到 element vertex");

                if (header.XIndex < 0 || header.YIndex < 0 || header.ZIndex < 0)
                    throw new InvalidDataException("PLY vertex 属性中未找到 x/y/z");

                if (header.Format == PlyFormat.Ascii)
                {
                    ReadVerticesAscii(fs, header, cloud);
                }
                else if (header.Format == PlyFormat.BinaryLittleEndian)
                {
                    ReadVerticesBinaryLittle(fs, header, cloud);
                }
                else if (header.Format == PlyFormat.BinaryBigEndian)
                {
                    throw new NotSupportedException("暂不支持 binary_big_endian");
                }
                else
                {
                    throw new NotSupportedException("未知 PLY 格式");
                }
            }
        }

        // ---------------- header parsing ----------------

        private enum PlyFormat { Ascii, BinaryLittleEndian, BinaryBigEndian }

        private enum PlyScalarType
        {
            Int8, UInt8, Int16, UInt16, Int32, UInt32, Float32, Float64
        }

        private sealed class PlyProperty
        {
            public string Name;
            public PlyScalarType Type;
        }

        private sealed class PlyHeader
        {
            public PlyFormat Format;
            public int VertexCount;
            public List<PlyProperty> VertexProperties = new List<PlyProperty>();

            public int XIndex = -1;
            public int YIndex = -1;
            public int ZIndex = -1;
        }

        private static PlyHeader ParseHeader(FileStream fs)
        {
            var header = new PlyHeader();
            bool inVertexElement = false;

            string line = ReadLineAscii(fs);
            if (line == null || !string.Equals(line.Trim(), "ply", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("不是有效的 PLY 文件（缺少 ply 头）");

            while (true)
            {
                line = ReadLineAscii(fs);
                if (line == null) throw new EndOfStreamException("PLY header 未正常结束（缺少 end_header）");

                string s = line.Trim();
                if (s.Length == 0) continue;

                if (s.StartsWith("comment", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (s.StartsWith("format", StringComparison.OrdinalIgnoreCase))
                {
                    // format ascii 1.0 / format binary_little_endian 1.0 / format binary_big_endian 1.0
                    var parts = SplitWs(s);
                    if (parts.Length < 3) throw new InvalidDataException("PLY format 行不合法");

                    string fmt = parts[1];
                    if (fmt == "ascii") header.Format = PlyFormat.Ascii;
                    else if (fmt == "binary_little_endian") header.Format = PlyFormat.BinaryLittleEndian;
                    else if (fmt == "binary_big_endian") header.Format = PlyFormat.BinaryBigEndian;
                    else throw new InvalidDataException("未知 PLY format: " + fmt);

                    continue;
                }

                if (s.StartsWith("element", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = SplitWs(s);
                    if (parts.Length < 3) throw new InvalidDataException("PLY element 行不合法");

                    string elementName = parts[1];
                    int count = int.Parse(parts[2], CultureInfo.InvariantCulture);

                    inVertexElement = string.Equals(elementName, "vertex", StringComparison.OrdinalIgnoreCase);
                    if (inVertexElement)
                        header.VertexCount = count;

                    continue;
                }

                if (s.StartsWith("property", StringComparison.OrdinalIgnoreCase))
                {
                    if (!inVertexElement)
                        continue; // 只关心 vertex 的 property

                    var parts = SplitWs(s);
                    if (parts.Length < 3) throw new InvalidDataException("PLY property 行不合法");

                    // property list ... 这里暂不支持
                    if (string.Equals(parts[1], "list", StringComparison.OrdinalIgnoreCase))
                        throw new NotSupportedException("暂不支持 vertex element 中的 list property");

                    var type = ParseScalarType(parts[1]);
                    var name = parts[2];

                    int idx = header.VertexProperties.Count;
                    header.VertexProperties.Add(new PlyProperty { Name = name, Type = type });

                    if (string.Equals(name, "x", StringComparison.OrdinalIgnoreCase)) header.XIndex = idx;
                    else if (string.Equals(name, "y", StringComparison.OrdinalIgnoreCase)) header.YIndex = idx;
                    else if (string.Equals(name, "z", StringComparison.OrdinalIgnoreCase)) header.ZIndex = idx;

                    continue;
                }

                if (string.Equals(s, "end_header", StringComparison.OrdinalIgnoreCase))
                    break;
            }

            return header;
        }

        private static PlyScalarType ParseScalarType(string t)
        {
            // 兼容常见别名
            switch (t)
            {
                case "char":
                case "int8":
                    return PlyScalarType.Int8;

                case "uchar":
                case "uint8":
                    return PlyScalarType.UInt8;

                case "short":
                case "int16":
                    return PlyScalarType.Int16;

                case "ushort":
                case "uint16":
                    return PlyScalarType.UInt16;

                case "int":
                case "int32":
                    return PlyScalarType.Int32;

                case "uint":
                case "uint32":
                    return PlyScalarType.UInt32;

                case "float":
                case "float32":
                    return PlyScalarType.Float32;

                case "double":
                case "float64":
                    return PlyScalarType.Float64;

                default:
                    throw new NotSupportedException("不支持的 PLY 标量类型: " + t);
            }
        }

        // ---------------- vertex reading ----------------

        private static void ReadVerticesAscii(FileStream fs, PlyHeader header, PointCloudXYZ cloud)
        {
            // 注意：leaveOpen=true 保持 FileStream 不被关闭
            using (var sr = new StreamReader(fs, Encoding.ASCII, false, 1 << 20, true))
            {
                var ci = CultureInfo.InvariantCulture;
                int needMaxIndex = Math.Max(header.XIndex, Math.Max(header.YIndex, header.ZIndex));

                for (int i = 0; i < header.VertexCount; i++)
                {
                    string line;
                    do
                    {
                        line = sr.ReadLine();
                        if (line == null) throw new EndOfStreamException("PLY 顶点数据不足");
                        line = line.Trim();
                    } while (line.Length == 0);

                    var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length <= needMaxIndex)
                        throw new InvalidDataException("第 " + i + " 个顶点数据列数不足");

                    double x = double.Parse(parts[header.XIndex], ci);
                    double y = double.Parse(parts[header.YIndex], ci);
                    double z = double.Parse(parts[header.ZIndex], ci);

                    cloud.Push(x, y, z);
                }
            }
        }

        private static void ReadVerticesBinaryLittle(FileStream fs, PlyHeader header, PointCloudXYZ cloud)
        {
            using (var br = new BinaryReader(fs, Encoding.ASCII, true))
            {
                int propCount = header.VertexProperties.Count;

                for (int i = 0; i < header.VertexCount; i++)
                {
                    double x = 0, y = 0, z = 0;

                    for (int p = 0; p < propCount; p++)
                    {
                        var prop = header.VertexProperties[p];
                        double val = ReadScalarAsDoubleLittle(br, prop.Type);

                        if (p == header.XIndex) x = val;
                        else if (p == header.YIndex) y = val;
                        else if (p == header.ZIndex) z = val;
                    }

                    cloud.Push(x, y, z);
                }
            }
        }

        private static double ReadScalarAsDoubleLittle(BinaryReader br, PlyScalarType type)
        {
            switch (type)
            {
                case PlyScalarType.Int8: return (sbyte)br.ReadByte();
                case PlyScalarType.UInt8: return br.ReadByte();
                case PlyScalarType.Int16: return br.ReadInt16();
                case PlyScalarType.UInt16: return br.ReadUInt16();
                case PlyScalarType.Int32: return br.ReadInt32();
                case PlyScalarType.UInt32: return br.ReadUInt32();
                case PlyScalarType.Float32: return br.ReadSingle();
                case PlyScalarType.Float64: return br.ReadDouble();
                default: throw new NotSupportedException();
            }
        }

        // ---------------- helpers ----------------

        private static string[] SplitWs(string s)
        {
            return s.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string ReadLineAscii(FileStream fs)
        {
            // 读取一行 ASCII（以 \n 结束），返回不含 \r\n
            List<byte> bytes = new List<byte>(128);

            while (true)
            {
                int b = fs.ReadByte();
                if (b < 0)
                {
                    if (bytes.Count == 0) return null;
                    break;
                }

                if (b == '\n') break;
                bytes.Add((byte)b);
            }

            if (bytes.Count > 0 && bytes[bytes.Count - 1] == '\r')
                bytes.RemoveAt(bytes.Count - 1);

            return Encoding.ASCII.GetString(bytes.ToArray());
        }
    }
}


