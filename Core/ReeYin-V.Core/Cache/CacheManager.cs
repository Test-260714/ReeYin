using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;

namespace ReeYin_V.Core.Cache
{
    /// <summary>
    /// 提供缓存服务的管理器
    /// </summary>
    [ExposedService(Lifetime.Singleton, 2,typeof(ICacheManager))]
    class CacheManager : ICacheManager
    {
        private IConfigManager configManager;
        private Dictionary<string, string> cacheNames;
        private readonly object cacheLock = new object();
        public CacheManager(IConfigManager configManager)
        {
            this.configManager = configManager;
            cacheNames = configManager.Read<Dictionary<string, string>>(ConfigKey.CacheConfig) ?? new Dictionary<string, string>();
        }


        /// <summary>
        /// 获取本地缓存内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Get<T>(ValueType key, out T value)
        {
            value = default(T);
            string obj = key.ToString();
            lock (cacheLock)
            {
                if (!cacheNames.ContainsKey(key.GetFullName()))
                    return false;
                obj = cacheNames[key.GetFullName()];
            }

            try
            {
                value = JsonHelper.Deserialize<T>(obj);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void Set<T>(ValueType key, T value)
        {
            Assert.NotNull(key);
            Assert.NotNull(value);

            string content = JsonHelper.Serialize(value);

            lock (cacheLock)
            {
                if (cacheNames.ContainsKey(key.GetFullName()))
                {
                    //若存在则修改
                    cacheNames[key.GetFullName()] = content;
                }
                else
                {
                    cacheNames.Add(key.GetFullName(), content);
                }

                configManager.Write(ConfigKey.CacheConfig, cacheNames);

            }
        }

        /// <summary>
        /// 删除缓存内容
        /// </summary>
        /// <param name="key"></param>
        public void Delete(ValueType key)
        {
            lock (cacheLock)
            {
                if (cacheNames.ContainsKey(key.GetFullName()))
                {
                    cacheNames.Remove(key.GetFullName());
                    configManager.Write(ConfigKey.CacheConfig, cacheNames);
                }
            }
        }

        public void Refresh(ValueType key)
        {
            lock (cacheLock)
            {
                if (cacheNames.ContainsKey(key.GetFullName()))
                {
                    cacheNames.Remove(key.GetFullName());
                    configManager.Write(ConfigKey.CacheConfig, cacheNames);
                }
            }
        }
    }
}
