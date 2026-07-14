using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Cache
{
    /// <summary>
    /// 用枚举区分缓存
    /// </summary>
    public enum CacheKey
    {
        /// <summary>
        /// 当前用户
        /// </summary>
        User,

        /// <summary>
        /// 是否自动登录 
        /// </summary>
        IsAutoLogin,

        /// <summary>
        /// 是否记住密码
        /// </summary>
        IsRemember,

        /// <summary>
        /// 缓存数据
        /// </summary>
        AppData,

        /// <summary>
        /// 过滤器算法缓存数据
        /// </summary>
        Filters,

        /// <summary>
        /// 当前语言
        /// </summary>
        Language,
    }
}
