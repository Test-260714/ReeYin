using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.JingCe.CustomUI.Defines
{
    public class General
    {
        public enum OuterTriggerType
        {
            单端触发模式,
            _485触发模式,
            编码器
        }

        public enum PointDataFormat
        {
            XZI模式,
            XZ模式,
            XI模式
        }

        public enum FrameRateMode
        {
            正常 = 1,
            快速 = 2,
            超快速 = 4,
            最快速 = 8
        }

        public enum HighLevelSwitch
        {
            关,
            开
        }

        public enum ExternalTriggerSwitch
        {
            打开外触发,
            关闭外触发
        }
    }
}
