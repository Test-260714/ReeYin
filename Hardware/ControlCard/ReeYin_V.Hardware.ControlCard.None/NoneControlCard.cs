using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.None
{
    /// <summary>
    /// 仿真控制卡
    /// </summary>
    class NoneControlCard : ControlCardBase
    {
        protected override void DoClose()
        {

        }

        protected override void DoConfigure()
        {

        }

        protected override bool DoGetAxisEnable(En_AxisNum axisType)
        {
            return true;
        }

        protected override bool DoGetAxisStopped(En_AxisNum axisType)
        {
            return true;
        }

        protected override bool DoGoHome(out string message)
        {
            message = "回零成功";
            return true;
        }

        protected override bool DoInit()
        {
            Provider.OnChanged += (() =>
            {
                //todo... 当控制卡参数发生变化时，在这里重新设置控制卡的参数
            });



            return true;
        }

        protected override bool DoMoveAxis(En_AxisNum axisType, double um)
        {
            Thread.Sleep((int)(um * 1));

            return true;
        }

        protected override bool DoMoveContinue(En_AxisNum axisType, MoveDirection moveDirection)
        {
            return true;
        }

        protected override bool DoSetAxisEnable(En_AxisNum axisType, bool v)
        {
            int enable = v ? 1 : 0;
            int axis = (int)axisType;

            return true;
        }

        /// <summary>
        /// 停止轴运动
        /// </summary>
        /// <param name="axisType"></param>
        /// <param name="stopMode"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected override void DoStop(En_AxisNum? axisType, AxisStopMode stopMode)
        {
            if (axisType.HasValue)
            {
                StopAxis(axisType.Value);
            }
            else
            {
                foreach (var item in AxisTypes)
                {
                    StopAxis(item);
                }
            }
        }

        public virtual bool GetAllPosInfos(ref double[] allPosInfos, short core = 2)
        {
            throw new NotImplementedException();
        }

        private void StopAxis(En_AxisNum axisType)
        {
            Thread.Sleep(150);//耗时模拟
        }
    }
}
