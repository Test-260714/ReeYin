using ALGO.ShapeMatching;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ALGO.ShapeMatching.Views
{
    /// <summary>
    /// 形状匹配模块参数配置视图。
    /// </summary>
    public partial class ShapeMatchingView : UserControl
    {
        /// <summary>
        /// 初始化形状匹配参数视图。
        /// </summary>
        public ShapeMatchingView()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// 将带 Description 的枚举值转换为界面区域可见性。
    /// </summary>
    public class EnumToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 当前枚举描述与参数文本一致时返回可见，否则折叠。
        /// </summary>
        /// <param name="value">当前绑定的枚举值。</param>
        /// <param name="targetType">目标属性类型。</param>
        /// <param name="parameter">需要匹配的中文描述文本。</param>
        /// <param name="culture">当前区域信息。</param>
        /// <returns>匹配结果对应的可见性。</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string enumDescription = GetEnumDescription((Enum)value);

            return enumDescription == parameter.ToString()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// 将可见区域对应的中文描述转换回枚举值。
        /// </summary>
        /// <param name="value">当前可见性。</param>
        /// <param name="targetType">目标枚举类型。</param>
        /// <param name="parameter">需要匹配的中文描述文本。</param>
        /// <param name="culture">当前区域信息。</param>
        /// <returns>匹配到的枚举值，未匹配时不更新绑定。</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Binding.DoNothing;

            if (!(value is Visibility visibility))
                return Binding.DoNothing;

            if (visibility == Visibility.Visible)
            {
                foreach (var field in targetType.GetFields())
                {
                    var attribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                         .FirstOrDefault() as DescriptionAttribute;
                    string description = attribute?.Description ?? field.Name;

                    if (description == parameter.ToString())
                    {
                        return Enum.Parse(targetType, field.Name);
                    }
                }
            }

            return Binding.DoNothing;
        }

        /// <summary>
        /// 读取枚举字段上的中文 Description，缺失时使用枚举名。
        /// </summary>
        /// <param name="enumValue">需要读取描述的枚举值。</param>
        /// <returns>枚举的中文描述文本。</returns>
        private string GetEnumDescription(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                  .FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? enumValue.ToString();
        }
    }

    /// <summary>
    /// 根据参数控件类型选择数字、文本或下拉模板。
    /// </summary>
    public class ParamTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// 数值型参数使用的编辑模板。
        /// </summary>
        public DataTemplate NumberTemplate { get; set; } = null!;

        /// <summary>
        /// 文本型参数使用的编辑模板。
        /// </summary>
        public DataTemplate TextTemplate { get; set; } = null!;

        /// <summary>
        /// 下拉枚举型参数使用的编辑模板。
        /// </summary>
        public DataTemplate ComboBoxTemplate { get; set; } = null!;

        /// <summary>
        /// 按参数定义中的 UIType 返回对应模板。
        /// </summary>
        /// <param name="item">参数定义对象。</param>
        /// <param name="container">模板容器。</param>
        /// <returns>匹配到的参数编辑模板。</returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ParamDefinition param)
            {
                return param.UIType switch
                {
                    ParamUIType.Number => NumberTemplate,
                    ParamUIType.Text => TextTemplate,
                    ParamUIType.ComboBox => ComboBoxTemplate,
                    _ => TextTemplate
                };
            }
            return base.SelectTemplate(item, container);
        }
    }
}
