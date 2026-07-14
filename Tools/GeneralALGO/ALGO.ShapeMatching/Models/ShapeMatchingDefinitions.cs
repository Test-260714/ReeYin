using Prism.Mvvm;
using System.Collections.Generic;
using System.ComponentModel;

namespace ALGO.ShapeMatching
{
    #region 枚举定义

    /// <summary>
    /// 形状匹配模块支持的模板匹配算法类型。
    /// </summary>
    public enum ShapeMatchingMode
    {
        [Description("形状匹配")]
        形状匹配,

        [Description("相关性匹配")]
        相关性匹配,
    }

    /// <summary>
    /// 模板学习区域的来源方式。
    /// </summary>
    public enum RegionCreatMode
    {
        [Description("链接输入")]
        链接输入,

        [Description("绘制矩形")]
        绘制矩形,

        [Description("绘制旋转矩形")]
        绘制旋转矩形,

        [Description("绘制圆形")]
        绘制圆形,
    }

    #endregion

    #region 参数模型

    /// <summary>
    /// 模板文件在界面列表中的显示信息。
    /// </summary>
    public class ShapeFileDefinition : BindableBase
    {
        private string _name = string.Empty;
        private string _path = string.Empty;
        private bool _isSelected;

        /// <summary>
        /// 模板文件名称。
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        /// <summary>
        /// 模板文件完整路径。
        /// </summary>
        public string Path
        {
            get { return _path; }
            set { SetProperty(ref _path, value); }
        }

        /// <summary>
        /// 当前模板是否在界面中选中。
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        /// <summary>
        /// 创建空的模板文件显示项。
        /// </summary>
        public ShapeFileDefinition()
        {
        }

        /// <summary>
        /// 创建模板文件显示项。
        /// </summary>
        /// <param name="name">模板文件名称。</param>
        /// <param name="path">模板文件完整路径。</param>
        /// <param name="isSelected">是否默认选中。</param>
        public ShapeFileDefinition(string name, string path, bool isSelected = false)
        {
            Name = name;
            Path = path;
            IsSelected = isSelected;
        }
    }

    /// <summary>
    /// 算法参数在界面中的编辑控件类型。
    /// </summary>
    public enum ParamUIType
    {
        Number,
        Text,
        ComboBox
    }

    /// <summary>
    /// 算法参数转换为 HALCON HTuple 时使用的数据类型。
    /// </summary>
    public enum ParamValueType
    {
        Int,
        Double,
        String,
        StringInt,
        StringDouble
    }

    /// <summary>
    /// 形状匹配或 NCC 匹配的单项算法参数定义。
    /// </summary>
    public class ParamDefinition : BindableBase
    {
        private object _value = string.Empty;
        private bool _isVisible = true;

        /// <summary>
        /// 参数在界面中的中文名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数使用的界面编辑控件类型。
        /// </summary>
        public ParamUIType UIType { get; set; }

        /// <summary>
        /// 数值控件允许的最小值。
        /// </summary>
        public double MinValue { get; set; } = 0;

        /// <summary>
        /// 数值控件允许的最大值。
        /// </summary>
        public double MaxValue { get; set; } = 99999999;

        /// <summary>
        /// 数值控件每次微调的步长。
        /// </summary>
        public double SmallChange { get; set; } = 1;

        /// <summary>
        /// 参数写入 HALCON HTuple 时的转换类型。
        /// </summary>
        public ParamValueType ValueType { get; set; } = ParamValueType.Double;

        /// <summary>
        /// 参数当前值，单位和语义由 HALCON 对应算子决定。
        /// </summary>
        public object Value
        {
            get => _value;
            set { SetProperty(ref _value, value); }
        }

        /// <summary>
        /// 是否在普通算法参数页显示；false 表示保留为内部高级参数。
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set { SetProperty(ref _isVisible, value); }
        }

        /// <summary>
        /// 下拉控件的可选值列表。
        /// </summary>
        public List<string> Options { get; set; } = new List<string>();
    }

    #endregion
}
