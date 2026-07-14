using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Share
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OutputParamAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public OutputParamAttribute(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }

    public class OutputParamInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Type MemberType { get; set; }
        public MemberInfo MemberInfo { get; set; }
        public bool IsField { get; set; }
    }

    public static class OutputParamCollector
    {
        public static List<OutputParamInfo> GetDataPoints(Type type)
        {
            var result = new List<OutputParamInfo>();

            // 处理字段
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<OutputParamAttribute>();
                if (attr != null)
                {
                    result.Add(new OutputParamInfo
                    {
                        Name = attr.Name ?? field.Name,
                        Description = attr.Description,
                        MemberType = field.FieldType,
                        MemberInfo = field,
                        IsField = true
                    });
                }
            }

            // 处理属性
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute<OutputParamAttribute>();
                if (attr != null && property.CanWrite) // 新增：属性必须可写（有 setter）
                {
                    result.Add(new OutputParamInfo
                    {
                        Name = attr.Name ?? property.Name,
                        Description = attr.Description,
                        MemberType = property.PropertyType,
                        MemberInfo = property,
                        IsField = false
                    });
                }
            }

            return result;
        }

        // 原有：获取对象中标记参数的值（保持不变）
        public static Dictionary<string, object> GetDataPointValues(object obj)
        {
            if (obj == null) return new Dictionary<string, object>();

            var result = new Dictionary<string, object>();
            var dataPoints = GetDataPoints(obj.GetType());

            foreach (var dataPoint in dataPoints)
            {
                object value = null;
                if (dataPoint.IsField)
                {
                    value = ((FieldInfo)dataPoint.MemberInfo).GetValue(obj);
                }
                else
                {
                    value = ((PropertyInfo)dataPoint.MemberInfo).GetValue(obj);
                }

                //result[dataPoint.Name] = value.DeepCopy(); // 假设 DeepCopy 已实现深拷贝
                result[dataPoint.Name] = value; // 假设 DeepCopy 已实现深拷贝
            }

            return result;
        }

    }

    //// 假设已实现的深拷贝扩展方法（原有逻辑，保持不变）
    //public static class ObjectExtension
    //{
    //    public static object DeepCopy(this object obj)
    //    {
    //        // 此处为深拷贝逻辑（根据实际需求实现，如二进制序列化、反射拷贝等）
    //        // 示例（简化版，实际需处理复杂类型）：
    //        if (obj == null) return null;
    //        var type = obj.GetType();
    //        if (type.IsValueType || type == typeof(string))
    //            return obj;

    //        // 复杂类型深拷贝（示例用 Activator + 反射，实际建议用更高效的方式如 Json 序列化）
    //        var copy = Activator.CreateInstance(type);
    //        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    //        {
    //            var fieldValue = field.GetValue(obj);
    //            field.SetValue(copy, fieldValue.DeepCopy());
    //        }
    //        return copy;
    //    }
    //}
}
