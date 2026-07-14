using Prism.Events;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Services.WorkStatus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Events
{
    public class WorkStatusChangeEvent : PubSubEvent<WorkStatus>
    {


    }
}
