using System;

namespace ReeYin_V.Core.Helper
{
    /// <summary>
    /// 反序列化时把旧字段名映射到当前字段/属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class JsonAliasAttribute : Attribute
    {
        public JsonAliasAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
