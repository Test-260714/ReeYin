using FileTool.BRJReportOutput.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace FileTool.BRJReportOutput.Services
{
    public sealed class BrjDefectVisualStyle : BindableBase
    {
        private bool _isVisible = true;

        public int Index { get; init; }

        public string DefectType { get; init; } = string.Empty;

        public string MarkerKind { get; init; } = string.Empty;

        public string ShapeDisplay => string.Equals(MarkerKind, "Triangle", StringComparison.OrdinalIgnoreCase)
            ? "三角"
            : string.Equals(MarkerKind, "Cross", StringComparison.OrdinalIgnoreCase)
                ? "叉号"
                : "圆点";

        public string ColorHex { get; init; } = string.Empty;

        public int Count { get; init; }

        public bool IsVisible
        {
            get { return _isVisible; }
            set { SetProperty(ref _isVisible, value); }
        }

        public Brush Fill { get; init; } = Brushes.Transparent;

        public Brush Stroke { get; init; } = Brushes.Transparent;
    }

    internal static class BrjDefectStyleResolver
    {
        private static readonly (string MarkerKind, string FillColor)[] StyleTemplates =
        {
            ("Circle", "#DC2626"),
            ("Triangle", "#EAB308"),
            ("Cross", "#111111"),
            ("Cross", "#FFFFFF"),
        };

        public static Dictionary<string, BrjDefectVisualStyle> ResolveStyles(IEnumerable<BrjDefectRecord> defects)
        {
            List<BrjDefectRecord> source = (defects ?? Enumerable.Empty<BrjDefectRecord>()).ToList();
            Dictionary<string, int> counts = source
                .GroupBy(item => NormalizeDefectType(item.DefectType), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            var styles = new Dictionary<string, BrjDefectVisualStyle>(StringComparer.OrdinalIgnoreCase);
            foreach (BrjDefectRecord defect in source)
            {
                string defectType = NormalizeDefectType(defect.DefectType);
                if (styles.ContainsKey(defectType))
                {
                    continue;
                }

                var template = StyleTemplates[styles.Count % StyleTemplates.Length];
                styles[defectType] = new BrjDefectVisualStyle
                {
                    Index = styles.Count + 1,
                    DefectType = defectType,
                    MarkerKind = template.MarkerKind,
                    ColorHex = template.FillColor,
                    Count = counts.TryGetValue(defectType, out int count) ? count : 0,
                    Fill = CreateFrozenBrush(template.FillColor),
                    Stroke = CreateFrozenBrush("#334155"),
                };
            }

            return styles;
        }

        public static BrjDefectVisualStyle ResolveStyle(IReadOnlyDictionary<string, BrjDefectVisualStyle> styles, string defectType)
        {
            string key = NormalizeDefectType(defectType);
            if (styles != null && styles.TryGetValue(key, out BrjDefectVisualStyle? style))
            {
                return style;
            }

            return new BrjDefectVisualStyle
            {
                Index = 1,
                DefectType = key,
                MarkerKind = StyleTemplates[0].MarkerKind,
                ColorHex = StyleTemplates[0].FillColor,
                Fill = CreateFrozenBrush(StyleTemplates[0].FillColor),
                Stroke = CreateFrozenBrush("#334155"),
            };
        }

        private static string NormalizeDefectType(string defectType)
        {
            return string.IsNullOrWhiteSpace(defectType) ? "Unknown" : defectType.Trim();
        }

        private static Brush CreateFrozenBrush(string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            return brush;
        }
    }
}
