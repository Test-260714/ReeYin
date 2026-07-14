using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.ResultsDisplay
{
    public interface IResultsDisplay
    {
        /// <summary>
        /// 谁传递的结果
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 传递时间
        /// </summary>
        DateTime TransferTime { get; set; }

        /// <summary>
        /// 是否为有效的
        /// </summary>
        bool IsValid { get; set; }

        /// <summary>
        /// 未被定义其他的结果
        /// Key:参数名称
        /// Value:实际参数值（公共类型）
        /// </summary>
        Dictionary<string,object> OtherResult { get; set; }

    }
}
