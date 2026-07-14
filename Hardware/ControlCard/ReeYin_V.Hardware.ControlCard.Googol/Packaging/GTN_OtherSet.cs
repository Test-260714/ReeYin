using NLog.LayoutRenderers;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using static GTN.mc;

namespace GoogolMotion
{
    /// <summary>
    /// 其他设置
    /// </summary>
    public partial class GoogolGTMotion
    {
        #region 位置补偿
        /// <summary>
        /// 二维位置补偿(球面)
        /// </summary>
        /// <returns></returns>
        public bool SetCompensate2DPos(double[] input, short core = 2)
        {
            try
            {
                int X_Scale = 10000;
                int Y_Scale = 10000;
                int Z_Scale = 2000;   //当量
                int i, j;
                short rtn;
                short M, N;                                                        //M行点数，N列点数
                double m, n;                                                       //行间距，列间距
                double R = X_Scale * Convert.ToDouble(input[0]);              //工件球心半径
                double a = X_Scale * Convert.ToDouble(input[1]);              //工件长度a
                double b = Y_Scale * Convert.ToDouble(input[2]);              //工件宽度b

                double x1 = X_Scale * Convert.ToDouble(input[3]);             //顶点坐标值x
                double y1 = Y_Scale * Convert.ToDouble(input[4]);             //顶点坐标值y
                double xPosNow = X_Scale * Convert.ToDouble(input[5]);        //起始点X轴位置
                double yPosNow = Y_Scale * Convert.ToDouble(input[6]);        //起始点Y轴位置
                double zPosNow = Z_Scale * Convert.ToDouble(input[7]);        //起始点Z轴位置
                TCompensate2DTable pTable = new TCompensate2DTable();
                TCompensate2D pComp2D = new TCompensate2D();
                //计算行数和列数

                N = Convert.ToInt16(Math.Sqrt(25600 * b / a));             //行数
                M = Convert.ToInt16(25600 / N);                            //列数

                //计算行间距和列间距
                m = a / M;                                      //行间距，单位：pulse
                n = b / N;                                      //列间距，单位：pulse

                double[] table1 = new double[25600];

                double[] X = new double[25600];
                double[] Y = new double[25600];
                double[,] table2 = new double[160, 160];

                double z;                                       //工件任意一点与圆心的距离z轴距离
                double z0;                                      //球心z轴坐标
                double Z;                                       //工件任意点的z轴坐标
                double Z1;                                      //工件任意一点相对于最高点的补偿值
                double Z2;                                      //工件相对于任意起始点的补偿值
                double Z3;                                      //起始点的Z轴坐标
                double Z4;                                      //任意点补偿后坐标

                for (i = 0; i < N; i++)
                {
                    for (j = 0; j < M; j++)
                    {
                        z = R * R - ((n * i - b / 2) * (n * i - b / 2)) - ((m * j - a / 2) * (m * j - a / 2));
                        z0 = R * R - (a * a + b * b) / 4;
                        Z = Math.Sqrt(z) - Math.Sqrt(z0);
                        Z4 = R - Math.Sqrt(z0);
                        //任一点Z轴坐标
                        Z1 = (Z - (R - Math.Sqrt(z0)));              //任意点相对于最高点的补偿值

                        Z3 = zPosNow;                                //起始点坐标	

                        Z2 = (Z - Z3);                               //工件表面任意点相对于起始点的补偿值			

                        table2[i, j] = Z1;                           //Z向下为正，所以数据全部取反

                        table1[i * M + j] = Z1;
                    }
                }
                //补偿表类型转换
                int[] data = new int[25600];
                for (i = 0; i < 25600; i++)
                {
                    data[i] = Convert.ToInt32(table1[i]);
                }

                pTable.count1 = M;
                pTable.count2 = N;
                pTable.posBegin1 = Convert.ToInt32(x1 - a / 2);            //补偿起点从开始
                pTable.posBegin2 = Convert.ToInt32(y1 - b / 2);
                pTable.step1 = Convert.ToInt32(a / (M - 1));               //补偿步长为
                pTable.step2 = Convert.ToInt32(b / (N - 1));
                //ExternComp = 0; //是否自动扩展补偿区域。
                rtn = GTN_SetCompensate2DTable(core, 1, ref pTable, ref data[0], 0); //写入补偿
                Console.WriteLine($"GTN_SetCompensate2DTable" + rtn);

                pComp2D.enable = 1;                                 //误差补偿使能
                pComp2D.tableIndex = 1;                             //补偿表，定为第四套补偿表
                pComp2D.axisType1 = MC_ENCODER;                     //查表所使用的 X 位置为规划位置
                pComp2D.axisIndex1 = 1;                             //1 轴作为二维补偿运动的 X 轴
                pComp2D.axisType2 = MC_ENCODER;                     //查表所使用Y位置为规划位置，与X位置一致
                pComp2D.axisIndex2 = 2;                             //2 轴作为二维补偿运动的 Y 轴
                rtn = GTN_SetCompensate2D(core, 5, ref pComp2D);
                Console.WriteLine($"GTN_SetCompensate2D" + rtn);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                return false;
            }
        }

