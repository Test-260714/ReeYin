using Prism.Commands;
using Prism.Events;
using ReeYin.ProjectManager.Models;
using ReeYin.ProjectManager.Services;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.ProjectManager.ViewModels
{
    public class SolutionManagerViewModel : DialogViewModelBase
    {
        #region Fields
        private readonly SolutionManagerService _service;
        private readonly IConfigManager _configManager;
        #endregion

        #region Properties

        /// <summary>
        /// 最近使用的解决方案列表（从Core的SolutionManager获取）
        /// </summary>
        public ObservableCollection<ProjectItemBaseInfo> RecentSolutions =>
            new ObservableCollection<ProjectItemBaseInfo>(PrismProvider.ProjectManager.SolutionManager.ProjectsBaseInfo);

        private ProjectItemBaseInfo? _selectedSolution;
        /// <summary>
        /// 选中的解决方案
        /// </summary>
        public ProjectItemBaseInfo? SelectedSolution
        {
            get => _selectedSolution;
            set
            {
                SetProperty(ref _selectedSolution, value);
                RaisePropertyChanged(nameof(HasSelectedSolution));
                RaisePropertyChanged(nameof(CurrentName));
                RaisePropertyChanged(nameof(CurrentDescription));
                RaisePropertyChanged(nameof(CurrentPath));
                RaisePropertyChanged(nameof(CurrentGuid));
                RaisePropertyChanged(nameof(CurrentModifiedAt));
                RaisePropertyChanged(nameof(IsSelectedLoaded));
                IsEditingName = false;
            }
        }

        private bool _isCreatingNew;
        /// <summary>
        /// 是否正在创建新解决方案
        /// </summary>
        public bool IsCreatingNew
        {
            get => _isCreatingNew;
            set
            {
                SetProperty(ref _isCreatingNew, value);
                RaisePropertyChanged(nameof(DetailsVisibility));
                RaisePropertyChanged(nameof(NewFormVisibility));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // 新建表单字段
        private string _newName = string.Empty;
        public string NewName
        {
            get => _newName;
            set => SetProperty(ref _newName, value);
        }

        private string _newDescription = string.Empty;
        public string NewDescription
        {
            get => _newDescription;
            set => SetProperty(ref _newDescription, value);
        }

        private string _newFilePath = string.Empty;
        public string NewFilePath
        {
            get => _newFilePath;
            set => SetProperty(ref _newFilePath, value);
        }

        private bool _isEditingName;
        public bool IsEditingName
        {
            get => _isEditingName;
            set
            {
                SetProperty(ref _isEditingName, value);
                RaisePropertyChanged(nameof(NameViewVisibility));
                RaisePropertyChanged(nameof(NameEditVisibility));
            }
        }

        private string _editingName = string.Empty;
        public string EditingName
        {
            get => _editingName;
            set => SetProperty(ref _editingName, value);
        }

        // UI绑定属性
        public bool HasSelectedSolution => SelectedSolution != null;
        public Visibility DetailsVisibility => IsCreatingNew ? Visibility.Collapsed : Visibility.Visible;
        public Visibility NewFormVisibility => IsCreatingNew ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NameViewVisibility => IsEditingName ? Visibility.Collapsed : Visibility.Visible;
        public Visibility NameEditVisibility => IsEditingName ? Visibility.Visible : Visibility.Collapsed;

        public string CurrentName => SelectedSolution?.Name ?? PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.Name ?? "无";
        public string CurrentDescription => SelectedSolution?.Description ?? PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.Description ?? "暂无描述";
        public string CurrentPath => SelectedSolution?.FilePath ?? PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.FilePath ?? "未设置";
        public string CurrentGuid => (SelectedSolution?.Guid ?? PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.Guid ?? Guid.Empty).ToString();
        public DateTime? CurrentModifiedAt => SelectedSolution?.ModifyTime ?? PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.ModifyTime;

        /// <summary>当前已加载的解决方案名称（始终来自 DefaultBaseInfo）</summary>
        public string LoadedSolutionName => PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.Name ?? "无";

        /// <summary>选中的解决方案是否就是当前已加载的那个</summary>
        public bool IsSelectedLoaded =>
            SelectedSolution != null &&
            PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.Guid == SelectedSolution.Guid;

        #endregion

        #region Constructor
        public SolutionManagerViewModel(IConfigManager configManager, IEventAggregator eventAggregator)
        {
            _configManager = configManager;
            _service = new SolutionManagerService(eventAggregator);

            Title = "解决方案管理器";
        }
        #endregion

        #region Commands

        public DelegateCommand LoadCommand => new DelegateCommand(async () =>
        {
            await LoadDataAsync();
        });

        public DelegateCommand StartEditNameCommand => new DelegateCommand(() =>
        {
            EditingName = CurrentName;
            IsEditingName = true;
        }, () => HasSelectedSolution).ObservesProperty(() => HasSelectedSolution);

        public DelegateCommand SaveNameCommand => new DelegateCommand(() =>
        {
            if (string.IsNullOrWhiteSpace(EditingName) || SelectedSolution == null) return;

            SelectedSolution.Name = EditingName;

            // 如果选中的就是当前加载的方案，同步更新 DefaultBaseInfo
            var defaultInfo = PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo;
            if (defaultInfo?.Guid == SelectedSolution.Guid)
                defaultInfo.Name = EditingName;

            PrismProvider.ProjectManager.SolutionManager.AddOrUpdateRecent(SelectedSolution);
            _configManager.Write(ConfigKey.ProjectConfig, PrismProvider.ProjectManager.SolutionManager);

            IsEditingName = false;
            RaisePropertyChanged(nameof(CurrentName));
            RaisePropertyChanged(nameof(LoadedSolutionName));
            RaisePropertyChanged(nameof(RecentSolutions));
            StatusMessage = "名称已更新";
        });

        public DelegateCommand CancelEditNameCommand => new DelegateCommand(() =>
        {
            IsEditingName = false;
        });

        public DelegateCommand NewCommand => new DelegateCommand(() =>
        {
            IsCreatingNew = true;
            NewName = $"解决方案_{DateTime.Now:yyyyMMdd_HHmmss}";
            NewDescription = "";
            NewFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ReeYin解决方案", $"{NewName}.rysl");
        });

        public DelegateCommand OpenCommand => new DelegateCommand(async () =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ReeYin解决方案文件 (*.rysl)|*.rysl|所有文件 (*.*)|*.*",
                Title = "打开解决方案"
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.FileName))
            {
                await OpenSolutionFileAsync(dialog.FileName);
            }
        });

        public DelegateCommand OpenSelectedCommand => new DelegateCommand(async () =>
        {
            if (SelectedSolution == null)
            {
                HandyControl.Controls.MessageBox.Show("请先选择一个解决方案", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await OpenSolutionAsync(SelectedSolution);
        }, () => HasSelectedSolution).ObservesProperty(() => HasSelectedSolution);

        public DelegateCommand SaveCommand => new DelegateCommand(async () =>
        {
            var currentSolution = PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo;
            if (currentSolution == null || string.IsNullOrEmpty(currentSolution.FilePath))
            {
                HandyControl.Controls.MessageBox.Show("当前没有打开的解决方案", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await SaveCurrentSolutionAsync();
        });

        public DelegateCommand SaveAsCommand => new DelegateCommand(() =>
        {
            var currentSolution = PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo;
            if (currentSolution == null)
            {
                HandyControl.Controls.MessageBox.Show("当前没有打开的解决方案", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsCreatingNew = true;
            NewName = currentSolution.Name + "_副本";
            NewDescription = currentSolution.Description ?? string.Empty;
            var directory = Path.GetDirectoryName(currentSolution.FilePath);
            NewFilePath = Path.Combine(
                directory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"{NewName}.rysl");
        });

        public DelegateCommand ImportOldCommand => new DelegateCommand(() =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "旧项目文件 (*.ryv)|*.ryv|所有文件 (*.*)|*.*",
                Title = "导入旧工程"
            };

            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName))
                return;

            var result = HandyControl.Controls.MessageBox.Show(
                "导入旧工程会基于旧工程内容创建一个新的解决方案，并释放当前已加载的方案，确定继续吗？",
                "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            _ = ImportLegacySolutionAsync(dialog.FileName);
        });

        public DelegateCommand DeleteCommand => new DelegateCommand(() =>
        {
            _ = DeleteSelectedSolutionAsync();
            return;

            if (SelectedSolution == null)
            {
                HandyControl.Controls.MessageBox.Show("请先选择要删除的解决方案", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = HandyControl.Controls.MessageBox.Show(
                $"确定要删除解决方案 '{SelectedSolution.Name}' 吗？\n\n注意：这只会从列表中移除，不会删除文件。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                PrismProvider.ProjectManager.SolutionManager.RemoveProject(SelectedSolution);
                _configManager.Write(ConfigKey.ProjectConfig, PrismProvider.ProjectManager.SolutionManager);
                SelectedSolution = null;
                StatusMessage = "已从列表中移除";
                RaisePropertyChanged(nameof(RecentSolutions));
            }
        }, () => HasSelectedSolution).ObservesProperty(() => HasSelectedSolution);

        public DelegateCommand BrowseNewPathCommand => new DelegateCommand(() =>
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ReeYin解决方案文件 (*.rysl)|*.rysl",
                Title = "选择保存位置",
                FileName = NewName
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.FileName))
            {
                NewFilePath = dialog.FileName;
            }
        });

        public DelegateCommand ConfirmNewCommand => new DelegateCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                HandyControl.Controls.MessageBox.Show("请输入解决方案名称", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewFilePath))
            {
                HandyControl.Controls.MessageBox.Show("请选择保存路径", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await CreateNewSolutionAsync();
        });

        public DelegateCommand CancelNewCommand => new DelegateCommand(() =>
        {
            IsCreatingNew = false;
            NewName = "";
            NewDescription = "";
            NewFilePath = "";
        });

        public DelegateCommand CloseCommand => new DelegateCommand(() =>
        {
            if (HasUnsavedChanges)
            {
                var result = HandyControl.Controls.MessageBox.Show(
                    "有未保存的更改，确定要关闭吗？",
                    "确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            CloseDialog(ButtonResult.OK);
        });

        #endregion

        #region Methods

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载...";

                await Task.Run(() =>
                {
                    // 从Core的ProjectManager加载
                    var solutionManager = PrismProvider.ProjectManager.SolutionManager;
                    if (solutionManager != null)
                    {
                        RaisePropertyChanged(nameof(RecentSolutions));
                    }
                });

                StatusMessage = $"已加载 {RecentSolutions.Count} 个解决方案";
            }
            catch (Exception ex)
            {
                Logs.LogError($"加载数据失败: {ex.Message}");
                StatusMessage = "加载失败";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CreateNewSolutionAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在创建解决方案...";

                // 创建SolutionInfo用于文件操作
                var solutionInfo = new SolutionInfo
                {
                    Guid = Guid.NewGuid(),
                    Name = NewName,
                    Description = NewDescription,
                    FilePath = NewFilePath,
                    LastModified = DateTime.Now
                };

                var (success, message, solution) = await _service.CreateSolutionAsync(
                    NewName, NewDescription, NewFilePath);

                if (success && solution != null)
                {
                    // 转换为ProjectItemBaseInfo并添加到Core的SolutionManager
                    var baseInfo = new ProjectItemBaseInfo
                    {
                        Guid = solution.Guid,
                        Name = solution.Name,
                        Description = solution.Description,
                        FilePath = solution.FilePath,
                        ModifyTime = solution.LastModified,
                        IsUsing = true
                    };

                    PrismProvider.ProjectManager.SolutionManager.AddOrUpdateRecent(baseInfo);
                    PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo = baseInfo;
                    _configManager.Write(ConfigKey.ProjectConfig, PrismProvider.ProjectManager.SolutionManager);

                    IsCreatingNew = false;
                    SelectedSolution = baseInfo;
                    HasUnsavedChanges = false;

                    HandyControl.Controls.MessageBox.Show("创建成功！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusMessage = "创建成功";
                    RaisePropertyChanged(nameof(RecentSolutions));
                    RaisePropertyChanged(nameof(CurrentModifiedAt));
                }
                else
                {
                    HandyControl.Controls.MessageBox.Show(message, "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = message;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"创建解决方案失败: {ex.Message}");
                HandyControl.Controls.MessageBox.Show($"创建失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "创建失败";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OpenSolutionAsync(ProjectItemBaseInfo baseInfo)
        {
            try
            {
                var result = HandyControl.Controls.MessageBox.Show(
                    "打开新解决方案将释放当前解决方案的所有资源，确定继续吗？",
                    "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                IsLoading = true;
                StatusMessage = "正在打开解决方案...";

                // 转换为SolutionInfo用于文件操作
                var solutionInfo = new SolutionInfo
                {
                    Guid = baseInfo.Guid,
                    Name = baseInfo.Name,
                    Description = baseInfo.Description,
                    FilePath = baseInfo.FilePath,
                    LastModified = baseInfo.ModifyTime
                };

                var (success, message) = await _service.OpenSolutionAsync(solutionInfo);

                if (success)
                {
                    // 更新Core的SolutionManager
                    baseInfo.ModifyTime = DateTime.Now;
                    baseInfo.IsUsing = true;
                    PrismProvider.ProjectManager.SolutionManager.AddOrUpdateRecent(baseInfo);
                    PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo = baseInfo;
                    _configManager.Write(ConfigKey.ProjectConfig, PrismProvider.ProjectManager.SolutionManager);

                    // 更新当前标记
                    foreach (var s in RecentSolutions)
                        s.IsUsing = false;
                    baseInfo.IsUsing = true;

                    HasUnsavedChanges = false;
                    StatusMessage = "打开成功";

                    HandyControl.Controls.MessageBox.Show("解决方案已打开！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    RaisePropertyChanged(nameof(RecentSolutions));
                    RaisePropertyChanged(nameof(CurrentModifiedAt));
                    CloseDialog(ButtonResult.OK);
                }
                else
                {
                    HandyControl.Controls.MessageBox.Show(message, "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = message;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"打开解决方案失败: {ex.Message}");
                StatusMessage = "打开失败";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OpenSolutionFileAsync(string filePath)
        {
            try
            {
                if (!_service.ValidateSolutionFile(filePath))
                {
                    HandyControl.Controls.MessageBox.Show("无效的解决方案文件", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 检查是否已在列表中
                var existing = RecentSolutions.FirstOrDefault(s =>
                    s.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    await OpenSolutionAsync(existing);
                }
                else
                {
                    // 创建新的项目基础信息
                    var baseInfo = new ProjectItemBaseInfo
                    {
                        Guid = Guid.NewGuid(),
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        FilePath = filePath,
                        ModifyTime = File.GetLastWriteTime(filePath)
                    };

                    await OpenSolutionAsync(baseInfo);
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"打开文件失败: {ex.Message}");
                HandyControl.Controls.MessageBox.Show($"打开失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveCurrentSolutionAsync()
        {
            try
            {
                var currentBaseInfo = PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo;
                if (currentBaseInfo == null || string.IsNullOrEmpty(currentBaseInfo.FilePath))
                {
                    StatusMessage = "没有当前解决方案";
                    return;
                }

                IsLoading = true;
                StatusMessage = "正在保存...";

                // 转换为SolutionInfo用于文件操作
                var solutionInfo = new SolutionInfo
                {
                    Guid = currentBaseInfo.Guid,
                    Name = currentBaseInfo.Name,
                    Description = currentBaseInfo.Description,
                    FilePath = currentBaseInfo.FilePath,
                    LastModified = currentBaseInfo.ModifyTime
                };

                var (success, message) = await _service.SaveCurrentSolutionAsync(solutionInfo);

                if (success)
                {
                    currentBaseInfo.ModifyTime = DateTime.Now;
                    PrismProvider.ProjectManager.SolutionManager.AddOrUpdateRecent(currentBaseInfo);
                    _configManager.Write(ConfigKey.ProjectConfig, PrismProvider.ProjectManager.SolutionManager);
                    HasUnsavedChanges = false;

                    HandyControl.Controls.MessageBox.Show("保存成功！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusMessage = "保存成功";
                    RaisePropertyChanged(nameof(RecentSolutions));
                    RaisePropertyChanged(nameof(CurrentModifiedAt));
                }
                else
                {
                    HandyControl.Controls.MessageBox.Show(message, "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = message;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"保存失败: {ex.Message}");
                StatusMessage = "保存失败";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ImportLegacySolutionAsync(string legacyProjectFilePath)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在导入旧工程...";

                var (success, message, solution) = await _service.ImportLegacySolutionAsync(legacyProjectFilePath);

                if (success && solution != null)
                {
                    foreach (var item in PrismProvider.ProjectManager.SolutionManager.ProjectsBaseInfo)
                        item.IsUsing = false;

                    var baseInfo = new ProjectItemBaseInfo
                    {
                        Guid = solution.Guid,
                        Name = solution.Name,
                        Description = solution.Description,
                        FilePath = solution.FilePath,
                        ModifyTime = solution.LastModified,
                        IsUsing = true
                    };

                    PrismProvider.ProjectManager.SolutionManager.AddOrUpdateRecent(baseInfo);
                    PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo = baseInfo;
                    _configManager.Write(ConfigKey.ProjectConfig, PrismProvider.ProjectManager.SolutionManager);

                    SelectedSolution = baseInfo;
                    IsCreatingNew = false;
                    HasUnsavedChanges = false;
                    StatusMessage = "导入成功";

                    RaisePropertyChanged(nameof(LoadedSolutionName));
                    RaisePropertyChanged(nameof(RecentSolutions));
                    RaisePropertyChanged(nameof(CurrentModifiedAt));

                    HandyControl.Controls.MessageBox.Show(
                        $"已基于旧工程创建新工程：{solution.Name}",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    CloseDialog(ButtonResult.OK);
                }
                else
                {
                    HandyControl.Controls.MessageBox.Show(message, "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = message;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"导入旧工程失败: {ex.Message}");
                HandyControl.Controls.MessageBox.Show($"导入失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "导入失败";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteSelectedSolutionAsync()
        {
            if (SelectedSolution == null)
            {
                HandyControl.Controls.MessageBox.Show("请先选择要删除的解决方案", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var targetSolution = SelectedSolution;
            var isCurrentLoaded = PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.Guid == targetSolution.Guid;
            var confirmMessage = isCurrentLoaded
                ? $"当前解决方案“{targetSolution.Name}”正在使用中。继续后将清空当前已加载内容，恢复为空白新建工程状态，并从列表中移除该项，同时删除磁盘上的解决方案文件。是否继续？"
                : $"确定删除解决方案“{targetSolution.Name}”吗？该操作会从列表中移除该项，并删除磁盘上的解决方案文件。";
            var result = HandyControl.Controls.MessageBox.Show(
                confirmMessage,
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = isCurrentLoaded ? "正在清空当前工程..." : "正在删除解决方案...";

                if (isCurrentLoaded)
                {
                    var (success, message) = await _service.ResetCurrentSolutionAsync();
                    if (!success)
                    {
                        HandyControl.Controls.MessageBox.Show(message, "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusMessage = message;
                        return;
                    }
                }

                DeleteSolutionFile(targetSolution.FilePath);
                PrismProvider.ProjectManager.SolutionManager.RemoveProject(targetSolution);
                _configManager.Write(ConfigKey.ProjectConfig, PrismProvider.ProjectManager.SolutionManager);

                SelectedSolution = null;
                HasUnsavedChanges = false;
                StatusMessage = isCurrentLoaded ? "当前工程已删除并清空" : "解决方案已删除";

                RaisePropertyChanged(nameof(CurrentName));
                RaisePropertyChanged(nameof(CurrentDescription));
                RaisePropertyChanged(nameof(CurrentPath));
                RaisePropertyChanged(nameof(CurrentGuid));
                RaisePropertyChanged(nameof(CurrentModifiedAt));
                RaisePropertyChanged(nameof(LoadedSolutionName));
                RaisePropertyChanged(nameof(IsSelectedLoaded));
                RaisePropertyChanged(nameof(RecentSolutions));
            }
            catch (Exception ex)
            {
                Logs.LogError($"删除解决方案失败: {ex.Message}");
                HandyControl.Controls.MessageBox.Show($"删除失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "删除失败";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static void DeleteSolutionFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            File.Delete(filePath);
        }

        #endregion
    }
}
