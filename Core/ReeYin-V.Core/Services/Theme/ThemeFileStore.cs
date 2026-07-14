using ReeYin_V.Core.Enums;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace ReeYin_V.Core.Services.Theme
{
    public static class ThemeFileStore
    {
        private const string ThemeFolderName = "Themes";
        private const string LocalThemeRelativeDirectory = "Config\\Style\\" + ThemeFolderName;
        private const string UiThemePackUriPrefix = "pack://application:,,,/ReeYin_V.UI;component/Style/Themes";

        public static string LocalThemeDirectory =>
            Path.Combine(AppContext.BaseDirectory, "Config", "Style", ThemeFolderName);

        public static string EnsureLocalThemeDirectory()
        {
            Directory.CreateDirectory(LocalThemeDirectory);
            EnsureLocalThemeFile(ThemeType.LightTheme);
            EnsureLocalThemeFile(ThemeType.DarkTheme);
            return LocalThemeDirectory;
        }

        public static string GetLocalThemeFile(ThemeType theme)
        {
            return Path.Combine(LocalThemeDirectory, $"{theme}.xaml");
        }

        public static ResourceDictionary LoadThemeResourceDictionary(ThemeType theme)
        {
            EnsureLocalThemeDirectory();

            var localThemeFile = GetLocalThemeFile(theme);
            if (File.Exists(localThemeFile))
            {
                return LoadThemeResourceDictionary(localThemeFile);
            }

            return LoadBundledThemeResourceDictionary(theme);
        }

        public static ResourceDictionary LoadThemeResourceDictionary(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return (ResourceDictionary)XamlReader.Load(stream);
            }
        }

        public static string ImportThemeFile(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Theme file does not exist.", sourceFilePath);
            }

            EnsureLocalThemeDirectory();

            var targetFilePath = Path.Combine(LocalThemeDirectory, Path.GetFileName(sourceFilePath));
            if (string.Equals(Path.GetFullPath(sourceFilePath), Path.GetFullPath(targetFilePath), StringComparison.OrdinalIgnoreCase))
            {
                return targetFilePath;
            }

            File.Copy(sourceFilePath, targetFilePath, true);
            return targetFilePath;
        }

        private static void EnsureLocalThemeFile(ThemeType theme)
        {
            var localThemeFile = GetLocalThemeFile(theme);
            if (File.Exists(localThemeFile))
            {
                return;
            }

            var sourceThemeFile = FindSourceThemeFile($"{theme}.xaml");
            if (!string.IsNullOrWhiteSpace(sourceThemeFile))
            {
                File.Copy(sourceThemeFile, localThemeFile, false);
                return;
            }

            if (TryCopyEmbeddedThemeFile(theme, localThemeFile))
            {
                return;
            }

            SaveBundledThemeToFile(theme, localThemeFile);
        }

        private static string FindSourceThemeFile(string fileName)
        {
            var startPaths = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory,
                AppDomain.CurrentDomain.BaseDirectory
            };

            foreach (var startPath in startPaths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var directory = new DirectoryInfo(startPath);
                for (var i = 0; directory != null && i < 12; i++, directory = directory.Parent)
                {
                    var candidate = Path.Combine(directory.FullName, "Core", "ReeYin_V.UI", "Style", ThemeFolderName, fileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return string.Empty;
        }

        private static bool TryCopyEmbeddedThemeFile(ThemeType theme, string targetFilePath)
        {
            var resourceName = $"ReeYin_V.Core.Services.Theme.{theme}.xaml";
            using (var stream = typeof(ThemeFileStore).Assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return false;
                }

                using (var target = File.Create(targetFilePath))
                {
                    stream.CopyTo(target);
                }
            }

            return true;
        }

        private static ResourceDictionary LoadBundledThemeResourceDictionary(ThemeType theme)
        {
            return (ResourceDictionary)Application.LoadComponent(
                new Uri($"{UiThemePackUriPrefix}/{theme}.xaml", UriKind.Absolute));
        }

        private static void SaveBundledThemeToFile(ThemeType theme, string targetFilePath)
        {
            var dictionary = LoadBundledThemeResourceDictionary(theme);
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = true
            };

            using (var writer = XmlWriter.Create(targetFilePath, settings))
            {
                XamlWriter.Save(dictionary, writer);
            }
        }
    }
}