        /// <summary>
        /// 取消二维位置补偿
        /// </summary>
        /// <returns></returns>
        public bool CancelCompensate2DPos(short core = 2)
        {
            try
            {
                short rtn;

                TCompensate2D pComp2D = new TCompensate2D();
                pComp2D.enable = 0;                                 //误差补偿使能
                pComp2D.tableIndex = 1;                             //补偿表，定为第四套补偿表
                pComp2D.axisType1 = MC_ENCODER;                     //查表所使用的 X 位置为规划位置
                pComp2D.axisIndex1 = 1;                             //1 轴作为二维补偿运动的 X 轴
                pComp2D.axisType2 = MC_ENCODER;                     //查表所使用Y位置为规划位置，与X位置一致
                pComp2D.axisIndex2 = 2;                             //2 轴作为二维补偿运动的 Y 轴
                rtn = GTN_SetCompensate2D(core, 5, ref pComp2D);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"");
                return false;
            }
        }

        /// <summary>
        /// 螺距误差补偿
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="startPos">开始位置</param>
        /// <param name="lenPos">补偿区域总长</param>
        /// <param name="posN">反向补偿点</param>
        /// <param name="posP">正向补偿点</param>
        /// <returns></returns>
        public bool SetScrewCompensate(short axis, int startPos, int lenPos, int[] posN, int[] posP, short core = 2)
        {
            try
            {
                short rtn;
                short n = 1;
                //posN = new int[10] { 10, 20, 10, 20, 10, 20, 10, 20, 10, 20 };    //正向补偿数据，此处举例补偿10个点（可以通过激光干涉仪打标确定）
                //posP = new int[10] { 10, 20, 10, 20, 10, 20, 10, 20, 10, 20 };    //负向补偿数据。
                n = (short)posP.Length;

                rtn = GTN_SetLeadScrewComp(core, axis, n, startPos, lenPos, ref posP[0], ref posN[0]);
                Console.WriteLine($"GTN_SetLeadScrewComp()_Result:{rtn}");

                rtn = GTN_EnableLeadScrewComp(core, axis, 1);
                Console.WriteLine($"GTN_EnableLeadScrewComp()_Result:{rtn}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// 取消螺距误差补偿
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <returns></returns>
        public bool CancelScrewCompensate(short axis, short core = 2)
        {
            try
            {
                short rtn;
                //mode：1表示开启/0表示关闭
                rtn = GTN_EnableLeadScrewComp(core, axis, 0);
                Console.WriteLine($"GTN_EnableLeadScrewComp()_Result:{rtn}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                return false;
            }
        }


        #endregion

        #region 反向间隙补偿

        #endregion

        #region 辅助方法
        public static List<(double X, double Y)> GetPointsOnLine(
        double Sx, double Sy,
        double Ex, double Ey,
        double step)
        {
            var points = new List<(double X, double Y)>();

            // 线段向量
            double dx = Ex - Sx;
            double dy = Ey - Sy;

            // 线段长度
            double length = Math.Sqrt(dx * dx + dy * dy);

            // 特殊情况：起终点重合
            if (length == 0)
            {
                points.Add((Sx, Sy));
                return points;
            }

            // 单位方向向量
            double ux = dx / length;
            double uy = dy / length;

            // 点的数量（包含起点和终点）
            int count = (int)(length / step);

            // 添加每个间隔点
            for (int i = 0; i <= count; i++)
            {
                double px = Sx + ux * step * i;
                double py = Sy + uy * step * i;
                points.Add((px, py));
            }

            // 确保加上终点
            if (points[points.Count - 1].X != Ex || points[points.Count - 1].Y != Ey)
                points.Add((Ex, Ey));

            return points;
        }
        #endregion
    }

}
