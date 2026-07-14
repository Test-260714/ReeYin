using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Recipe;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ReeYin.RecipeManager.Models
{
    public sealed class RecipeManagerNodeModel
    {
        public Task EnsureRecipeNodesInitializedAsync()
        {
            List<object> nodes = CollectRecipeNodes(PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeCaches)
                .ToList();
            if (nodes.Count == 0)
            {
                return Task.CompletedTask;
            }

            foreach (object node in nodes)
            {
                int serial = ResolveNodeSerial(node);
                if (serial < 0)
                {
                    continue;
                }

                IModuleParam moduleParam = GetPropertyValue(node, "ModuleParam") as IModuleParam;
                if (moduleParam == null)
                {
                    moduleParam = new ModuleParamBase();
                    SetPropertyValue(node, "ModuleParam", moduleParam);
                }

                EnsureModuleParamInitialized(moduleParam, serial);

                if (moduleParam is ModelParamBase model)
                {
                    model.OnceInit();
                }
            }

            return Task.CompletedTask;
        }

        public List<ModelParamBase> CollectRuntimeRecipeModels()
        {
            Dictionary<int, ModelParamBase> models = new();
            Dictionary<string, object> nodeCaches = PrismProvider.ProjectManager?.SltCurSolutionRuntimeData?.NodeParamCaches;
            if (nodeCaches != null)
            {
                foreach (ModelParamBase model in nodeCaches.Values
                    .OfType<ModelParamBase>()
                    .Where(item => item.Serial >= 0)
                    .GroupBy(item => item.Serial)
                    .Select(item => item.First()))
                {
                    models[model.Serial] = model;
                }
            }

            foreach (object node in CollectRecipeNodes(PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeCaches))
            {
                // 运行时缓存里可能还没有全部模块参数，这里再从节点树补一遍。
                if (GetPropertyValue(node, "ModuleParam") is not ModelParamBase model)
                {
                    continue;
                }

                int serial = model.Serial >= 0 ? model.Serial : ResolveNodeSerial(node);
                if (serial < 0)
                {
                    continue;
                }

                if (model.Serial < 0)
                {
                    model.Serial = serial;
                }

                if (!models.ContainsKey(serial))
                {
                    models[serial] = model;
                }
            }

            return models.Values
                .Where(item => item != null && item.Serial >= 0)
                .OrderBy(item => item.Serial)
                .ToList();
        }

        public bool TryCreatePageEditorModel(
            ProjectRecipeNodeGroup group,
            RecipeParamInfo parameter,
            out ModelParamBase editorModel,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            editorModel = null;

            ModelParamBase runtimeModel = FindRuntimeModel(parameter, group);
            if (runtimeModel != null)
            {
                try
                {
                    // 优先复制运行时对象，避免页面编辑器丢失运行期已展开的嵌套对象。
                    editorModel = runtimeModel.DeepClone();
                }
                catch (Exception ex)
                {
                    Logs.LogWarning($"复制页面编辑模型失败，将尝试重新创建实例：{ex.Message}");
                }
            }

            editorModel ??= CreateEditorModelInstance(runtimeModel?.GetType() ?? parameter?.DeclaringType);
            if (editorModel == null)
            {
                errorMessage = "当前参数无法创建对应的编辑模型。";
                return false;
            }

            int serial = parameter?.Serial >= 0 ? parameter.Serial : group?.Serial ?? -1;
            if (serial >= 0)
            {
                editorModel.Serial = serial;
            }

            if (parameter != null &&
                !RecipeValueConverter.IsValueEmpty(parameter.Value) &&
                !ReassignParamCollector.TrySetMarkedParamValue(editorModel, parameter.Path, parameter.Value, createMissingObjects: true))
            {
                errorMessage = "当前参数值无法加载到编辑页面，请检查参数路径或数据格式。";
                return false;
            }

            return true;
        }

        public static bool TryUpdateParameterValueFromEditorModel(
            RecipeParamInfo parameter,
            ModelParamBase model,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (parameter == null || model == null)
            {
                errorMessage = "编辑结果无效。";
                return false;
            }

            if (!ReassignParamCollector.TryGetMarkedParamValue(model, parameter.Path, out object value))
            {
                errorMessage = "无法从编辑页面读取修改后的参数值。";
                return false;
            }

            parameter.Value = RecipeValueConverter.SerializeValue(value, parameter.MemberType);
            return true;
        }

        private ModelParamBase FindRuntimeModel(RecipeParamInfo parameter, ProjectRecipeNodeGroup group)
        {
            int serial = parameter?.Serial >= 0 ? parameter.Serial : group?.Serial ?? -1;
            if (serial < 0)
            {
                return null;
            }

            ModelParamBase runtimeModel = PrismProvider.ProjectManager?.SltCurSolutionRuntimeData?.NodeParamCaches?.Values
                .OfType<ModelParamBase>()
                .FirstOrDefault(model => model.Serial == serial);
            if (runtimeModel != null)
            {
                return runtimeModel;
            }

            return CollectRecipeNodes(PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeCaches)
                .Select(node => GetPropertyValue(node, "ModuleParam") as ModelParamBase)
                .FirstOrDefault(model => model?.Serial == serial);
        }

        private static ModelParamBase CreateEditorModelInstance(Type modelType)
        {
            if (modelType == null || !typeof(ModelParamBase).IsAssignableFrom(modelType))
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance(modelType) as ModelParamBase;
            }
            catch
            {
                return null;
            }
        }

        private static void EnsureModuleParamInitialized(IModuleParam moduleParam, int serial)
        {
            if (moduleParam == null)
            {
                return;
            }

            moduleParam.Serial = serial;
            moduleParam.moduleInputParam ??= new ModuleParam();
            moduleParam.moduleOutputParam ??= new ModuleParam();
        }

        private IEnumerable<object> CollectRecipeNodes(object nodeSource)
        {
            foreach (object node in EnumerateItems(nodeSource))
            {
                if (node == null)
                {
                    continue;
                }

                yield return node;

                object innerView = GetPropertyValue(node, "InnerView");
                if (innerView == null)
                {
                    continue;
                }

                foreach (object innerNode in CollectRecipeNodes(GetPropertyValue(innerView, "Nodes")))
                {
                    yield return innerNode;
                }
            }
        }

        private IEnumerable<object> EnumerateItems(object source)
        {
            if (source is not IEnumerable enumerable || source is string)
            {
                yield break;
            }

            foreach (object item in enumerable)
            {
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        private int ResolveNodeSerial(object node)
        {
            if (GetPropertyValue(node, "ModuleParam") is IModuleParam moduleParam && moduleParam.Serial >= 0)
            {
                return moduleParam.Serial;
            }

            object menuInfo = GetPropertyValue(node, "MenuInfo");
            object serialValue = GetPropertyValue(menuInfo, "Serial");
            if (serialValue is int serial)
            {
                return serial;
            }

            return int.TryParse(serialValue?.ToString(), out serial) ? serial : -1;
        }

        private static object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return property?.GetValue(instance);
        }

        private static void SetPropertyValue(object instance, string propertyName, object value)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property?.CanWrite == true)
            {
                property.SetValue(instance, value);
            }
        }
    }
}
