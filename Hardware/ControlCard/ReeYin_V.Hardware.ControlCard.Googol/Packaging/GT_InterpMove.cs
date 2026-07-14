using GTN;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GTN.mc;
using static System.Formats.Asn1.AsnWriter;

namespace GoogolMotion
{
    /// <summary>
    /// 插补相关指令
    /// </summary>
    public partial class GoogolGTMotion
    {
        #region 插补
        /// <summary>
        /// 清除插补缓存区内的插补数据
        /// </summary>
        /// <param name="crd">坐标系从1开始</param>
        /// <param name="fifo">缓存区编号  默认0  1是辅助运动</param>
        /// <returns></returns>
        public bool CrdBufClear(short crd, short fifo = 0, short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_CrdClear(core, crd, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            ExitCrd(core,crd); //清除插补缓存区时若插补运动还未结束则退出循环
            return true;
        }

        /// <summary>
        /// 用于在使用前瞻时。调用该指令表示后续没有新的数据，将会一次性把前瞻缓存区的数据压入运动缓存区
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="fifo">缓存区编号默认0  1是辅助运动</param>
        /// <returns></returns>
        public bool CrdData(short crd, short fifo = 0, short core = 2)
        {
            short sRtn;
            IntPtr crdDataNULL = new IntPtr();

            sRtn = mc.GTN_CrdData(core, crd, crdDataNULL, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存区内延时设置指令
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="dTime">延时时间ms  0~16383</param>
        /// <param name="fifo">缓存区编号默认0  1为辅助运动</param>
        /// <returns></returns>
        public bool CrdBufDelay(short crd, ushort dTime, short fifo = 0,short core =2)
        {
            short sRtn;
            sRtn = mc.GTN_BufDelay(core, crd, dTime, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 映射数字输出（将I/O模块的输出对应到本地）一个坐标系最多16个
        /// </summary>
        /// <param name="gpo">控制器的 MC_GPO 资源序号，从 1 开始，GEN 最大取值 32</param>
        /// <param name="slaveno">所连接 Glink 从站的序号，从 0 开始，映射 GPO 从站必须有 DO 资源</param>
        /// <param name="indx">需要映射的 IO 点的编号从0开始</param>
        /// <returns></returns>
        public bool Glink_MappingDout(short gpo, short slaveno, short indx)
        {
            short sRtn;
            short bitoffset = (short)(indx % 8);  //一个byte8位  可以储存8个IO信号
            short byteOffset = (short)(indx / 8);//  一个通讯模块16个IO 要两个byte 所以需要通讯偏移
            sRtn = glink.GT_RelateGlinkToMcGpoBit(gpo, slaveno, bitoffset, byteOffset);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 映射数字输入（将I/O模块的输入对应到本地）一个坐标系最多16个
        /// </summary>
        /// <param name="gpo">控制器的 MC_GPI 资源序号，从 1 开始，GEN 最大取值 32</param>
        /// <param name="slaveno">所连接 Glink 从站的序号，从 0 开始，映射 GPO 从站必须有 DO 资源</param>
        /// <param name="indx">需要映射的 IO 点的编号从0开始</param>
        /// <returns></returns>
        public bool MappingDi(short gpo, short slaveno, short indx)
        {
            short sRtn;
            short bitoffset = (short)(indx % 8);//一个byte8位  可以储存8个IO信号
            short byteOffset = (short)(indx / 8);//  一个通讯模块16个IO 要两个byte 所以需要通讯偏移
            sRtn = glink.GT_RelateGlinkToMcGpiBit(gpo, slaveno, bitoffset, byteOffset);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gpo"></param>
        /// <param name="slaveno"></param>
        /// <param name="indx"></param>
        /// <returns></returns>
        public bool MappingDO(short gpo, short slaveno, short indx,short core = 2)
        {
            short sRtn;
            short bitoffset = (short)(indx % 8);//一个byte8位  可以储存8个IO信号
            short byteOffset = (short)(indx / 8);//  一个通讯模块16个IO 要两个byte 所以需要通讯偏移
            sRtn = mc.GTN_RelateEcatSlaveToMcGpoBit(core, 2, 0, 0, 7, 1);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 设置数字输出I/O的值 
        /// </summary>
        /// <param name="value">0低电平 1高电平</param>
        /// <returns></returns>
        public bool CrdSetDOut(int value,short core = 2)
        {
            short sRtn;
            //指定数字 IO 类型。
            //MC_ENABLE(该宏定义为 10)：驱动器使能。
            //MC_CLEAR(该宏定义为 11)：报警清除。
            //MC_GPO(该宏定义为 12)：通用输出。
            sRtn = mc.GTN_SetDo(core, 12, value);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存区内数字量 IO 输出设置指令
        /// </summary>
        /// <param name="crd">坐标系号</param>
        /// <param name="doMask">输出索引1~16</param>
        /// <param name="doValue">输出的值</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdBufIO(short crd, ushort doMask, ushort doValue, short fifo = 0, short core = 2)
        {
            short sRtn;
            //指定数字 IO 类型。
            //MC_ENABLE(该宏定义为 10)：驱动器使能。
            //MC_CLEAR(该宏定义为 11)：报警清除。
            //MC_GPO(该宏定义为 12)：通用输出。                   
            sRtn = mc.GTN_BufIO(core, crd, mc.MC_GPO, doMask, doValue, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存区内设置轴的停止 IO 信息
        /// </summary>
        /// <param name="crd">坐标系号</param>
        /// <param name="axisid">轴号从1开始</param>
        /// <param name="indx">输入索引</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdBufSetStopIo(short crd, short axisid, short indx, short fifo = 0,short core = 2)
        {
            short sRtn;
            //设置的数字量输入的类型。
            //MC_LIMIT_POSITIVE(该宏定义为 0)：正限位。
            //MC_LIMIT_NEGATIVE(该宏定义为 1)：负限位。
            //MC_ALARM(该宏定义为 2)：驱动报警。
            //MC_HOME(该宏定义为 3)：原点开关。
            //MC_GPI(该宏定义为 4)：通用输入。
            //MC_ARRIVE(该宏定义为 5)：电机到位信号。
            //stopType 0：紧急停止类型。
            //         1：平滑停止类型。
            sRtn = mc.GTN_BufSetStopIo(core, crd, axisid, 0, MC_GPI, indx, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 建立XY坐标系
        /// </summary>
        /// <param name="csId">坐标系号从1开始</param>
        /// <param name="xAxisId">X轴轴号</param>
        /// <param name="yAxisId">Y轴轴号</param>
        /// <param name="velMax">最大速度</param>
        /// <param name="accMax">最大加速度</param>
        /// <param name="evenTime">最小匀速段时间</param>
        /// <returns></returns>
        public bool CrdXYSetPrm(short csId, short xAxisId, short yAxisId, double velMax, double accMax, short evenTime = 50,short core = 2)
        {
            short sRtn;
            //dimension：坐标系的维数。取值范围：[1, 4]。
            //Profile[8]：坐标系与规划器的映射关系。Profile[0..7]对应规划轴 1~8，如果规划轴
            //没有对应到该坐标系，则 profile[x]的值为 0；如果对应到了 X 轴，则 profile[x]为 1，
            //Y 轴对应为 2，Z 轴对应为 3，A 轴对应为 4。不允许多个规划轴映射到相同坐标系
            //的相同坐标轴，也不允许把相同规划轴对应到不同的坐标系，否则该指令将会返回
            //错误值。每个元素的取值范围：[0, 4]。
            //synVelMax：该坐标系的最大合成速度。如果用户在输入插补段的时候所设置的目
            //标速度大于了该速度，则将会被限制为该速度。取值范围：(0, 32767)。单位：
            //pulse / ms。
            //synAccMax：该坐标系的最大合成加速度。如果用户在输入插补段的时候所设置的
            //加速度大于了该加速度，则将会被限制为该加速度。取值范围：(0, 32767)。单位：
            //pulse / ms2。
            //evenTime：每个插补段的最小匀速段时间。取值范围：[0, 32767)。单位：ms。
            //setOriginFlag：表示是否需要指定坐标系的原点坐标的规划位置，该参数可以方便
            //用户建立区别于机床坐标系的加工坐标系。0：不需要指定原点坐标值，则坐标系
            //的原点在当前规划位置上。1：需要指定原点坐标值，坐标系的原点在 originPos 指
            //定的规划位置上。
            //originPos[8]：指定的坐标系原点的规划位置值。
            TCrdPrm crdPrm;
            crdPrm.dimension = 2;            // 坐标系为二维坐标系
            crdPrm.synVelMax = velMax * 0.001;
            crdPrm.synAccMax = accMax * 0.001 * 0.001;
            crdPrm.evenTime = evenTime;
            crdPrm.profile1 = 0;
            crdPrm.profile2 = 0;
            crdPrm.profile3 = 0;
            crdPrm.profile4 = 0;
            crdPrm.profile5 = 0;
            crdPrm.profile6 = 0;
            crdPrm.profile7 = 0;
            crdPrm.profile8 = 0;
            crdPrm.setOriginFlag = 1;
            crdPrm.originPos1 = 0;
            crdPrm.originPos2 = 0;
            crdPrm.originPos3 = 0;
            crdPrm.originPos4 = 0;
            crdPrm.originPos5 = 0;
            crdPrm.originPos6 = 0;
            crdPrm.originPos7 = 0;
            crdPrm.originPos8 = 0;
            short[] axis = new short[2] { xAxisId, yAxisId };
            if (axis.Max() - axis.Min() >= 8)
            {
                //LogHelper.Error("轴号ID之间差值必须<8");
                ErrMessage("轴号ID之间差值必须<8");
                return false;
            }
            sRtn = mc.GTN_SetCrdMapBase(core, 1, axis.Min());
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            switch (xAxisId - axis.Min())
            {
                case 0:
                    crdPrm.profile1 = 1;
                    break;
                case 1:
                    crdPrm.profile2 = 1;
                    break;
                case 2:
                    crdPrm.profile3 = 1;
                    break;
                case 3:
                    crdPrm.profile4 = 1;
                    break;
                case 4:
                    crdPrm.profile5 = 1;
                    break;
                case 5:
                    crdPrm.profile6 = 1;
                    break;
                case 6:
                    crdPrm.profile7 = 1;
                    break;
                case 7:
                    crdPrm.profile8 = 1;
                    break;
                default:
                    break;
            }
            switch (yAxisId - axis.Min())
            {
                case 0:
                    crdPrm.profile1 = 2;
                    break;
                case 1:
                    crdPrm.profile2 = 2;
                    break;
                case 2:
                    crdPrm.profile3 = 2;
                    break;
                case 3:
                    crdPrm.profile4 = 2;
                    break;
                case 4:
                    crdPrm.profile5 = 2;
                    break;
                case 5:
                    crdPrm.profile6 = 2;
                    break;
                case 6:
                    crdPrm.profile7 = 2;
                    break;
                case 7:
                    crdPrm.profile8 = 2;
                    break;
                default:
                    break;
            }
            sRtn = mc.GTN_SetCrdPrm(core, csId, ref crdPrm);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            int lModeX;
            int lModeY;
            sRtn = mc.GTN_GetPrfMode(core, xAxisId, out lModeX, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_GetPrfMode(core, yAxisId, out lModeY, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            //轴运动模式。
            //0：点位运动，控制器上电后默认为该模式；
            //1：Jog 模式		
            //2：PT 模式；
            //3：电子齿轮模式；
            //4：Follow 模式；
            //5：插补模式；
            //6：Pvt 模式
            if (lModeX != 5 || lModeY != 5)
            {
                ErrMessage("没有进入插补模式!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 建立XYZ坐标系
        /// </summary>
        /// <param name="csId">坐标系号从1开始</param>
        /// <param name="xAxisId">X轴轴号</param>
        /// <param name="yAxisId">Y轴轴号</param>
        /// <param name="zAxisId">Z轴轴号</param>
        /// <param name="velMax">最大速度</param>
        /// <param name="accMax">最大加速度</param>
        /// <param name="evenTime">最小匀速段时间</param>
        /// <returns></returns>
        public bool CrdXYZSetPrm(short csId, short xAxisId, short yAxisId, short zAxisId, double velMax, double accMax, short evenTime = 50,short core = 2)
        {
            short sRtn;
            //dimension：坐标系的维数。取值范围：[1, 4]。
            //Profile[8]：坐标系与规划器的映射关系。Profile[0..7]对应规划轴 1~8，如果规划轴
            //没有对应到该坐标系，则 profile[x]的值为 0；如果对应到了 X 轴，则 profile[x]为 1，
            //Y 轴对应为 2，Z 轴对应为 3，A 轴对应为 4。不允许多个规划轴映射到相同坐标系
            //的相同坐标轴，也不允许把相同规划轴对应到不同的坐标系，否则该指令将会返回
            //错误值。每个元素的取值范围：[0, 4]。
            //synVelMax：该坐标系的最大合成速度。如果用户在输入插补段的时候所设置的目
            //标速度大于了该速度，则将会被限制为该速度。取值范围：(0, 32767)。单位：
            //pulse / ms。
            //synAccMax：该坐标系的最大合成加速度。如果用户在输入插补段的时候所设置的
            //加速度大于了该加速度，则将会被限制为该加速度。取值范围：(0, 32767)。单位：
            //pulse / ms2。
            //evenTime：每个插补段的最小匀速段时间。取值范围：[0, 32767)。单位：ms。
            //setOriginFlag：表示是否需要指定坐标系的原点坐标的规划位置，该参数可以方便
            //用户建立区别于机床坐标系的加工坐标系。0：不需要指定原点坐标值，则坐标系
            //的原点在当前规划位置上。1：需要指定原点坐标值，坐标系的原点在 originPos 指
            //定的规划位置上。
            //originPos[8]：指定的坐标系原点的规划位置值。
            TCrdPrm crdPrm;
            crdPrm.dimension = 3;            // 坐标系为二维坐标系
            crdPrm.synVelMax = velMax * 0.001;
            crdPrm.synAccMax = accMax * 0.001 * 0.001;
            crdPrm.evenTime = evenTime;
            crdPrm.profile1 = 0;
            crdPrm.profile2 = 0;
            crdPrm.profile3 = 0;
            crdPrm.profile4 = 0;
            crdPrm.profile5 = 0;
            crdPrm.profile6 = 0;
            crdPrm.profile7 = 0;
            crdPrm.profile8 = 0;
            crdPrm.setOriginFlag = 1;
            crdPrm.originPos1 = 0;
            crdPrm.originPos2 = 0;
            crdPrm.originPos3 = 0;
            crdPrm.originPos4 = 0;
            crdPrm.originPos5 = 0;
            crdPrm.originPos6 = 0;
            crdPrm.originPos7 = 0;
            crdPrm.originPos8 = 0;
            short[] axis = new short[3] { xAxisId, yAxisId, zAxisId };
            if (axis.Max() - axis.Min() >= 8)
            {
                ErrMessage("轴号ID之间差值必须<8");
                return false;
            }
            sRtn = mc.GTN_SetCrdMapBase(core, 1, axis.Min());
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            switch (xAxisId - axis.Min())
            {
                case 0:
                    crdPrm.profile1 = 1;
                    break;
                case 1:
                    crdPrm.profile2 = 1;
                    break;
                case 2:
                    crdPrm.profile3 = 1;
                    break;
                case 3:
                    crdPrm.profile4 = 1;
                    break;
                case 4:
                    crdPrm.profile5 = 1;
                    break;
                case 5:
                    crdPrm.profile6 = 1;
                    break;
                case 6:
                    crdPrm.profile7 = 1;
                    break;
                case 7:
                    crdPrm.profile8 = 1;
                    break;
                default:
                    break;
            }
            switch (yAxisId - axis.Min())
            {
                case 0:
                    crdPrm.profile1 = 2;
                    break;
                case 1:
                    crdPrm.profile2 = 2;
                    break;
                case 2:
                    crdPrm.profile3 = 2;
                    break;
                case 3:
                    crdPrm.profile4 = 2;
                    break;
                case 4:
                    crdPrm.profile5 = 2;
                    break;
                case 5:
                    crdPrm.profile6 = 2;
                    break;
                case 6:
                    crdPrm.profile7 = 2;
                    break;
                case 7:
                    crdPrm.profile8 = 2;
                    break;
                default:
                    break;
            }
            switch (zAxisId - axis.Min())
            {
                case 0:
                    crdPrm.profile1 = 3;
                    break;
                case 1:
                    crdPrm.profile2 = 3;
                    break;
                case 2:
                    crdPrm.profile3 = 3;
                    break;
                case 3:
                    crdPrm.profile4 = 3;
                    break;
                case 4:
                    crdPrm.profile5 = 3;
                    break;
                case 5:
                    crdPrm.profile6 = 3;
                    break;
                case 6:
                    crdPrm.profile7 = 3;
                    break;
                case 7:
                    crdPrm.profile8 = 3;
                    break;
                default:
                    break;
            }
            sRtn = mc.GTN_SetCrdPrm(core, csId, ref crdPrm);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            int lModeX;
            int lModeY;
            int lModeZ;
            sRtn = mc.GTN_GetPrfMode(core, xAxisId, out lModeX, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_GetPrfMode(core, yAxisId, out lModeY, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_GetPrfMode(core, zAxisId, out lModeZ, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            //轴运动模式。
            //0：点位运动，控制器上电后默认为该模式；
            //1：Jog 模式		
            //2：PT 模式；
            //3：电子齿轮模式；
            //4：Follow 模式；
            //5：插补模式；
            //6：Pvt 模式
            if (lModeX != 5 || lModeY != 5 || lModeZ != 5)
            {
                ErrMessage("没有进入插补模式!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 建立XYZR坐标系
        /// </summary>
        /// <param name="csId">坐标系号从1开始</param>
        /// <param name="xAxisId">X轴轴号</param>
        /// <param name="yAxisId">Y轴轴号</param>
        /// <param name="zAxisId">Z轴轴号</param>
        /// <param name="rAxisId">R轴轴号</param>
        /// <param name="velMax">最大速度</param>
        /// <param name="accMax">最大加速度</param>
        /// <param name="evenTime">最小匀速段时间</param>
        /// <returns></returns>
        public bool CrdXYZRSetPrm(short csId, short xAxisId, short yAxisId, short zAxisId, short rAxisId, double velMax, double accMax, short evenTime = 50,short core = 2)
        {
            short sRtn;
            //dimension：坐标系的维数。取值范围：[1, 4]。
            //Profile[8]：坐标系与规划器的映射关系。Profile[0..7]对应规划轴 1~8，如果规划轴
            //没有对应到该坐标系，则 profile[x]的值为 0；如果对应到了 X 轴，则 profile[x]为 1，
            //Y 轴对应为 2，Z 轴对应为 3，A 轴对应为 4。不允许多个规划轴映射到相同坐标系
            //的相同坐标轴，也不允许把相同规划轴对应到不同的坐标系，否则该指令将会返回
            //错误值。每个元素的取值范围：[0, 4]。
            //synVelMax：该坐标系的最大合成速度。如果用户在输入插补段的时候所设置的目
            //标速度大于了该速度，则将会被限制为该速度。取值范围：(0, 32767)。单位：
            //pulse / ms。
            //synAccMax：该坐标系的最大合成加速度。如果用户在输入插补段的时候所设置的
            //加速度大于了该加速度，则将会被限制为该加速度。取值范围：(0, 32767)。单位：
            //pulse / ms2。
            //evenTime：每个插补段的最小匀速段时间。取值范围：[0, 32767)。单位：ms。
            //setOriginFlag：表示是否需要指定坐标系的原点坐标的规划位置，该参数可以方便
            //用户建立区别于机床坐标系的加工坐标系。0：不需要指定原点坐标值，则坐标系
            //的原点在当前规划位置上。1：需要指定原点坐标值，坐标系的原点在 originPos 指
            //定的规划位置上。
            //originPos[8]：指定的坐标系原点的规划位置值。
            TCrdPrm crdPrm;
            crdPrm.dimension = 4;// 坐标系为二维坐标系
            crdPrm.synVelMax = velMax * 0.001;
            crdPrm.synAccMax = accMax * 0.001 * 0.001;
            crdPrm.evenTime = evenTime;
            crdPrm.profile1 = 0;
            crdPrm.profile2 = 0;
            crdPrm.profile3 = 0;
            crdPrm.profile4 = 0;
            crdPrm.profile5 = 0;
            crdPrm.profile6 = 0;
            crdPrm.profile7 = 0;
            crdPrm.profile8 = 0;
            crdPrm.setOriginFlag = 1;
            crdPrm.originPos1 = 0;
            crdPrm.originPos2 = 0;
            crdPrm.originPos3 = 0;
            crdPrm.originPos4 = 0;
            crdPrm.originPos5 = 0;
            crdPrm.originPos6 = 0;
            crdPrm.originPos7 = 0;
            crdPrm.originPos8 = 0;
            short[] axis = new short[4] { xAxisId, yAxisId, zAxisId, rAxisId };
            if (axis.Max() - axis.Min() >= 8)
            {
                ErrMessage("轴号ID之间差值必须<8");
                return false;
            }
            sRtn = mc.GTN_SetCrdMapBase(core, 1, axis.Min());
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            switch (xAxisId - axis.Min())
            {
                case 0:
                    crdPrm.profile1 = 1;
                    break;
                case 1:
                    crdPrm.profile2 = 1;
                    break;
                case 2:
                    crdPrm.profile3 = 1;
                    break;
                case 3:
                    crdPrm.profile4 = 1;
                    break;
                case 4:
                    crdPrm.profile5 = 1;
                    break;
                case 5:
                    crdPrm.profile6 = 1;
                    break;
                case 6:
                    crdPrm.profile7 = 1;
                    break;
                case 7:
                    crdPrm.profile8 = 1;
                    break;
                default:
                    break;
            }
            switch (yAxisId - axis.Min())
            {
                case 0:
                    crdPrm.profile1 = 2;
                    break;
                case 1:
                    crdPrm.profile2 = 2;
                    break;
                case 2:
                    crdPrm.profile3 = 2;
                    break;
                case 3:
                    crdPrm.profile4 = 2;
                    break;
                case 4:
                    crdPrm.profile5 = 2;
                    break;
                case 5:
                    crdPrm.profile6 = 2;
                    break;
                case 6:
                    crdPrm.profile7 = 2;
                    break;
                case 7:
                    crdPrm.profile8 = 2;
                    break;
                default:
                    break;
            }
            switch (zAxisId - axis.Min())
            {
                case 0:
                    crdPrm.profile1 = 3;
                    break;
                case 1:
                    crdPrm.profile2 = 3;
                    break;
                case 2:
                    crdPrm.profile3 = 3;
                    break;
                case 3:
                    crdPrm.profile4 = 3;
                    break;
                case 4:
                    crdPrm.profile5 = 3;
                    break;
                case 5:
                    crdPrm.profile6 = 3;
                    break;
                case 6:
                    crdPrm.profile7 = 3;
                    break;
                case 7:
                    crdPrm.profile8 = 3;
                    break;
                default:
                    break;
            }
            switch (rAxisId - axis.Min())
            {
                case 0:
                    crdPrm.profile1 = 4;
                    break;
                case 1:
                    crdPrm.profile2 = 4;
                    break;
                case 2:
                    crdPrm.profile3 = 4;
                    break;
                case 3:
                    crdPrm.profile4 = 4;
                    break;
                case 4:
                    crdPrm.profile5 = 4;
                    break;
                case 5:
                    crdPrm.profile6 = 4;
                    break;
                case 6:
                    crdPrm.profile7 = 4;
                    break;
                case 7:
                    crdPrm.profile8 = 4;
                    break;
                default:
                    break;
            }
            sRtn = mc.GTN_SetCrdPrm(core, csId, ref crdPrm);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            int lModeX;
            int lModeY;
            int lModeZ;
            int lModeR;
            sRtn = mc.GTN_GetPrfMode(core, xAxisId, out lModeX, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_GetPrfMode(core, yAxisId, out lModeY, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_GetPrfMode(core, zAxisId, out lModeZ, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_GetPrfMode(core, rAxisId, out lModeR, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            //轴运动模式。
            //0：点位运动，控制器上电后默认为该模式；
            //1：Jog 模式		
            //2：PT 模式；
            //3：电子齿轮模式；
            //4：Follow 模式；
            //5：插补模式；
            //6：Pvt 模式
            if (lModeX != 5 || lModeY != 5 || lModeZ != 5 || lModeR != 5)
            {
                ErrMessage("没有进入插补模式!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 初始化插补前瞻缓存区
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="T">拐弯时间</param>
        /// <param name="accMax">最大加速度</param>
        /// <param name="n">插补缓存区大小[0, 32767)</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool InitLookAhead(short crd, double T, double accMax, short n, short fifo = 0,short core = 2)
        {
            short sRtn;
            // 定义前瞻缓存区内存区  
            mc.TCrdData[] crdData = new mc.TCrdData[n];
            //T：范围1ms~10ms,T 越大，计算出来的终点速度越大，但却降低了加工精度
            if (T > 10 || T < 1)
            {
                T = 5;
            }
            sRtn = mc.GTN_InitLookAhead(core, crd, fifo, T, accMax, n, ref crdData[0]);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 启动插补运动
        /// </summary>
        /// <param name="crd">坐标系号</param>
        /// <param name="waitforend">是否需要等待</param>
        /// <returns></returns>
        public bool CrdMoveStart(short crd, bool waitforend,short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_CrdStart(core, (short)(1 << (crd - 1)), 0);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            if (waitforend)
            {
                return InterpWaitForEnd(crd, 0, core);
            }
            return true;
            if (core == 1)
            {
                sRtn = mc.GTN_CrdStart(core, (short)(1 << (crd - 1)), 0);
                return true;
            }
            else
            {
                sRtn = mc.GTN_CrdStart(core, (short)(1 << (crd - 1)), 0);
                _axisCheckWatch.Start();
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    return false;
                }
                if (waitforend)
                {
                    return MoveInterpolationWaitForEnd(crd, 0, core);
                }
            }

            return true;
        }

        /// <summary>
        /// 启动插补运动
        /// </summary>
        /// <param name="crd">坐标系号</param>
        /// <param name="waitforend">是否需要等待</param>
        /// <returns></returns>
        public bool CrdMoveResume(short crd,short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_CrdStart(core, (short)(1 << (crd - 1)), 0);
            _axisCheckWatch.Start();
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }


        private Stopwatch _axisCheckWatch = new Stopwatch();

        /// <summary>
        /// 等待坐标系插补运动结束
        /// </summary>
        /// <param name="crd">坐标系号</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool MoveInterpolationWaitForEnd(short crd, short fifo = 0,short core = 2)
        {
            _axisCheckWatch.Restart();
            short uRun = 1;
            int seg;
            _isCrdExits[crd - 1] = false;
            while (_isCrdExits[crd - 1] == false && (uRun == 1))
            {
                if (_axisCheckWatch.Elapsed.TotalMilliseconds > 60000)
                {
                    ErrMessage("没有进入插补模式!");
                    _axisCheckWatch.Stop();
                    return false;
                }

                if (_axisCheckWatch.IsRunning)
                {
                    //读取插补运动状态。0：该坐标系的该 FIFO 没有在运动；1：该坐标系的该 FIFO 正在进行插补运动。
                    short sRtn = mc.GTN_CrdStatus(core, crd, out uRun, out seg, fifo);
                    if (sRtn != 0)
                    {
                        ErrMessage(sRtn);
                        return false;
                    }
                }
                Thread.Sleep(1);
            }
            _axisCheckWatch.Stop();
            if (_isCrdExits[crd - 1])
            {
                _isCrdExits[crd - 1] = false;
                ErrMessage("坐标系退出!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 等待插补移动结束
        /// </summary>
        /// <param name="crd"></param>
        /// <param name="fifo"></param>
        /// <param name="core"></param>
        /// <returns></returns>
        public bool InterpWaitForEnd(short crd, short fifo = 0, short core = 2)
        {
            short uRun = 1;
            int seg;
            DateTime starttime = DateTime.Now;

            while (uRun == 1)
            {
                if ((DateTime.Now - starttime).TotalSeconds > 60000)
                {
                    Console.WriteLine("运动超过999s");
                    break;
                }

                //读取插补运动状态。0：该坐标系的该 FIFO 没有在运动；1：该坐标系的该 FIFO 正在进行插补运动。
                short sRtn = mc.GTN_CrdStatus(core, crd, out uRun, out seg, fifo);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    return false;
                }
                Thread.Sleep(1);
            }

            return true;
        }
        /// <summary>
        /// 实现刀向跟随功能，启动某个轴点位运动。绝对位置
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="moveAxis">需要进行点位运动的轴号，该轴不能处于坐标系中</param>
        /// <param name="pos">点位运动的目标位置，单位：pulse</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="modal">点位运动的模式 0不阻塞后续插补缓存区指令执行 1阻塞(合成速度,加速度)</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdBufMove(short crd, short moveAxis, int pos, double[] speed, short modal, short fifo = 0,short core = 2)
        {
            short sRtn;
            double[] fSpeed = { 0, 0, 0 };
            fSpeed[1] = speed[1] * 0.001;
            fSpeed[2] = speed[2] * 0.001 * 0.001;
            //内核，正整数
            //坐标系号。正整数
            //点位运动终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //点位运动的模式。
            //0：该指令为非模态指令，即不阻塞后续的插补缓存区指令的执行。
            //1：该指令为模态指令，将会阻塞后续的插补缓存区指令的执行。
            sRtn = mc.GTN_BufMove(core, crd, moveAxis, pos, fSpeed[1], fSpeed[2], 1, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 实现刀向跟随功能，启动某个轴跟随运动 相对位置
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="gearAxis">需要进行跟随运动的轴号，取值范围：[1, 8]。该轴不能处于坐标系中</param>
        /// <param name="pos">跟随运动的位移量</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdBufGear(short crd, short gearAxis, int pos, short fifo = 0,short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_BufGear(core, crd, gearAxis, pos, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 查询插补缓存区剩余空间
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="pSpace">插补缓存区中的剩余空间</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdBufSpace(short crd, ref int pSpace, short fifo = 0,short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_CrdSpace(core, crd, out pSpace, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 查询插补运动坐标系状态
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="run">读取插补运动状态。0：该坐标系的该 FIFO 没有在运动；1：该坐标系的该 FIFO正在进行插补运动</param>
        /// <param name="segment">读取当前已经完成的插补段数</param>
        /// <param name="fifo">所要查询运动状态的插补缓存区号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdStatus(short crd, ref short run, ref int segment, short fifo = 0,short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_CrdStatus(core, crd, out run, out segment, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 停止坐标系运动
        /// </summary>
        /// <param name="axisId">坐标系的轴号</param>
        /// <returns></returns>
        public bool StopCrdMove(short crd, short axisId, short core = 2)
        {
            short sRtn;
            int mask = 0;

            mask |= 1 << (axisId - 1);
            sRtn = mc.GTN_Stop(core, mask, mask);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            ExitCrd(core,axisId);//先停止轴运动再退出等待循环  避免出现一个轴跑到一半 另一个轴同步开始动作
            return true;
        }

        /// <summary>
        /// 暂停坐标系运动
        /// </summary>
        /// <param name="axisId">坐标系的任意一轴</param>
        /// <returns></returns>
        public bool PauseCrdMove(int axisId,short core = 2)
        {
            short sRtn;
            int mask = 0;
            _axisCheckWatch.Stop();
            mask |= 1 << (axisId - 1);
            sRtn = mc.GTN_Stop(core, mask, mask);
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
