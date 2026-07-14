using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ReeYin_V.UI.UserControls.WxLED
{
    public class WxLED : Control
    {
        static WxLED()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WxLED),
                new FrameworkPropertyMetadata(typeof(WxLED)));
        }

        #region 依赖属性

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(WxLED),
                new PropertyMetadata(false, OnIsActiveChanged));

        public static readonly DependencyProperty IsBreathingProperty =
            DependencyProperty.Register("IsBreathing", typeof(bool), typeof(WxLED),
                new PropertyMetadata(false, OnIsBreathingChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(WxLED),
                new PropertyMetadata(new CornerRadius(50)));

        public static readonly DependencyProperty ActiveColorProperty =
            DependencyProperty.Register("ActiveColor", typeof(Color), typeof(WxLED),
                new PropertyMetadata(Colors.Green));

        public static readonly DependencyProperty InactiveColorProperty =
            DependencyProperty.Register("InactiveColor", typeof(Color), typeof(WxLED),
                new PropertyMetadata(Colors.Red));

        public static readonly DependencyProperty BreathingDurationProperty =
            DependencyProperty.Register("BreathingDuration", typeof(double), typeof(WxLED),
                new PropertyMetadata(1.5, OnBreathingDurationChanged));

        #endregion

        #region 属性包装器

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public bool IsBreathing
        {
            get => (bool)GetValue(IsBreathingProperty);
            set => SetValue(IsBreathingProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Color ActiveColor
        {
            get => (Color)GetValue(ActiveColorProperty);
            set => SetValue(ActiveColorProperty, value);
        }

        public Color InactiveColor
        {
            get => (Color)GetValue(InactiveColorProperty);
            set => SetValue(InactiveColorProperty, value);
        }

        public double BreathingDuration
        {
            get => (double)GetValue(BreathingDurationProperty);
            set => SetValue(BreathingDurationProperty, value);
        }

        #endregion

        #region 私有字段

        private Border _lightBorder;
        private Storyboard _breathingStoryboard;
        private ColorAnimation _breathingAnimation;

        #endregion

        #region 重写方法

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _lightBorder = GetTemplateChild("PART_LightBorder") as Border;

            UpdateLightAppearance();
            SetupBreathingAnimation();

            // 初始状态检查
            ToggleBreathingAnimation();
        }

        #endregion

        #region 属性变更回调

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = (WxLED)d;
            indicator.UpdateLightAppearance();
            indicator.ToggleBreathingAnimation();
        }

        private static void OnIsBreathingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = (WxLED)d;
            indicator.ToggleBreathingAnimation();
        }

        private static void OnBreathingDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = (WxLED)d;
            indicator.UpdateBreathingAnimationDuration();
        }

        #endregion

        #region 私有方法

        private void UpdateLightAppearance()
        {
            if (_lightBorder == null) return;

            var targetColor = IsActive ? ActiveColor : InactiveColor;
            _lightBorder.Background = new SolidColorBrush(targetColor);
        }

        private void SetupBreathingAnimation()
        {
            if (_lightBorder == null) return;

            // 创建呼吸动画
            _breathingStoryboard = new Storyboard();
            _breathingStoryboard.RepeatBehavior = RepeatBehavior.Forever;
            _breathingStoryboard.AutoReverse = true;

            _breathingAnimation = new ColorAnimation
            {
                From = ActiveColor,
                To = Color.FromArgb(100, ActiveColor.R, ActiveColor.G, ActiveColor.B), // 半透明状态
                Duration = TimeSpan.FromSeconds(BreathingDuration)
            };

            // 设置动画目标
            Storyboard.SetTarget(_breathingAnimation, _lightBorder);
            Storyboard.SetTargetProperty(_breathingAnimation, new PropertyPath("(Border.Background).(SolidColorBrush.Color)"));

            _breathingStoryboard.Children.Add(_breathingAnimation);
        }

        private void UpdateBreathingAnimationDuration()
        {
            if (_breathingAnimation != null)
            {
                _breathingAnimation.Duration = TimeSpan.FromSeconds(BreathingDuration);
            }
        }

        private void ToggleBreathingAnimation()
        {
            if (_breathingStoryboard == null || _lightBorder == null) return;

            // 确保背景是SolidColorBrush
            if (!(_lightBorder.Background is SolidColorBrush))
            {
                _lightBorder.Background = new SolidColorBrush(IsActive ? ActiveColor : InactiveColor);
            }

            if (IsActive && IsBreathing)
            {
                // 更新动画颜色
                _breathingAnimation.From = ActiveColor;
                _breathingAnimation.To = Color.FromArgb(100, ActiveColor.R, ActiveColor.G, ActiveColor.B);

                _breathingStoryboard.Begin(_lightBorder, true);
            }
            else
            {
                _breathingStoryboard.Stop(_lightBorder);

                // 重置为正常颜色
                var brush = _lightBorder.Background as SolidColorBrush;
                if (brush != null)
                {
                    brush.Color = IsActive ? ActiveColor : InactiveColor;
                }
            }
        }

        #endregion
    }
}
