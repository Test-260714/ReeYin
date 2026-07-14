using Newtonsoft.Json;
using Nodify.FlowApp;
using Prism.Events;
using ReeYin.ProjectManager.Models;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Core.Services.Project.Models;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReeYin.ProjectManager.Services
{
    /// <summary>
    /// 解决方案管理服务
    /// </summary>
    public class SolutionManagerService
    {
        private readonly IEventAggregator _eventAggregator;
        private const string SolutionExtension = ".rysl";

        public SolutionManagerService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// 创建新解决方案
        /// </summary>
        public async Task<(bool success, string message, SolutionInfo solution)> CreateSolutionAsync(
            string name, string description, string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return (false, "解决方案名称不能为空", null);

                if (string.IsNullOrWhiteSpace(filePath))
                    return (false, "文件路径不能为空", null);

                if (!filePath.EndsWith(SolutionExtension, StringComparison.OrdinalIgnoreCase))
                    filePath += SolutionExtension;

                if (File.Exists(filePath))
                    return (false, "文件已存在，请选择其他路径", null);

                var solution = new SolutionInfo
                {
                    Guid = Guid.NewGuid(),
                    Name = name,
                    Description = description,
                    FilePath = filePath,
                    LastModified = DateTime.Now
                };

                var solutionManager = PrismProvider.ProjectManager.SolutionManager;
                PrismProvider.ProjectManager.ResetCurrentSolutionRuntimeData();

                // 重置为全新 ProjectItem
                solutionManager.DefaultProject = new();
                solutionManager.DefaultBaseInfo = new ProjectItemBaseInfo
                {
                    Guid = solution.Guid,
                    Name = solution.Name,
                    Description = solution.Description,
                    FilePath = solution.FilePath,
                    ModifyTime = solution.LastModified,
                    IsUsing = true
                };

                // 通过 Core 保存到文件
                await Task.Run(() => solutionManager.SaveProject(filePath));

                Logs.LogInfo($"创建解决方案成功: {name}");
                return (true, "创建成功", solution);
            }
            catch (Exception ex)
            {
                Logs.LogError($"创建解决方案失败: {ex.Message}");
                return (false, $"创建失败: {ex.Message}", null);
            }
        }

        /// <summary>
        /// 导入旧工程文件，并以旧工程名称创建新的解决方案
        /// </summary>
        public async Task<(bool success, string message, SolutionInfo solution)> ImportLegacySolutionAsync(string legacyProjectFilePath)
        {
            var projectManager = PrismProvider.ProjectManager;
            var previousRuntimeData = projectManager.SltCurSolutionRuntimeData;
            var importRuntimeData = projectManager.CreateSolutionRuntimeData();

            try
            {
                projectManager.SetCurrentSolutionRuntimeData(importRuntimeData);

                if (!LegacyProjectImporter.TryLoad(legacyProjectFilePath, out var importedAppView, out var importMessage))
                {
                    projectManager.SetCurrentSolutionRuntimeData(previousRuntimeData);
                    return (false, importMessage, null);
                }

                var (solutionName, solutionFilePath) =
                    BuildUniqueImportedSolutionTarget(Path.GetFileNameWithoutExtension(legacyProjectFilePath));

                var solution = new SolutionInfo
                {
                    Guid = Guid.NewGuid(),
                    Name = solutionName,
                    Description = $"由旧工程 {Path.GetFileName(legacyProjectFilePath)} 导入",
                    FilePath = solutionFilePath,
                    LastModified = DateTime.Now
                };

                importedAppView.guid = solution.Guid;

                var baseInfo = new ProjectItemBaseInfo
                {
                    Guid = solution.Guid,
                    Name = solution.Name,
                    Description = solution.Description,
                    FilePath = solution.FilePath,
                    ModifyTime = solution.LastModified,
                    IsUsing = true
                };

                var currentSolutionItem = new NodifySolutionItem
                {
                    Guid = solution.Guid,
                    Name = solution.Name,
                    Description = solution.Description,
                    FilePath = Path.GetDirectoryName(solution.FilePath) ?? string.Empty,
                    ModifyTime = solution.LastModified,
                    IsUsing = true
                };

                var project = new ProjectItem
                {
                    BaseInfo = baseInfo,
                    CurSolutionItem = currentSolutionItem,
                    OtherConfig = new Dictionary<string, object>
                    {
                        ["NodifyAppView"] = importedAppView
                    }
                };

                await Task.Run(() =>
                    JsonHelper.JsonObjectSerialize(project, solution.FilePath, TypeNameHandling.Auto));

                _eventAggregator.GetEvent<SolutionOperationEvent>().Publish("释放");

                var solutionManager = projectManager.SolutionManager;
                solutionManager.DefaultProject = project;
                solutionManager.DefaultBaseInfo = baseInfo;
                projectManager.SltCurSolutionItem = currentSolutionItem;

                _eventAggregator.GetEvent<SolutionOperationEvent>().Publish("打开");

                Logs.LogInfo($"导入旧工程成功: {legacyProjectFilePath} -> {solution.FilePath}");
                return (true, "导入成功", solution);
            }
            catch (Exception ex)
            {
                projectManager.SetCurrentSolutionRuntimeData(previousRuntimeData);
                Logs.LogError($"导入旧工程失败: {ex.Message}");
                return (false, $"导入失败: {ex.Message}", null);
            }
        }

        /// <summary>
        /// 打开解决方案
        /// </summary>
        public async Task<(bool success, string message)> OpenSolutionAsync(SolutionInfo solution)
        {
            try
            {
                if (solution == null)
                    return (false, "解决方案信息为空");

                if (!File.Exists(solution.FilePath))
                    return (false, "解决方案文件不存在");

                _eventAggregator.GetEvent<SolutionOperationEvent>().Publish("释放");

                // 通过 Core 加载 ProjectItem
                var solutionManager = PrismProvider.ProjectManager.SolutionManager;
                var loaded = await Task.Run(() => solutionManager.LoadProject(solution.FilePath));
                if (!loaded)
                {
                    _eventAggregator.GetEvent<SolutionOperationEvent>().Publish("打开");
                    return (false, "解决方案加载失败，请检查文件是否存在且内容有效");
                }

                _eventAggregator.GetEvent<SolutionOperationEvent>().Publish("打开");

                solution.LastModified = DateTime.Now;
                Logs.LogInfo($"打开解决方案成功: {solution.Name}");
                return (true, "打开成功");
            }
            catch (Exception ex)
            {
                Logs.LogError($"打开解决方案失败: {ex.Message}");
                return (false, $"打开失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存当前解决方案
        /// </summary>
        public async Task<(bool success, string message)> SaveCurrentSolutionAsync(SolutionInfo solution)
        {
            try
            {
                if (solution == null)
                    return (false, "解决方案信息为空");

                // 通过 Core 保存 ProjectItem
                var solutionManager = PrismProvider.ProjectManager.SolutionManager;
                await Task.Run(() => solutionManager.SaveProject(solution.FilePath));

                solution.LastModified = DateTime.Now;

                _eventAggregator.GetEvent<SolutionOperationEvent>().Publish("保存");

                Logs.LogInfo($"保存解决方案成功: {solution.Name}");
                return (true, "保存成功");
            }
            catch (Exception ex)
            {
                Logs.LogError($"保存解决方案失败: {ex.Message}");
                return (false, $"保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证解决方案文件
        /// </summary>
        public async Task<(bool success, string message)> ResetCurrentSolutionAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    _eventAggregator.GetEvent<SolutionOperationEvent>().Publish("释放");

                    var solutionManager = PrismProvider.ProjectManager.SolutionManager;
                    var emptyBaseInfo = new ProjectItemBaseInfo
                    {
                        Guid = Guid.Empty,
                        Name = null,
                        Description = null,
                        FilePath = string.Empty,
                        ModifyTime = DateTime.Now,
                        IsUsing = false
                    };

                    var emptyProject = new ProjectItem
                    {
                        BaseInfo = emptyBaseInfo,
                        CurSolutionItem = new NodifySolutionItem()
                    };

                    solutionManager.DefaultProject = emptyProject;
                    solutionManager.DefaultBaseInfo = emptyBaseInfo;
                    PrismProvider.ProjectManager.ResetCurrentSolutionRuntimeData();
                    PrismProvider.ProjectManager.SltCurSolutionItem = emptyProject.CurSolutionItem;
                    PrismProvider.ProjectManager.IsOpenSolution = false;

                    _eventAggregator.GetEvent<SolutionOperationEvent>().Publish("打开");
                });

                Logs.LogInfo("已清空当前加载的解决方案");
                return (true, "已清空当前加载的解决方案");
            }
            catch (Exception ex)
            {
                Logs.LogError($"清空当前解决方案失败: {ex.Message}");
                return (false, $"清空失败: {ex.Message}");
            }
        }

        public bool ValidateSolutionFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                return File.ReadAllBytes(filePath).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static (string solutionName, string filePath) BuildUniqueImportedSolutionTarget(string requestedName)
        {
            var baseDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ReeYin解决方案");

            var normalizedName = SanitizeSolutionName(requestedName);
            var candidateName = normalizedName;
            var index = 1;

            while (true)
            {
                var candidatePath = Path.Combine(baseDirectory, $"{candidateName}{SolutionExtension}");
                if (!File.Exists(candidatePath))
                    return (candidateName, candidatePath);

                candidateName = $"{normalizedName}_{index++}";
            }
        }

        private static string SanitizeSolutionName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "导入解决方案";

            var invalidChars = Path.GetInvalidFileNameChars();
            var chars = name.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalidChars, chars[i]) >= 0)
                    chars[i] = '_';
            }

            var sanitized = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? "导入解决方案" : sanitized;
        }
    }
}
