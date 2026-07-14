using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Events
{
    /// <summary>
    /// 输出结果
    /// string：谁传递的
    /// object：结果
    /// </summary>
    public class OutputResultEvent : PubSubEvent<(string, object)>
    {

    }

}
