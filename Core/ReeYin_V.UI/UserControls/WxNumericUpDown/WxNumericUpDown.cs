using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.WxNumericUpDown
{
    public class WxNumericUpDown : ScrollBar
    {
        private Border _inputBorder;
        private Button _buttonDown;
        private Button _buttonUp;
        private TextBox _inputTextBox;

        static WxNumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WxNumericUpDown), new FrameworkPropertyMetadata(typeof(WxNumericUpDown)));
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
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(WxNumericUpDown), new PropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// 左侧标题
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(WxNumericUpDown), new PropertyMetadata(null));

        /// <summary>
        /// 左侧标题宽度
        /// </summary>
        public double TitleWidth
        {
            get => (double)GetValue(TitleWidthProperty);
            set => SetValue(TitleWidthProperty, value);
        }

        public static readonly DependencyProperty TitleWidthProperty =
            DependencyProperty.Register(nameof(TitleWidth), typeof(double), typeof(WxNumericUpDown), new PropertyMetadata(double.NaN));

        /// <summary>
        /// 左侧标题背景色
        /// </summary>
        public Brush TitleBackground
        {
            get => (Brush)GetValue(TitleBackgroundProperty);
            set => SetValue(TitleBackgroundProperty, value);
        }

        public static readonly DependencyProperty TitleBackgroundProperty =
            DependencyProperty.Register(nameof(TitleBackground), typeof(Brush), typeof(WxNumericUpDown), new PropertyMetadata(Brushes.Transparent));

        #region 1. 权限相关依赖属性（保持不变）
        public int AuthorityLevel
        {
            get => (int)GetValue(AuthorityLevelProperty);
            set => SetValue(AuthorityLevelProperty, value);
        }

        public static readonly DependencyProperty AuthorityLevelProperty =
        DependencyProperty.Register("AuthorityLevel", typeof(int), typeof(WxNumericUpDown), new PropertyMetadata(1, OnAuthorityLevelChanged));

        private static void OnAuthorityLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = d as WxNumericUpDown;
            var minLevel = (int)obj.GetValue(AuthorityLevelMinProperty);
            obj.IsEnabled = (int)e.NewValue >= minLevel;
        }

        public int AuthorityLevelMin
        {
            get => (int)GetValue(AuthorityLevelMinProperty);
            set => SetValue(AuthorityLevelMinProperty, value);
        }

        public static readonly DependencyProperty AuthorityLevelMinProperty =
        DependencyProperty.Register("AuthorityLevelMin", typeof(int), typeof(WxNumericUpDown), new PropertyMetadata(1, OnAuthorityLevelMinChanged));

        private static void OnAuthorityLevelMinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = d as WxNumericUpDown;
            var currentLevel = (int)obj.GetValue(AuthorityLevelProperty);
            obj.IsEnabled = currentLevel >= (int)e.NewValue;
        }
        #endregion

        #region 2. 数值与精度相关依赖属性（优化 Value 的 set 逻辑）
        /// <summary>
        /// 数据精度（小数位数）
        /// </summary>
        public int Precision
        {
            get => (int)GetValue(PrecisionProperty);
            set => SetValue(PrecisionProperty, Math.Clamp(value, 0, 15)); // 限制精度范围（0-15 位）
        }

        public static readonly DependencyProperty PrecisionProperty =
        DependencyProperty.Register("Precision", typeof(int), typeof(WxNumericUpDown), new PropertyMetadata(2, OnPrecisionChanged));

        private static void OnPrecisionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = d as WxNumericUpDown;
            obj.Value = Math.Round(obj.Value, (int)e.NewValue); // 精度变化时重新格式化值
            obj.SyncInputBoxText(); // 同步更新输入框显示
        }

        /// <summary>
        /// 数值（double 类型，优化 set 逻辑：依赖 CoerceValue 而非手动判断）
        /// </summary>
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value); // 移除手动范围判断，完全依赖 CoerceValue
        }

        public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(double), typeof(WxNumericUpDown),
        new PropertyMetadata(0.0, OnValueChanged, CoerceValue));

        // 值变化时同步更新输入框显示
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = d as WxNumericUpDown;
            obj.SyncInputBoxText(); // 确保 UI 与实际值一致
            obj.RaiseEvent(new RoutedPropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue, RangeBase.ValueChangedEvent));
        }

        // 核心：强制值在 [Minimum, Maximum] 范围内并按精度四舍五入（所有值修改都会经过此逻辑）
        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            var obj = d as WxNumericUpDown;
            double value = (double)baseValue;

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                value = obj.Minimum;
            }

            // 1. 范围限制：强制值在 Minimum 和 Maximum 之间
            value = Math.Clamp(value, obj.Minimum, obj.Maximum);
            // 2. 精度处理：按当前精度四舍五入
            value = Math.Round(value, obj.Precision);

            return value;
        }

        /// <summary>
        /// 最小值（double 类型）
        /// </summary>
        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register("Minimum", typeof(double), typeof(WxNumericUpDown),
        new PropertyMetadata(0.0, OnMinMaxChanged));

        /// <summary>
        /// 最大值（double 类型）
        /// </summary>
        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(double), typeof(WxNumericUpDown),
        new PropertyMetadata(100.0, OnMinMaxChanged));

        // 最大 / 最小值变化时，强制更新当前值并同步 UI
        private static void OnMinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = d as WxNumericUpDown;

            if (obj.Minimum > obj.Maximum)
            {
                if (e.Property == MinimumProperty)
                    obj.Maximum = obj.Minimum;
                else
                    obj.Minimum = obj.Maximum;
            }

            obj.CoerceValue(ValueProperty); // 重新校验当前值是否超限
            obj.SyncInputBoxText(); // 同步输入框显示
        }

        // 小步长、大步长（保持不变）
        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public static readonly DependencyProperty SmallChangeProperty =
        DependencyProperty.Register("SmallChange", typeof(double), typeof(WxNumericUpDown), new PropertyMetadata(0.1));

        public double LargeChange
        {
            get => (double)GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        public static readonly DependencyProperty LargeChangeProperty =
        DependencyProperty.Register("LargeChange", typeof(double), typeof(WxNumericUpDown), new PropertyMetadata(1.0));
        #endregion

        #region 3. 模板应用与事件绑定（核心：处理输入框逻辑）
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 解绑旧事件（避免多次绑定导致重复触发）
            UnbindOldEvents();

            _buttonUp = GetTemplateChild("PART_ButtonUp") as Button;
            if (_buttonUp != null)
                _buttonUp.Click += BtnUp_Click;

            _buttonDown = GetTemplateChild("PART_ButtonDown") as Button;
            if (_buttonDown != null)
                _buttonDown.Click += BtnDown_Click;

            _inputBorder = GetTemplateChild("PART_Border") as Border;
            if (_inputBorder != null)
                _inputBorder.MouseWheel += Border_MouseWheel;

            // 2. 绑定输入框（关键：获取模板中的输入框并处理输入逻辑）
            if (GetTemplateChild("PART_InputTextBox") is TextBox inputTextBox)
            {
                _inputTextBox = inputTextBox;
                // 输入框事件：失去焦点、按回车时校验值
                _inputTextBox.LostFocus += InputTextBox_LostFocus;
                _inputTextBox.GotKeyboardFocus += InputTextBox_GotKeyboardFocus;
                _inputTextBox.PreviewMouseLeftButtonDown += InputTextBox_PreviewMouseLeftButtonDown;
                _inputTextBox.KeyDown += InputTextBox_KeyDown;
                // 初始同步显示：确保输入框默认值正确
                SyncInputBoxText();
            }
        }

        // 解绑旧事件：避免模板重新应用时重复绑定
        private void UnbindOldEvents()
        {
            if (_buttonUp != null)
            {
                _buttonUp.Click -= BtnUp_Click;
                _buttonUp = null;
            }

            if (_buttonDown != null)
            {
                _buttonDown.Click -= BtnDown_Click;
                _buttonDown = null;
            }

            if (_inputBorder != null)
            {
                _inputBorder.MouseWheel -= Border_MouseWheel;
                _inputBorder = null;
            }

            if (_inputTextBox != null)
            {
                _inputTextBox.LostFocus -= InputTextBox_LostFocus;
                _inputTextBox.GotKeyboardFocus -= InputTextBox_GotKeyboardFocus;
                _inputTextBox.PreviewMouseLeftButtonDown -= InputTextBox_PreviewMouseLeftButtonDown;
                _inputTextBox.KeyDown -= InputTextBox_KeyDown;
                _inputTextBox = null;
            }
        }

        // 输入框失去焦点：校验输入值
        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateAndUpdateInputValue();
        }

        private void InputTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _inputTextBox?.SelectAll();
        }

        private void InputTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_inputTextBox == null || _inputTextBox.IsKeyboardFocusWithin)
            {
                return;
            }

            e.Handled = true;
            _inputTextBox.Focus();
        }

        // 输入框按回车：校验输入值（增强用户体验）
        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    ValidateAndUpdateInputValue();
                    // 按回车后失去焦点（避免光标停留）
                    Keyboard.ClearFocus();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    SyncInputBoxText();
                    _inputTextBox?.SelectAll();
                    e.Handled = true;
                    break;
                case Key.Up:
                    ValidateAndUpdateInputValue();
                    StepValue(SmallChange);
                    e.Handled = true;
                    break;
                case Key.Down:
                    ValidateAndUpdateInputValue();
                    StepValue(-SmallChange);
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    ValidateAndUpdateInputValue();
                    StepValue(LargeChange);
                    e.Handled = true;
                    break;
                case Key.PageDown:
                    ValidateAndUpdateInputValue();
                    StepValue(-LargeChange);
                    e.Handled = true;
                    break;
            }
        }

        // 核心：校验输入值并更新（处理超限、格式错误）
        private void ValidateAndUpdateInputValue()
        {
            if (_inputTextBox == null) return;

            string inputText = _inputTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(inputText))
            {
                SyncInputBoxText();
                return;
            }

            // 1. 尝试解析输入为 double（支持不同文化格式，如小数点 / 千分位）
            if (TryParseValue(inputText, out double parsedValue))
            {
                // 2. 直接赋值：CoerceValue 会自动处理范围和精度
                ClearInputError();
                Value = parsedValue;
            }
            else
            {
                // 3. 解析失败（如输入非数字）：给出明显错误态，方便用户直接重输
                SetInputError("输入格式错误，请输入数字。");
            }
        }

        // 同步输入框文本与当前 Value（确保显示与实际值一致）
        private void SyncInputBoxText()
        {
            if (_inputTextBox == null) return;

            // 按当前精度格式化显示（如精度 2 则显示 “123.45”）
            string format = Precision > 0 ? $"F{Precision}" : "F0";
            string displayText = Value.ToString(format, CultureInfo.CurrentCulture);

            if (!string.Equals(_inputTextBox.Text, displayText, StringComparison.Ordinal))
            {
                _inputTextBox.Text = displayText;
            }

            ClearInputError();
        }

        private static bool TryParseValue(string inputText, out double parsedValue)
        {
            return double.TryParse(inputText, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out parsedValue)
                || double.TryParse(inputText, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsedValue);
        }

        private void SetInputError(string message)
        {
            if (_inputTextBox == null)
            {
                return;
            }

            _inputTextBox.ToolTip = message;
            _inputTextBox.Foreground = Brushes.Red;
            _inputTextBox.SelectAll();
        }

        private void ClearInputError()
        {
            if (_inputTextBox == null)
            {
                return;
            }

            _inputTextBox.ToolTip = null;
            _inputTextBox.ClearValue(Control.ForegroundProperty);
        }
        #endregion

        #region 4. 原有交互逻辑（保持不变）
        protected virtual void OnLostFocus(RoutedEventArgs e)
        {
            // 此处逻辑可保留，CoerceValue 已确保值不超限
            Value = Value;
        }

        private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ValidateAndUpdateInputValue();
            StepValue(e.Delta > 0 ? SmallChange : -SmallChange);
            e.Handled = true;
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndUpdateInputValue();
            StepValue(-SmallChange);
        }

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndUpdateInputValue();
            StepValue(SmallChange);
        }

        private void StepValue(double delta)
        {
            Value += delta; // CoerceValue 自动处理范围
            SyncInputBoxText();
            _inputTextBox?.Focus();
            _inputTextBox?.SelectAll();
        }
        #endregion
    }
}
