using ReeYin_V.Core.Events;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    /// <summary>
    /// 圆弧插补
    /// </summary>
    public partial class GoogolControlCard : ControlCardBase
    {
        /// <summary>
        /// XY圆弧插补运动
        /// (和CustomInterpolationMoving联用)
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override bool ArcInterpoMoving(ArcInterPoParam param)
        {
            try
            {
                int[] Destination = [(int)(Config.DefaultInterpCS.PulseEquivalent * param.Destination.X), 
                    (int)(Config.DefaultInterpCS.PulseEquivalent * param.Destination.Y)];
                short circleDir = param.Dir == DirOfRotation.顺时针 ? (short)0 : (short)1;

                //执行圆弧插补时直接压入会提示指令错误，先压入一条直线在写入圆弧就没问题
                var targetZWithDecs = new float[Enum.GetValues(typeof(AxisStatusFlags)).Length];
                CrdMoveAbsoluteToPointXY(Coordinate, 
                    [100, 100]);

                switch (param.DrawArcMethod)
                {
                    case DrawArc.Angle:
                        {
                            //已知两点和圆心角 求出圆心坐标和半径
                            if (!CalArcCenterAndRadius(param.Origin, param.Destination, param.Angle, out Point center, out double radius))
                            {
                                Console.WriteLine($"计算圆心坐标和半径失败");
                                return false;
                            }
                            param.Center = center;
                            param.Radius = radius;

                            foreach (var core in Cores)
                            {
                                //使用XY轴进行圆弧插补
                                if (!Motion.CrdMoveAbsoluteToArcXYR(Coordinate, [Destination[0], Destination[1]], param.Radius * Config.DefaultInterpCS.PulseEquivalent, circleDir, ConvertToPluse(En_AxisNum.X, GetSpeed(En_AxisNum.X, param.DefaultSpeed)),0,0,core))
                                {
                                    Console.WriteLine($"CrdMoveAbsoluteToArcXYR()_圆弧插补失败");
                                }
                                //if (!Motion.CrdMoveAbsoluteToPointXY(1, point, RunineSpeed[csid - 1], 0, core))
                                //{
                                //    return false;
                                //}

                            }

                        }break;

                    case DrawArc.Radius:
                        {
                            foreach (var core in Cores)
                            {
                                if (!Motion.CrdMoveAbsoluteToArcXYR(Coordinate, [Destination[0], Destination[1]], param.Radius * Config.DefaultInterpCS.PulseEquivalent, circleDir, ConvertToPluse(En_AxisNum.X, GetSpeed(En_AxisNum.X, param.DefaultSpeed)),0,0, core))
                                {
                                    Console.WriteLine($"CrdMoveAbsoluteToArcXYR()_圆弧插补失败");
                                }

                            }

                        }break;

                    case DrawArc.Center:
                        {
                            int[] Center = [
                                (int)(Config.DefaultInterpCS.PulseEquivalent * (param.Center.X - param.Origin.X)),
                                (int)(Config.DefaultInterpCS.PulseEquivalent * (param.Center.Y - param.Origin.Y))
                            ];

                            // GTN_ArcXYC 使用“终点绝对坐标 + 圆心相对起点偏移”描述圆弧。
                            // 当终点与当前起点相同且圆心有效时，可直接表达整圆。
                            //使用XY轴进行圆弧插补
                            if (!Motion.CrdMoveAbsoluteToArcXYC(Coordinate, [Destination[0], Destination[1]], Center, circleDir, ConvertToPluse(En_AxisNum.X, GetSpeed(En_AxisNum.X, param.DefaultSpeed))))
                            {
                                Console.WriteLine($"CrdMoveAbsoluteToArcXYC()_圆弧插补失败");
                            }
                            
                        }break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ArcInterpoMoving()_圆弧插补运动异常：{ex.StackTrace}");
                return false;
            }
        }

        #region Methods
        /// <summary>
        /// 已知两点和圆心角 求出圆心坐标和半径
        /// </summary>
        /// <param name="Origin">起点</param>
        /// <param name="Destination">终点</param>
        /// <param name="angle">角度</param>
        /// <param name="Center">圆心</param>
        /// <param name="Radius">半径</param>
        /// <returns></returns>
        public bool CalArcCenterAndRadius(Point Origin, Point Destination, double angle,out Point Center, out double Radius)
        {
            //角度转为弧度
            angle = angle * Math.PI / 180;

            // 计算中点坐标
            double mx = (Origin.X + Destination.X) / 2;
            double my = (Origin.Y + Destination.Y) / 2;

            // 计算起点和终点连线的向量
            double dx = Destination.X - Origin.X;
            double dy = Destination.Y - Origin.Y;

            // 计算向量的垂直平分线方向
            double nx = -dy;
            double ny = dx;

            // 计算起点和终点之间的距离
            double d = Math.Sqrt(dx * dx + dy * dy);

            // 计算半径
            Radius = d / (2 * Math.Sin(angle / 2));

            // 计算圆心坐标
            double cosHalfAngle = Math.Cos(angle / 2);
            double sinHalfAngle = Math.Sin(angle / 2);
            double factor = Radius * sinHalfAngle / d;

            Center.X = mx + nx * factor;
            Center.Y = my + ny * factor;

            return true;
        }

        #endregion

    }
}
