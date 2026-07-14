using DryIoc.ImTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReeYin_V.UI.Helper
{
    public class DataGridHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetNoDataTips(DependencyObject obj)
        {
            return (string)obj.GetValue(NoDataTipsProperty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetNoDataTips(DependencyObject obj, string value)
        {
            obj.SetValue(NoDataTipsProperty, value);
        }

        public static readonly DependencyProperty NoDataTipsProperty =
            DependencyProperty.RegisterAttached("NoDataTips", typeof(string), typeof(DataGridHelper));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool GetIsActive(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsActiveProperty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetIsActive(DependencyObject obj, bool value)
        {
            obj.SetValue(IsActiveProperty, value);
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.RegisterAttached("IsActive", typeof(bool), typeof(DataGridHelper));

    }

    public static class DataGridRowNumberBehavior
    {
        #region Public Attached Properties
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(DataGridRowNumberBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
        public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.RegisterAttached(
                "Header",
                typeof(string),
                typeof(DataGridRowNumberBehavior),
                new PropertyMetadata("序号", OnOptionsChanged));

        public static void SetHeader(DependencyObject element, string value) => element.SetValue(HeaderProperty, value);
        public static string GetHeader(DependencyObject element) => (string)element.GetValue(HeaderProperty);

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.RegisterAttached(
                "Width",
                typeof(double),
                typeof(DataGridRowNumberBehavior),
                new PropertyMetadata(60d, OnOptionsChanged));

        public static void SetWidth(DependencyObject element, double value) => element.SetValue(WidthProperty, value);
        public static double GetWidth(DependencyObject element) => (double)element.GetValue(WidthProperty);

        public static readonly DependencyProperty StartIndexProperty =
            DependencyProperty.RegisterAttached(
                "StartIndex",
                typeof(int),
                typeof(DataGridRowNumberBehavior),
                new PropertyMetadata(1, OnOptionsChanged));

        public static void SetStartIndex(DependencyObject element, int value) => element.SetValue(StartIndexProperty, value);
        public static int GetStartIndex(DependencyObject element) => (int)element.GetValue(StartIndexProperty);

        /// <summary>
        /// 行号显示格式，默认 "{0}"。XAML 要写成 "{}{0}" 才能显示。
        /// </summary>
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.RegisterAttached(
                "Format",
                typeof(string),
                typeof(DataGridRowNumberBehavior),
                new PropertyMetadata("{0}", OnOptionsChanged));

        public static void SetFormat(DependencyObject element, string value) => element.SetValue(FormatProperty, value);
        public static string GetFormat(DependencyObject element) => (string)element.GetValue(FormatProperty);

        public static readonly DependencyProperty FreezeProperty =
            DependencyProperty.RegisterAttached(
                "Freeze",
                typeof(bool),
                typeof(DataGridRowNumberBehavior),
                new PropertyMetadata(true, OnOptionsChanged));

        public static void SetFreeze(DependencyObject element, bool value) => element.SetValue(FreezeProperty, value);
        public static bool GetFreeze(DependencyObject element) => (bool)element.GetValue(FreezeProperty);
        #endregion

        #region Internal Marker Attached Property
        private static readonly DependencyProperty IsRowNumberColumnProperty =
            DependencyProperty.RegisterAttached(
                "IsRowNumberColumn",
                typeof(bool),
                typeof(DataGridRowNumberBehavior),
                new PropertyMetadata(false));

        private static void SetIsRowNumberColumn(DependencyObject obj, bool value) => obj.SetValue(IsRowNumberColumnProperty, value);
        private static bool GetIsRowNumberColumn(DependencyObject obj) => (bool)obj.GetValue(IsRowNumberColumnProperty);
        #endregion

        private sealed class Bag
        {
            public DataGridTemplateColumn? Column;
            public int OriginalFrozenColumnCount;
            public int OriginalAlternationCount;

            public NotifyCollectionChangedEventHandler? ColumnsChanged;
            public NotifyCollectionChangedEventHandler? ItemsChanged;

            public RowNumberMultiConverter Converter = new RowNumberMultiConverter();
        }

        private static readonly ConditionalWeakTable<DataGrid, Bag> _bags = new();

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid dg) return;

            if ((bool)e.NewValue) Attach(dg);
            else Detach(dg);
        }

        private static void OnOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid dg) return;
            if (!GetEnable(dg)) return;

            ApplyOrUpdate(dg);
        }

        private static void Attach(DataGrid dg)
        {
            if (_bags.TryGetValue(dg, out _))
                return;

            var bag = new Bag
            {
                OriginalFrozenColumnCount = dg.FrozenColumnCount,
                OriginalAlternationCount = dg.AlternationCount
            };

            // Items 变化时（增删/移动），调整 AlternationCount，避免 int.MaxValue 异常序号
            bag.ItemsChanged = (s, e) =>
            {
                EnsureAlternationCount(dg);
                // 重排（Move）也会进来，保证触发刷新
                dg.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyOrUpdate(dg);
                }), DispatcherPriority.Loaded);
            };
            ((INotifyCollectionChanged)dg.Items).CollectionChanged += bag.ItemsChanged;

            bag.ColumnsChanged = (s, e) =>
            {
                EnsureRowNumberColumn(dg, bag);
                ApplyOrUpdate(dg);
            };
            dg.Columns.CollectionChanged += bag.ColumnsChanged;

            _bags.Add(dg, bag);

            EnsureAlternationCount(dg);
            EnsureRowNumberColumn(dg, bag);
            ApplyOrUpdate(dg);

            dg.Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureAlternationCount(dg);
                EnsureRowNumberColumn(dg, bag);
                ApplyOrUpdate(dg);
            }), DispatcherPriority.Loaded);
        }

        private static void Detach(DataGrid dg)
        {
            if (!_bags.TryGetValue(dg, out var bag))
                return;

            if (bag.ColumnsChanged != null)
                dg.Columns.CollectionChanged -= bag.ColumnsChanged;

            if (bag.ItemsChanged != null)
                ((INotifyCollectionChanged)dg.Items).CollectionChanged -= bag.ItemsChanged;

            if (bag.Column != null && dg.Columns.Contains(bag.Column))
                dg.Columns.Remove(bag.Column);

            dg.FrozenColumnCount = bag.OriginalFrozenColumnCount;
            dg.AlternationCount = bag.OriginalAlternationCount;

            _bags.Remove(dg);
        }

        private static void EnsureAlternationCount(DataGrid dg)
        {
            // 给一个“刚好够用”的 AlternationCount：保证 AlternationIndex 不重复且不会出现 int.MaxValue 这种怪值
            // +1 是为了兼容 CanUserAddRows 时可能出现的新行占位
            int desired = Math.Max(1, dg.Items.Count + 1);

            // 避免极端情况下溢出
            if (desired < 0) desired = int.MaxValue - 1;

            if (dg.AlternationCount != desired)
                dg.AlternationCount = desired;
        }

        private static void EnsureRowNumberColumn(DataGrid dg, Bag bag)
        {
            if (bag.Column != null && !dg.Columns.Contains(bag.Column))
                bag.Column = null;

            if (bag.Column == null)
            {
                foreach (var col in dg.Columns)
                {
                    if (GetIsRowNumberColumn(col))
                    {
                        bag.Column = col as DataGridTemplateColumn;
                        break;
                    }
                }
            }

            if (bag.Column == null)
            {
                var col = CreateRowNumberColumn(dg, bag);
                dg.Columns.Insert(0, col);
                bag.Column = col;
            }
            else
            {
                int idx = dg.Columns.IndexOf(bag.Column);
                if (idx > 0)
                {
                    dg.Columns.Remove(bag.Column);
                    dg.Columns.Insert(0, bag.Column);
                }
            }
        }

        private static DataGridTemplateColumn CreateRowNumberColumn(DataGrid dg, Bag bag)
        {
            bag.Converter = new RowNumberMultiConverter
            {
                StartIndex = GetStartIndex(dg),
                Format = GetFormat(dg) ?? "{0}"
            };

            var factory = new FrameworkElementFactory(typeof(TextBlock));
            factory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            factory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            // 关键：用 MultiBinding
            // - binding[0] 绑定到 DataGridRow 本身，用 row.GetIndex() 计算真实序号（最可靠）
            // - binding[1] 绑定 AlternationIndex 只是为了在重排/虚拟化变化时触发刷新
            var mb = new MultiBinding
            {
                Converter = bag.Converter
            };
            mb.Bindings.Add(new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1),
                Path = new PropertyPath(".")
            });
            mb.Bindings.Add(new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1),
                Path = new PropertyPath("(ItemsControl.AlternationIndex)")
            });

            factory.SetBinding(TextBlock.TextProperty, mb);

            var template = new DataTemplate { VisualTree = factory };

            var col = new DataGridTemplateColumn
            {
                Header = GetHeader(dg),
                Width = new DataGridLength(GetWidth(dg)),
                CellTemplate = template,
                IsReadOnly = true,
                CanUserReorder = false,
                CanUserResize = false,
                CanUserSort = false
            };

            SetIsRowNumberColumn(col, true);
            return col;
        }

        private static void ApplyOrUpdate(DataGrid dg)
        {
            if (!_bags.TryGetValue(dg, out var bag)) return;

            EnsureAlternationCount(dg);
            EnsureRowNumberColumn(dg, bag);

            if (bag.Column != null)
            {
                bag.Column.Header = GetHeader(dg);
                bag.Column.Width = new DataGridLength(GetWidth(dg));
            }

            bag.Converter.StartIndex = GetStartIndex(dg);
            bag.Converter.Format = GetFormat(dg) ?? "{0}";

            if (GetFreeze(dg))
            {
                int desired = Math.Max(1, bag.OriginalFrozenColumnCount + 1);
                if (dg.FrozenColumnCount < desired)
                    dg.FrozenColumnCount = desired;
            }
            else
            {
                // 可选：取消 Freeze 时尽量恢复
                if (dg.FrozenColumnCount == bag.OriginalFrozenColumnCount + 1)
                    dg.FrozenColumnCount = bag.OriginalFrozenColumnCount;
            }
        }

        private sealed class RowNumberMultiConverter : IMultiValueConverter
        {
            public int StartIndex { get; set; } = 1;
            public string Format { get; set; } = "{0}";

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                // values[0] = DataGridRow, values[1] = AlternationIndex(触发刷新用，可忽略)
                if (values == null || values.Length == 0) return string.Empty;

                if (values[0] is not DataGridRow row) return string.Empty;

                int idx = row.GetIndex(); // 0-based
                if (idx < 0) return string.Empty;

                int n = idx + StartIndex;
                try
                {
                    return string.Format(Format ?? "{0}", n);
                }
                catch
                {
                    // 防止 Format 写错导致异常
                    return n.ToString(culture);
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
