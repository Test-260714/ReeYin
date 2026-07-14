using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ReeYin_V.UI.UserControls.FastDrawer
{
    public partial class FastDrawer : UserControl
    {
        public static readonly DependencyProperty IsShowProperty =
            DependencyProperty.Register("IsShow", typeof(bool), typeof(FastDrawer), new PropertyMetadata(false, IsShowChange));

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            //this.IsShow = false;
        }
        /// <summary>
        /// IsShow变为false时触发
        /// </summary>
        private static void IsShowChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var drawer = d as FastDrawer;
            if (!drawer.IsShow)
            {
                // 触发隐藏事件
                drawer.RaiseOnHideEvent();
            }
            else if (drawer.IsShow)
            {
                // 触发显示事件
                drawer.RaiseOnShowEvent();
            }
        }

        /// <summary>
        /// 是否展开显示
        /// </summary>
        public bool IsShow
        {
            get { return (bool)GetValue(IsShowProperty); }
            set { SetValue(IsShowProperty, value); }
        }

        /// <summary>
        /// 自定义的路由事件
        /// </summary>
        public static readonly RoutedEvent OnHideEvent =
            EventManager.RegisterRoutedEvent("OnHide", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FastDrawer));

        public event RoutedEventHandler OnHide
        {
            add { AddHandler(OnHideEvent, value); }
            remove { RemoveHandler(OnHideEvent, value); }
        }

        public void RaiseOnHideEvent()
        {
            RoutedEventArgs eventArgs = new RoutedEventArgs(FastDrawer.OnHideEvent);
            this.RaiseEvent(eventArgs);
        }

        // Add by Lemon
        public static readonly RoutedEvent OnShowEvent =
            EventManager.RegisterRoutedEvent("OnShow", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FastDrawer));

        public event RoutedEventHandler OnShow
        {
            add { AddHandler(OnShowEvent, value); }
            remove { RemoveHandler(OnShowEvent, value); }
        }

        public void RaiseOnShowEvent()
        {
            RoutedEventArgs eventArgs = new RoutedEventArgs(FastDrawer.OnShowEvent);
            this.RaiseEvent(eventArgs);
        }
    }

    public class MultiplyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
               object parameter, CultureInfo culture)
        {
            double result = 1.0;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is double)
                    result *= (double)values[i];
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes,
               object parameter, CultureInfo culture)
        {
            throw new Exception("Not implemented");
        }
    }
}
