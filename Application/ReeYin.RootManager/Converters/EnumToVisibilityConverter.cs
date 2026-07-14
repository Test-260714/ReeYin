using ReeYin_V.Core.Models.Database.Tables;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ReeYin.RootManager.Converters
{
    /// <summary>
    /// 枚举值转可见性转换器
    /// 当枚举值与参数匹配时显示，否则隐藏
    /// </summary>
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();

            return enumValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 规则类型转描述转换器
    /// </summary>
    public class RuleTypeToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ModuleLoadRuleType ruleType)
            {
                return ruleType switch
                {
                    ModuleLoadRuleType.Always => "始终加载",
                    ModuleLoadRuleType.Disabled => "始终禁用",
                    ModuleLoadRuleType.BySite => "按站点加载",
                    ModuleLoadRuleType.MutualExclusive => "互斥组",
                    ModuleLoadRuleType.Dependency => "依赖其他模块",
                    ModuleLoadRuleType.ByLicense => "按许可证加载",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
