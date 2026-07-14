using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Recipe.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Recipe
{
    /// <summary>
    /// 配方核心业务逻辑实现
    /// </summary>
    public class RecipeService : IRecipeService
    {
        private readonly IRecipeRepository _repository;
        private readonly IRecipeParamCollector _collector;
        private readonly RecipeParamApplierService _applierService;
        private readonly RecipeSyncService _syncService;

        public RecipeService(
            IRecipeRepository repository = null,
            IRecipeParamCollector collector = null)
        {
            _repository = repository ?? new RecipeRepository();
            _collector = collector ?? new RecipeParamCollectorAdapter();
            _applierService = new RecipeParamApplierService(_collector, _repository);
            _syncService = new RecipeSyncService(_collector, _repository);
        }

        public async Task<ProjectRecipeConfig> LoadRecipeConfigAsync()
        {
            return await _repository.LoadAsync();
        }

        public async Task<bool> SaveRecipeConfigAsync(ProjectRecipeConfig config)
        {
            return await _repository.SaveAsync(config);
        }

        public ProjectRecipeInfo CreateRecipe(string recipeName = null, string operatorName = null)
        {
            ProjectRecipeConfig config = _repository.Load();
            string finalName = string.IsNullOrWhiteSpace(recipeName)
                ? BuildNextRecipeName(config, "配方")
                : recipeName.Trim();

            ProjectRecipeInfo recipe = CreateEmptyRecipe(finalName, operatorName);
            if (!TryPopulateRecipeFromRuntime(recipe))
            {
                ProjectRecipeInfo template = config.CurrentRecipe ?? config.Recipes.FirstOrDefault();
                if (template != null)
                {
                    foreach (ProjectRecipeNodeGroup group in template.Groups)
                        recipe.Groups.Add(group.CreateCopy());
                }
            }

            return recipe;
        }

        public bool ApplyRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            return _applierService.ApplyRecipeParams(model, recipeParams);
        }

        public bool SyncRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            return _syncService.SyncRecipeParams(model, recipeParams);
        }

        public bool RemoveRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            return _syncService.RemoveRecipeParams(model, recipeParams);
        }

        public List<RecipeParamInfo> GetMarkedParams(object model)
        {
            return _collector.GetMarkedParams(model);
        }

        public bool TryGetMarkedParamValue(object model, string path, out object value)
        {
            return _collector.TryGetMarkedParamValue(model, path, out value);
        }

        #region Helpers

        private bool TryPopulateRecipeFromRuntime(ProjectRecipeInfo recipe)
        {
            if (recipe == null)
                return false;

            List<ModelParamBase> models = CollectAvailableRecipeModels();
            if (models.Count == 0)
                return false;

            foreach (ModelParamBase model in models)
            {
                List<RecipeParamInfo> infos = NormalizeRecipeParams(
                    model,
                    model.RecipeParams?.Count > 0 ? model.RecipeParams : GetMarkedParams(model),
                    includeCurrentValues: true);

                if (infos.Count == 0)
                    continue;

                ProjectRecipeNodeGroup group = recipe.GetOrCreateGroup(model.Serial, ResolveModelSubjection(model, infos));
                foreach (RecipeParamInfo info in infos)
                {
                    RecipeParamInfo copy = info.CreateCopy();
                    copy.Id = Guid.NewGuid();
                    group.Parameters.Add(copy);
                }
            }

            return recipe.Groups.Count > 0;
        }

        private List<ModelParamBase> CollectAvailableRecipeModels()
        {
            Dictionary<int, ModelParamBase> models = new();

            AddRuntimeModels(models);
            AddNodeGraphModels(models, PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeCaches);

            return models.Values
                .Where(item => item != null && item.Serial >= 0)
                .OrderBy(item => item.Serial)
                .ToList();
        }

        private void AddRuntimeModels(IDictionary<int, ModelParamBase> models)
        {
            Dictionary<string, object> nodeCaches = PrismProvider.ProjectManager?.SltCurSolutionRuntimeData?.NodeParamCaches;
            if (nodeCaches == null || nodeCaches.Count == 0)
                return;

            foreach (ModelParamBase model in nodeCaches.Values
                .OfType<ModelParamBase>()
                .Where(item => item.Serial >= 0)
                .GroupBy(item => item.Serial)
                .Select(item => item.First()))
            {
                if (!models.ContainsKey(model.Serial))
                    models[model.Serial] = model;
            }
        }

        private void AddNodeGraphModels(IDictionary<int, ModelParamBase> models, object nodeSource)
        {
            foreach (object node in EnumerateItems(nodeSource))
            {
                if (node == null)
                    continue;

                AddModelFromNode(models, node);

                object innerView = GetPropertyValue(node, "InnerView");
                if (innerView != null)
                    AddNodeGraphModels(models, GetPropertyValue(innerView, "Nodes"));
            }
        }

        private void AddModelFromNode(IDictionary<int, ModelParamBase> models, object node)
        {
            ModelParamBase model = GetPropertyValue(node, "ModuleParam") as ModelParamBase;
            int serial = ResolveNodeSerial(node, model);
            if (model == null || serial < 0)
                return;

            if (model.Serial < 0)
                model.Serial = serial;

            if (models.ContainsKey(serial))
                return;

            models[serial] = model;
        }

        private IEnumerable<object> EnumerateItems(object source)
        {
            if (source is not IEnumerable enumerable || source is string)
                yield break;

            foreach (object item in enumerable)
            {
                if (item != null)
                    yield return item;
            }
        }

        private object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            PropertyInfo property = instance.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);

            return property?.GetValue(instance);
        }

        private int ResolveNodeSerial(object node, ModelParamBase model)
        {
            if (model?.Serial >= 0)
                return model.Serial;

            object menuInfo = GetPropertyValue(node, "MenuInfo");
            object serialValue = GetPropertyValue(menuInfo, "Serial");

            if (serialValue is int serial)
                return serial;

            return int.TryParse(serialValue?.ToString(), out serial) ? serial : -1;
        }

        private List<RecipeParamInfo> NormalizeRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams, bool includeCurrentValues)
        {
            List<RecipeParamInfo> sourceInfos = recipeParams?
                .Where(item => item != null)
                .ToList() ?? new List<RecipeParamInfo>();

            string subjection = ResolveModelSubjection(model, sourceInfos);
            List<RecipeParamInfo> normalizedInfos = new();

            foreach (RecipeParamInfo source in sourceInfos)
            {
                RecipeParamInfo info = source.CreateCopy();

                if (model?.Serial >= 0)
                    info.Serial = model.Serial;

                if (string.IsNullOrWhiteSpace(info.Subjection))
                    info.Subjection = subjection;

                info.RecipeKey = RecipeKeyHelper.Normalize(info.Serial, info.Path, info.RecipeKey);

                if (includeCurrentValues)
                {
                    string currentValue = GetCurrentValueText(model, info);
                    if (string.IsNullOrWhiteSpace(info.Value?.ToString()))
                        info.Value = currentValue;
                }

                normalizedInfos.Add(info);
            }

            return normalizedInfos;
        }

        private string GetCurrentValueText(ModelParamBase model, RecipeParamInfo info)
        {
            if (model == null || info == null)
                return string.Empty;

            return TryGetMarkedParamValue(model, info.Path, out object value)
                ? RecipeValueConverter.SerializeValue(value, info.MemberType)
                : string.Empty;
        }

        private string ResolveModelSubjection(ModelParamBase model, IEnumerable<RecipeParamInfo> infos)
        {
            string subjection = infos?
                .Select(item => item?.Subjection)
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

            if (!string.IsNullOrWhiteSpace(subjection))
                return subjection;

            if (!string.IsNullOrWhiteSpace(model?.Name))
                return model.Name;

            return model?.GetType().Name ?? string.Empty;
        }

        private string BuildRecipeKey(int serial, string path)
        {
            return RecipeKeyHelper.Build(serial, path);
        }

        private string ResolveOperatorName()
        {
            string userName = PrismProvider.User?.CurUser?.UserName;
            return string.IsNullOrWhiteSpace(userName) ? Environment.UserName : userName;
        }

        private ProjectRecipeInfo CreateEmptyRecipe(string recipeName, string operatorName)
        {
            DateTime now = DateTime.Now;
            string finalOperator = string.IsNullOrWhiteSpace(operatorName) ? ResolveOperatorName() : operatorName;

            return new ProjectRecipeInfo
            {
                Id = Guid.NewGuid(),
                Name = recipeName ?? string.Empty,
                Description = string.Empty,
                Category = "通用",
                Version = "1.0.0",
                Author = finalOperator,
                LastModifiedBy = finalOperator,
                CreatedAt = now,
                ModifiedAt = now,
                IsEnabled = true
            };
        }

        private string BuildNextRecipeName(ProjectRecipeConfig config, string prefix)
        {
            int index = 1;
            string candidate;
            do
            {
                candidate = $"{prefix}_{index:000}";
                index++;
            } while (config.Recipes.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)));

            return candidate;
        }

        #endregion
    }
}

