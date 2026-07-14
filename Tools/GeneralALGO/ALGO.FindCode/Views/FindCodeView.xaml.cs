using ALGO.FindCode.ViewModels;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static ALGO.FindCode.ViewModels.FindCodeViewModel;

namespace ALGO.FindCode.Views
{
    /// <summary>
    /// 扫码识别参数页，负责承载输入链接、码型参数、预览和输出配置。
    /// </summary>
    public partial class FindCodeView : UserControl
    {
        #region 初始化
        /// <summary>
        /// 初始化扫码识别参数页控件。
        /// </summary>
        public FindCodeView()
        {
            InitializeComponent();
        }
        #endregion
    }

    /// <summary>
    /// 根据当前码型把一维码或二维码参数区域切换为可见。
    /// </summary>
    public class EnumToVisibilityConverter : IValueConverter
    {
        #region 可见性转换
        /// <summary>
        /// 判断当前码型是否属于指定枚举范围，匹配时显示对应参数区域。
        /// </summary>
        /// <param name="value">当前选择的 CodeType 枚举值。</param>
        /// <param name="targetType">绑定目标类型。</param>
        /// <param name="parameter">数字表示前 N 个码型，last:N 表示后 N 个码型，也可传逗号分隔枚举名。</param>
        /// <param name="culture">当前区域文化信息。</param>
        /// <returns>匹配时返回 Visible，否则返回 Collapsed。</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string param = parameter.ToString() ?? string.Empty;
            string valueName = value.ToString() ?? string.Empty;

            if (int.TryParse(param, out int n))
            {
                var firstN = Enum.GetValues(typeof(CodeType))
                                 .Cast<CodeType>()
                                 .Take(n)
                                 .Select(e => e.ToString());

                return firstN.Contains(valueName)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (param.StartsWith("last:", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(param.Substring(5), out int m))
            {
                var lastN = Enum.GetValues(typeof(CodeType))
                                .Cast<CodeType>()
                                .Reverse()
                                .Take(m)
                                .Select(e => e.ToString());

                return lastN.Contains(valueName)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            var validValues = param.Split(',');
            return validValues.Contains(valueName)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// 参数区域显示状态不需要反向写回码型。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    /// <summary>
    /// 按参数定义中的 UIType 选择数字、文本或下拉输入模板。
    /// </summary>
    public class ParamTemplateSelector : DataTemplateSelector
    {
        #region 参数模板选择
        /// <summary>
        /// 数值参数使用的输入模板。
        /// </summary>
        public DataTemplate NumberTemplate { get; set; } = null!;

        /// <summary>
        /// 文本参数使用的输入模板。
        /// </summary>
        public DataTemplate TextTemplate { get; set; } = null!;

        /// <summary>
        /// 下拉参数使用的输入模板。
        /// </summary>
        public DataTemplate ComboBoxTemplate { get; set; } = null!;

        /// <summary>
        /// 根据参数定义选择对应输入模板。
        /// </summary>
        /// <param name="item">参数定义对象。</param>
        /// <param name="container">模板宿主控件。</param>
        /// <returns>匹配的输入模板。</returns>
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
        #endregion
    }
}
