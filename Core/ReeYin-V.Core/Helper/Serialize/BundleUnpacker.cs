using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Helper
{
    /// <summary>
    /// 文件条目信息
    /// </summary>
    public class FileEntry
    {
        public string FileName { get; set; }
        public string FileType { get; set; } // 扩展名或MIME类型
        public long Offset { get; set; }     // 在包中的偏移量
        public long Size { get; set; }       // 文件大小
        public byte[] Hash { get; set; }     // 文件哈希值
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 文件包头部信息
    /// </summary>
    public class BundleHeader
    {
        public string MagicNumber { get; set; } = "FBPK"; // 文件标识
        public int Version { get; set; } = 1;
        public int FileCount { get; set; }
        public long IndexOffset { get; set; } // 索引表偏移量
        public long IndexSize { get; set; }   // 索引表大小
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 文件打包器
    /// </summary>
    public class FileBundlePacker
    {
        private const int BUFFER_SIZE = 8192;

        /// <summary>
        /// 打包多个文件成一个文件
        /// </summary>
        public void PackFiles(string outputPath, params string[] inputFiles)
        {
            using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(outputStream))
            {
                // 1. 预留头部空间
                long headerPosition = outputStream.Position;
                writer.Write(new byte[256]); // 预留256字节给头部

                // 2. 写入文件数据并记录信息
                var fileEntries = new List<FileEntry>();
                foreach (var filePath in inputFiles)
                {
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException($"文件不存在: {filePath}");

                    var entry = PackSingleFile(writer, filePath);
                    fileEntries.Add(entry);
                }

                // 3. 写入索引表（JSON格式）
                long indexPosition = outputStream.Position;
                var indexData = JsonSerializer.SerializeToUtf8Bytes(fileEntries);
                writer.Write(indexData);

                // 4. 写回头部信息
                outputStream.Position = headerPosition;
                var header = new BundleHeader
                {
                    FileCount = fileEntries.Count,
                    IndexOffset = indexPosition,
                    IndexSize = indexData.Length,
                    Properties =
                    {
                        ["Created"] = DateTime.UtcNow.ToString("O"),
                        ["Tool"] = "FileBundlePacker v1.0"
                    }
                };

                // 序列化头部
                var headerJson = JsonSerializer.Serialize(header);
                var headerBytes = Encoding.UTF8.GetBytes(headerJson);

                if (headerBytes.Length > 256)
                    throw new InvalidOperationException("头部信息过大");

                writer.Write(headerBytes);
                writer.Write(new byte[256 - headerBytes.Length]); // 填充剩余空间
            }
        }

        private FileEntry PackSingleFile(BinaryWriter writer, string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var entry = new FileEntry
            {
                FileName = Path.GetFileName(filePath),
                FileType = Path.GetExtension(filePath).ToLowerInvariant(),
                Offset = writer.BaseStream.Position,
                Size = fileInfo.Length
            };

            // 计算哈希值
            using (var sha256 = SHA256.Create())
            using (var fileStream = File.OpenRead(filePath))
            {
                entry.Hash = sha256.ComputeHash(fileStream);
            }

            // 写入文件数据
            using (var fileStream = File.OpenRead(filePath))
            {
                var buffer = new byte[BUFFER_SIZE];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                }
            }

            // 添加元数据
            entry.Metadata["OriginalPath"] = filePath;
            entry.Metadata["Created"] = fileInfo.CreationTimeUtc.ToString("O");
            entry.Metadata["LastModified"] = fileInfo.LastWriteTimeUtc.ToString("O");

            return entry;
        }
    }

    /// <summary>
    /// 文件解包器
    /// </summary>
    public class FileBundleUnpacker
    {
        /// <summary>
        /// 从包中读取所有文件
        /// </summary>
        public Dictionary<string, byte[]> UnpackAll(string bundlePath)
        {
            var result = new Dictionary<string, byte[]>();

            using (var inputStream = new FileStream(bundlePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(inputStream))
            {
                // 1. 读取头部
                var header = ReadHeader(reader);

                // 2. 读取索引表
                inputStream.Position = header.IndexOffset;
                var indexBytes = reader.ReadBytes((int)header.IndexSize);
                var fileEntries = JsonSerializer.Deserialize<List<FileEntry>>(indexBytes);

                // 3. 读取每个文件
                foreach (var entry in fileEntries)
                {
                    inputStream.Position = entry.Offset;
                    var fileData = reader.ReadBytes((int)entry.Size);

                    // 验证哈希值（可选）
                    if (entry.Hash != null && entry.Hash.Length > 0)
                    {
                        using (var sha256 = SHA256.Create())
                        {
                            var computedHash = sha256.ComputeHash(fileData);
                            if (!computedHash.SequenceEqual(entry.Hash))
                            {
                                Console.WriteLine($"警告: 文件 {entry.FileName} 哈希值不匹配");
                            }
                        }
                    }

                    result[entry.FileName] = fileData;
                }
            }

            return result;
        }

        /// <summary>
        /// 从包中提取特定文件
        /// </summary>
        public byte[] ExtractFile(string bundlePath, string fileName)
        {
            using (var inputStream = new FileStream(bundlePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(inputStream))
            {
                // 1. 读取头部
                var header = ReadHeader(reader);

                // 2. 读取索引表
                inputStream.Position = header.IndexOffset;
                var indexBytes = reader.ReadBytes((int)header.IndexSize);
                var fileEntries = JsonSerializer.Deserialize<List<FileEntry>>(indexBytes);

                // 3. 查找指定文件
                var entry = fileEntries.FirstOrDefault(e => e.FileName == fileName);
                if (entry == null)
                    throw new FileNotFoundException($"在包中未找到文件: {fileName}");

                // 4. 读取文件数据
                inputStream.Position = entry.Offset;
                return reader.ReadBytes((int)entry.Size);
            }
        }

        /// <summary>
        /// 获取包内文件列表
        /// </summary>
        public List<FileEntry> GetFileList(string bundlePath)
        {
            using (var inputStream = new FileStream(bundlePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(inputStream))
            {
                var header = ReadHeader(reader);
                inputStream.Position = header.IndexOffset;
                var indexBytes = reader.ReadBytes((int)header.IndexSize);
                return JsonSerializer.Deserialize<List<FileEntry>>(indexBytes);
            }
        }

        private BundleHeader ReadHeader(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;
            var headerBytes = reader.ReadBytes(256);
            var headerJson = Encoding.UTF8.GetString(headerBytes).TrimEnd('\0');
            return JsonSerializer.Deserialize<BundleHeader>(headerJson);
        }
    }

     //示例
     //static void Main(string[] args)
     //   {
     //       var packer = new FileBundlePacker();
     //       var unpacker = new FileBundleUnpacker();

     //       // 打包文件
     //       string[] filesToPack =
     //       {
     //       "config.json",
     //       "image.png",
     //       "model.onnx",
     //       "readme.txt"
     //   };

     //       Console.WriteLine("开始打包文件...");
     //       packer.PackFiles("resources.bundle", filesToPack);
     //       Console.WriteLine("打包完成！");

     //       // 解包所有文件
     //       Console.WriteLine("\n解包所有文件...");
     //       var allFiles = unpacker.UnpackAll("resources.bundle");
     //       foreach (var file in allFiles)
     //       {
     //           Console.WriteLine($"提取: {file.Key} ({file.Value.Length} 字节)");

     //           // 保存到文件
     //           File.WriteAllBytes($"extracted_{file.Key}", file.Value);
     //       }

     //       // 提取单个文件
     //       Console.WriteLine("\n提取单个文件...");
     //       var jsonData = unpacker.ExtractFile("resources.bundle", "config.json");
     //       Console.WriteLine($"config.json: {jsonData.Length} 字节");

     //       // 获取文件列表
     //       Console.WriteLine("\n包内文件列表:");
     //       var fileList = unpacker.GetFileList("resources.bundle");
     //       foreach (var entry in fileList)
     //       {
     //           Console.WriteLine($"{entry.FileName} ({entry.FileType}) - {entry.Size} 字节");
     //       }
     //   }
    }
