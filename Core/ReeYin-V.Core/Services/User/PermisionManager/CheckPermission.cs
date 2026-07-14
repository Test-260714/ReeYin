using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ReeYin_V.Core.Services
{
    public static class CheckPermission
    {
        public static readonly DependencyProperty RequiredProperty =
            DependencyProperty.RegisterAttached(
                "Required",
                typeof(string),
                typeof(CheckPermission),
                new PropertyMetadata(null, OnRequiredChanged));

        public static string GetRequired(DependencyObject obj) =>
            (string)obj.GetValue(RequiredProperty);

        public static void SetRequired(DependencyObject obj, string value) =>
            obj.SetValue(RequiredProperty, value);

        private static void OnRequiredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                string perm = e.NewValue as string;

                // 第一次加载时检查权限
                ApplyPermission(element, perm);

                // 权限变化时重新检查
                PermissionManager.PermissionChanged += () =>
                {
                    ApplyPermission(element, perm);
                };

                // 拦截按钮点击
                if (element is Button btn)
                {
                    btn.Click += (s, ev) =>
                    {
                        if (!PermissionManager.Has(perm))
                        {
                            ev.Handled = true;
                            MessageBox.Show($"无权限执行：{perm}");
                        }
                    };
                }
            }
        }

        private static void ApplyPermission(UIElement element, string perm)
        {
            bool hasPerm = PermissionManager.Has(perm);
            element.IsEnabled = hasPerm;
        }
    }

}
