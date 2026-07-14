using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Update
{
    [ExposedService(Lifetime.Singleton, 3, typeof(IUpdateService))]
    public class UpdateService : BindableBase, IUpdateService
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isUpdating;
        private string _serverUrl;
        private string _backupPath;
        #endregion

        #region Properties
        public string ServerUrl
        {
            get => _serverUrl;
            set => SetProperty(ref _serverUrl, value);
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            private set => SetProperty(ref _isUpdating, value);
        }
        #endregion

        #region Constructor
        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(30);
            _serverUrl = "http://localhost:5000/api/update";
            _backupPath = Path.Combine(FileHelper.AppHiddenPath, "Backups");
        }
        #endregion

        #region IUpdateService Implementation

        public async Task<List<UpdatePackageInfo>> CheckForUpdatesAsync(UpdateQueryCondition condition)
        {
            try
            {
                var requestUrl = $"{ServerUrl}/check";
                var content = new StringContent(
                    JsonConvert.SerializeObject(condition),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(requestUrl, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<UpdatePackageInfo>>(json) ?? new List<UpdatePackageInfo>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查更新失败: {ex.Message}");
                return new List<UpdatePackageInfo>();
            }
        }

        public async Task<string> DownloadUpdateAsync(UpdatePackageInfo package, IProgress<UpdateProgress> progress = null)
        {
            if (IsUpdating)
                throw new InvalidOperationException("已有更新任务正在进行中");

            IsUpdating = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Downloading,
                    Percentage = 0,
                    Message = "开始下载..."
                });

                var downloadPath = Path.Combine(FileHelper.AppHiddenPath, "Updates");
                Directory.CreateDirectory(downloadPath);

                var fileName = $"{package.PackageId}_{package.Version}.zip";
                var filePath = Path.Combine(downloadPath, fileName);

                using var response = await _httpClient.GetAsync(
                    package.DownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    _cancellationTokenSource.Token);

                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? package.FileSize;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, _cancellationTokenSource.Token);
                    totalBytesRead += bytesRead;

                    var percentage = totalBytes > 0 ? (int)(totalBytesRead * 100 / totalBytes) : 0;
                    progress?.Report(new UpdateProgress
                    {
                        Stage = UpdateStage.Downloading,
                        Percentage = percentage,
                        BytesDownloaded = totalBytesRead,
                        TotalBytes = totalBytes,
                        Message = $"下载中... {percentage}%"
                    });
                }

                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Downloading,
                    Percentage = 100,
                    Message = "下载完成"
                });

                return filePath;
            }
            catch (OperationCanceledException)
            {
                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Failed,
                    Message = "下载已取消"
                });
                throw;
            }
            catch (Exception ex)
            {
                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Failed,
                    Message = $"下载失败: {ex.Message}"
                });
                throw;
            }
            finally
            {
                IsUpdating = false;
            }
        }

        public async Task<bool> InstallUpdateAsync(string packagePath, IProgress<UpdateProgress> progress = null)
        {
            if (!File.Exists(packagePath))
            {
                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Failed,
                    Message = "更新包不存在"
                });
                return false;
            }

            IsUpdating = true;
            _cancellationTokenSource = new CancellationTokenSource();

            var extractPath = Path.Combine(FileHelper.AppHiddenPath, "Updates", "Extracted");
            var backupDir = Path.Combine(_backupPath, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            var installedFiles = new List<string>();

            try
            {
                // 解压阶段
                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Extracting,
                    Percentage = 0,
                    Message = "正在解压更新包..."
                });

                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                Directory.CreateDirectory(extractPath);
                Directory.CreateDirectory(backupDir);

                await Task.Run(() => ZipFile.ExtractToDirectory(packagePath, extractPath), _cancellationTokenSource.Token);

                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Extracting,
                    Percentage = 30,
                    Message = "解压完成"
                });

                // 读取清单文件（如果存在）
                var manifestPath = Path.Combine(extractPath, "manifest.json");
                string targetBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules");

                if (File.Exists(manifestPath))
                {
                    var manifestJson = await File.ReadAllTextAsync(manifestPath);
                    var manifest = JsonConvert.DeserializeObject<PackageManifest>(manifestJson);

                    if (manifest?.InstallPath != null)
                    {
                        targetBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, manifest.InstallPath);
                    }
                }

                // 安装阶段
                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Installing,
                    Percentage = 30,
                    Message = "正在安装更新..."
                });

                var files = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories);
                // 排除清单文件
                files = Array.FindAll(files, f => !f.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase));

                var totalFiles = files.Length;
                var processedFiles = 0;

                foreach (var file in files)
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    var relativePath = file.Substring(extractPath.Length + 1);
                    var destPath = Path.Combine(targetBasePath, relativePath);

                    var destDir = Path.GetDirectoryName(destPath);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    // 备份原文件
                    if (File.Exists(destPath))
                    {
                        var backupFilePath = Path.Combine(backupDir, relativePath);
                        var backupFileDir = Path.GetDirectoryName(backupFilePath);
                        if (!Directory.Exists(backupFileDir))
                            Directory.CreateDirectory(backupFileDir);

                        File.Copy(destPath, backupFilePath, true);
                    }

                    File.Copy(file, destPath, true);
                    installedFiles.Add(destPath);
                    processedFiles++;

                    var percentage = 30 + (processedFiles * 60 / totalFiles);
                    progress?.Report(new UpdateProgress
                    {
                        Stage = UpdateStage.Installing,
                        Percentage = percentage,
                        CurrentFile = relativePath,
                        Message = $"安装中... {relativePath}"
                    });
                }

                // 清理临时文件
                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Installing,
                    Percentage = 95,
                    Message = "正在清理临时文件..."
                });

                Directory.Delete(extractPath, true);

                // 保存安装记录
                await SaveInstallRecordAsync(packagePath, installedFiles, backupDir);

                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Completed,
                    Percentage = 100,
                    Message = "更新安装完成，请重启应用"
                });

                return true;
            }
            catch (OperationCanceledException)
            {
                // 回滚已安装的文件
                await RollbackAsync(installedFiles, backupDir, progress);

                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Failed,
                    Message = "安装已取消，已回滚更改"
                });
                return false;
            }
            catch (Exception ex)
            {
                // 回滚已安装的文件
                await RollbackAsync(installedFiles, backupDir, progress);

                progress?.Report(new UpdateProgress
                {
                    Stage = UpdateStage.Failed,
                    Message = $"安装失败: {ex.Message}，已回滚更改"
                });
                return false;
            }
            finally
            {
                IsUpdating = false;
            }
        }

        /// <summary>
        /// 回滚更新
        /// </summary>
        private async Task RollbackAsync(List<string> installedFiles, string backupDir, IProgress<UpdateProgress> progress)
        {
            if (!Directory.Exists(backupDir)) return;

            progress?.Report(new UpdateProgress
            {
                Stage = UpdateStage.Installing,
                Message = "正在回滚更改..."
            });

            try
            {
                foreach (var file in installedFiles)
                {
                    var relativePath = file.Substring(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules").Length + 1);
                    var backupFile = Path.Combine(backupDir, relativePath);

                    if (File.Exists(backupFile))
                    {
                        File.Copy(backupFile, file, true);
                    }
                    else if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"回滚失败: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 保存安装记录
        /// </summary>
        private async Task SaveInstallRecordAsync(string packagePath, List<string> installedFiles, string backupDir)
        {
            var record = new InstallRecord
            {
                PackagePath = packagePath,
                InstallTime = DateTime.Now,
                InstalledFiles = installedFiles,
                BackupDir = backupDir
            };

            var recordPath = Path.Combine(FileHelper.AppHiddenPath, "InstallRecords");
            Directory.CreateDirectory(recordPath);

            var recordFile = Path.Combine(recordPath, $"record_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await File.WriteAllTextAsync(recordFile, JsonConvert.SerializeObject(record, Formatting.Indented));
        }

        public void CancelUpdate()
        {
            _cancellationTokenSource?.Cancel();
        }

        public string GetCurrentVersion()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            return assembly?.GetName().Version?.ToString() ?? "1.0.0";
        }

        public async Task<bool> VerifyPackageAsync(string packagePath, string expectedHash)
        {
            if (!File.Exists(packagePath))
                return false;

            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(packagePath);
                var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
                var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// 更新包清单
    /// </summary>
    public class PackageManifest
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string MinRequiredVersion { get; set; }
        public List<string> Files { get; set; }
        public string ChangeLog { get; set; }
        /// <summary>
        /// 安装目标路径（相对于应用根目录）
        /// </summary>
        public string InstallPath { get; set; }
    }

    /// <summary>
    /// 安装记录
    /// </summary>
    public class InstallRecord
    {
        public string PackagePath { get; set; }
        public DateTime InstallTime { get; set; }
        public List<string> InstalledFiles { get; set; }
        public string BackupDir { get; set; }
    }
}
