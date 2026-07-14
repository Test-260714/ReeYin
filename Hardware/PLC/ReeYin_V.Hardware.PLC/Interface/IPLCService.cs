using ReeYin_V.Core;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.PLC.Interface
{
    public interface IPLCService
    {
        /// <summary>
        /// 设备状态
        /// </summary>
        HardwareState State { get; set; }

    }
}
