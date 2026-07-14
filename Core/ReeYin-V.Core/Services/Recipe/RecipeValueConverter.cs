using Newtonsoft.Json;
using System;
using System.Collections;
using System.Globalization;

namespace ReeYin_V.Core.Services.Recipe
{
    /// <summary>
    /// 配方参数值转换工具
    /// </summary>
    public static class RecipeValueConverter
    {
        /// <summary>
        /// 将对象值序列化为字符串
        /// </summary>
        public static string SerializeValue(object value, Type targetType = null)
        {
            if (value == null)
                return string.Empty;

            Type actualType = targetType == null
                ? value.GetType()
                : Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value is string existingText && ShouldUseJsonSerialization(actualType))
                return existingText;

            if (actualType == typeof(DateTime) && value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            if (actualType == typeof(Guid) && value is Guid guid)
                return guid.ToString("D");

            if (actualType == typeof(TimeSpan) && value is TimeSpan timeSpan)
                return timeSpan.ToString("c", CultureInfo.InvariantCulture);

            if (actualType.IsEnum)
                return SerializeEnumValue(value, actualType);

            if (ShouldUseJsonSerialization(actualType))
                return JsonConvert.SerializeObject(value);

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 将字符串反序列化为指定类型的值
        /// </summary>
        public static object DeserializeValue(string valueText, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(valueText))
                return null;

            Type actualType = targetType == null
                ? typeof(string)
                : Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (actualType == typeof(string))
                    return valueText;

                if (actualType == typeof(int))
                    return int.Parse(valueText, CultureInfo.InvariantCulture);

                if (actualType == typeof(long))
                    return long.Parse(valueText, CultureInfo.InvariantCulture);

                if (actualType == typeof(double))
                    return double.Parse(valueText, CultureInfo.InvariantCulture);

                if (actualType == typeof(float))
                    return float.Parse(valueText, CultureInfo.InvariantCulture);

                if (actualType == typeof(decimal))
                    return decimal.Parse(valueText, CultureInfo.InvariantCulture);

                if (actualType == typeof(bool))
                    return bool.Parse(valueText);

                if (actualType == typeof(DateTime))
                    return DateTime.ParseExact(valueText, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                if (actualType == typeof(Guid))
                    return Guid.Parse(valueText);

                if (actualType == typeof(TimeSpan))
                    return TimeSpan.Parse(valueText, CultureInfo.InvariantCulture);

                if (actualType.IsEnum)
                    return Enum.Parse(actualType, valueText, ignoreCase: true);

                if (ShouldUseJsonSerialization(actualType))
                    return JsonConvert.DeserializeObject(valueText, actualType);

                return valueText;
            }
            catch
            {
                return valueText;
            }
        }

        /// <summary>
        /// 检查Value是否为空或默认值
        /// </summary>
        public static bool IsValueEmpty(object value)
        {
            if (value == null)
                return true;

            if (value is string str)
                return string.IsNullOrWhiteSpace(str);

            return false;
        }

        /// <summary>
        /// 获取Value的字符串表示
        /// </summary>
        public static string GetValueAsString(object value)
        {
            return value?.ToString() ?? string.Empty;
        }

        private static bool ShouldUseJsonSerialization(Type type)
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

            if (actualType == typeof(string) ||
                actualType == typeof(decimal) ||
                actualType == typeof(DateTime) ||
                actualType == typeof(DateTimeOffset) ||
                actualType == typeof(TimeSpan) ||
                actualType == typeof(Guid))
            {
                return false;
            }

            return actualType.IsArray ||
                   typeof(IEnumerable).IsAssignableFrom(actualType) ||
                   actualType.IsClass ||
                   (actualType.IsValueType && !typeof(IConvertible).IsAssignableFrom(actualType));
        }

        private static string SerializeEnumValue(object value, Type enumType)
        {
            if (value == null || enumType == null || !enumType.IsEnum)
            {
                return string.Empty;
            }

            if (enumType.IsInstanceOfType(value))
            {
                return value.ToString() ?? string.Empty;
            }

            if (value is string enumText)
            {
                try
                {
                    return Enum.Parse(enumType, enumText, ignoreCase: true).ToString();
                }
                catch
                {
                    return enumText;
                }
            }

            try
            {
                Type underlyingType = Enum.GetUnderlyingType(enumType);
                object numericValue = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
                return Enum.ToObject(enumType, numericValue).ToString();
            }
            catch
            {
                return value.ToString() ?? string.Empty;
            }
        }
    }
}

