using ReeYin.RecipeManager.Services;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Recipe;
using ReeYin_V.Core.Services.Recipe.Interfaces;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReeYin.RecipeManager.Models
{
    public sealed class RecipeManagerRuntimeSyncModel
    {
        private readonly IRecipeService _recipeService;
        private readonly RecipeManagerService _service;
        private readonly RecipeManagerNodeModel _nodeModel;

        public RecipeManagerRuntimeSyncModel(
            IRecipeService recipeService,
            RecipeManagerService service,
            RecipeManagerNodeModel nodeModel)
        {
            _recipeService = recipeService;
            _service = service;
            _nodeModel = nodeModel;
        }

        public async Task<RecipeRuntimeSyncResult> SyncRecipeParametersFromRuntimeAsync(ProjectRecipeConfig config)
        {
            bool changed = MergeMissingRecipeParametersFromRuntime(config);
            if (!changed)
            {
                return new RecipeRuntimeSyncResult
                {
                    Changed = false,
                    Saved = true
                };
            }

            var (success, message, storageScope) = await _service.SaveAsync(config);
            if (!success)
            {
                Logs.LogWarning($"配方加载后自动同步配方参数失败：{message}");
            }

            return new RecipeRuntimeSyncResult
            {
                Changed = true,
                Saved = success,
                StorageScope = storageScope
            };
        }

        public void RefreshParameterDefinitionsFromRuntime(ProjectRecipeConfig config)
        {
            Dictionary<string, RecipeParamInfo> runtimeDefinitions = BuildRuntimeParameterDefinitionMap();
            if (runtimeDefinitions.Count == 0 || config?.Recipes == null)
            {
                return;
            }

            foreach (ProjectRecipeInfo recipe in config.Recipes)
            {
                foreach (ProjectRecipeNodeGroup group in recipe.Groups)
                {
                    foreach (RecipeParamInfo parameter in group.Parameters)
                    {
                        if (!TryResolveRuntimeDefinition(runtimeDefinitions, group, parameter, out RecipeParamInfo definition))
                        {
                            continue;
                        }

                        parameter.UpdateDefinition(definition);

                        if (string.IsNullOrWhiteSpace(parameter.OptionsText))
                        {
                            parameter.OptionsText = definition.OptionsText;
                        }
                    }
                }
            }
        }

        public static string BuildLoadStatusMessage(int recipeCount, bool runtimeParamChanged, bool runtimeParamSaved)
        {
            if (recipeCount == 0)
            {
                return "暂无可用配方。";
            }

            if (!runtimeParamChanged)
            {
                return $"已加载 {recipeCount} 个配方。";
            }

            return runtimeParamSaved
                ? $"已加载 {recipeCount} 个配方，并已同步新增或移除的标记参数。"
                : $"已加载 {recipeCount} 个配方，检测到标记参数发生变化但自动保存失败，请手动保存。";
        }

        private bool MergeMissingRecipeParametersFromRuntime(ProjectRecipeConfig config)
        {
            if (config?.Recipes == null || config.Recipes.Count == 0)
            {
                return false;
            }

            List<RuntimeRecipeGroupSnapshot> runtimeGroups = BuildRuntimeRecipeGroupSnapshots();
            if (runtimeGroups.Count == 0)
            {
                return false;
            }

            bool changed = false;
            // 以运行时标记结果为准，同步新增参数并移除已取消标记的旧参数。
            foreach (ProjectRecipeInfo recipe in config.Recipes)
            {
                HashSet<ProjectRecipeNodeGroup> matchedGroups = new();
                foreach (RuntimeRecipeGroupSnapshot runtimeGroup in runtimeGroups)
                {
                    ProjectRecipeNodeGroup group = FindMatchingRecipeGroup(recipe, runtimeGroup, matchedGroups);
                    if (group == null)
                    {
                        if (runtimeGroup.Parameters.Count == 0)
                        {
                            continue;
                        }

                        group = recipe.GetOrCreateGroup(runtimeGroup.Serial, runtimeGroup.Subjection);
                        foreach (RecipeParamInfo runtimeParameter in runtimeGroup.Parameters)
                        {
                            RecipeParamInfo copy = runtimeParameter.CreateCopy();
                            copy.Id = Guid.NewGuid();
                            group.Parameters.Add(copy);
                        }

                        matchedGroups.Add(group);
                        changed = true;
                        continue;
                    }

                    matchedGroups.Add(group);
                    if (group.Serial != runtimeGroup.Serial)
                    {
                        group.Serial = runtimeGroup.Serial;
                        changed = true;
                    }

                    if (!string.Equals(group.Subjection ?? string.Empty, runtimeGroup.Subjection ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        group.Subjection = runtimeGroup.Subjection;
                        changed = true;
                    }

                    foreach (RecipeParamInfo staleParameter in group.Parameters
                        .Where(item => !runtimeGroup.Parameters.Any(runtimeParameter => IsSameRecipeParameter(item, runtimeParameter)))
                        .ToList())
                    {
                        group.Parameters.Remove(staleParameter);
                        changed = true;
                    }

                    foreach (RecipeParamInfo runtimeParameter in runtimeGroup.Parameters)
                    {
                        RecipeParamInfo existing = FindMatchingRuntimeParameter(group, runtimeParameter);
                        if (existing == null)
                        {
                            RecipeParamInfo copy = runtimeParameter.CreateCopy();
                            copy.Id = Guid.NewGuid();
                            group.Parameters.Add(copy);
                            changed = true;
                            continue;
                        }

                        if (UpdateRecipeParameterDefinition(existing, runtimeParameter))
                        {
                            changed = true;
                        }
                    }

                    if (group.Parameters.Count == 0)
                    {
                        recipe.Groups.Remove(group);
                        changed = true;
                    }
                }

                foreach (ProjectRecipeNodeGroup staleGroup in recipe.Groups
                    .Where(group => group.Serial >= 0 && !matchedGroups.Contains(group))
                    .ToList())
                {
                    recipe.Groups.Remove(staleGroup);
                    changed = true;
                }
            }

            return changed;
        }

        private Dictionary<string, RecipeParamInfo> BuildRuntimeParameterDefinitionMap()
        {
            Dictionary<string, RecipeParamInfo> definitions = new(StringComparer.OrdinalIgnoreCase);
            foreach (RuntimeRecipeGroupSnapshot runtimeGroup in BuildRuntimeRecipeGroupSnapshots())
            {
                foreach (RecipeParamInfo info in runtimeGroup.Parameters)
                {
                    // 旧配方里有的按 RecipeKey 命中，有的仍按 Path 命中，这里两种都保留。
                    AddRuntimeDefinition(definitions, info.Serial, info.RecipeKey, info);
                    AddRuntimeDefinition(definitions, info.Serial, info.Path, info);
                }
            }

            return definitions;
        }

        private List<RuntimeRecipeGroupSnapshot> BuildRuntimeRecipeGroupSnapshots()
        {
            List<RuntimeRecipeGroupSnapshot> runtimeGroups = new();
            foreach (ModelParamBase model in _nodeModel.CollectRuntimeRecipeModels())
            {
                List<RecipeParamInfo> infos = _recipeService.GetMarkedParams(model) ?? new List<RecipeParamInfo>();
                if (infos.Count == 0 && model.RecipeParams?.Count > 0)
                {
                    infos = model.RecipeParams.ToList();
                }

                List<RecipeParamInfo> normalizedInfos = NormalizeRuntimeRecipeParameters(model, infos);
                runtimeGroups.Add(new RuntimeRecipeGroupSnapshot
                {
                    Serial = model.Serial,
                    Subjection = ResolveModelSubjection(model, normalizedInfos),
                    Parameters = normalizedInfos
                });
            }

            return runtimeGroups;
        }

        private List<RecipeParamInfo> NormalizeRuntimeRecipeParameters(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            List<RecipeParamInfo> sourceInfos = recipeParams?
                .Where(item => item != null)
                .ToList() ?? new List<RecipeParamInfo>();
            if (sourceInfos.Count == 0)
            {
                return new List<RecipeParamInfo>();
            }

            string subjection = ResolveModelSubjection(model, sourceInfos);
            List<RecipeParamInfo> normalizedInfos = new();
            foreach (RecipeParamInfo source in sourceInfos)
            {
                RecipeParamInfo info = source.CreateCopy();

                if (model?.Serial >= 0)
                {
                    info.Serial = model.Serial;
                }

                if (string.IsNullOrWhiteSpace(info.Subjection))
                {
                    info.Subjection = subjection;
                }

                info.RecipeKey = RecipeKeyHelper.Normalize(info.Serial, info.Path, info.RecipeKey);

                // 新加入的标记参数如果还没有落盘值，就从当前运行模型取一份快照。
                if (RecipeValueConverter.IsValueEmpty(info.Value) &&
                    model != null &&
                    _recipeService.TryGetMarkedParamValue(model, info.Path, out object currentValue))
                {
                    info.Value = RecipeValueConverter.SerializeValue(currentValue, info.MemberType);
                }

                normalizedInfos.Add(info);
            }

            return normalizedInfos;
        }

        private static void AddRuntimeDefinition(Dictionary<string, RecipeParamInfo> definitions, int serial, string identifier, RecipeParamInfo definition)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return;
            }

            definitions[BuildRuntimeDefinitionKey(serial, identifier)] = definition;
        }

        private static bool TryResolveRuntimeDefinition(
            IReadOnlyDictionary<string, RecipeParamInfo> runtimeDefinitions,
            ProjectRecipeNodeGroup group,
            RecipeParamInfo parameter,
            out RecipeParamInfo definition)
        {
            int serial = parameter?.Serial >= 0 ? parameter.Serial : group?.Serial ?? -1;

            return runtimeDefinitions.TryGetValue(BuildRuntimeDefinitionKey(serial, parameter?.RecipeKey), out definition) ||
                   runtimeDefinitions.TryGetValue(BuildRuntimeDefinitionKey(serial, parameter?.Path), out definition);
        }

        private static string BuildRuntimeDefinitionKey(int serial, string identifier)
        {
            return $"{serial:D3}:{(identifier ?? string.Empty).Trim()}";
        }

        private static RecipeParamInfo FindMatchingRuntimeParameter(ProjectRecipeNodeGroup group, RecipeParamInfo runtimeParameter)
        {
            if (group == null || runtimeParameter == null)
            {
                return null;
            }

            RecipeParamInfo exactMatch = !string.IsNullOrWhiteSpace(runtimeParameter.RecipeKey)
                ? group.FindParameter(runtimeParameter.RecipeKey)
                : null;
            if (exactMatch != null)
            {
                return exactMatch;
            }

            RecipeParamInfo pathMatch = !string.IsNullOrWhiteSpace(runtimeParameter.Path)
                ? group.Parameters.FirstOrDefault(item =>
                    string.Equals(item.Path, runtimeParameter.Path, StringComparison.OrdinalIgnoreCase))
                : null;
            if (pathMatch != null)
            {
                return pathMatch;
            }

            if (string.IsNullOrWhiteSpace(runtimeParameter.Name))
            {
                return null;
            }

            List<RecipeParamInfo> nameMatches = group.Parameters
                .Where(item => string.Equals(item.Name, runtimeParameter.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return nameMatches.Count == 1 ? nameMatches[0] : null;
        }

        private static ProjectRecipeNodeGroup FindMatchingRecipeGroup(
            ProjectRecipeInfo recipe,
            RuntimeRecipeGroupSnapshot runtimeGroup,
            ISet<ProjectRecipeNodeGroup> matchedGroups)
        {
            if (recipe?.Groups == null || runtimeGroup == null)
            {
                return null;
            }

            ProjectRecipeNodeGroup serialMatch = recipe.Groups
                .FirstOrDefault(item => item.Serial == runtimeGroup.Serial && !matchedGroups.Contains(item));
            if (serialMatch != null)
            {
                return serialMatch;
            }

            if (string.IsNullOrWhiteSpace(runtimeGroup.Subjection) || runtimeGroup.Parameters.Count == 0)
            {
                return null;
            }

            List<ProjectRecipeNodeGroup> candidates = recipe.Groups
                .Where(item =>
                    !matchedGroups.Contains(item) &&
                    string.Equals(item.Subjection ?? string.Empty, runtimeGroup.Subjection ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                    runtimeGroup.Parameters.Any(runtimeParameter =>
                        item.Parameters.Any(parameter => IsSameRecipeParameter(parameter, runtimeParameter))))
                .ToList();

            return candidates.Count == 1 ? candidates[0] : null;
        }

        private static bool UpdateRecipeParameterDefinition(RecipeParamInfo parameter, RecipeParamInfo definition)
        {
            if (parameter == null || definition == null)
            {
                return false;
            }

            bool changed =
                !string.Equals(parameter.Name, definition.Name, StringComparison.Ordinal) ||
                !string.Equals(parameter.Description, definition.Description, StringComparison.Ordinal) ||
                parameter.RequiresPageEditor != definition.RequiresPageEditor ||
                !string.Equals(parameter.EditorPageName, definition.EditorPageName, StringComparison.Ordinal) ||
                parameter.Serial != definition.Serial ||
                !string.Equals(parameter.Subjection, definition.Subjection, StringComparison.Ordinal) ||
                !string.Equals(parameter.RecipeKey, definition.RecipeKey, StringComparison.Ordinal) ||
                !string.Equals(parameter.Path, definition.Path, StringComparison.Ordinal) ||
                !string.Equals(parameter.MemberTypeName, definition.MemberTypeName, StringComparison.Ordinal);

            parameter.UpdateDefinition(definition);

            if (string.IsNullOrWhiteSpace(parameter.OptionsText) &&
                !string.IsNullOrWhiteSpace(definition.OptionsText))
            {
                parameter.OptionsText = definition.OptionsText;
                changed = true;
            }

            return changed;
        }

        private static bool IsSameRecipeParameter(RecipeParamInfo parameter, RecipeParamInfo definition)
        {
            if (parameter == null || definition == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(parameter.RecipeKey) &&
                !string.IsNullOrWhiteSpace(definition.RecipeKey) &&
                string.Equals(parameter.RecipeKey, definition.RecipeKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(parameter.Path) &&
                !string.IsNullOrWhiteSpace(definition.Path) &&
                string.Equals(parameter.Path, definition.Path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(parameter.Name) &&
                   !string.IsNullOrWhiteSpace(definition.Name) &&
                   string.Equals(parameter.Name, definition.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveModelSubjection(ModelParamBase model, IEnumerable<RecipeParamInfo> infos)
        {
            string subjection = infos?
                .Select(item => item?.Subjection)
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

            if (!string.IsNullOrWhiteSpace(subjection))
            {
                return subjection;
            }

            if (!string.IsNullOrWhiteSpace(model?.Name))
            {
                return model.Name;
            }

            return model?.GetType().Name ?? string.Empty;
        }

        private static string BuildRecipeKey(int serial, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return serial >= 0 ? $"{serial:D3}:" : string.Empty;
            }

            return RecipeKeyHelper.Build(serial, path);
        }

        private sealed class RuntimeRecipeGroupSnapshot
        {
            public int Serial { get; set; }

            public string Subjection { get; set; } = string.Empty;

            public List<RecipeParamInfo> Parameters { get; set; } = new();
        }
    }

    public sealed class RecipeRuntimeSyncResult
    {
        public bool Changed { get; set; }

        public bool Saved { get; set; }

        public string StorageScope { get; set; } = string.Empty;
    }
}
