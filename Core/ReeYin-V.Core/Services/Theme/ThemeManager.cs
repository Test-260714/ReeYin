using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ReeYin_V.Core.Services.Theme
{
    [ExposedService(Lifetime.Singleton, 3,typeof(IThemeManager))]
    public class ThemeManager : IThemeManager
    {
        private ResourceDictionary Resource { get; set; }
        private string Uri { get; set; }
        public ThemeManager()
        {
            Set(ThemeType.LightTheme);//设置默认语言为亮色主题
        }

        /// <summary>
        /// 索引器
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                if (Resource != null && Resource.Contains(key))
                {
                    return Resource[key].ToString();
                }

                return this[key];
            }
        }

        public ThemeType Current { get; private set; } = ThemeType.LightTheme;

        public void Set(ThemeType theme)
        {
            Assert.NotNull(theme);

            if (Uri == null)
            {
                ResourceDictionary resourceDictionary = Application.Current.Resources.MergedDictionaries[1];
                string path = resourceDictionary.Source.AbsolutePath;
                Uri = path.Remove(path.LastIndexOf("/"));
            }

            string target = $"{Uri}/{theme}.xaml";
            Resource = (ResourceDictionary)Application.LoadComponent(new Uri(target, UriKind.RelativeOrAbsolute));
            Application.Current.Resources.MergedDictionaries.RemoveAt(1);//移出第0个元素
            Application.Current.Resources.MergedDictionaries.Insert(1, Resource);
            ApplyThemeResourceOverrides();

            if (Current != theme)
            {
                Current = theme;

                //todo 保存到系统设置里面
            }
        }

        private void ApplyThemeResourceOverrides()
        {
            if (Application.Current == null || Resource == null || !TryGetThemeColor("PrimaryColor", out var primaryColor))
            {
                return;
            }

            var resources = Application.Current.Resources;
            var titleBarColor = TryGetThemeColor("TitleBarColor", out var configuredTitleBarColor)
                ? configuredTitleBarColor
                : primaryColor;

            SetResource(resources, "ReeYinPrimaryColor", primaryColor);
            SetResource(resources, "PrimaryColor", primaryColor);
            SetResource(resources, "TitleBarColor", titleBarColor);
            SetResource(resources, "DarkPrimaryColor", primaryColor);
            SetResource(resources, "LightPrimaryColor", primaryColor);

            SetBrush(resources, "PrimaryBrush", primaryColor);
            SetBrush(resources, "CPrimaryBrush", primaryColor);
            SetBrush(resources, "TitleBarBrush", titleBarColor);
            SetBrush(resources, "DarkPrimaryBrush", primaryColor);
            SetBrush(resources, "SelectedBrush", primaryColor);
            SetBrush(resources, "BtnOverColor", primaryColor);
            SetBrush(resources, "BtnCheckedColor", primaryColor);

            SetBrush(resources, "PrimaryBrushOpacity08", primaryColor, 0.08);
            SetBrush(resources, "PrimaryBrushOpacity13", primaryColor, 0.13);
            SetBrush(resources, "PrimaryBrushOpacity14", primaryColor, 0.14);
            SetBrush(resources, "PrimaryBrushOpacity20", primaryColor, 0.20);
            SetBrush(resources, "PrimaryBrushOpacity53", primaryColor, 0.53);
            SetBrush(resources, "PrimaryBrushOpacity80", primaryColor, 0.80);
            SetBrush(resources, "LightPrimaryBrush", primaryColor, 0.13);
        }

        private bool TryGetThemeColor(string key, out Color color)
        {
            color = Colors.Transparent;
            if (!Resource.Contains(key))
            {
                return false;
            }

            var value = Resource[key];
            if (value is Color themeColor)
            {
                color = themeColor;
                return true;
            }

            try
            {
                var converted = ColorConverter.ConvertFromString(value?.ToString());
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

        private static void SetBrush(ResourceDictionary resources, string key, Color color, double opacity = 1)
        {
            SetResource(resources, key, new SolidColorBrush(color) { Opacity = opacity });
        }

        private static void SetResource(ResourceDictionary resources, string key, object value)
        {
            resources[key] = value;
        }

    }
}
