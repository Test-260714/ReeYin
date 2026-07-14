using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using ReeYin_V.Core.Models;

namespace ReeYin_V.Share.Events
{
    /// <summary>
    /// 用户登录成功时
    /// </summary>
    public class LoginSuccessEvent : PubSubEvent<CurrentUser>
    {
    }
}
