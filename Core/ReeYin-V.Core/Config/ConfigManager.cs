using Azure.Core.Serialization;
using Newtonsoft.Json;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Config
{
    [ExposedService(Lifetime.Singleton, 2,typeof(IConfigManager))]
    public class ConfigManager : IConfigManager
    {
        #region Fields
        private const string root = "Config";
        #endregion

        private string GetFullPath(ValueType key)
        {
            return Path.Combine(root, key.GetType().FullName + "." + key.ToString() + ".json");
        }

        /// <summary>
        /// 读配置文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Read<T>(ValueType key)
        {
            string filename = GetFullPath(key);
            var temp = JsonHelper.Load<T>(filename);
            return temp;
        }

        /// <summary>
        /// 写配置文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Write<T>(ValueType key, T value)
        {
            Assert.NotNull(key);
            Assert.NotNull(value);
            Directory.CreateDirectory(root);
            string filename = GetFullPath(key);
            JsonHelper.Save(value, filename, true);
        }
    }
}
