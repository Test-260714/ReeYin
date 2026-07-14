using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.ChroCodile.Defines
{
    public class General
    {
        /// <summary>
        /// 传感器型号
        /// </summary>
        public enum PreciTecModel
        {
            CHR1 = 0,
            CHR2 = 1,
            CHRMultiChannel = 2,
            CHRCompact = 3
        }

        /// <summary>
        /// 轴号
        /// </summary>
        public enum ShaftNumber
        {
            X,
            Y,
            Z,
            U,
            V
        }
    }
}
