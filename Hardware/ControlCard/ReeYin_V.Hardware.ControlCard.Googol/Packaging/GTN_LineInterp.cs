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
    /// 直线插补
    /// </summary>
    public partial class GoogolGTMotion
    {

        #region 直线插补
        /// <summary>
        /// XY 平面二维直线插补
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="xy">XY坐标(pluse)</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToPointXY(short crd, int[] xy, double[] speed, short fifo = 0, short core = 2)
        {
            short sRtn;
            double[] fSpeed = { 0, 0, 0 };
            fSpeed[0] = speed[0] * 0.001;
            fSpeed[1] = speed[1] * 0.001 * 0.001;
            fSpeed[2] = speed[2] * 0.001;
            //XY 平面二维直线插补
            //内核，正整数
            //坐标系号。正整数
            //插补段 x 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 y 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //插补段的终点速度。取值范围：[0, 32767)，单位：pulse/ms。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为：0
            sRtn = mc.GTN_LnXY(core, crd, xy[0], xy[1], fSpeed[0], fSpeed[1], fSpeed[2], fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// XYZ 平面三维直线插补
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="xyz">XYZ坐标(pluse)</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToPointXYZ(short crd, int[] xyz, double[] speed, short fifo = 0, short core = 2)
        {
            short sRtn;
            double[] fSpeed = { 0, 0, 0 };
            fSpeed[0] = speed[0] * 0.001;
            fSpeed[1] = speed[1] * 0.001 * 0.001;
            fSpeed[2] = speed[2] * 0.001;
            //XYZ 平面二维直线插补
            //内核，正整数
            //坐标系号。正整数
            //插补段 x 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 y 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 z 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //插补段的终点速度。取值范围：[0, 32767)，单位：pulse/ms。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为：0
            sRtn = mc.GTN_LnXYZ(core, crd, xyz[0], xyz[1], xyz[2], fSpeed[0], fSpeed[1], fSpeed[2], fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// XYZR 平面四维直线插补
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="xyzr">XYZR坐标(pluse)</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToPointXYZR(short crd, int[] xyzr, double[] speed, short fifo = 0, short core = 2)
        {
            short sRtn;
            double[] fSpeed = { 0, 0, 0 };
            fSpeed[1] = speed[1] * 0.001;
            fSpeed[2] = speed[2] * 0.001 * 0.001;
            //XYZ 平面二维直线插补
            //内核，正整数
            //坐标系号。正整数
            //插补段 x 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 y 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 z 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 r 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //插补段的终点速度。取值范围：[0, 32767)，单位：pulse/ms。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为：0
            sRtn = mc.GTN_LnXYZA(core, crd, xyzr[0], xyzr[1], xyzr[2], xyzr[3], fSpeed[1], fSpeed[2], 0, fifo);
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
