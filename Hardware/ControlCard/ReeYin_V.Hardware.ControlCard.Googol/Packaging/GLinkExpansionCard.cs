using GTN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogolMotion
{
    /// <summary>
    /// 拓展卡指令
    /// </summary>
    public partial class GoogolGTMotion
    {
        #region 拓展卡指令
        /// <summary>
        /// 读取拓展卡全部输入信号
        /// </summary>
        /// <param name="iSlave">拓展卡编号0~15</param>
        /// <param name="uStatus">读取的信号</param>
        /// <returns></returns>
        public bool GLink_GetDIn(short iSlave, ref ushort uStatus)
        {
            short sRtn;
            var bStatus = new byte[2];
            sRtn = glink.GT_GetGLinkDi(iSlave, 0, out bStatus[0], 2);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            uStatus = (ushort)((bStatus[1] << 8) + bStatus[0]);
            return true;
        }
        /// <summary>
        /// 设置拓展卡全部输出信号
        /// </summary>
        /// <param name="iSlave"></param>
        /// <param name="uData"></param>
        /// <returns></returns>
        public bool GLink_SetDOut(short iSlave, ushort uData)
        {
            short sRtn;
            var bData = BitConverter.GetBytes(uData);
            sRtn = glink.GT_SetGLinkDo(iSlave, 0, ref bData[0], 2);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 读取拓展卡全部输出信号
        /// </summary>
        /// <param name="iSlave">拓展卡编号0~15</param>
        /// <param name="uStatus">读取的信号</param>
        /// <returns></returns>
        public bool GLink_GetDOut(short iSlave, ref ushort uStatus)
        {
            short sRtn;
            var bStatus = new byte[2];
            sRtn = glink.GT_GetGLinkDo(iSlave, 0, ref bStatus[0], 2);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            uStatus = (ushort)((bStatus[1] << 8) + bStatus[0]);
            return true;
        }
        /// <summary>
        /// 读取拓展卡单个输入信号
        /// </summary>
        /// <param name="iSlave">拓展卡编号0~15</param>
        /// <param name="index">输入索引</param>
        /// <param name="value">读取的信号</param>
        /// <returns></returns>
        public bool GLink_GetSingleDIn(short iSlave, short index, ref byte value)
        {
            short sRtn;
            sRtn = glink.GT_GetGLinkDiBit(iSlave, index, out value);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 设置拓展卡单个输出信号
        /// </summary>
        /// <param name="iSlave">拓展卡编号0~15</param>
        /// <param name="index">输出索引</param>
        /// <param name="value">设置的值0或1,设置超过1好像有问题</param>
        /// <returns></returns>
        public bool GLink_SetSingleDOut(short iSlave, short index, bool value)
        {
            short sRtn;
            sRtn = glink.GT_SetGLinkDoBit(iSlave, index, (byte)(value == true ? 1 : 0));
            if (sRtn != 0)
            {
                ErrMessage($"IO 触发错误：端口号{index}，触发状态{value}");
                return false;
            }
            return true;
        }
        /// <summary>
        /// 读取拓展卡模拟量
        /// </summary>
        /// <param name="iSlave">拓展卡编号0~15</param>
        /// <param name="iIndex">输出索引从0开始</param>
        /// <param name="data">读取的值</param>
        /// <returns></returns>
        public bool GLink_GetAIn(short iSlave, ushort iIndex, ref short[] data, ushort count)
        {
            short sRtn;
            sRtn = glink.GT_GetGLinkAi(iSlave, iIndex, out data[0], count);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        #endregion

    }
}
