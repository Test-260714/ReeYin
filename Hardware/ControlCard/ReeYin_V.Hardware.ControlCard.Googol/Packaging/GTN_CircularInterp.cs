using GTN;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static GTN.mc;
using static System.Formats.Asn1.AsnWriter;

namespace GoogolMotion
{
    /// <summary>
    /// 直线插补
    /// </summary>
    public partial class GoogolGTMotion
    {
        #region 圆弧插补
        /// <summary>
        /// 缓存区指令，XY 平面圆弧插补(以终点位置和半径为输入参数)
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="xyendpoint">XY坐标(pluse)</param>
        /// <param name="radius">圆弧插补的圆弧半径值</param>
        /// <param name="circleDir">圆弧的旋转方向</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="velEnd">终点速度</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToArcXYR(short crd, int[] xyendpoint, double radius, short circleDir, double[] speed, double velEnd = 0, short fifo = 0, short core = 2)
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
            //radius 圆弧插补的圆弧半径值。取值范围：[-4194304, 4194303]，单位：pulse
            //半径为正时，表示圆弧为小于等于 180°圆弧
            //半径为负时，表示圆弧为大于 180°圆弧
            //半径描述方式不能用来描述整圆。
            //circleDir 圆弧的旋转方向
            //0：顺时针圆弧。
            //1：逆时针圆弧。
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //插补段的终点速度。取值范围：[0, 32767)，单位：pulse/ms。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为：0
            sRtn = mc.GTN_ArcXYR(core, crd, xyendpoint[0], xyendpoint[1], radius, circleDir, fSpeed[1], fSpeed[2], velEnd, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存区指令，XY 平面圆弧插补(使用圆心描述方法描述圆弧)
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="xyendpoint">XY坐标(pluse)</param>
        /// <param name="xCenter">圆弧插补的圆心 x 方向相对于起点位置的偏移量</param>
        /// <param name="yCenter">圆弧插补的圆心 y 方向相对于起点位置的偏移量</param>
        /// <param name="circleDir">圆弧的旋转方向</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="velEnd">终点速度</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToArcXYC(short crd, int[] xyendpoint, int[] Center, short circleDir, double[] speed, double velEnd = 0, short fifo = 0, short core = 2)
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
            //radius 圆弧插补的圆弧半径值。取值范围：[-4194304, 4194303]，单位：pulse
            //半径为正时，表示圆弧为小于等于 180°圆弧
            //半径为负时，表示圆弧为大于 180°圆弧
            //半径描述方式不能用来描述整圆。
            //circleDir 圆弧的旋转方向
            //0：顺时针圆弧。
            //1：逆时针圆弧。
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //插补段的终点速度。取值范围：[0, 32767)，单位：pulse/ms。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为：0
            sRtn = mc.GTN_ArcXYC(core, crd, xyendpoint[0], xyendpoint[1], Center[0], Center[1], circleDir, fSpeed[1], fSpeed[2], velEnd, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存区指令，YZ 平面圆弧插补(以终点位置和半径为输入参数)
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="yzendpoint">YZ坐标(pluse)</param>
        /// <param name="radius">圆弧插补的圆弧半径值</param>
        /// <param name="circleDir">圆弧的旋转方向</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="velEnd">终点速度</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToArcYZR(short crd, int[] yzendpoint, double radius, short circleDir, double[] speed, double velEnd = 0, short fifo = 0, short core = 2)
        {
            short sRtn;
            double[] fSpeed = { 0, 0, 0 };
            fSpeed[1] = speed[1] * 0.001;
            fSpeed[2] = speed[2] * 0.001 * 0.001;
            //XYZ 平面二维直线插补
            //内核，正整数
            //坐标系号。正整数
            //插补段 y 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 z 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //radius 圆弧插补的圆弧半径值。取值范围：[-4194304, 4194303]，单位：pulse
            //半径为正时，表示圆弧为小于等于 180°圆弧
            //半径为负时，表示圆弧为大于 180°圆弧
            //半径描述方式不能用来描述整圆。
            //circleDir 圆弧的旋转方向
            //0：顺时针圆弧。
            //1：逆时针圆弧。
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //插补段的终点速度。取值范围：[0, 32767)，单位：pulse/ms。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为：0
            sRtn = mc.GTN_ArcYZR(core, crd, yzendpoint[0], yzendpoint[1], radius, circleDir, fSpeed[1], fSpeed[2], velEnd, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存区指令，YZ 平面圆弧插补(使用圆心描述方法描述圆弧)
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="yzendpoint">YZ坐标(pluse)</param>
        /// <param name="yCenter">圆弧插补的圆心 x 方向相对于起点位置的偏移量</param>
        /// <param name="zCenter">圆弧插补的圆心 y 方向相对于起点位置的偏移量</param>
        /// <param name="circleDir">圆弧的旋转方向</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="velEnd">终点速度</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToArcYZC(short crd, int[] yzendpoint, double yCenter, double zCenter, short circleDir, double[] speed, double velEnd = 0, short fifo = 0, short core = 2)
        {
            short sRtn;
            double[] fSpeed = { 0, 0, 0 };
            fSpeed[1] = speed[1] * 0.001;
            fSpeed[2] = speed[2] * 0.001 * 0.001;
            //XYZ 平面二维直线插补
            //内核，正整数
            //坐标系号。正整数
            //插补段 y 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 z 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //radius 圆弧插补的圆弧半径值。取值范围：[-4194304, 4194303]，单位：pulse
            //半径为正时，表示圆弧为小于等于 180°圆弧
            //半径为负时，表示圆弧为大于 180°圆弧
            //半径描述方式不能用来描述整圆。
            //circleDir 圆弧的旋转方向
            //0：顺时针圆弧。
            //1：逆时针圆弧。
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //插补段的终点速度。取值范围：[0, 32767)，单位：pulse/ms。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为：0
            sRtn = mc.GTN_ArcYZC(core, crd, yzendpoint[0], yzendpoint[1], yCenter, zCenter, circleDir, fSpeed[1], fSpeed[2], velEnd, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存区指令，ZX 平面圆弧插补(以终点位置和半径为输入参数)
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="zxendpoint">ZX坐标(pluse)</param>
        /// <param name="radius">圆弧插补的圆弧半径值</param>
        /// <param name="circleDir">圆弧的旋转方向</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="velEnd">终点速度</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToArcZXR(short crd, int[] zxendpoint, double radius, short circleDir, double[] speed, double velEnd = 0, short fifo = 0, short core = 2)
        {
            short sRtn;
            double[] fSpeed = { 0.0, 0.0, 0.0 };
            fSpeed[1] = speed[1] * 0.001;
            fSpeed[2] = speed[2] * 0.001 * 0.001;
            //XYZ 平面二维直线插补
            //内核，正整数
            //坐标系号。正整数
            //插补段 z 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 x 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //radius 圆弧插补的圆弧半径值。取值范围：[-4194304, 4194303]，单位：pulse
            //半径为正时，表示圆弧为小于等于 180°圆弧
            //半径为负时，表示圆弧为大于 180°圆弧
            //半径描述方式不能用来描述整圆。
            //circleDir 圆弧的旋转方向
            //0：顺时针圆弧。
            //1：逆时针圆弧。
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //插补段的终点速度。取值范围：[0, 32767)，单位：pulse/ms。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为：0
            sRtn = mc.GTN_ArcZXR(core, crd, zxendpoint[0], zxendpoint[1], radius, circleDir, fSpeed[1], fSpeed[2], velEnd, fifo);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存区指令，ZX 平面圆弧插补(使用圆心描述方法描述圆弧)
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <param name="zxendpoint">ZX坐标(pluse)</param>
        /// <param name="zCenter">圆弧插补的圆心 z 方向相对于起点位置的偏移量</param>
        /// <param name="xCenter">圆弧插补的圆心 x 方向相对于起点位置的偏移量</param>
        /// <param name="circleDir">圆弧的旋转方向</param>
        /// <param name="speed">速度(合成速度,加速度)</param>
        /// <param name="velEnd">终点速度</param>
        /// <param name="fifo">缓存区编号默认0 1为辅助运动</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToArcZXC(short crd, int[] zxendpoint, double zCenter, double xCenter, short circleDir, double[] speed, double velEnd = 0, short fifo = 0, short core = 2)
        {
            short sRtn;
            double[] fSpeed = { 0, 0, 0 };
            fSpeed[1] = speed[1] * 0.001;
            fSpeed[2] = speed[2] * 0.001 * 0.001;
            //XYZ 平面二维直线插补
            //内核，正整数
            //坐标系号。正整数
            //插补段 z 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //插补段 x 轴终点坐标值。取值范围：[-1073741824, 1073741823]，单位：pulse
            //radius 圆弧插补的圆弧半径值。取值范围：[-4194304, 4194303]，单位：pulse
            //半径为正时，表示圆弧为小于等于 180°圆弧
            //半径为负时，表示圆弧为大于 180°圆弧
            //半径描述方式不能用来描述整圆。
            //circleDir 圆弧的旋转方向
            //0：顺时针圆弧。
            //1：逆时针圆弧。
            //插补段的目标合成速度。取值范围：(0, 32767)，单位：pulse/ms
            //插补段的合成加速度。取值范围：(0, 32767)，单位：pulse/ms2
            //插补段的终点速度。取值范围：[0, 32767)，单位：pulse/ms。该值只有在没有使用前瞻预处理功能时才有意义，否则该值无效。默认值为：0
            sRtn = mc.GTN_ArcZXC(core, crd, zxendpoint[0], zxendpoint[1], zCenter, xCenter, circleDir, fSpeed[1], fSpeed[2], velEnd, fifo);
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
