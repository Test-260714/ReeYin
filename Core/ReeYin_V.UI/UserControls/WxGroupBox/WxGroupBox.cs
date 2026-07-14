using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.WxGroupBox
{
    public class WxGroupBox : GroupBox
    {
        static WxGroupBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WxGroupBox), new FrameworkPropertyMetadata(typeof(WxGroupBox)));
        }

        /// <summary>
        /// 圆角
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(WxGroupBox), new PropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// 图标（Iconfont字符，默认 &#xe621;）
        /// </summary>
        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(WxGroupBox), new PropertyMetadata("\ue621"));

        /// <summary>
        /// 图标尺寸
        /// </summary>
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register("IconSize", typeof(double), typeof(WxGroupBox), new PropertyMetadata(14d));

        /// <summary>
        /// 标题位置
        /// </summary>
        public GroupBoxTitlePosition TitlePosition
        {
            get => (GroupBoxTitlePosition)GetValue(TitlePositionProperty);
            set => SetValue(TitlePositionProperty, value);
        }
        public static readonly DependencyProperty TitlePositionProperty =
            DependencyProperty.Register("TitlePosition", typeof(GroupBoxTitlePosition), typeof(WxGroupBox), new PropertyMetadata(GroupBoxTitlePosition.Top));

        /// <summary>
        /// 标题前景色（图标+文字颜色，默认白色）
        /// </summary>
        public Brush HeaderForeground
        {
            get => (Brush)GetValue(HeaderForegroundProperty);
            set => SetValue(HeaderForegroundProperty, value);
        }
        public static readonly DependencyProperty HeaderForegroundProperty =
            DependencyProperty.Register("HeaderForeground", typeof(Brush), typeof(WxGroupBox), new PropertyMetadata(Brushes.White));
    }

    public enum GroupBoxTitlePosition
    {
        Top,
        Left
    }
}
