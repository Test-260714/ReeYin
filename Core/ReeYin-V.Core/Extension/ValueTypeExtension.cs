using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Extension
{
    public static class ValueTypeExtension
    {
        /// <summary>
        /// 获取值类型的命令空间+类型名称的字符串
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static string GetFullName(this ValueType valueType)
        {
            return $"{valueType.GetType().FullName}.{valueType}";
        }

        /// <summary>
        /// 获取值类型的JSON全文件名
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static string GetJsonFullName(this ValueType valueType)
        {
            return $"{valueType.GetFullName()}.json";
        }
    }
}
