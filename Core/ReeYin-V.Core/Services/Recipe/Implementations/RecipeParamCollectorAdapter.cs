using ReeYin_V.Core.Services.Recipe.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ReeYin_V.Core.Services.Recipe
{
    /// <summary>
    /// 配方参数收集适配器 - 通过反射调用ReeYin_V.Share.ReassignParamCollector
    /// </summary>
    public class RecipeParamCollectorAdapter : IRecipeParamCollector
    {
        private const string CollectorTypeName = "ReeYin_V.Share.ReassignParamCollector";
        private const string CollectorAssemblyName = "ReeYin_V.Share";

        private static Type _collectorType;
        private static MethodInfo _getMarkedParamsMethod;
        private static MethodInfo _tryGetMarkedParamValueMethod;
        private static MethodInfo _setMarkedParamValuesMethod;

        /// <summary>
        /// 获取被标记的配方参数
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public List<RecipeParamInfo> GetMarkedParams(object model)
        {
            if (model == null)
                return new List<RecipeParamInfo>();

            MethodInfo method = ResolveGetMarkedParamsMethod();
            if (method == null)
                return new List<RecipeParamInfo>();

            try
            {
                object result = method.Invoke(null, new[] { model });
                if (result is IEnumerable<RecipeParamInfo> infos)
                    return infos.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"收集配方参数失败：{ex}");
            }

            return new List<RecipeParamInfo>();
        }

        public bool TryGetMarkedParamValue(object model, string path, out object value)
        {
            value = null;
            if (model == null || string.IsNullOrWhiteSpace(path))
                return false;

            MethodInfo method = ResolveTryGetMarkedParamValueMethod();
            if (method == null)
                return false;

            try
            {
                object[] parameters = { model, path, null };
                bool result = method.Invoke(null, parameters) is bool invokeResult && invokeResult;
                value = parameters[2];
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取配方参数值失败：{ex}");
                value = null;
                return false;
            }
        }

        public int SetMarkedParamValues(object model, IReadOnlyDictionary<string, object> values, bool throwOnError)
        {
            if (model == null || values == null || values.Count == 0)
                return 0;

            MethodInfo method = ResolveSetMarkedParamValuesMethod();
            if (method == null)
                return 0;

            try
            {
                object[] parameters = { model, values, throwOnError };
                return method.Invoke(null, parameters) is int invokeResult ? invokeResult : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用配方参数失败：{ex}");
                return 0;
            }
        }

        #region Reflection Helpers

        private static MethodInfo ResolveGetMarkedParamsMethod()
        {
            if (_getMarkedParamsMethod != null)
                return _getMarkedParamsMethod;

            Type collectorType = ResolveCollectorType();
            _getMarkedParamsMethod = collectorType?.GetMethod(
                "GetMarkedParams",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(object) },
                null);

            return _getMarkedParamsMethod;
        }

        private static MethodInfo ResolveTryGetMarkedParamValueMethod()
        {
            if (_tryGetMarkedParamValueMethod != null)
                return _tryGetMarkedParamValueMethod;

            Type collectorType = ResolveCollectorType();
            _tryGetMarkedParamValueMethod = collectorType?.GetMethod(
                "TryGetMarkedParamValue",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(object), typeof(string), typeof(object).MakeByRefType() },
                null);

            return _tryGetMarkedParamValueMethod;
        }

        private static MethodInfo ResolveSetMarkedParamValuesMethod()
        {
            if (_setMarkedParamValuesMethod != null)
                return _setMarkedParamValuesMethod;

            Type collectorType = ResolveCollectorType();
            _setMarkedParamValuesMethod = collectorType?.GetMethod(
                "SetMarkedParamValues",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(object), typeof(IReadOnlyDictionary<string, object>), typeof(bool) },
                null);

            return _setMarkedParamValuesMethod;
        }

        private static Type ResolveCollectorType()
        {
            if (_collectorType != null)
                return _collectorType;

            _collectorType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(item => item.GetType(CollectorTypeName, false))
                .FirstOrDefault(item => item != null);

            _collectorType ??= Type.GetType($"{CollectorTypeName}, {CollectorAssemblyName}", false);
            return _collectorType;
        }

        #endregion
    }
}

