using HalconDotNet;
using ImageTool.Halcon;
using NetTaste;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project.Models;
using ReeYin_V.Logger;
using ReeYin_V.Share.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;

namespace ReeYin_V.Core.Services.Project
{
    [ExposedService(Lifetime.Singleton, 7, typeof(IProjectManager))]
    [Serializable]
    public partial class ProjectManager : BindableBase, IProjectManager
    {
        #region Fields
        public IConfigManager ConfigManager = null;
        /// <summary>
        /// 是否打开了解决方案
        /// </summary>
        public bool IsOpenSolution = false;

        /// <summary>
        /// 当前是否正在从文件加载解决方案。
        /// 仅用于抑制加载阶段的副作用写回。
        /// </summary>
        [JsonIgnore]
        public bool IsLoadingProject { get; internal set; }

        private const string ProjectRuntimeStateFolderName = "ProjectRuntime";
        private const string RunningMarkerFileName = "ReeYin_V.running";
        private const string ProjectBackupExtension = ".bak";

        /// <summary>
        /// 上一次程序是否没有经过正常退出流程。
        /// </summary>
        [JsonIgnore]
        public bool WasLastSessionAbnormal { get; private set; }

        /// <summary>
        /// 存储路径
        /// </summary>
        public string FilePath = System.IO.Path.Combine(PrismProvider.AppBasePath, @"Solutions\");

        private NodifySolutionItem _sltCurSolutionItem = new NodifySolutionItem();
        private NodifySolutionRuntimeData _sltCurSolutionRuntimeData = new NodifySolutionRuntimeData();

        /// <summary>
        /// 当前选中的解决方案。
        /// 兼容项目内历史访问入口，真正承载数据的是 DefaultProject.CurSolutionItem。
        /// </summary>
        [JsonIgnore]
        public NodifySolutionItem SltCurSolutionItem
        {
            get
            {
                var currentProject = EnsureCurrentProjectContainer();
                _sltCurSolutionItem = currentProject.CurSolutionItem;
                return _sltCurSolutionItem;
            }
            set
            {
                _sltCurSolutionItem = value ?? new NodifySolutionItem();
                EnsureCurrentProjectContainer().CurSolutionItem = _sltCurSolutionItem;
                RaisePropertyChanged(nameof(SltCurSolutionItem));
            }
        }

        /// <summary>
        /// 当前解决方案的运行时临时数据。
        /// 不参与序列化，且不应随着反序列化替换。
        /// </summary>
        [JsonIgnore]
        public NodifySolutionRuntimeData SltCurSolutionRuntimeData
        {
            get { return _sltCurSolutionRuntimeData ??= new NodifySolutionRuntimeData(); }
            private set
            {
                _sltCurSolutionRuntimeData = value ?? new NodifySolutionRuntimeData();
                RaisePropertyChanged(nameof(SltCurSolutionRuntimeData));
            }
        }

        /// <summary>
        /// 解决方案管理器
        /// </summary>
        public NodifySolutionManagerModel NodifySolutionManager { get; set; }

        /// <summary>
        /// 解决方案管理
        /// </summary>
        public SolutionManagerModel SolutionManager { get; set; }

        #endregion

        #region Properties

        #endregion

        #region Constructor
        public ProjectManager(IConfigManager configManager)
        {
            ConfigManager = configManager;
            MarkApplicationStarted();

            // 加载解决方案管理配置
            SolutionManager = configManager.Read<SolutionManagerModel>(ConfigKey.ProjectConfig) ?? new SolutionManagerModel();

            // 加载 Nodify 解决方案配置
            NodifySolutionManager = configManager.Read<NodifySolutionManagerModel>(ConfigKey.SolutionConfig) ?? new NodifySolutionManagerModel();

            EnsureCurrentProjectContainer();
            _sltCurSolutionItem = SolutionManager.DefaultProject.CurSolutionItem;

        }
        #endregion

        #region Methods

        public void MarkApplicationStarted()
        {
            try
            {
                string markerFilePath = GetRunningMarkerFilePath();
                WasLastSessionAbnormal = File.Exists(markerFilePath);

                string markerDirectory = Path.GetDirectoryName(markerFilePath);
                if (!string.IsNullOrWhiteSpace(markerDirectory))
                {
                    Directory.CreateDirectory(markerDirectory);
                }

                File.WriteAllText(
                    markerFilePath,
                    $"StartedAt={DateTime.Now:O}{Environment.NewLine}ProcessId={Environment.ProcessId}");

                if (WasLastSessionAbnormal)
                {
                    Logs.LogInfo("检测到上次程序未正常退出。");
                }
            }
            catch (Exception ex)
            {
                WasLastSessionAbnormal = false;
                Logs.LogError($"写入程序运行标记失败：{ex.Message}");
            }
        }

        public void MarkApplicationClosed()
        {
            try
            {
                string markerFilePath = GetRunningMarkerFilePath();
                if (File.Exists(markerFilePath))
                {
                    File.Delete(markerFilePath);
                }

                WasLastSessionAbnormal = false;
            }
            catch (Exception ex)
            {
                Logs.LogError($"清理程序运行标记失败：{ex.Message}");
            }
        }

        public void AcknowledgeLastSessionAbnormal()
        {
            WasLastSessionAbnormal = false;
        }

        public static string GetProjectBackupFilePath(string projectFilePath)
        {
            return string.IsNullOrWhiteSpace(projectFilePath)
                ? string.Empty
                : projectFilePath + ProjectBackupExtension;
        }

        public static bool HasProjectBackup(string projectFilePath)
        {
            try
            {
                string backupFilePath = GetProjectBackupFilePath(projectFilePath);
                return !string.IsNullOrWhiteSpace(backupFilePath) &&
                       File.Exists(backupFilePath) &&
                       new FileInfo(backupFilePath).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static string GetRunningMarkerFilePath()
        {
            string appBasePath = PrismProvider.AppBasePath;
            if (string.IsNullOrWhiteSpace(appBasePath))
            {
                appBasePath = AppContext.BaseDirectory;
            }

            return Path.Combine(appBasePath, "Config", ProjectRuntimeStateFolderName, RunningMarkerFileName);
        }

        private ProjectItem EnsureCurrentProjectContainer()
        {
            SolutionManager ??= new SolutionManagerModel();
            SolutionManager.DefaultProject ??= new ProjectItem();
            SolutionManager.DefaultProject.CurSolutionItem ??= _sltCurSolutionItem ?? new NodifySolutionItem();
            return SolutionManager.DefaultProject;
        }

        public NodifySolutionRuntimeData CreateSolutionRuntimeData()
        {
            return new NodifySolutionRuntimeData();
        }

        public void SetCurrentSolutionRuntimeData(NodifySolutionRuntimeData runtimeData)
        {
            SltCurSolutionRuntimeData = runtimeData;
        }

        public void ResetCurrentSolutionRuntimeData()
        {
            SltCurSolutionRuntimeData = new NodifySolutionRuntimeData();
        }

        /// <summary>
        /// 无项目配置时在内存中初始化一个默认项目，不写入磁盘；
        /// 实际保存由用户手动触发（新建/保存操作）。
        /// </summary>
        public void EnsureDefaultProject(bool resetBaseInfo = false)
        {
            SolutionManager ??= new SolutionManagerModel();

            if (resetBaseInfo || string.IsNullOrWhiteSpace(SolutionManager.DefaultBaseInfo?.FilePath))
            {
                SolutionManager.DefaultBaseInfo = new ProjectItemBaseInfo
                {
                    Guid = Guid.NewGuid(),
                    Name = "Default",
                    Description = "默认创建的解决方案",
                    FilePath = Path.Combine(FilePath, "Default", "Default.rysl"),
                    ModifyTime = DateTime.Now,
                    IsUsing = true
                };
            }

            var defaultBaseInfo = SolutionManager.DefaultBaseInfo;
            defaultBaseInfo.IsUsing = true;

            var defaultSolutionItem = new NodifySolutionItem
            {
                Guid = defaultBaseInfo.Guid,
                Name = defaultBaseInfo.Name,
                Description = defaultBaseInfo.Description,
                FilePath = Path.GetDirectoryName(defaultBaseInfo.FilePath) ?? string.Empty,
                ModifyTime = defaultBaseInfo.ModifyTime,
                IsUsing = true
            };

            SolutionManager.DefaultProject = new ProjectItem
            {
                BaseInfo = defaultBaseInfo,
                CurSolutionItem = defaultSolutionItem
            };

            ResetCurrentSolutionRuntimeData();
            SltCurSolutionItem = defaultSolutionItem;
            IsOpenSolution = false;

            Logs.LogInfo("未检测到有效方案，已在内存中初始化默认项目，等待用户保存。");
        }

        #endregion
    }

    /// <summary>
    /// 解决方案管理
    /// </summary>
    public class SolutionManagerModel : BindableBase
    {
        #region Fields
        private readonly object _lock = new();
        private readonly object _saveLock = new();

        /// <summary>
        /// 解决方案基本信息
        /// </summary>
        public List<ProjectItemBaseInfo> ProjectsBaseInfo { get; set; }

        /// <summary>
        /// 默认加载条目
        /// </summary>
        public ProjectItemBaseInfo DefaultBaseInfo { get; set; }

        private ProjectItem _defaultProject = new ProjectItem();

        /// <summary>
        /// 默认解决方案（配置直接存储在此，无需独立快照文件）
        /// </summary>
        [JsonIgnore]
        public ProjectItem DefaultProject
        {
            get { return _defaultProject; }
            set
            {
                _defaultProject = value ?? new ProjectItem();
                _defaultProject.CurSolutionItem ??= new NodifySolutionItem();
                _defaultProject.BaseInfo ??= new ProjectItemBaseInfo();
                _defaultProject.OtherConfig ??= new Dictionary<string, object>();
            }
        }
        #endregion

        #region Constructor
        public SolutionManagerModel()
        {
            DefaultProject = new ProjectItem();
            DefaultBaseInfo ??= new ProjectItemBaseInfo();
            ProjectsBaseInfo ??= [];
        }
        #endregion

        #region Methods

        /// <summary>
        /// 从文件加载 ProjectItem
        /// </summary>
        public bool LoadProject(string filePath)
        {
            return LoadProjectCore(filePath, filePath);
        }

        /// <summary>
        /// 启动阶段加载项目；如果上次程序异常退出且存在备份，则提示是否加载备份配置。
        /// </summary>
        public bool LoadProjectWithRecovery(string filePath)
        {
            var projectManager = PrismProvider.ProjectManager;
            bool shouldCheckBackup = projectManager?.WasLastSessionAbnormal == true;
            projectManager?.AcknowledgeLastSessionAbnormal();

            if (shouldCheckBackup && ProjectManager.HasProjectBackup(filePath))
            {
                string backupFilePath = ProjectManager.GetProjectBackupFilePath(filePath);
                var result = ShowAbnormalShutdownRecoveryPrompt(filePath, backupFilePath);

                if (result == MessageBoxResult.Yes)
                {
                    bool loadedBackup = LoadProjectCore(backupFilePath, filePath);
                    if (loadedBackup)
                    {
                        Logs.LogInfo($"已加载异常退出前的项目备份配置: {backupFilePath}");
                        return true;
                    }

                    ShowMessageBox(
                        "备份配置文件加载失败，将继续加载原项目文件。",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            else if (shouldCheckBackup)
            {
                Logs.LogInfo("检测到上次程序未正常退出，但当前项目没有可用备份配置文件。");
            }

            return LoadProject(filePath);
        }

        private bool LoadProjectCore(string contentFilePath, string projectFilePath)
        {
            NodifySolutionItem previousSolutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            NodifySolutionRuntimeData previousRuntimeData = PrismProvider.ProjectManager?.SltCurSolutionRuntimeData;
            var projectManager = PrismProvider.ProjectManager;

            try
            {
                if (projectManager == null)
                {
                    Logs.LogError("加载项目失败：ProjectManager 未初始化。");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(contentFilePath) || !File.Exists(contentFilePath))
                {
                    RecoverMissingDefaultProject(projectFilePath);
                    return false;
                }

                projectManager.IsLoadingProject = true;

                // 为反序列化阶段准备一个新的运行态实例，保证 OnDeserialized
                // 写入的缓存落到稳定对象上，并与上一份项目的运行时数据隔离。
                var runtimeSolutionItem = new NodifySolutionItem();
                var runtimeData = projectManager.CreateSolutionRuntimeData();
                projectManager.SetCurrentSolutionRuntimeData(runtimeData);
                projectManager.SltCurSolutionItem = runtimeSolutionItem;

                DefaultProject = JsonHelper.JsonDisObjectSerialize<ProjectItem>(
                    contentFilePath, out _, TypeNameHandling.Auto) ?? new ProjectItem();

                if (DefaultProject.BaseInfo != null)
                {
                    DefaultBaseInfo = DefaultProject.BaseInfo;
                }

                MergePersistentSolutionItem(runtimeSolutionItem, DefaultProject.CurSolutionItem);
                DefaultProject.CurSolutionItem = runtimeSolutionItem;
                NormalizeLoadedProjectPaths(projectFilePath);

                return true;
            }
            catch (Exception ex)
            {
                if (PrismProvider.ProjectManager != null)
                {
                    // 加载失败时恢复原运行时上下文，避免把临时态切到半成品对象上。
                    PrismProvider.ProjectManager.SltCurSolutionItem = previousSolutionItem;
                    PrismProvider.ProjectManager.SetCurrentSolutionRuntimeData(previousRuntimeData);
                }

                Logs.LogError($"加载项目失败：{ex.Message}");
                return false;
            }
            finally
            {
                if (projectManager != null)
                {
                    projectManager.IsLoadingProject = false;
                }
            }
        }

        private static MessageBoxResult ShowAbnormalShutdownRecoveryPrompt(string projectFilePath, string backupFilePath)
        {
            string message = "检测到程序上次非正常关闭，是否加载备份配置文件？";
            if (!string.IsNullOrWhiteSpace(projectFilePath) || !string.IsNullOrWhiteSpace(backupFilePath))
            {
                message += $"{Environment.NewLine}{Environment.NewLine}项目文件：{projectFilePath}{Environment.NewLine}备份文件：{backupFilePath}";
            }

            return ShowMessageBox(message, "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        private static MessageBoxResult ShowMessageBox(
            string message,
            string caption,
            MessageBoxButton button,
            MessageBoxImage image)
        {
            if (PrismProvider.Dispatcher != null && !PrismProvider.Dispatcher.CheckAccess())
            {
                return PrismProvider.Dispatcher.Invoke(() =>
                    MessageBox.Show(message, caption, button, image));
            }

            return MessageBox.Show(message, caption, button, image);
        }

        private static void RecoverMissingDefaultProject(string filePath)
        {
            var projectManager = PrismProvider.ProjectManager;
            var defaultBaseInfo = projectManager?.SolutionManager?.DefaultBaseInfo;

            if (projectManager == null ||
                defaultBaseInfo == null ||
                string.IsNullOrWhiteSpace(filePath) ||
                string.IsNullOrWhiteSpace(defaultBaseInfo.FilePath) ||
                !PathsEqual(filePath, defaultBaseInfo.FilePath))
            {
                return;
            }

            var solutionName = string.IsNullOrWhiteSpace(defaultBaseInfo.Name)
                ? Path.GetFileNameWithoutExtension(defaultBaseInfo.FilePath)
                : defaultBaseInfo.Name;
            var message = string.IsNullOrWhiteSpace(solutionName)
                ? "之前的方案不存在，已创建一个新的默认方案。"
                : $"之前的方案“{solutionName}”不存在，已创建一个新的默认方案。";

            if (PrismProvider.Dispatcher != null)
            {
                PrismProvider.Dispatcher.Invoke(() =>
                    MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning));
            }
            else
            {
                MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            projectManager.EnsureDefaultProject(resetBaseInfo: true);
            projectManager.ConfigManager?.Write(ConfigKey.ProjectConfig, projectManager.SolutionManager);
            Logs.LogInfo($"默认方案文件不存在，已回退到新的默认方案: {filePath}");
        }

        private static bool PathsEqual(string left, string right)
        {
            try
            {
                return string.Equals(
                    Path.GetFullPath(left),
                    Path.GetFullPath(right),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void NormalizeLoadedProjectPaths(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            string solutionFilePath;
            try
            {
                solutionFilePath = Path.GetFullPath(filePath);
            }
            catch
            {
                solutionFilePath = filePath;
            }

            DefaultProject ??= new ProjectItem();
            DefaultProject.BaseInfo ??= new ProjectItemBaseInfo();
            DefaultProject.CurSolutionItem ??= new NodifySolutionItem();

            DefaultProject.BaseInfo.FilePath = solutionFilePath;
            DefaultBaseInfo = DefaultProject.BaseInfo;

            DefaultProject.CurSolutionItem.FilePath = Path.GetDirectoryName(solutionFilePath) ?? string.Empty;
        }

        /// <summary>
        /// 将 ProjectItem 保存到文件
        /// </summary>
        public bool SaveProject(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            lock (_saveLock)
            {
                try
                {
                    ProjectItem projectSnapshot;
                    lock (_lock)
                    {
                        projectSnapshot = CreateSaveSnapshot();
                    }

                    if (!TryBackupProjectFile(filePath))
                    {
                        return false;
                    }

                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    // UpdateItem 会异步触发保存；写文件用独立锁串行化，避免阻塞 GetItem/UpdateItem。
                    JsonHelper.JsonObjectSerialize(projectSnapshot, filePath, TypeNameHandling.Auto);
                    return true;
                }
                catch (Exception ex)
                {
                    Logs.LogError($"保存项目失败：{ex.Message}");
                    return false;
                }
            }
        }

        private ProjectItem CreateSaveSnapshot()
        {
            DefaultProject ??= new ProjectItem();
            DefaultProject.OtherConfig ??= new Dictionary<string, object>();
            DefaultBaseInfo ??= DefaultProject.BaseInfo ?? new ProjectItemBaseInfo();

            DefaultProject.BaseInfo = DefaultBaseInfo;
            DefaultProject.CurSolutionItem =
                PrismProvider.ProjectManager?.SltCurSolutionItem ??
                DefaultProject.CurSolutionItem ??
                new NodifySolutionItem();

            return new ProjectItem
            {
                BaseInfo = CloneBaseInfo(DefaultBaseInfo),
                CurSolutionItem = CloneSolutionItem(DefaultProject.CurSolutionItem),
                OtherConfig = new Dictionary<string, object>(
                    DefaultProject.OtherConfig,
                    DefaultProject.OtherConfig.Comparer)
            };
        }

        private static ProjectItemBaseInfo CloneBaseInfo(ProjectItemBaseInfo source)
        {
            source ??= new ProjectItemBaseInfo();

            return new ProjectItemBaseInfo
            {
                Name = source.Name,
                Guid = source.Guid,
                Description = source.Description,
                FilePath = source.FilePath,
                ModifyTime = source.ModifyTime,
                IsUsing = source.IsUsing
            };
        }

        private static NodifySolutionItem CloneSolutionItem(NodifySolutionItem source)
        {
            source ??= new NodifySolutionItem();

            return new NodifySolutionItem
            {
                NodesOutputCache = CloneNodesOutputCache(source.NodesOutputCache),
                CustomGlobalParams = new ObservableCollection<TransmitParam>(
                    source.CustomGlobalParams ?? new ObservableCollection<TransmitParam>()),
                GlobalParams = new ObservableCollection<TransmitParam>(
                    source.GlobalParams ?? new ObservableCollection<TransmitParam>()),
                IsManual = source.IsManual,
                IsRapidMode = source.IsRapidMode,
                ID = source.ID,
                Name = source.Name,
                Guid = source.Guid,
                Description = source.Description,
                FilePath = source.FilePath,
                ModifyTime = source.ModifyTime,
                IsUsing = source.IsUsing
            };
        }

        private static Dictionary<string, ObservableCollection<TransmitParam>> CloneNodesOutputCache(
            Dictionary<string, ObservableCollection<TransmitParam>> source)
        {
            var result = new Dictionary<string, ObservableCollection<TransmitParam>>();
            if (source == null)
                return result;

            foreach (var pair in source)
            {
                result[pair.Key] = pair.Value == null
                    ? new ObservableCollection<TransmitParam>()
                    : new ObservableCollection<TransmitParam>(pair.Value);
            }

            return result;
        }

        private static bool TryBackupProjectFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return true;
                }

                string backupFilePath = ProjectManager.GetProjectBackupFilePath(filePath);
                string backupDirectory = Path.GetDirectoryName(backupFilePath);
                if (!string.IsNullOrWhiteSpace(backupDirectory) && !Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                File.Copy(filePath, backupFilePath, true);
                Logs.LogInfo($"保存项目之前已备份项目文件: {backupFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError($"备份项目文件失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 添加或更新最近使用的项目
        /// </summary>
        public void AddOrUpdateRecent(ProjectItemBaseInfo baseInfo)
        {
            if (baseInfo == null) return;

            // 查找是否已存在
            var existing = ProjectsBaseInfo.FirstOrDefault(p => p.Guid == baseInfo.Guid);
            if (existing != null)
            {
                // 更新现有项
                existing.Name = baseInfo.Name;
                existing.Description = baseInfo.Description;
                existing.FilePath = baseInfo.FilePath;
                existing.ModifyTime = DateTime.Now;

                // 移到列表顶部
                ProjectsBaseInfo.Remove(existing);
                ProjectsBaseInfo.Insert(0, existing);
            }
            else
            {
                // 添加新项到顶部
                baseInfo.ModifyTime = DateTime.Now;
                ProjectsBaseInfo.Insert(0, baseInfo);

                // 限制列表大小（最多20个）
                while (ProjectsBaseInfo.Count > 20)
                {
                    ProjectsBaseInfo.RemoveAt(ProjectsBaseInfo.Count - 1);
                }
            }
        }

        /// <summary>
        /// 移除项目
        /// </summary>
        public void RemoveProject(ProjectItemBaseInfo baseInfo)
        {
            if (baseInfo == null) return;
            ProjectsBaseInfo.Remove(baseInfo);
        }

        /// <summary>
        /// 获取默认项目
        /// </summary>
        public ProjectItemBaseInfo GetDefaultProject()
        {
            return DefaultBaseInfo ?? ProjectsBaseInfo.FirstOrDefault();
        }

        private static void MergePersistentSolutionItem(NodifySolutionItem target, NodifySolutionItem source)
        {
            target ??= new NodifySolutionItem();
            source ??= new NodifySolutionItem();

            target.NodesOutputCache ??= new Dictionary<string, ObservableCollection<TransmitParam>>();
            target.NodesOutputCache.Clear();
            foreach (var pair in source.NodesOutputCache ?? new Dictionary<string, ObservableCollection<TransmitParam>>())
            {
                target.NodesOutputCache[pair.Key] = pair.Value ?? new ObservableCollection<TransmitParam>();
            }

            target.CustomGlobalParams ??= new ObservableCollection<TransmitParam>();
            target.CustomGlobalParams.Clear();
            foreach (var item in source.CustomGlobalParams ?? new ObservableCollection<TransmitParam>())
            {
                target.CustomGlobalParams.Add(item);
            }

            target.GlobalParams ??= new ObservableCollection<TransmitParam>();
            target.GlobalParams.Clear();
            foreach (var item in source.GlobalParams ?? new ObservableCollection<TransmitParam>())
            {
                target.GlobalParams.Add(item);
            }

            target.IsManual = source.IsManual;
            target.IsRapidMode = source.IsRapidMode;
            target.ID = source.ID;
            target.Name = source.Name;
            target.Guid = source.Guid;
            target.Description = source.Description;
            target.FilePath = source.FilePath;
            target.ModifyTime = source.ModifyTime;
            target.IsUsing = source.IsUsing;
        }

        /// <summary>
        /// 更新指定条目
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public bool UpdateItem(string Name, object Value)
        {
            if (Name == null)
                return false;

            try
            {
                string filePath;
                lock (_lock)
                {
                    DefaultProject ??= new ProjectItem();
                    DefaultProject.OtherConfig ??= new Dictionary<string, object>();
                    DefaultProject.OtherConfig[Name] = Value;

                    filePath = DefaultBaseInfo?.FilePath;
                }

                // 触发保存放在锁外，避免保存任务与当前线程在同一把锁上互相等待。
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    _ = Task.Run(() => SaveProject(filePath));
                }

                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 获取条目
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public object GetItem(string Name)
        {
            if (Name == null)
                return null;

            try
            {
                lock (_lock)
                {
                    if (DefaultProject?.OtherConfig == null)
                        return null;

                    return DefaultProject.OtherConfig.TryGetValue(Name, out var value)
                        ? value
                        : null;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError(ex.ToString());
                return null;
            }
        }
        #endregion
    }

    /// <summary>
    /// 解决方案基础信息
    /// </summary>
    [Serializable]
    public class ProjectItemBaseInfo : BindableBase
    {
        [JsonIgnore]
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Guid _guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return _guid; }
            set { _guid = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _filePath = PrismProvider.AppBasePath;
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FileExists));
            }
        }

        [JsonIgnore]
        private DateTime _modifyTime = DateTime.Now;
        public DateTime ModifyTime
        {
            get { return _modifyTime; }
            set
            {
                _modifyTime = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DisplayLastModified));
            }
        }

        [JsonIgnore]
        private bool _isUsing;

        public bool IsUsing
        {
            get { return _isUsing; }
            set
            {
                _isUsing = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsCurrent));
            }
        }

        [JsonIgnore]
        public bool IsCurrent => IsUsing;

        [JsonIgnore]
        public bool FileExists => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

        [JsonIgnore]
        public string DisplayLastModified
        {
            get
            {
                var span = DateTime.Now - ModifyTime;
                if (span.TotalMinutes < 1) return "刚刚";
                if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} 分钟前";
                if (span.TotalDays < 1) return $"{(int)span.TotalHours} 小时前";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays} 天前";
                return ModifyTime.ToString("yyyy-MM-dd");
            }
        }
    }
}
