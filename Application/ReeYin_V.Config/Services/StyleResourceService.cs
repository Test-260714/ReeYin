using ReeYin_V.Config.Models;
using ReeYin_V.Core.Services.Theme;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace ReeYin_V.Config.Services
{
    public sealed class StyleResourceService
    {
        private static readonly XNamespace PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        private static readonly XNamespace SystemNamespace = "clr-namespace:System;assembly=mscorlib";

        private static readonly IReadOnlyDictionary<string, string> ResourceDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PrimaryColor"] = "主色",
            ["TitleBarColor"] = "标题栏颜色",
            ["DarkPrimaryColor"] = "深主色",
            ["SecondaryColor"] = "辅助色",
            ["ColorYellowish"] = "黄色系边框/强调色",
            ["TitleForegroundColor"] = "标题文字色",
            ["TextColor"] = "正文文字色",
            ["MainTopTextBlockColor"] = "顶部文字色",
            ["MainPrimaryTextColor"] = "主区域文字色",
            ["MainPrimaryTextIconColor"] = "主区域图标文字色",
            ["BackgroudColor"] = "基础背景色",
            ["MainBackgroundColor"] = "主背景色",
            ["RegionBackgroundColor"] = "区域背景色",
            ["MenuBackgroundColor"] = "菜单背景色",
            ["ExpanderHeaderBackgroundColor"] = "折叠面板标题背景色",
            ["ExpanderPanelBackgroundColor"] = "折叠面板内容背景色",
            ["MainBorderColor"] = "主边框色",
            ["ChartBorderColor"] = "图表边框色",
            ["RegionSideColor"] = "区域分隔线色",
            ["HoverColor"] = "悬停色",
            ["SplitColor"] = "分割线色",
            ["SelectedColor"] = "选中色",
            ["HeaderFontSize"] = "一级标题字号",
            ["TitleFontSize"] = "标题字号",
            ["SubTitleFontSize"] = "副标题字号",
            ["DefaultFontSize"] = "默认字号",
            ["SmallFontSize"] = "小字号",
            ["IconFontSize"] = "图标字号"
        };

        private static readonly IReadOnlyDictionary<string, string[]> BrushDependencies = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["PrimaryColor"] = new[] { "PrimaryBrush", "CPrimaryBrush", "SelectedBrush", "BtnOverColor", "BtnCheckedColor", "DarkPrimaryBrush" },
            ["TitleBarColor"] = new[] { "TitleBarBrush" },
            ["SecondaryColor"] = new[] { "SecondaryBrush" },
            ["ColorYellowish"] = new[] { "BrushYellowish" },
            ["TitleForegroundColor"] = new[] { "TitleForegroundBrush" },
            ["TextColor"] = new[] { "TextBrush" },
            ["MainPrimaryTextColor"] = new[] { "MainPrimaryTextBrush" },
            ["MainPrimaryTextIconColor"] = new[] { "MainPrimaryTextIconBrush" },
            ["BackgroudColor"] = new[] { "BackgroudBrush" },
            ["MainBackgroundColor"] = new[] { "MainBackgroundBrush" },
            ["RegionBackgroundColor"] = new[] { "RegionBackgroundBrush" },
            ["MenuBackgroundColor"] = new[] { "MenuBackgroundBrush" },
            ["ExpanderHeaderBackgroundColor"] = new[] { "ExpanderHeaderBackgroundBrush" },
            ["ExpanderPanelBackgroundColor"] = new[] { "ExpanderPanelBackgroundBrush" },
            ["MainBorderColor"] = new[] { "MainTopSepBrush" },
            ["RegionSideColor"] = new[] { "RegionSideBrush" },
            ["HoverColor"] = new[] { "HoverBrush" },
            ["SplitColor"] = new[] { "SplitBrush" }
        };

        private static readonly ISet<string> HiddenResourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ReeYinPrimaryColor"
        };

        private static readonly IReadOnlyDictionary<string, string[]> ColorAliasDependencies = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["PrimaryColor"] = new[] { "ReeYinPrimaryColor" }
        };

        private static readonly IReadOnlyDictionary<string, string> LightDefaultColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PrimaryColor"] = "#BF9000",
            ["TitleBarColor"] = "#BF9000",
            ["DarkPrimaryColor"] = "#00BF66",
            ["SecondaryColor"] = "#03DAC5",
            ["ColorYellowish"] = "#DFC798",
            ["TitleForegroundColor"] = "#ffffff",
            ["TextColor"] = "#000000",
            ["MainTopTextBlockColor"] = "#3B3B3B",
            ["MainPrimaryTextColor"] = "#ffffff",
            ["MainPrimaryTextIconColor"] = "#A2A4B1",
            ["BackgroudColor"] = "Transparent",
            ["MainBackgroundColor"] = "#F5F5F5",
            ["RegionBackgroundColor"] = "#FFFFFF",
            ["MenuBackgroundColor"] = "#F5F5F5",
            ["ExpanderHeaderBackgroundColor"] = "#E0E1E1",
            ["ExpanderPanelBackgroundColor"] = "#E9EAE9",
            ["MainBorderColor"] = "#E1E0E0",
            ["ChartBorderColor"] = "#E1E0E0",
            ["RegionSideColor"] = "#A4A3A3",
            ["HoverColor"] = "#E8E8E8",
            ["SplitColor"] = "#E8E8E8",
            ["SelectedColor"] = "#BF9000",
            ["BrushTransparent"] = "#22FFFFFF",
            ["ExpanderHeaderForegroundBrush"] = "#313233",
            ["SplitBrush"] = "#E8E8E8",
            ["BtnPressedColor"] = "#FFFFFF",
            ["BtnBackgroundColor"] = "#A2A4B1"
        };

        private static readonly IReadOnlyDictionary<string, string> DarkDefaultColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PrimaryColor"] = "#BF9000",
            ["TitleBarColor"] = "#BF9000",
            ["DarkPrimaryColor"] = "#BF9000",
            ["SecondaryColor"] = "#ffffff",
            ["ColorYellowish"] = "#DFC798",
            ["TitleForegroundColor"] = "#ffffff",
            ["TextColor"] = "#ffffff",
            ["MainTopTextBlockColor"] = "#3B3B3B",
            ["MainPrimaryTextColor"] = "#323232",
            ["MainPrimaryTextIconColor"] = "#A2A4B1",
            ["BackgroudColor"] = "Transparent",
            ["MainBackgroundColor"] = "#1A1C24",
            ["RegionBackgroundColor"] = "#323232",
            ["MenuBackgroundColor"] = "#eeeeee",
            ["ExpanderHeaderBackgroundColor"] = "#262626",
            ["ExpanderPanelBackgroundColor"] = "#323232",
            ["MainBorderColor"] = "#3B3B3B",
            ["ChartBorderColor"] = "#1A1C24",
            ["RegionSideColor"] = "#262626",
            ["HoverColor"] = "#E8E8E8",
            ["SplitColor"] = "#E8E8E8",
            ["SelectedColor"] = "#BF9000",
            ["BrushTransparent"] = "#22FFFFFF",
            ["ExpanderHeaderForegroundBrush"] = "#FFFFFF",
            ["SplitBrush"] = "#E8E8E8",
            ["BtnPressedColor"] = "#FFFFFF",
            ["BtnBackgroundColor"] = "#A2A4B1"
        };

        public string FindDefaultThemeDirectory()
        {
            return ThemeFileStore.EnsureLocalThemeDirectory();
        }

        public string ImportThemeToLocal(string filePath)
        {
            return ThemeFileStore.ImportThemeFile(filePath);
        }

        public IReadOnlyList<ThemeOption> GetAvailableThemes(string themeDirectory)
        {
            if (string.IsNullOrWhiteSpace(themeDirectory) || !Directory.Exists(themeDirectory))
            {
                return Array.Empty<ThemeOption>();
            }

            var themes = new List<ThemeOption>();
            AddThemeIfExists(themes, themeDirectory, "LightTheme.xaml", "Light");
            AddThemeIfExists(themes, themeDirectory, "DarkTheme.xaml", "Dark");

            foreach (var file in Directory.EnumerateFiles(themeDirectory, "*Theme.xaml"))
            {
                if (themes.Any(item => string.Equals(item.FilePath, file, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                themes.Add(new ThemeOption(Path.GetFileNameWithoutExtension(file), file));
            }

            return themes;
        }

        public StyleThemeDocument LoadTheme(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("主题文件不存在。", filePath);
            }

            var document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var theme = new StyleThemeDocument
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                Document = document
            };

            var root = document.Root;
            if (root == null)
            {
                return theme;
            }

            foreach (var element in root.Elements())
            {
                var key = GetResourceKey(element);
                if (string.IsNullOrWhiteSpace(key) || HiddenResourceKeys.Contains(key))
                {
                    continue;
                }

                if (element.Name == PresentationNamespace + "Color")
                {
                    theme.Colors.Add(CreateItem(key, element.Value.Trim(), StyleResourceKind.Color, StyleResourceValueSource.ColorElement));
                    continue;
                }

                if (element.Name == SystemNamespace + "Double")
                {
                    theme.FontSizes.Add(CreateItem(key, element.Value.Trim(), StyleResourceKind.FontSize, StyleResourceValueSource.DoubleElement));
                    continue;
                }

                if (element.Name == PresentationNamespace + "SolidColorBrush")
                {
                    var colorAttribute = element.Attribute("Color");
                    if (colorAttribute != null && IsEditableColor(colorAttribute.Value))
                    {
                        theme.Colors.Add(CreateItem(key, colorAttribute.Value.Trim(), StyleResourceKind.Color, StyleResourceValueSource.SolidColorBrushColorAttribute));
                        continue;
                    }

                    var value = element.Value.Trim();
                    if (IsEditableColor(value))
                    {
                        theme.Colors.Add(CreateItem(key, value, StyleResourceKind.Color, StyleResourceValueSource.SolidColorBrushText));
                    }
                }
            }

            return theme;
        }

        public void SaveTheme(StyleThemeDocument themeDocument)
        {
            if (themeDocument == null)
            {
                throw new ArgumentNullException(nameof(themeDocument));
            }

            var allItems = themeDocument.Colors.Concat(themeDocument.FontSizes).ToArray();
            foreach (var item in allItems)
            {
                if (!TryValidate(item, out var error))
                {
                    throw new InvalidOperationException($"{item.Key}: {error}");
                }

                WriteResourceValue(themeDocument.Document, item);
            }

            SynchronizeColorAliases(themeDocument.Document, allItems);

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = true
            };

            using (var writer = XmlWriter.Create(themeDocument.FilePath, settings))
            {
                themeDocument.Document.Save(writer);
            }

            foreach (var item in allItems)
            {
                item.AcceptChanges();
            }
        }

        public int ResetColorsToDefault(StyleThemeDocument themeDocument)
        {
            if (themeDocument == null)
            {
                throw new ArgumentNullException(nameof(themeDocument));
            }

            var defaults = GetThemeDefaultColors(themeDocument);
            var changedCount = 0;

            foreach (var item in themeDocument.Colors)
            {
                string defaultValue;
                if (defaults != null)
                {
                    if (!defaults.TryGetValue(item.Key, out defaultValue))
                    {
                        continue;
                    }
                }
                else
                {
                    defaultValue = item.OriginalValue;
                }

                if (string.Equals(item.Value, defaultValue, StringComparison.Ordinal))
                {
                    continue;
                }

                item.Value = defaultValue;
                item.Error = string.Empty;
                changedCount++;
            }

            return changedCount;
        }

        public bool TryValidate(StyleResourceItem item, out string error)
        {
            error = string.Empty;
            if (item == null)
            {
                error = "资源为空";
                return false;
            }

            if (item.Kind == StyleResourceKind.Color)
            {
                if (!IsEditableColor(item.Value))
                {
                    error = "请输入有效颜色，例如 #BF9000、#80BF9000 或 Transparent";
                    return false;
                }

                return true;
            }

            if (!double.TryParse(item.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSize) &&
                !double.TryParse(item.Value, NumberStyles.Float, CultureInfo.CurrentCulture, out fontSize))
            {
                error = "请输入有效数字";
                return false;
            }

            if (fontSize <= 0 || fontSize > 200)
            {
                error = "字号范围应为 0 到 200";
                return false;
            }

            item.Value = fontSize.ToString("0.##", CultureInfo.InvariantCulture);
            return true;
        }

        public void ApplyToApplication(StyleThemeDocument themeDocument)
        {
            if (themeDocument == null || Application.Current == null)
            {
                return;
            }

            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ApplyToApplication(themeDocument));
                return;
            }

            var resources = Application.Current.Resources;
            var colorElements = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in themeDocument.Colors)
            {
                if (!TryParseColor(item.Value, out var color))
                {
                    continue;
                }

                if (item.ValueSource == StyleResourceValueSource.ColorElement)
                {
                    colorElements[item.Key] = color;
                    SetResourceValue(resources, item.Key, color);
                    if (ColorAliasDependencies.TryGetValue(item.Key, out var aliasKeys))
                    {
                        foreach (var aliasKey in aliasKeys)
                        {
                            SetResourceValue(resources, aliasKey, color);
                        }
                    }
                }
                else
                {
                    SetResourceValue(resources, item.Key, new SolidColorBrush(color));
                }
            }

            foreach (var item in themeDocument.FontSizes)
            {
                if (TryParseDouble(item.Value, out var fontSize))
                {
                    SetResourceValue(resources, item.Key, fontSize);
                }
            }

            foreach (var pair in colorElements)
            {
                if (!BrushDependencies.TryGetValue(pair.Key, out var brushKeys))
                {
                    continue;
                }

                foreach (var brushKey in brushKeys)
                {
                    SetResourceValue(resources, brushKey, new SolidColorBrush(pair.Value));
                }
            }
        }

        private static IEnumerable<string> GetSearchStartPaths()
        {
            var paths = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory,
                AppDomain.CurrentDomain.BaseDirectory
            };

            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string FindThemeDirectoryFrom(string startPath)
        {
            var directory = new DirectoryInfo(startPath);
            for (var i = 0; directory != null && i < 12; i++, directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "LightTheme.xaml")) &&
                    File.Exists(Path.Combine(directory.FullName, "DarkTheme.xaml")))
                {
                    return directory.FullName;
                }

                var candidate = Path.Combine(directory.FullName, "Core", "ReeYin_V.UI", "Style", "Themes");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static void AddThemeIfExists(ICollection<ThemeOption> themes, string directory, string fileName, string displayName)
        {
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path))
            {
                themes.Add(new ThemeOption(displayName, path));
            }
        }

        private static IReadOnlyDictionary<string, string> GetThemeDefaultColors(StyleThemeDocument themeDocument)
        {
            var name = string.IsNullOrWhiteSpace(themeDocument.Name)
                ? Path.GetFileNameWithoutExtension(themeDocument.FilePath)
                : themeDocument.Name;

            if (name?.IndexOf("Dark", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return DarkDefaultColors;
            }

            if (name?.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LightDefaultColors;
            }

            return null;
        }

        private static StyleResourceItem CreateItem(string key, string value, StyleResourceKind kind, StyleResourceValueSource source)
        {
            var item = new StyleResourceItem
            {
                Key = key,
                Kind = kind,
                ValueSource = source,
                Description = GetResourceDescription(key)
            };
            item.SetInitialValue(value);
            return item;
        }

        private static string GetResourceDescription(string key)
        {
            if (Application.Current?.TryFindResource($"StyleResourceDescription{key}") is string localized &&
                !string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            return ResourceDescriptions.TryGetValue(key, out var description) ? description : string.Empty;
        }

        private static string GetResourceKey(XElement element)
        {
            return element.Attribute(XamlNamespace + "Key")?.Value ?? string.Empty;
        }

        private static bool IsEditableColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                return false;
            }

            return TryParseColor(value, out _);
        }

        private static bool TryParseColor(string value, out Color color)
        {
            color = Colors.Transparent;
            try
            {
                var converted = ColorConverter.ConvertFromString(value.Trim());
                if (converted is Color parsed)
                {
                    color = parsed;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryParseDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
                   double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
        }

        private static void WriteResourceValue(XDocument document, StyleResourceItem item)
        {
            var element = document.Root?
                .Elements()
                .FirstOrDefault(candidate =>
                    string.Equals(GetResourceKey(candidate), item.Key, StringComparison.Ordinal) &&
                    MatchesValueSource(candidate, item.ValueSource));

            if (element == null)
            {
                throw new InvalidOperationException($"找不到资源键: {item.Key}");
            }

            switch (item.ValueSource)
            {
                case StyleResourceValueSource.ColorElement:
                case StyleResourceValueSource.SolidColorBrushText:
                case StyleResourceValueSource.DoubleElement:
                    element.Value = item.Value.Trim();
                    break;
                case StyleResourceValueSource.SolidColorBrushColorAttribute:
                    element.SetAttributeValue("Color", item.Value.Trim());
                    break;
            }
        }

        private static void SynchronizeColorAliases(XDocument document, IEnumerable<StyleResourceItem> items)
        {
            foreach (var item in items)
            {
                if (!ColorAliasDependencies.TryGetValue(item.Key, out var aliasKeys))
                {
                    continue;
                }

                foreach (var aliasKey in aliasKeys)
                {
                    WriteColorElementValueIfExists(document, aliasKey, item.Value);
                }
            }
        }

        private static void WriteColorElementValueIfExists(XDocument document, string key, string value)
        {
            var element = document.Root?
                .Elements()
                .FirstOrDefault(candidate =>
                    string.Equals(GetResourceKey(candidate), key, StringComparison.Ordinal) &&
                    candidate.Name == PresentationNamespace + "Color");

            if (element != null)
            {
                element.Value = value.Trim();
            }
        }

        private static bool MatchesValueSource(XElement element, StyleResourceValueSource source)
        {
            switch (source)
            {
                case StyleResourceValueSource.ColorElement:
                    return element.Name == PresentationNamespace + "Color";
                case StyleResourceValueSource.DoubleElement:
                    return element.Name == SystemNamespace + "Double";
                case StyleResourceValueSource.SolidColorBrushText:
                    return element.Name == PresentationNamespace + "SolidColorBrush" && element.Attribute("Color") == null;
                case StyleResourceValueSource.SolidColorBrushColorAttribute:
                    return element.Name == PresentationNamespace + "SolidColorBrush" && element.Attribute("Color") != null;
                default:
                    return false;
            }
        }

        private static void SetResourceValue(ResourceDictionary resources, string key, object value)
        {
            SetResourceValueRecursive(resources, key, value);
            resources[key] = value;
        }

        private static void SetResourceValueRecursive(ResourceDictionary dictionary, string key, object value)
        {
            if (dictionary.Contains(key))
            {
                dictionary[key] = value;
            }

            foreach (var merged in dictionary.MergedDictionaries)
            {
                SetResourceValueRecursive(merged, key, value);
            }
        }
    }
}
