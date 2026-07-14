using Microsoft.Win32;
using Newtonsoft.Json;
using ReeYin.RootManager.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Update;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.RootManager.ViewModels
{
    public class UpdateViewModel : BindableBase
    {
        #region Fields
        private readonly IUpdateService _updateService;
        #endregion

        #region Properties
        private UpdateModel _model = new UpdateModel();
        public UpdateModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }
        #endregion

        #region Constructor
        public UpdateViewModel(IUpdateService updateService)
        {
            _updateService = updateService;
            Model.CurrentVersion = _updateService.GetCurrentVersion();
            Model.UpdateStatus = "就绪";
        }
        #endregion

        #region 在线更新方法

        private async Task CheckOnlineUpdatesAsync()
        {
            if (Model.IsChecking) return;

            Model.IsChecking = true;
            Model.CurrentStage = UpdateStage.Checking;
            Model.UpdateStatus = "正在连接服务器...";
            Model.AvailableUpdates.Clear();

            try
            {
                _updateService.ServerUrl = Model.ServerUrl;

                var condition = new UpdateQueryCondition
                {
                    CurrentVersion = Model.CurrentVersion,
                    ComponentName = Model.ComponentFilter,
                    LicenseKey = Model.LicenseKey,
                    IncludePreRelease = Model.IncludePreRelease,
                    ClientId = Environment.MachineName
                };

                var updates = await _updateService.CheckForUpdatesAsync(condition);

                foreach (var update in updates)
                    Model.AvailableUpdates.Add(update);

                Model.UpdateStatus = updates.Count > 0
                    ? $"发现 {updates.Count} 个可用更新"
                    : "当前已是最新版本";
            }
            catch (Exception ex)
            {
                Model.UpdateStatus = $"检查更新失败: {ex.Message}";
                Model.CurrentStage = UpdateStage.Failed;
            }
            finally
            {
                Model.IsChecking = false;
            }
        }

        private async Task DownloadAndInstallOnlineAsync()
        {
            if (Model.SelectedUpdate == null || Model.IsUpdating) return;

            Model.IsUpdating = true;
            Model.UpdateProgress = 0;

            var progress = new Progress<UpdateProgress>(p =>
            {
                Model.UpdateProgress = p.Percentage;
                Model.UpdateStatus = p.Message;
                Model.CurrentStage = p.Stage;
            });

            try
            {
                var packagePath = await _updateService.DownloadUpdateAsync(Model.SelectedUpdate, progress);

                Model.UpdateStatus = "正在验证更新包...";
                Model.CurrentStage = UpdateStage.Verifying;
                if (!string.IsNullOrEmpty(Model.SelectedUpdate.FileHash))
                {
                    var isValid = await _updateService.VerifyPackageAsync(packagePath, Model.SelectedUpdate.FileHash);
                    if (!isValid)
                    {
                        Model.UpdateStatus = "更新包验证失败，文件可能已损坏";
                        Model.CurrentStage = UpdateStage.Failed;
                        return;
                    }
                }

                var success = await _updateService.InstallUpdateAsync(packagePath, progress);
                if (success)
                {
                    Model.UpdateStatus = "更新安装完成，请重启应用以应用更新";
                    Model.CurrentStage = UpdateStage.Completed;
                }
            }
            catch (OperationCanceledException)
            {
                Model.UpdateStatus = "更新已取消";
                Model.CurrentStage = UpdateStage.Failed;
            }
            catch (Exception ex)
            {
                Model.UpdateStatus = $"更新失败: {ex.Message}";
                Model.CurrentStage = UpdateStage.Failed;
            }
            finally
            {
                Model.IsUpdating = false;
            }
        }

        #endregion

        #region 离线更新方法

        private void SelectOfflinePackage()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择离线更新包",
                Filter = "更新包文件 (*.zip;*.reeyin)|*.zip;*.reeyin|所有文件 (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                Model.OfflinePackagePath = dialog.FileName;
                ValidateOfflinePackage(dialog.FileName);
            }
        }

        private void ValidateOfflinePackage(string filePath)
        {
            Model.OfflinePackageInfo = new OfflinePackageInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            try
            {
                var fileInfo = new FileInfo(filePath);
                Model.OfflinePackageInfo.FileSize = fileInfo.Length;
                Model.OfflinePackageInfo.CreateTime = fileInfo.CreationTime;

                using var archive = ZipFile.OpenRead(filePath);
                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry != null)
                {
                    using var stream = manifestEntry.Open();
                    using var reader = new StreamReader(stream);
                    var manifest = JsonConvert.DeserializeObject<PackageManifest>(reader.ReadToEnd());
                    if (manifest != null)
                    {
                        Model.OfflinePackageInfo.Version = manifest.Version;
                        Model.OfflinePackageInfo.IsValid = true;
                        Model.OfflinePackageInfo.ValidationMessage = "更新包验证通过";
                        Model.UpdateStatus = $"已加载离线包: {manifest.Name} v{manifest.Version}";
                        return;
                    }
                }

                var parts = Path.GetFileNameWithoutExtension(filePath).Split('_');
                Model.OfflinePackageInfo.Version = parts.Length >= 2 ? parts[^1] : "未知";
                Model.OfflinePackageInfo.IsValid = true;
                Model.OfflinePackageInfo.ValidationMessage = "更新包格式有效（无清单文件）";
                Model.UpdateStatus = "已加载离线包（无版本信息）";
            }
            catch (InvalidDataException)
            {
                Model.OfflinePackageInfo.IsValid = false;
                Model.OfflinePackageInfo.ValidationMessage = "无效的压缩包格式";
                Model.UpdateStatus = "离线包格式无效";
            }
            catch (Exception ex)
            {
                Model.OfflinePackageInfo.IsValid = false;
                Model.OfflinePackageInfo.ValidationMessage = $"验证失败: {ex.Message}";
                Model.UpdateStatus = $"离线包验证失败: {ex.Message}";
            }
        }

        private async Task InstallOfflinePackageAsync()
        {
            if (string.IsNullOrEmpty(Model.OfflinePackagePath) || Model.IsUpdating) return;
            if (Model.OfflinePackageInfo == null || !Model.OfflinePackageInfo.IsValid)
            {
                Model.UpdateStatus = "请先选择有效的离线更新包";
                return;
            }

            Model.IsUpdating = true;
            Model.UpdateProgress = 0;

            var progress = new Progress<UpdateProgress>(p =>
            {
                Model.UpdateProgress = p.Percentage;
                Model.UpdateStatus = p.Message;
                Model.CurrentStage = p.Stage;
            });

            try
            {
                var success = await _updateService.InstallUpdateAsync(Model.OfflinePackagePath, progress);
                if (success)
                {
                    Model.UpdateStatus = "离线更新安装完成，请重启应用以应用更新";
                    Model.CurrentStage = UpdateStage.Completed;

                    var result = MessageBox.Show("更新已安装完成，是否立即重启应用？", "更新完成",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                        RestartApplication();
                }
            }
            catch (Exception ex)
            {
                Model.UpdateStatus = $"离线更新安装失败: {ex.Message}";
                Model.CurrentStage = UpdateStage.Failed;
            }
            finally
            {
                Model.IsUpdating = false;
            }
        }

        private static void RestartApplication()
        {
            var appPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(appPath)) return;

            if (appPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                appPath = appPath[..^4] + ".exe";

            System.Diagnostics.Process.Start(appPath);
            Application.Current.Shutdown();
        }

        private void ClearOfflinePackage()
        {
            Model.OfflinePackagePath = null;
            Model.OfflinePackageInfo = null;
            Model.UpdateStatus = "就绪";
        }

        #endregion

        #region 通用方法

        private void CancelUpdate()
        {
            _updateService.CancelUpdate();
            Model.UpdateStatus = "正在取消...";
        }

        private void SwitchMode(bool isOffline)
        {
            Model.IsOfflineMode = isOffline;
            Model.UpdateStatus = isOffline ? "离线模式 - 请选择更新包" : "在线模式 - 请检查更新";
            Model.AvailableUpdates.Clear();
            Model.SelectedUpdate = null;
        }

        #endregion

        #region Commands

        private DelegateCommand? _checkOnlineCommand;
        public DelegateCommand CheckOnlineCommand => _checkOnlineCommand ??= new DelegateCommand(async () => await CheckOnlineUpdatesAsync());

        private DelegateCommand? _downloadInstallCommand;
        public DelegateCommand DownloadInstallCommand => _downloadInstallCommand ??= new DelegateCommand(async () => await DownloadAndInstallOnlineAsync());

        private DelegateCommand? _selectOfflinePackageCommand;
        public DelegateCommand SelectOfflinePackageCommand => _selectOfflinePackageCommand ??= new DelegateCommand(SelectOfflinePackage);

        private DelegateCommand? _installOfflineCommand;
        public DelegateCommand InstallOfflineCommand => _installOfflineCommand ??= new DelegateCommand(async () => await InstallOfflinePackageAsync());

        private DelegateCommand? _clearOfflinePackageCommand;
        public DelegateCommand ClearOfflinePackageCommand => _clearOfflinePackageCommand ??= new DelegateCommand(ClearOfflinePackage);

        private DelegateCommand? _cancelUpdateCommand;
        public DelegateCommand CancelUpdateCommand => _cancelUpdateCommand ??= new DelegateCommand(CancelUpdate);

        private DelegateCommand? _switchToOnlineCommand;
        public DelegateCommand SwitchToOnlineCommand => _switchToOnlineCommand ??= new DelegateCommand(() => SwitchMode(false));

        private DelegateCommand? _switchToOfflineCommand;
        public DelegateCommand SwitchToOfflineCommand => _switchToOfflineCommand ??= new DelegateCommand(() => SwitchMode(true));

        #endregion
    }
}
