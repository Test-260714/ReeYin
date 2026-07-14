using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Core.Services.Recipe;
using ReeYin_V.Logger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace ReeYin_V.Core.Helper
{
    /// <summary>
    /// 为旧项目中缺失或被反序列化为 null 的模型参数补默认实例。
    /// </summary>
    public static class ModelParamCompatibilityDefaults
    {
        private const int MaxDepth = 4;
        private const int MaxLoggedMembers = 20;

        public static void Normalize(ModelParamBase modelParam)
        {
            if (modelParam == null)
            {
                return;
            }

            List<string> repairedMembers = new List<string>();
            HashSet<object> visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

            EnsureCoreModelParamMembers(modelParam, repairedMembers);
            NormalizeObject(modelParam, modelParam.GetType().Name, repairedMembers, visited, 0);

            if (repairedMembers.Count == 0)
            {
                return;
            }

            string members = string.Join(", ", repairedMembers.Take(MaxLoggedMembers));
            if (repairedMembers.Count > MaxLoggedMembers)
            {
                members += $" ...(+{repairedMembers.Count - MaxLoggedMembers})";
            }

            Logs.LogInfo(
                $"模型参数默认值修复：Serial={modelParam.Serial:D3}, Type={modelParam.GetType().FullName}, " +
                $"Count={repairedMembers.Count}, Members={members}");
        }

        private static void EnsureCoreModelParamMembers(ModelParamBase modelParam, List<string> repairedMembers)
        {
            if (modelParam.moduleInputParam == null)
            {
                modelParam.moduleInputParam = new ModuleParam();
                repairedMembers.Add(nameof(ModelParamBase.moduleInputParam));
            }

            if (modelParam.moduleOutputParam == null)
            {
                modelParam.moduleOutputParam = new ModuleParam();
                repairedMembers.Add(nameof(ModelParamBase.moduleOutputParam));
            }

            if (modelParam.InputNodeStatus == null)
            {
                modelParam.InputNodeStatus = new List<(int, NodeStatus)>();
                repairedMembers.Add(nameof(ModelParamBase.InputNodeStatus));
            }

            if (modelParam.OutputParams == null)
            {
                modelParam.OutputParams = new ObservableCollection<TransmitParam>();
                repairedMembers.Add(nameof(ModelParamBase.OutputParams));
            }

            if (modelParam.RecipeParams == null)
            {
                modelParam.RecipeParams = new ObservableCollection<RecipeParamInfo>();
                repairedMembers.Add(nameof(ModelParamBase.RecipeParams));
            }
        }

        private static void NormalizeObject(
            object target,
            string path,
            List<string> repairedMembers,
            HashSet<object> visited,
            int depth)
        {
            if (target == null || depth > MaxDepth || IsSimpleType(target.GetType()) || !visited.Add(target))
            {
                return;
            }

            foreach (MemberInfo member in GetSerializableMembers(target.GetType()))
            {
                Type memberType = GetMemberType(member);
                if (memberType == null || IsSimpleType(memberType) || IsUnsafeAutoCreateType(memberType))
                {
                    continue;
                }

                object value = GetMemberValue(member, target);
                string memberPath = $"{path}.{member.Name}";
                if (value == null)
                {
                    object defaultValue = CreateDefaultValue(memberType);
                    if (defaultValue == null)
                    {
                        continue;
                    }

                    if (SetMemberValue(member, target, defaultValue))
                    {
                        value = defaultValue;
                        repairedMembers.Add(memberPath);
                    }
                }

                NormalizeChildValue(value, memberPath, repairedMembers, visited, depth + 1);
            }
        }

        private static void NormalizeChildValue(
            object value,
            string path,
            List<string> repairedMembers,
            HashSet<object> visited,
            int depth)
        {
            if (value == null || depth > MaxDepth)
            {
                return;
            }

            if (value is IDictionary dictionary)
            {
                foreach (object item in dictionary.Values)
                {
                    NormalizeChildValue(item, path + "[]", repairedMembers, visited, depth + 1);
                }

                return;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                foreach (object item in enumerable)
                {
                    NormalizeChildValue(item, path + "[]", repairedMembers, visited, depth + 1);
                }

                return;
            }

            NormalizeObject(value, path, repairedMembers, visited, depth);
        }

        private static IEnumerable<MemberInfo> GetSerializableMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (PropertyInfo property in current.GetProperties(flags))
                {
                    if (IsSerializableProperty(property))
                    {
                        yield return property;
                    }
                }

                foreach (FieldInfo field in current.GetFields(flags))
                {
                    if (IsSerializableField(field))
                    {
                        yield return field;
                    }
                }
            }
        }

        private static bool IsSerializableProperty(PropertyInfo property)
        {
            if (property == null ||
                property.GetIndexParameters().Length != 0 ||
                !property.CanRead ||
                !property.CanWrite ||
                HasJsonIgnore(property))
            {
                return false;
            }

            return property.GetCustomAttribute<JsonPropertyAttribute>() != null ||
                (property.GetMethod?.IsPublic == true && property.SetMethod?.IsPublic == true);
        }

        private static bool IsSerializableField(FieldInfo field)
        {
            if (field == null || field.IsStatic || field.IsInitOnly || HasJsonIgnore(field))
            {
                return false;
            }

            return field.GetCustomAttribute<JsonPropertyAttribute>() != null || field.IsPublic;
        }

        private static bool HasJsonIgnore(MemberInfo member)
        {
            return member.GetCustomAttribute<JsonIgnoreAttribute>() != null;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null
            };
        }

        private static object GetMemberValue(MemberInfo member, object target)
        {
            try
            {
                return member switch
                {
                    PropertyInfo property => property.GetValue(target),
                    FieldInfo field => field.GetValue(target),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private static bool SetMemberValue(MemberInfo member, object target, object value)
        {
            try
            {
                switch (member)
                {
                    case PropertyInfo property:
                        property.SetValue(target, value);
                        return true;
                    case FieldInfo field:
                        field.SetValue(target, value);
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static object CreateDefaultValue(Type type)
        {
            Type concreteType = ResolveConcreteType(type);
            if (concreteType == null || IsUnsafeAutoCreateType(concreteType))
            {
                return null;
            }

            try
            {
                if (concreteType.IsArray)
                {
                    return Array.CreateInstance(concreteType.GetElementType() ?? typeof(object), 0);
                }

                if (concreteType.GetConstructor(Type.EmptyTypes) == null)
                {
                    return null;
                }

                return Activator.CreateInstance(concreteType);
            }
            catch
            {
                return null;
            }
        }

        private static Type ResolveConcreteType(Type type)
        {
            if (type == null || type == typeof(string))
            {
                return null;
            }

            if (!type.IsInterface && !type.IsAbstract)
            {
                return type;
            }

            if (type.IsGenericType)
            {
                Type genericType = type.GetGenericTypeDefinition();
                Type[] args = type.GetGenericArguments();

                if (genericType == typeof(IList<>) ||
                    genericType == typeof(ICollection<>) ||
                    genericType == typeof(IEnumerable<>))
                {
                    return typeof(List<>).MakeGenericType(args);
                }

                if (genericType == typeof(IDictionary<,>))
                {
                    return typeof(Dictionary<,>).MakeGenericType(args);
                }
            }

            return null;
        }

        private static bool IsSimpleType(Type type)
        {
            Type actualType = Nullable.GetUnderlyingType(type) ?? type;
            return actualType.IsPrimitive ||
                actualType.IsEnum ||
                actualType == typeof(string) ||
                actualType == typeof(decimal) ||
                actualType == typeof(DateTime) ||
                actualType == typeof(DateTimeOffset) ||
                actualType == typeof(TimeSpan) ||
                actualType == typeof(Guid);
        }

        private static bool IsUnsafeAutoCreateType(Type type)
        {
            if (type == null || typeof(Delegate).IsAssignableFrom(type))
            {
                return true;
            }

            string fullName = type.FullName ?? string.Empty;
            return fullName.StartsWith("System.Windows.", StringComparison.Ordinal) ||
                fullName.StartsWith("Microsoft.", StringComparison.Ordinal) ||
                fullName.StartsWith("HalconDotNet.", StringComparison.Ordinal) ||
                fullName.StartsWith("ImageTool.", StringComparison.Ordinal);
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
