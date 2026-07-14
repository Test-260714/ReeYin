using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Cache
{
    /// <summary>
    /// 缓存接口
    /// </summary>
    public interface ICacheManager
    {
        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Get<T>(ValueType key, out T value);

        /// <summary>
        /// 保存缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Set<T>(ValueType key, T value);

        /// <summary>
        /// 删存缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        void Delete(ValueType key);

        /// <summary>
        /// 刷新缓存（删除旧缓存，下次获取时重新加载）
        /// </summary>
        /// <param name="key">缓存键</param>
        void Refresh(ValueType key);
    }
}
