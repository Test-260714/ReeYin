using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ReeYin_V.Core.Helper
{
    /// <summary>
    /// 统一处理项目 JSON 反序列化兼容项。
    /// </summary>
    public sealed class JsonCompatibilityContractResolver : DefaultContractResolver
    {
        public static readonly JsonCompatibilityContractResolver Instance = new JsonCompatibilityContractResolver();

        private JsonCompatibilityContractResolver()
        {
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            HashSet<string> existingNames = new HashSet<string>(
                properties.Select(property => property.PropertyName),
                StringComparer.OrdinalIgnoreCase);

            foreach (JsonProperty property in properties.ToList())
            {
                MemberInfo member = ResolveMember(type, property.UnderlyingName);
                if (member == null)
                {
                    continue;
                }

                foreach (JsonAliasAttribute alias in member.GetCustomAttributes<JsonAliasAttribute>(inherit: true))
                {
                    if (string.IsNullOrWhiteSpace(alias.Name) || existingNames.Contains(alias.Name))
                    {
                        continue;
                    }

                    JsonProperty aliasProperty = CreateProperty(member, memberSerialization);
                    aliasProperty.PropertyName = alias.Name;
                    aliasProperty.Readable = false;
                    aliasProperty.Writable = property.Writable;
                    aliasProperty.ShouldSerialize = _ => false;
                    aliasProperty.ValueProvider = property.ValueProvider;
                    aliasProperty.PropertyType = property.PropertyType;
                    aliasProperty.NullValueHandling = property.NullValueHandling;
                    aliasProperty.ObjectCreationHandling = property.ObjectCreationHandling;
                    aliasProperty.DefaultValueHandling = property.DefaultValueHandling;
                    aliasProperty.ReferenceLoopHandling = property.ReferenceLoopHandling;
                    aliasProperty.TypeNameHandling = property.TypeNameHandling;
                    aliasProperty.Converter = property.Converter;

                    properties.Add(aliasProperty);
                    existingNames.Add(alias.Name);
                }
            }

            return properties;
        }

        private static MemberInfo ResolveMember(Type type, string memberName)
        {
            if (type == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                MemberInfo member = current.GetMember(memberName, flags).FirstOrDefault(item =>
                    item.MemberType == MemberTypes.Property || item.MemberType == MemberTypes.Field);
                if (member != null)
                {
                    return member;
                }
            }

            return null;
        }
    }
}
