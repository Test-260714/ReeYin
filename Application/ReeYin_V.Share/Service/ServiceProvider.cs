using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Services.Language;
using ReeYin_V.Core.Services.Menu;
using ReeYin_V.Core.Services.Theme;
using ReeYin_V.Core.Services.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services
{
    [ExposedService(Lifetime.Singleton, 4, AutoInitialize = true)]
    public sealed class ServiceProvider
    {
        public ServiceProvider(
            IThemeManager theme,
            ILanguageManager language,
            IMenuService menu
            )
        {
            ThemeManager = theme;
            LanguageManager = language;
            MenuService = menu;
        }

        /// <summary>
        /// 主题助手
        /// </summary>
        public static IThemeManager ThemeManager { get; private set; }

        /// <summary>
        /// 语言助手
        /// </summary>
        public static ILanguageManager LanguageManager { get; private set; }

        /// <summary>
        /// 菜单助手
        /// </summary>
        public static IMenuService MenuService { get; private set; }




    }

}
