using GTN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogolMotion
{
    public partial class GoogolGTMotion
    {
        #region 数字量(映射到了core1，所以使用1)

        /// <summary>
        /// 读取通用数字IO输入状态
        /// </summary>
        /// <param name="iSlave">拓展卡编号0~15</param>
        /// <param name="uStatus">读取的信号</param>
        /// <returns></returns>
        public bool GetDIn(ref int uStatus, short core = 1)
        {
            short sRtn;
            sRtn = mc.GTN_GetDi(core, (short)mc.MC_GPI, out uStatus);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 读取全部输出信号
        /// </summary>
        /// <param name="iSlave">拓展卡编号0~15</param>
        /// <param name="uStatus">读取的信号</param>
        /// <returns></returns>
        public bool GetDOut(ref int Status, short core = 1)
        {
            short sRtn;
            sRtn = mc.GTN_GetDo(core, (short)mc.MC_GPI, out Status);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 按位读取数字IO输入状态
        /// </summary>
        /// <returns></returns>
        public bool GetDIBit(short IOIndex, out short Status, short core = 1)
        {
            short sRtn;
            sRtn = mc.GTN_GetDiBit(core, (short)mc.MC_GPI, IOIndex, out Status);

            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 读取单个输入信号
        /// </summary>
        /// <param name="iSlave">编号0~15</param>
        /// <param name="index">输入索引</param>
        /// <param name="value">读取的信号</param>
        /// <returns></returns>
        public bool GetSingleDIn(short index, ref short value, short core = 1)
        {
            short sRtn;
            sRtn = mc.GTN_GetDiBit(core, (short)mc.MC_GPI, index, out value);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 设置单个输出信号
        /// 进行映射后，还能操作
        /// </summary>
        /// <param name="index">输出索引</param>
        /// <param name="value">设置的值0或1，信号好像是反的，true表示关闭</param>
        /// <returns></returns>
        public bool SetSingleDOut(short index, bool value, short core = 1)
        {
            short sRtn;
            sRtn = mc.GTN_SetDoBit(core, (short)mc.MC_GPO, index, (byte)(value == true ? 0 : 1));
            if (sRtn != 0)
            {
                ErrMessage($"IO 触发错误：端口号{index}，触发状态{value}");
                return false;
            }
            return true;
        }
        #endregion

        #region 模拟量
        /// <summary>
        /// 读取模拟量
        /// </summary>
        /// <param name="iSlave">拓展卡编号0~15</param>
        /// <param name="iIndex">输出索引从0开始</param>
        /// <param name="data">读取的值</param>
        /// <returns></returns>
        //public bool GetAIn(short iSlave, ushort iIndex, ref short[] data, ushort count)
        //{
        //    short sRtn;
        //    sRtn = mc.GTN_GetAi(iSlave, iIndex, out data[0], count);
        //    if (sRtn != 0)
        //    {
        //        ErrMessage(sRtn);
        //        return false;
        //    }
        //    return true;
        //}
        #endregion


        #region Discard
        /// <summary>
        /// 设置数字输出(进行映射后，还能操作的Out)
        /// </summary>
        /// <param name="port"></param>
        /// <param name="open"></param>
        /// <returns></returns>
        public bool CrdSetDOut(short port, bool open, short core = 1)
        {
            short sRtn;
            //指定数字 I0 类型。
            //MC_ENABLE(该宏定义为 10):驱动器使能,
            //MC_CLEAR(该宏定义为 11):报警清除。
            //MC_GPO(该宏定义为 12):通用输出。
            var value = open ? 1 : 0;
            sRtn = mc.GTN_SetDoBit(core, (short)mc.MC_GPO, (short)(port + 1), (short)value);
            if (sRtn != 0)
                return false;
            return true;
        }
        #endregion

    }
}
