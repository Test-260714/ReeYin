using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReeYin_V.Share.Helper
{
    /// <summary>
    /// 字符相关操作
    /// </summary>
    public class ChartOperation
    {

        /// <summary>
        /// 将字符串转换为Unicode编码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ReplaceUnicode(string input)
        {
            return Regex.Replace(input, @"\\u([0-9A-Fa-f]{4})", match => {
                int codePoint = int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                return ((char)codePoint).ToString();
            });
        }
    }
}
