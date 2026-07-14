using Dm;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Helper
{
    public static class FileHelper
    {
        /// <summary>
        /// 隐藏文件夹用来存储一些不开放给客户的配置
        /// </summary>
        public static string AppHiddenPath = "C:\\ProgramData\\ReeYin\\Project";

        private static string _ConfigFilePath = Directory.GetCurrentDirectory() + "\\ConfigFile\\";
        public static string ConfigFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_ConfigFilePath))
                {
                    _ConfigFilePath = Directory.GetCurrentDirectory() + "\\ConfigFile\\";
                }
                //if (!Directory.Exists(_ConfigFilePath))
                //{
                //    Directory.CreateDirectory(_ConfigFilePath);
                //}
                return _ConfigFilePath;
            }
        }

        public static List<string> GetImagePaths(string folderPath)
        {
            List<string> imagePaths = new List<string>();
            string[] supportedExtensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".ico" };

            DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
            FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);

            foreach (FileInfo file in files)
            {
                if (supportedExtensions.Contains(file.Extension.ToLower()))
                {
                    imagePaths.Add(file.FullName);
                }
            }

            return imagePaths;
        }

        /// <summary>
        /// 在ProgramData下创建隐藏文件夹
        /// </summary>
        /// <param name="folderName">要创建的文件夹名称（如"MyHiddenFolder"）</param>
        /// <returns>创建的文件夹完整路径</returns>
        /// <exception cref="IOException">IO操作失败时抛出</exception>
        /// <exception cref="UnauthorizedAccessException">权限不足时抛出</exception>
        public static string CreateHiddenFolderInProgramData()
        {
            try
            {
                // 1. 获取ProgramData的绝对路径（系统通用路径，如C:\ProgramData）
                string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                // 2. 拼接目标隐藏文件夹的完整路径
                string targetFolderPath = Path.Combine(programDataPath, AppHiddenPath);

                // 3. 创建文件夹（若已存在则跳过创建）
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                    Console.WriteLine($"文件夹创建成功：{targetFolderPath}");
                }
                else
                {
                    Console.WriteLine($"文件夹已存在：{targetFolderPath}");
                }

                // 4. 设置文件夹为隐藏属性（关键步骤）
                // 获取当前文件夹属性，追加Hidden属性（保留原有属性，如Readonly）
                FileAttributes attributes = File.GetAttributes(targetFolderPath);
                if (!attributes.HasFlag(FileAttributes.Hidden))
                {
                    File.SetAttributes(targetFolderPath, attributes | FileAttributes.Hidden);
                    Console.WriteLine("文件夹已设置为隐藏属性");
                }
                else
                {
                    Console.WriteLine("文件夹已是隐藏状态");
                }

                return targetFolderPath;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException("创建隐藏文件夹失败：权限不足，请以管理员身份运行程序", ex);
            }
            catch (IOException ex)
            {
                throw new IOException("创建隐藏文件夹失败：IO异常", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("创建隐藏文件夹失败：未知错误", ex);
            }
        }

        public static void DeleteFilesOlderThan(
            string folderPath,
            int days,
            string searchPattern = "*.*",
            bool includeSubFolders = false)
        {
            if (!Directory.Exists(folderPath))
                return;

            DateTime cutoff = DateTime.Now.AddDays(-days);

            var files = Directory.GetFiles(
                folderPath,
                searchPattern,
                includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                try
                {
                    FileInfo info = new FileInfo(file);

                    // 使用最后写入时间判断是否过期
                    if (info.LastWriteTime < cutoff)
                    {
                        info.Delete();
                        Console.WriteLine($"删除文件: {info.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法删除文件 {file}: {ex.Message}");
                }
            }
        }

        public static bool ContainsFolder(string parentPath, string folderName)
        {
            if (!Directory.Exists(parentPath))
                return false;

            DirectoryInfo parentDir = new DirectoryInfo(parentPath);
            return parentDir.GetDirectories()
                            .Any(subDir => subDir.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));
        }


        /// <summary>
        /// 删除超过设置时间的文件和文件夹
        /// </summary>
        /// <param name="folderPath">指定文件夹</param>
        /// <param name="timeLimit">时间</param>
        public static void CleanOldFilesAndFolders(string folderPath, TimeSpan timeLimit)
        {
            try
            {
                // 获取当前时间
                DateTime now = DateTime.Now;

                // 处理文件夹下的文件
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    DateTime creationTime = File.GetCreationTime(file);
                    if (now - creationTime > timeLimit)
                    {
                        try
                        {
                            File.Delete(file);

                            //Console.WriteLine($"已删除文件: {file}");
                        }
                        catch (Exception ex)
                        {

                            //Console.WriteLine($"删除文件 {file} 时出错: {ex.Message}");
                        }
                    }
                }

                // 处理文件夹下的子文件夹
                string[] subFolders = Directory.GetDirectories(folderPath);
                foreach (string subFolder in subFolders)
                {
                    DateTime creationTime = Directory.GetCreationTime(subFolder);
                    if (now - creationTime > timeLimit)
                    {
                        try
                        {
                            Directory.Delete(subFolder, true);

                            //Console.WriteLine($"已删除文件夹: {subFolder}");
                        }
                        catch (Exception ex)
                        {

                            //Console.WriteLine($"删除文件夹 {subFolder} 时出错: {ex.Message}");
                        }
                    }
                    else
                    {
                        // 递归处理子文件夹中的内容
                        CleanOldFilesAndFolders(subFolder, timeLimit);
                    }
                }
            }
            catch (Exception ex)
            {

                //Console.WriteLine($"处理文件夹 {folderPath} 时出错: {ex.Message}");
            }
        }


        public static void CleanDateFormattedFolders(string directoryPath, int daysToRetain, bool requireBothChecks = true, bool dryRun = false)
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(directoryPath))
                Logs.LogWarning("目录路径不能为空");

            if (!Directory.Exists(directoryPath))
                Logs.LogWarning($"指定的目录不存在: {directoryPath}");

            if (daysToRetain < 0)
                Logs.LogWarning($"保留天数不能为负数");

            // 日期格式正则表达式
            var dateFormatRegex = new Regex(@"^\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}$", RegexOptions.Compiled);
            var cutoffDate = DateTime.Now.AddDays(-daysToRetain);

            try
            {
                // 获取所有子目录
                var directories = Directory.GetDirectories(directoryPath);

                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    var dirInfo = new DirectoryInfo(dir);

                    // 验证文件夹名称格式
                    bool nameFormatValid = dateFormatRegex.IsMatch(dirName);
                    DateTime folderDate = DateTime.MinValue;

                    if (nameFormatValid && TryParseFolderDate(dirName, out folderDate))
                    {
                        // 检查文件夹名称日期是否过期
                        bool nameDateExpired = folderDate < cutoffDate;

                        // 检查实际创建时间是否过期
                        bool creationTimeExpired = dirInfo.CreationTime < cutoffDate;

                        // 根据requireBothChecks决定删除条件
                        bool shouldDelete = requireBothChecks
                            ? nameDateExpired && creationTimeExpired  // 两者都过期才删除
                            : nameDateExpired || creationTimeExpired; // 任一过期就删除

                        if (shouldDelete)
                        {
                            Console.WriteLine($"发现符合条件的文件夹: {dirName}");
                            Console.WriteLine($"  名称日期: {folderDate:yyyy-MM-dd HH:mm:ss}");
                            Console.WriteLine($"  创建时间: {dirInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");

                            if (!dryRun)
                            {
                                try
                                {
                                    // 删除文件夹及其内容
                                    Directory.Delete(dir, recursive: true);
                                    //CCDMainWindow.Instance.AddLog($"已删除: {dir}", LogEnum.Info);
                                    Console.WriteLine($"已删除: {dir}");
                                }
                                catch (Exception ex)
                                {
                                    //CCDMainWindow.Instance.AddLog($"删除失败 [{dir}]: {ex.Message}", LogEnum.Error);
                                    Console.WriteLine($"删除失败 [{dir}]: {ex.Message}");
                                }
                            }
                            else
                            {
                                //CCDMainWindow.Instance.AddLog($"[预览模式] 会删除: {dir}", LogEnum.Info);
                                Console.WriteLine($"[预览模式] 会删除: {dir}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //CCDMainWindow.Instance.AddLog($"清理过程中发生错误: {ex.Message}", LogEnum.Error);
                Console.WriteLine($"清理过程中发生错误: {ex.Message}");
                // 可根据需要记录日志或抛出异常
            }
        }

        // 辅助方法：解析文件夹名称为DateTime
        private static bool TryParseFolderDate(string folderName, out DateTime result)
        {
            result = DateTime.MinValue;

            try
            {
                // 解析"yyyy-MM-dd-HH-mm-ss"格式
                var parts = folderName.Split('-');
                if (parts.Length == 6 &&
                    int.TryParse(parts[0], out int year) &&
                    int.TryParse(parts[1], out int month) &&
                    int.TryParse(parts[2], out int day) &&
                    int.TryParse(parts[3], out int hour) &&
                    int.TryParse(parts[4], out int minute) &&
                    int.TryParse(parts[5], out int second))
                {
                    result = new DateTime(year, month, day, hour, minute, second);
                    return true;
                }
            }
            catch
            {
                // 解析失败
            }

            return false;
        }
    }
}
