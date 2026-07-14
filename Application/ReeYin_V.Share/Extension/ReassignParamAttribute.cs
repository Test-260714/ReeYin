using ReeYin_V.Core.Services.Recipe;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReeYin_V.Share
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RecipeParamAttribute : Attribute
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public bool RequiresPageEditor { get; set; }

        public string EditorPageName { get; set; }

        public RecipeParamAttribute(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }

    public static class ReassignParamCollector
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<ReassignParamDefinition>> Cache = new();
        private static readonly string[] JsonIgnoreAttributeTypeNames =
        {
            "Newtonsoft.Json.JsonIgnoreAttribute",
            "System.Text.Json.Serialization.JsonIgnoreAttribute"
        };

        public static List<RecipeParamInfo> GetMarkedParams(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return GetDefinitions(type).Select(item => item.ToInfo()).ToList();
        }

        public static List<RecipeParamInfo> GetMarkedParams(object obj)
        {
            if (obj == null)
            {
                return new List<RecipeParamInfo>();
            }

            return GetMarkedParams(obj.GetType());
        }

        public static Dictionary<string, object> GetMarkedParamValues(object obj, bool usePathAsKey = true)
        {
            if (obj == null)
            {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            Dictionary<string, object> result = new(StringComparer.OrdinalIgnoreCase);
            foreach (ReassignParamDefinition definition in GetDefinitions(obj.GetType()))
            {
                string key = usePathAsKey ? definition.Path : definition.Name;
                if (result.ContainsKey(key))
                {
                    throw new InvalidOperationException($"Duplicate marked parameter key detected: {key}. Use Path, or assign a unique Name.");
                }

                result[key] = definition.GetValue(obj);
            }

            return result;
        }

        public static bool TryGetMarkedParamValue(object obj, string key, out object value)
        {
            value = null;
            if (obj == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            ReassignParamDefinition definition = FindDefinition(obj.GetType(), key);
            if (definition == null)
            {
                return false;
            }

            value = definition.GetValue(obj);
            return true;
        }

        public static bool TrySetMarkedParamValue(object obj, string key, object value, bool createMissingObjects = true)
        {
            if (obj == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            ReassignParamDefinition definition = FindDefinition(obj.GetType(), key);
            return definition != null && definition.TrySetValue(obj, value, createMissingObjects);
        }

        public static int SetMarkedParamValues(object obj, IReadOnlyDictionary<string, object> values, bool createMissingObjects = true)
        {
            if (obj == null || values == null || values.Count == 0)
            {
                return 0;
            }

            int updatedCount = 0;
            IReadOnlyList<ReassignParamDefinition> definitions = GetDefinitions(obj.GetType());
            foreach ((string key, object value) in values)
            {
                ReassignParamDefinition definition = FindDefinition(definitions, key);
                if (definition != null && definition.TrySetValue(obj, value, createMissingObjects))
                {
                    updatedCount++;
                }
            }

            return updatedCount;
        }

        private static IReadOnlyList<ReassignParamDefinition> GetDefinitions(Type type)
        {
            return Cache.GetOrAdd(type, BuildDefinitions);
        }

        private static IReadOnlyList<ReassignParamDefinition> BuildDefinitions(Type type)
        {
            List<ReassignParamDefinition> result = new();
            CollectDefinitions(type, result, new List<MemberAccessor>(), new HashSet<Type>());
            return result;
        }

        private static void CollectDefinitions(
            Type currentType,
            List<ReassignParamDefinition> result,
            List<MemberAccessor> accessorChain,
            HashSet<Type> visitingTypes)
        {
            if (currentType == null || !visitingTypes.Add(currentType))
            {
                return;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (FieldInfo field in currentType.GetFields(flags))
            {
                if (field.IsStatic || field.IsDefined(typeof(CompilerGeneratedAttribute), true))
                {
                    continue;
                }

                AppendDefinition(field, result, accessorChain, visitingTypes);
            }

            foreach (PropertyInfo property in currentType.GetProperties(flags))
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                AppendDefinition(property, result, accessorChain, visitingTypes);
            }

            visitingTypes.Remove(currentType);
        }

        private static void AppendDefinition(
            MemberInfo member,
            List<ReassignParamDefinition> result,
            List<MemberAccessor> accessorChain,
            HashSet<Type> visitingTypes)
        {
            if (HasJsonIgnoreAttribute(member))
            {
                return;
            }

            MemberAccessor accessor = MemberAccessor.Create(member);
            if (accessor == null || !accessor.CanRead)
            {
                return;
            }

            accessorChain.Add(accessor);

            RecipeParamAttribute attribute = member.GetCustomAttribute<RecipeParamAttribute>();
            if (attribute != null && accessor.CanWrite)
            {
                string resolvedName = string.IsNullOrWhiteSpace(attribute.Name) ? member.Name : attribute.Name;
                result.Add(new ReassignParamDefinition
                {
                    Name = resolvedName,
                    Description = attribute.Description,
                    RequiresPageEditor = attribute.RequiresPageEditor,
                    EditorPageName = attribute.EditorPageName,
                    Path = BuildDisplayPath(accessorChain, resolvedName),
                    MemberPath = BuildMemberPath(accessorChain),
                    MemberType = accessor.MemberType,
                    DeclaringType = member.DeclaringType,
                    MemberInfo = member,
                    IsField = member is FieldInfo,
                    AccessorChain = accessorChain.ToArray()
                });
            }

            if (ShouldTraverseNestedType(accessor.MemberType))
            {
                CollectDefinitions(accessor.MemberType, result, accessorChain, visitingTypes);
            }

            accessorChain.RemoveAt(accessorChain.Count - 1);
        }

        private static bool HasJsonIgnoreAttribute(MemberInfo member)
        {
            if (member == null)
            {
                return false;
            }

            foreach (object attribute in member.GetCustomAttributes(true))
            {
                string attributeTypeName = attribute.GetType().FullName;
                if (JsonIgnoreAttributeTypeNames.Contains(attributeTypeName, StringComparer.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static ReassignParamDefinition FindDefinition(Type type, string key)
        {
            return FindDefinition(GetDefinitions(type), key);
        }

        private static ReassignParamDefinition FindDefinition(IReadOnlyList<ReassignParamDefinition> definitions, string key)
        {
            if (definitions == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            ReassignParamDefinition pathMatch = definitions
                .FirstOrDefault(item => string.Equals(item.Path, key, StringComparison.OrdinalIgnoreCase));
            if (pathMatch != null)
            {
                return pathMatch;
            }

            ReassignParamDefinition memberPathMatch = definitions
                .FirstOrDefault(item => string.Equals(item.MemberPath, key, StringComparison.OrdinalIgnoreCase));
            if (memberPathMatch != null)
            {
                return memberPathMatch;
            }

            List<ReassignParamDefinition> nameMatches = definitions
                .Where(item => string.Equals(item.Name, key, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (nameMatches.Count > 1)
            {
                throw new InvalidOperationException($"Multiple marked parameters match the name {key}. Use Path or MemberPath instead.");
            }

            return nameMatches.Count == 1 ? nameMatches[0] : null;
        }

        private static string BuildDisplayPath(IReadOnlyList<MemberAccessor> accessorChain, string leafName)
        {
            if (accessorChain == null || accessorChain.Count == 0)
            {
                return leafName ?? string.Empty;
            }

            if (accessorChain.Count == 1)
            {
                return leafName ?? accessorChain[0].MemberInfo.Name;
            }

            IEnumerable<string> segments = accessorChain
                .Take(accessorChain.Count - 1)
                .Select(item => item.MemberInfo.Name)
                .Append(leafName ?? accessorChain[^1].MemberInfo.Name);

            return string.Join(".", segments);
        }

        private static string BuildMemberPath(IReadOnlyList<MemberAccessor> accessorChain)
        {
            if (accessorChain == null || accessorChain.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(".", accessorChain.Select(item => item.MemberInfo.Name));
        }

        private static bool ShouldTraverseNestedType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            Type actualType = Nullable.GetUnderlyingType(type) ?? type;
            if (!actualType.IsClass || actualType == typeof(string) || actualType == typeof(object))
            {
                return false;
            }

            if (typeof(IEnumerable).IsAssignableFrom(actualType))
            {
                return false;
            }

            if (typeof(Delegate).IsAssignableFrom(actualType))
            {
                return false;
            }

            string namespaceName = actualType.Namespace ?? string.Empty;
            if (namespaceName.StartsWith("System", StringComparison.Ordinal)
                || namespaceName.StartsWith("Microsoft", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        private static bool TryConvertValue(object value, Type targetType, out object convertedValue)
        {
            convertedValue = null;
            if (targetType == null)
            {
                return false;
            }

            Type actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value == null)
            {
                if (actualType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    return false;
                }

                return true;
            }

            if (actualType == typeof(object) || actualType.IsInstanceOfType(value))
            {
                convertedValue = value;
                return true;
            }

            if (value is JToken token)
            {
                if (TryConvertJToken(token, targetType, out convertedValue))
                {
                    return true;
                }

                value = token.Type == JTokenType.String
                    ? token.Value<string>()
                    : token.ToString(Formatting.None);
            }

            if (value == null)
            {
                return Nullable.GetUnderlyingType(targetType) != null || !actualType.IsValueType;
            }

            Type valueType = value.GetType();

            try
            {
                if (actualType.IsEnum)
                {
                    if (value is string enumText)
                    {
                        convertedValue = Enum.Parse(actualType, enumText, true);
                        return true;
                    }

                    convertedValue = Enum.ToObject(actualType, value);
                    return true;
                }

                if (actualType == typeof(Guid) && value is string guidText && Guid.TryParse(guidText, out Guid guidValue))
                {
                    convertedValue = guidValue;
                    return true;
                }

                if (actualType == typeof(TimeSpan) && value is string timeSpanText && TimeSpan.TryParse(timeSpanText, CultureInfo.InvariantCulture, out TimeSpan timeSpanValue))
                {
                    convertedValue = timeSpanValue;
                    return true;
                }

                if (value is string jsonText && TryDeserializeComplexValue(jsonText, targetType, out convertedValue))
                {
                    return true;
                }

                TypeConverter targetConverter = TypeDescriptor.GetConverter(actualType);
                if (targetConverter.CanConvertFrom(valueType))
                {
                    convertedValue = targetConverter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
                    return true;
                }

                TypeConverter sourceConverter = TypeDescriptor.GetConverter(valueType);
                if (sourceConverter.CanConvertTo(actualType))
                {
                    convertedValue = sourceConverter.ConvertTo(null, CultureInfo.InvariantCulture, value, actualType);
                    return true;
                }

                if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(actualType))
                {
                    convertedValue = Convert.ChangeType(value, actualType, CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryConvertJToken(JToken token, Type targetType, out object convertedValue)
        {
            convertedValue = null;
            if (token == null || targetType == null)
            {
                return false;
            }

            try
            {
                convertedValue = token.ToObject(targetType);
                return convertedValue != null || Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryDeserializeComplexValue(string valueText, Type targetType, out object convertedValue)
        {
            convertedValue = null;
            if (string.IsNullOrWhiteSpace(valueText) || !ShouldUseJsonConversion(targetType))
            {
                return false;
            }

            try
            {
                convertedValue = JsonConvert.DeserializeObject(valueText, targetType);
                return convertedValue != null || Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType;
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldUseJsonConversion(Type type)
        {
            if (type == null)
            {
                return false;
            }

            Type actualType = Nullable.GetUnderlyingType(type) ?? type;
            if (actualType.IsPrimitive || actualType.IsEnum)
            {
                return false;
            }

            return actualType != typeof(string) &&
                   actualType != typeof(decimal) &&
                   actualType != typeof(DateTime) &&
                   actualType != typeof(DateTimeOffset) &&
                   actualType != typeof(TimeSpan) &&
                   actualType != typeof(Guid);
        }

        private sealed class ReassignParamDefinition
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public bool RequiresPageEditor { get; set; }

            public string EditorPageName { get; set; }

            public int Serial { get; set; }

            public string Subjection { get; set; }

            public string Path { get; set; }

            public string MemberPath { get; set; }

            public Type MemberType { get; set; }

            public Type DeclaringType { get; set; }

            public MemberInfo MemberInfo { get; set; }

            public bool IsField { get; set; }

            public MemberAccessor[] AccessorChain { get; set; }

            public RecipeParamInfo ToInfo()
            {
                return new RecipeParamInfo
                {
                    Name = Name,
                    Description = Description,
                    Serial = Serial,
                    Subjection = Subjection,
                    Path = Path,
                    //MemberPath = MemberPath,
                    MemberType = MemberType,
                    DeclaringType = DeclaringType,
                    MemberInfo = MemberInfo,
                    IsField = IsField,
                    OptionsText = BuildOptionsText(MemberType),
                    RequiresPageEditor = RequiresPageEditor,
                    EditorPageName = EditorPageName
                };
            }

            private static string BuildOptionsText(Type memberType)
            {
                Type actualType = Nullable.GetUnderlyingType(memberType) ?? memberType;
                return actualType?.IsEnum == true
                    ? string.Join("|", Enum.GetNames(actualType))
                    : string.Empty;
            }

            public object GetValue(object obj)
            {
                object current = obj;
                foreach (MemberAccessor accessor in AccessorChain)
                {
                    if (current == null)
                    {
                        return null;
                    }

                    current = accessor.GetValue(current);
                }

                return current;
            }

            public bool TrySetValue(object obj, object value, bool createMissingObjects)
            {
                if (obj == null || AccessorChain == null || AccessorChain.Length == 0)
                {
                    return false;
                }

                object current = obj;
                for (int i = 0; i < AccessorChain.Length - 1; i++)
                {
                    MemberAccessor accessor = AccessorChain[i];
                    if (current == null)
                    {
                        return false;
                    }

                    object next = accessor.GetValue(current);
                    if (next == null)
                    {
                        if (!createMissingObjects || !accessor.TryCreateAndAssignInstance(current, out next))
                        {
                            return false;
                        }
                    }

                    current = next;
                }

                if (current == null)
                {
                    return false;
                }

                MemberAccessor leafAccessor = AccessorChain[^1];
                if (!leafAccessor.CanWrite || !TryConvertValue(value, leafAccessor.MemberType, out object convertedValue))
                {
                    return false;
                }

                return leafAccessor.TrySetValue(current, convertedValue);
            }
        }

        private sealed class MemberAccessor
        {
            public MemberInfo MemberInfo { get; set; }

            public Type MemberType { get; set; }

            public bool CanRead { get; set; }

            public bool CanWrite { get; set; }

            public object GetValue(object obj)
            {
                return MemberInfo switch
                {
                    FieldInfo field => field.GetValue(obj),
                    PropertyInfo property => property.GetValue(obj),
                    _ => null
                };
            }

            public bool TrySetValue(object obj, object value)
            {
                if (!CanWrite)
                {
                    return false;
                }

                switch (MemberInfo)
                {
                    case FieldInfo field:
                        field.SetValue(obj, value);
                        return true;
                    case PropertyInfo property:
                        property.SetValue(obj, value);
                        return true;
                    default:
                        return false;
                }
            }

            public bool TryCreateAndAssignInstance(object owner, out object instance)
            {
                instance = null;
                Type actualType = Nullable.GetUnderlyingType(MemberType) ?? MemberType;
                if (!CanWrite || actualType.IsAbstract || actualType.IsInterface)
                {
                    return false;
                }

                try
                {
                    instance = Activator.CreateInstance(actualType, true);
                }
                catch
                {
                    return false;
                }

                return instance != null && TrySetValue(owner, instance);
            }

            public static MemberAccessor Create(MemberInfo member)
            {
                return member switch
                {
                    FieldInfo field when !field.IsStatic => new MemberAccessor
                    {
                        MemberInfo = field,
                        MemberType = field.FieldType,
                        CanRead = true,
                        CanWrite = !field.IsInitOnly && !field.IsLiteral
                    },
                    PropertyInfo property when property.GetIndexParameters().Length == 0 => new MemberAccessor
                    {
                        MemberInfo = property,
                        MemberType = property.PropertyType,
                        CanRead = property.GetGetMethod(true) != null,
                        CanWrite = property.GetSetMethod(true) != null
                    },
                    _ => null
                };
            }
        }
    }
}
