using System;

namespace ReeYin_V.Core.Interfaces
{
    /// <summary>
    /// 标记模型中的输入参数成员，便于基类统一定位和解析。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InputParamAttribute : Attribute
    {
        public string Name { get; }

        public string Description { get; }

        public bool NeedDeepCopy { get; }

        public InputParamAttribute(string name = null, string description = null, bool needDeepCopy = true)
        {
            Name = name;
            Description = description;
            NeedDeepCopy = needDeepCopy;
        }
    }
}
