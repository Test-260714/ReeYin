using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReeYin_V.Login.Views
{
    public class PasswordBindingBehavior : Behavior<PasswordBox>
    {
        // 定义依赖属性，用于绑定到ViewModel的密码属性
        public string BoundPassword
        {
            get => (string)GetValue(BoundPasswordProperty);
            set => SetValue(BoundPasswordProperty, value);
        }

        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.Register("BoundPassword", typeof(string), typeof(PasswordBindingBehavior),
                new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public ICommand PasswordChangedCommand
        {
            get => (ICommand)GetValue(PasswordChangedCommandProperty);
            set => SetValue(PasswordChangedCommandProperty, value);
        }

        public static readonly DependencyProperty PasswordChangedCommandProperty =
            DependencyProperty.Register("PasswordChangedCommand", typeof(ICommand), typeof(PasswordBindingBehavior),
                new PropertyMetadata(null));

        protected override void OnAttached()
        {
            base.OnAttached();
            // 此时 AssociatedObject 已赋值，可安全访问
            if (AssociatedObject == null)
            {
                throw new InvalidOperationException("行为未正确附加到PasswordBox控件");
            }
            // 注册事件（必须在OnAttached中执行）
            AssociatedObject.PasswordChanged += OnPasswordChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            // 分离时取消事件，避免内存泄漏
            if (AssociatedObject != null)
            {
                AssociatedObject.PasswordChanged -= OnPasswordChanged;
            }
        }

        // 其他方法中访问前先判断是否为null
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject == null) return;
            BoundPassword = AssociatedObject.Password;
            PasswordChangedCommand?.Execute(null);
        }

        // 依赖属性变化时，先判断AssociatedObject是否有效
        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBindingBehavior behavior && e.NewValue is string newPassword)
            {
                // 关键：访问前必须检查AssociatedObject是否为null
                if (behavior.AssociatedObject != null && behavior.AssociatedObject.Password != newPassword)
                {
                    behavior.AssociatedObject.Password = newPassword;
                }
            }
        }
    }
}
