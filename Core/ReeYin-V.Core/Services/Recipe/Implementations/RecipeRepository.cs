using Newtonsoft.Json.Linq;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Recipe.Interfaces;
using ReeYin_V.Logger;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Recipe
{
    /// <summary>
    /// 配方数据持久化实现
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        private const string RecipeStoreKey = "RecipeConfig";
        private const string LegacyRecipeStoreKey = "RecipeManagerModel";
        private static readonly object StoreLock = new();

        public async Task<ProjectRecipeConfig> LoadAsync()
        {
            return await Task.Run(Load);
        }

        public async Task<bool> SaveAsync(ProjectRecipeConfig config)
        {
            return await Task.Run(() => Save(config));
        }

        public ProjectRecipeConfig Load()
        {
            lock (StoreLock)
            {
                try
                {
                    object recipeObject = PrismProvider.ProjectManager?.SolutionManager?.GetItem(RecipeStoreKey);
                    ProjectRecipeConfig config = ConvertRecipeConfig(recipeObject);
                    if (config != null)
                        return EnsureValidState(config.CreateSnapshot());

                    object legacyObject = PrismProvider.ProjectManager?.SolutionManager?.GetItem(LegacyRecipeStoreKey);
                    return EnsureValidState(ConvertLegacyRecipeConfig(legacyObject) ?? new ProjectRecipeConfig());
                }
                catch (Exception ex)
                {
                    Logs.LogError($"加载工程配方配置失败：{ex}");
                    return new ProjectRecipeConfig();
                }
            }
        }

        public bool Save(ProjectRecipeConfig config)
        {
            lock (StoreLock)
            {
                try
                {
                    if (config == null)
                        return false;

                    ProjectRecipeConfig storeConfig = config.CreateSnapshot();
                    storeConfig.LastUpdatedAt = DateTime.Now;
                    return PrismProvider.ProjectManager?.SolutionManager?.UpdateItem(RecipeStoreKey, storeConfig) ?? false;
                }
                catch (Exception ex)
                {
                    Logs.LogError($"保存工程配方配置失败：{ex}");
                    return false;
                }
            }
        }

        #region Config Conversion

        private static ProjectRecipeConfig ConvertRecipeConfig(object value)
        {
            if (value == null)
                return null;

            try
            {
                return value switch
                {
                    ProjectRecipeConfig typed => typed,
                    JObject jObject => jObject.ToObject<ProjectRecipeConfig>(),
                    JToken token => token.ToObject<ProjectRecipeConfig>(),
                    _ => JObject.FromObject(value).ToObject<ProjectRecipeConfig>()
                };
            }
            catch
            {
                return null;
            }
        }

        private static ProjectRecipeConfig ConvertLegacyRecipeConfig(object legacyValue)
        {
            if (legacyValue == null)
                return null;

            try
            {
                JObject store = legacyValue as JObject ?? JObject.FromObject(legacyValue);
                ProjectRecipeConfig config = new()
                {
                    CurrentRecipeId = store.Value<Guid?>("ActiveRecipeId") ?? Guid.Empty,
                    LastUpdatedAt = store.Value<DateTime?>("LastUpdatedAt") ?? DateTime.Now
                };

                foreach (JObject recipeToken in (store["Recipes"] as JArray ?? new JArray()).OfType<JObject>())
                {
                    ProjectRecipeInfo recipe = new()
                    {
                        Id = recipeToken.Value<Guid?>("Id") ?? Guid.NewGuid(),
                        Name = recipeToken.Value<string>("Name") ?? string.Empty,
                        Description = recipeToken.Value<string>("Description") ?? string.Empty,
                        Category = recipeToken.Value<string>("Category") ?? string.Empty,
                        Version = recipeToken.Value<string>("Version") ?? "1.0.0",
                        CreatedAt = recipeToken.Value<DateTime?>("CreatedAt") ?? DateTime.Now,
                        ModifiedAt = recipeToken.Value<DateTime?>("ModifiedAt") ?? DateTime.Now,
                        Author = recipeToken.Value<string>("Author") ?? string.Empty,
                        LastModifiedBy = recipeToken.Value<string>("LastModifiedBy") ?? string.Empty,
                        IsEnabled = recipeToken.Value<bool?>("IsEnabled") ?? true
                    };

                    foreach (JObject parameterToken in (recipeToken["Parameters"] as JArray ?? new JArray()).OfType<JObject>())
                    {
                        RecipeParamInfo parameter = ConvertLegacyRecipeParameter(parameterToken);
                        ProjectRecipeNodeGroup group = recipe.GetOrCreateGroup(parameter.Serial, parameter.Subjection);
                        group.Parameters.Add(parameter);
                    }

                    config.Recipes.Add(recipe);
                }

                return EnsureValidState(config);
            }
            catch
            {
                return null;
            }
        }

        private static RecipeParamInfo ConvertLegacyRecipeParameter(JObject parameterToken)
        {
            string recipeKey = parameterToken?.Value<string>("Key") ?? string.Empty;
            string name = parameterToken?.Value<string>("Name") ?? string.Empty;
            string pathFromKey = TryParseLegacyPath(recipeKey, out int serial) ? ParseLegacyPath(recipeKey) : string.Empty;
            string displayName = string.IsNullOrWhiteSpace(name) ? pathFromKey : name;
            string subjection = string.Empty;
            string path = !string.IsNullOrWhiteSpace(pathFromKey) ? pathFromKey : displayName;

            int separatorIndex = displayName.IndexOf('.');
            if (separatorIndex > 0)
            {
                subjection = displayName[..separatorIndex];
                path = displayName[(separatorIndex + 1)..];
            }

            return new RecipeParamInfo
            {
                Id = parameterToken?.Value<Guid?>("Id") ?? Guid.NewGuid(),
                Name = string.IsNullOrWhiteSpace(path) ? displayName : path,
                Description = parameterToken?.Value<string>("Description") ?? string.Empty,
                Serial = serial,
                Subjection = subjection,
                RecipeKey = recipeKey,
                Path = path,
                Value = parameterToken?.Value<string>("Value") ?? string.Empty,
                Unit = parameterToken?.Value<string>("Unit") ?? string.Empty,
                IsEnable = parameterToken?.Value<bool?>("IsEnable") ??
                           parameterToken?.Value<bool?>("IsRequired") ?? false,
                IsRequired = false,
                OptionsText = parameterToken?.Value<string>("OptionsText") ?? string.Empty,
            };
        }

        private static bool TryParseLegacyPath(string recipeKey, out int serial)
        {
            serial = -1;
            if (string.IsNullOrWhiteSpace(recipeKey))
                return false;

            int index = recipeKey.IndexOf(':');
            if (index <= 0)
                return false;

            return int.TryParse(recipeKey[..index], out serial);
        }

        private static string ParseLegacyPath(string recipeKey)
        {
            if (string.IsNullOrWhiteSpace(recipeKey))
                return string.Empty;

            int index = recipeKey.IndexOf(':');
            return index < 0 ? recipeKey : recipeKey[(index + 1)..];
        }

        #endregion

        #region Helpers

        private static ProjectRecipeConfig EnsureValidState(ProjectRecipeConfig config)
        {
            config ??= new ProjectRecipeConfig();
            config.Recipes ??= new ObservableCollection<ProjectRecipeInfo>();

            foreach (ProjectRecipeInfo recipe in config.Recipes)
            {
                recipe.Groups ??= new ObservableCollection<ProjectRecipeNodeGroup>();
                foreach (ProjectRecipeNodeGroup group in recipe.Groups)
                    group.Parameters ??= new ObservableCollection<RecipeParamInfo>();
            }

            config.EnsureValidState();
            return config;
        }

        #endregion
    }
}

