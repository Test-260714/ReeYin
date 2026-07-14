using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.ControlCard.Leisai.App
{
    public partial class LeisaiEMCControlCard : ControlCardBase
    {
        protected override void DoClose()
        {
            throw new NotImplementedException();
        }

        protected override void DoConfigure()
        {
            throw new NotImplementedException();
        }

        protected override bool DoGetAxisEnable(En_AxisNum axisType)
        {
            throw new NotImplementedException();
        }

        protected override bool DoGetAxisStopped(En_AxisNum axisType)
        {
            throw new NotImplementedException();
        }

        protected override bool DoGoHome(out string message)
        {
            throw new NotImplementedException();
        }

        protected override bool DoInit()
        {
            throw new NotImplementedException();
        }

        protected override bool DoMoveAxis(En_AxisNum axisType, double um)
        {
            throw new NotImplementedException();
        }

        protected override bool DoMoveContinue(En_AxisNum axisType, MoveDirection moveDirection)
        {
            throw new NotImplementedException();
        }

        protected override bool DoSetAxisEnable(En_AxisNum axisType, bool v)
        {
            throw new NotImplementedException();
        }

        protected override void DoStop(En_AxisNum? axisType, AxisStopMode stopMode)
        {
            throw new NotImplementedException();
        }
    }
}
