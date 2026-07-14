#nullable enable

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public sealed class DefectMapTypeStyle : BindableBase
    {
        private const double DefaultMarkerSize = 10d;

        private string _typeKey = "Unknown";
        private string _displayName = "Unknown";
        private string _colorText = "#22C55E";
        private double _markerSize = DefaultMarkerSize;
        private DefectMapMarkerShape _markerShape = DefectMapMarkerShape.Circle;
        private DefectMapSeverity _defaultSeverity = DefectMapSeverity.Minor;
        private bool _isEnabled = true;
        private string _description = string.Empty;
        private object? _tag;

        public string TypeKey
        {
            get => _typeKey;
            set
            {
                string normalizedValue = NormalizeName(value, "Unknown");
                SetProperty(ref _typeKey, normalizedValue);
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                string normalizedValue = NormalizeName(value, TypeKey);
                SetProperty(ref _displayName, normalizedValue);
            }
        }

        public string ColorText
        {
            get => _colorText;
            set
            {
                string normalizedValue = NormalizeColorText(value, _colorText);
                if (SetProperty(ref _colorText, normalizedValue))
                {
                    RaisePropertyChanged(nameof(MarkerColor));
                    RaisePropertyChanged(nameof(MarkerBrush));
                }
            }
        }

        public Color MarkerColor => ParseColor(ColorText, Colors.LimeGreen);

        public Brush MarkerBrush => new SolidColorBrush(MarkerColor);

        public double MarkerSize
        {
            get => _markerSize;
            set => SetProperty(ref _markerSize, NormalizeMarkerSize(value));
        }

        public DefectMapMarkerShape MarkerShape
        {
            get => _markerShape;
            set => SetProperty(ref _markerShape, value);
        }

        public DefectMapSeverity DefaultSeverity
        {
            get => _defaultSeverity;
            set => SetProperty(ref _defaultSeverity, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value ?? string.Empty);
        }

        public object? Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        public DefectMapTypeStyle Clone()
        {
            return new DefectMapTypeStyle
            {
                TypeKey = TypeKey,
                DisplayName = DisplayName,
                ColorText = ColorText,
                MarkerSize = MarkerSize,
                MarkerShape = MarkerShape,
                DefaultSeverity = DefaultSeverity,
                IsEnabled = IsEnabled,
                Description = Description,
                Tag = Tag
            };
        }

        public static DefectMapTypeStyle CreateDefault(
            string typeKey,
            string displayName,
            string colorText,
            double markerSize,
            DefectMapMarkerShape markerShape,
            DefectMapSeverity defaultSeverity,
            string description = "")
        {
            return new DefectMapTypeStyle
            {
                TypeKey = typeKey,
                DisplayName = displayName,
                ColorText = colorText,
                MarkerSize = markerSize,
                MarkerShape = markerShape,
                DefaultSeverity = defaultSeverity,
                Description = description,
                IsEnabled = true
            };
        }

        public static IReadOnlyList<DefectMapTypeStyle> CreateDefaultStyles()
        {
            return new[]
            {
                CreateDefault("BlackSpot", "Black Spot", "#EF4444", 14d, DefectMapMarkerShape.Circle, DefectMapSeverity.Critical, "Simulated black spot defect"),
                CreateDefault("Scratch", "Scratch", "#F59E0B", 12d, DefectMapMarkerShape.Cross, DefectMapSeverity.Warning, "Simulated scratch defect"),
                CreateDefault("Stain", "Stain", "#22C55E", 10d, DefectMapMarkerShape.Rectangle, DefectMapSeverity.Minor, "Simulated stain defect"),
                CreateDefault("Bubble", "Bubble", "#38BDF8", 11d, DefectMapMarkerShape.Triangle, DefectMapSeverity.Warning, "Simulated bubble defect"),
                CreateDefault("PressureMark", "Pressure Mark", "#A855F7", 13d, DefectMapMarkerShape.Flag, DefectMapSeverity.Warning, "Simulated pressure mark defect")
            };
        }

        public static string NormalizeColorText(string? value, string fallback = "#22C55E")
        {
            if (TryParseColor(value, out Color parsedColor))
            {
                return FormatColor(parsedColor);
            }

            if (TryParseColor(fallback, out parsedColor))
            {
                return FormatColor(parsedColor);
            }

            return "#22C55E";
        }

        public static bool IsSameTypeKey(string? left, string? right)
        {
            return string.Equals(
                NormalizeName(left, string.Empty),
                NormalizeName(right, string.Empty),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeName(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static double NormalizeMarkerSize(double value)
        {
            if (!double.IsFinite(value))
            {
                return DefaultMarkerSize;
            }

            return Math.Clamp(value, 4d, 48d);
        }

        private static Color ParseColor(string? value, Color fallback)
        {
            return TryParseColor(value, out Color color) ? color : fallback;
        }

        private static bool TryParseColor(string? value, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                object? converted = ColorConverter.ConvertFromString(value.Trim());
                if (converted is Color parsedColor)
                {
                    color = parsedColor;
                    return true;
                }
            }
            catch (FormatException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }

            return false;
        }

        private static string FormatColor(Color color)
        {
            return color.A == byte.MaxValue
                ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
