using ReeYin_V.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Theme
{
    public interface IThemeManager
    {
        string this[string key] { get; }

        /// <summary>
        /// 当前语言
        /// </summary>
        ThemeType Current { get; }

        /// <summary>
        /// 设置语言
        /// </summary>
        /// <param name="type"></param>
        void Set(ThemeType type);

    }
}
