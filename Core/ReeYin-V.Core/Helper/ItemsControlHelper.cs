using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace ReeYin_V.Core.Helper
{
    public static class ItemsControlHelper
    {
        public static readonly DependencyProperty EnumValuesToItemsSourceProperty =
            DependencyProperty.RegisterAttached(
                "EnumValuesToItemsSource",
                typeof(bool),
                typeof(ItemsControlHelper),
                new PropertyMetadata(false, OnEnumValuesToItemsSourceChanged));

        public static void SetEnumValuesToItemsSource(DependencyObject element, bool value)
            => element.SetValue(EnumValuesToItemsSourceProperty, value);

        public static bool GetEnumValuesToItemsSource(DependencyObject element)
            => (bool)element.GetValue(EnumValuesToItemsSourceProperty);

        /// <summary>
        /// 可选：显式指定枚举类型（最稳，不依赖 SelectedItem 推断）
        /// 用法：helper:ItemsControlHelper.EnumType="{x:Type local:MyEnum}"
        /// </summary>
        public static readonly DependencyProperty EnumTypeProperty =
            DependencyProperty.RegisterAttached(
                "EnumType",
                typeof(Type),
                typeof(ItemsControlHelper),
                new PropertyMetadata(null, OnEnumTypeChanged));

        public static void SetEnumType(DependencyObject element, Type value)
            => element.SetValue(EnumTypeProperty, value);

        public static Type GetEnumType(DependencyObject element)
            => (Type)element.GetValue(EnumTypeProperty);

        // 内部标记：避免重复绑定
        private static readonly DependencyProperty IsEnumItemsSourceAppliedProperty =
            DependencyProperty.RegisterAttached(
                "IsEnumItemsSourceApplied",
                typeof(bool),
                typeof(ItemsControlHelper),
                new PropertyMetadata(false));

        private static void SetIsEnumItemsSourceApplied(DependencyObject element, bool value)
            => element.SetValue(IsEnumItemsSourceAppliedProperty, value);

        private static bool GetIsEnumItemsSourceApplied(DependencyObject element)
            => (bool)element.GetValue(IsEnumItemsSourceAppliedProperty);

        private static void OnEnumTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemsControl ic && GetEnumValuesToItemsSource(ic))
                TryApply(ic);
        }

        private static void OnEnumValuesToItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ItemsControl itemsControl) return;

            bool enabled = (bool)e.NewValue;

            if (enabled)
            {
                // Loaded 时尝试一次
                itemsControl.Loaded += ItemsControl_Loaded;

                // DataContext 变化时再尝试（解决“第二次打开/虚拟化复用”）
                itemsControl.DataContextChanged += ItemsControl_DataContextChanged;

                // 如果已经 Loaded，立即尝试
                if (itemsControl.IsLoaded)
                    TryApply(itemsControl);
            }
            else
            {
                itemsControl.Loaded -= ItemsControl_Loaded;
                itemsControl.DataContextChanged -= ItemsControl_DataContextChanged;
                SetIsEnumItemsSourceApplied(itemsControl, false);
            }
        }

        private static void ItemsControl_Loaded(object sender, RoutedEventArgs e)
        {
            TryApply((ItemsControl)sender);
        }

        private static void ItemsControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TryApply((ItemsControl)sender);
        }

        private static void TryApply(ItemsControl itemsControl)
        {
            if (!GetEnumValuesToItemsSource(itemsControl)) return;
            if (GetIsEnumItemsSourceApplied(itemsControl)) return;

            // ItemsControlHelper 仅对 Selector（ComboBox/ListBox等）有意义
            if (itemsControl is not Selector)
                return;

            // 如果用户已经手动绑定 ItemsSource，则不干预
            if (BindingOperations.GetBinding(itemsControl, ItemsControl.ItemsSourceProperty) != null)
                return;

            // 如果 Items 集合已被手动填充，则不干预
            if (itemsControl.Items.Count > 0)
                return;

            // 1) 优先使用显式指定的 EnumType
            var enumType = GetEnumType(itemsControl);
            if (enumType != null)
            {
                enumType = Nullable.GetUnderlyingType(enumType) ?? enumType;
                if (enumType.IsEnum)
                {
                    ApplyEnumItemsSource(itemsControl, enumType);
                    return;
                }
            }

            // 2) 从 SelectedItem 的绑定“已解析源”推断类型（更稳）
            var be = BindingOperations.GetBindingExpression(itemsControl, Selector.SelectedItemProperty);
            if (be == null) return;

            // 关键：ResolvedSource/ResolvedSourcePropertyName 只有在绑定解析后才有值
            var src = be.ResolvedSource;
            var propName = be.ResolvedSourcePropertyName;

            if (src == null || string.IsNullOrWhiteSpace(propName))
                return;

            var prop = src.GetType().GetProperty(propName);
            if (prop == null) return;

            var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (!t.IsEnum) return;

            ApplyEnumItemsSource(itemsControl, t);
        }

        private static void ApplyEnumItemsSource(ItemsControl itemsControl, Type enumType)
        {
            var binding = new Binding
            {
                Source = Enum.GetValues(enumType),
                Mode = BindingMode.OneWay
            };

            itemsControl.SetBinding(ItemsControl.ItemsSourceProperty, binding);
            SetIsEnumItemsSourceApplied(itemsControl, true);
        }
    }
}