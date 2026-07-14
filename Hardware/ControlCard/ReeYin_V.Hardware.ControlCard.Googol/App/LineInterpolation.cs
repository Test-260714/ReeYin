using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    /// <summary>
    /// 直线插补
    /// </summary>
    public partial class GoogolControlCard : ControlCardBase
    {
        /// <summary>
        /// 插补坐标系轴号
        /// </summary>
        public short Coordinate = 1;


        /// <summary>
        /// 添加一条XY插补运动
        /// </summary>
        /// <param name="csid">坐标系号1~8</param>
        /// <param name="pos">点位</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToPointXY(short csid, double[] pos)
        {
            int[] point = new int[2];
            point[0] = (int)(Config.DefaultInterpCS.PulseEquivalent * pos[0]);
            point[1] = (int)(Config.DefaultInterpCS.PulseEquivalent * pos[1]);
            foreach (var core in Cores)
            {
                if (!Motion.CrdMoveAbsoluteToPointXY(1, point, 
                    [
                        Config.DefaultInterpCS.MaxSpeed * Config.DefaultInterpCS.PulseEquivalent,
                        Config.DefaultInterpCS.AccSpeed * Config.DefaultInterpCS.PulseEquivalent,
                        Config.DefaultInterpCS.EndSpeed * Config.DefaultInterpCS.PulseEquivalent,
                    ], 0, core))
                {
                    return false;
                }
            }
            return true;
            //return Motion.CrdMoveAbsoluteToPointXY(1, point, RunineSpeed[csid - 1]);
        }

        /// <summary>
        /// 添加一条XYZ插补运动
        /// </summary>
        /// <param name="csid">坐标系号1~8</param>
        /// <param name="pos">点位</param>
        /// <param name="speedType">速度</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToPointXYZ(short csid, double[] pos)
        {
            int[] point = new int[3];
            point[0] = (int)(Config.DefaultInterpCS.PulseEquivalent * pos[0]);
            point[1] = (int)(Config.DefaultInterpCS.PulseEquivalent * pos[1]);
            point[2] = (int)(Config.DefaultInterpCS.PulseEquivalent * pos[2]);
            foreach (var core in Cores)
            {
                if (!Motion.CrdMoveAbsoluteToPointXYZ(1, point,
                     [
                        Config.DefaultInterpCS.MaxSpeed * Config.DefaultInterpCS.PulseEquivalent,
                        Config.DefaultInterpCS.AccSpeed * Config.DefaultInterpCS.PulseEquivalent,
                        Config.DefaultInterpCS.EndSpeed * Config.DefaultInterpCS.PulseEquivalent,
                    ]
                    , 0, core))
                {
                    return false;
                }

            }
            return true;
        }

        public bool CrdMoveAbsoluteToPointXYZ(short csid, double[] pos, double[] speed, En_AxisNum speedAxis)
        {
            return Motion.CrdMoveAbsoluteToPointXYZ(1, [
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[0]),
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[1]),
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[2])
                ], ConvertToPluse(speedAxis, speed));
        }

        /// <summary>
        /// 添加一条XYZR插补运动
        /// </summary>
        /// <param name="csid">坐标系号1~8</param>
        /// <param name="pos">点位</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToPointXYZR(short csid, double[] pos, EN_SpeedType speedType, En_AxisNum speedAxis = En_AxisNum.X)
        {
            return Motion.CrdMoveAbsoluteToPointXYZR(1, [
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[0]),
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[1]),
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[2]),
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[3])
                ], ConvertToPluse(speedAxis, GetSpeed(speedAxis, speedType)));
        }

        /// <summary>
        /// 添加一条XYZR插补运动
        /// </summary>
        /// <param name="csid">坐标系号1~8</param>
        /// <param name="pos">点位</param>
        /// <returns></returns>
        public bool CrdMoveAbsoluteToPointXYZR(short csid, double[] pos, double[] speed, En_AxisNum speedAxis)
        {
            return Motion.CrdMoveAbsoluteToPointXYZR(1, [
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[0]),
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[1]),
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[2]),
                (int)(Config.DefaultInterpCS.PulseEquivalent * pos[3])
                ], ConvertToPluse(speedAxis, speed));
        }

        public bool MoveToSafeZ()
        {
            SetSpeedAll(EN_SpeedType.Work);
            var targetAxisNumZs = new List<En_AxisNum> { En_AxisNum.Z, En_AxisNum.Z1, En_AxisNum.Z2 };

            // 使用LINQ查询获取所有匹配的AxisNo
            var AxisZs = Config.AllAxis.Where(model => targetAxisNumZs.Contains(model.AxisNum)).Select(model => model.AxisNum).ToList();

            foreach (En_AxisNum axis in AxisZs)
            {
                MoveAbsoluteAxis(axis, ConvertAxis(axis).SafetyDis, false);
            }
            var result = WaitAxisMoveEnd();
            return result;
        }

        /// <summary>
        /// 执行直线插补运动
        /// (和CustomInterpolationMoving联用)
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override bool LineInterpoMoving(LineInterPoParam param)
        {
            // 检查是否需要复位
            if (IsNeedReset)
            {
                Console.WriteLine("请先复位后，再移动轴");
                return false;
            }

            try
            {
                //移动前先判断是否满足限位条件
                foreach (var item in param.TargetPosDic)
                {
                    if (!ValidateLimitPosition(item.Key, item.Value, out string mas))
                    {
                        Logs.LogError($"轴{item.Key}，目标值{item.Value}超出了限位，具体信息：{mas}");
                        return false;
                    }
                }

                //两轴
                if(param.InterPoAxiss.Count == 2)
                {
                    CrdMoveAbsoluteToPointXY(Coordinate, new double[] { param.TargetPosDic[param.InterPoAxiss[0]],
                        param.TargetPosDic[param.InterPoAxiss[1]] });
                }
                //三轴
                else if(param.InterPoAxiss.Count == 3)
                {
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { param.TargetPosDic[param.InterPoAxiss[0]],
                        param.TargetPosDic[param.InterPoAxiss[1]], param.TargetPosDic[param.InterPoAxiss[2]] });
                }
                //四轴
                else if (param.InterPoAxiss.Count == 4)
                {
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { param.TargetPosDic[param.InterPoAxiss[0]],
                        param.TargetPosDic[param.InterPoAxiss[1]], param.TargetPosDic[param.InterPoAxiss[2]],param.TargetPosDic[param.InterPoAxiss[3]]});
                }

                //if (!success) return false;

                //// 执行移动并返回结果
                //return CrdData(Coordinate) && CrdMoveStart(Coordinate, param.waitforend);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"安全移动发生异常: {ex.StackTrace}");
                return false;
            }
        }


        #region Discard
        /// <summary>
        /// 三轴移动逻辑
        /// </summary>
        private bool MoveWith3Axes(LineInterPoParam param)
        {
            // 初始化坐标系参数
            if (!CrdXYZSetPrm(Coordinate, param.InterPoAxiss[0], param.InterPoAxiss[1], param.InterPoAxiss[2]))
                return false;

            // 获取当前位置
            float[] curPos = new float[3];
            var tempInfos = new double[4];
            if (!GetAllPosInfos())
            {
                return false;
            }
            for (int i = 0; i < Math.Min(curPos.Length, tempInfos.Length); i++)
            {
                curPos[i] = (float)tempInfos[i]; // 显式转换double到float
            }

            // 清除缓冲区并初始化前瞻
            if (!CrdBufClear(Coordinate) || !InitLookAhead(Coordinate))
                return false;

            //优先级较高的轴先移动到安全位置
            var HighPriorityAxis = Config.AllAxis.Where(p => (p.Priority == En_Priority.Top || p.Priority == En_Priority.High)).ToList();
            foreach (var axis in HighPriorityAxis)
            {
                // 执行Z轴慢速上抬
                ExecuteZAxisLift(axis.AxisNum, curPos, (float)axis.SafetyDis, param.upZSpeed, is3Axis: true);

                // 确保当前位置在安全高度
                EnsureSafeHeightCurrentPosition(axis.AxisNum, curPos, param.DefaultSpeed, is3Axis: true);
            }

            // 移动到目标位置（带减速区）
            MoveToTargetWithDecelerationZone(param);

            // 执行Z轴慢速下降
            if (Config.AllAxis.Where(p => p.AxisNum == En_AxisNum.Z).FirstOrDefault()?.DecelerateDis > 0)
            {
                CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { param.TargetPosDic[En_AxisNum.X],
                   param.TargetPosDic[En_AxisNum.Y],
                   param.TargetPosDic[En_AxisNum.Z] }, param.decZSpeed, En_AxisNum.X);
            }

            return true;
        }

        /// <summary>
        /// 四轴移动逻辑
        /// </summary>
        private bool MoveWith4Axes(LineInterPoParam param)
        {
            // 初始化坐标系参数
            if (!CrdXYZRSetPrm(Coordinate, param.InterPoAxiss[0], param.InterPoAxiss[1], param.InterPoAxiss[2], param.InterPoAxiss[3]))
                return false;

            // 获取当前位置
            float[] curPos = new float[4];
            var tempInfos = new double[4];
            if (!GetAllPosInfos())
            {
                return false;
            }
            for (int i = 0; i < Math.Min(curPos.Length, tempInfos.Length); i++)
            {
                curPos[i] = (float)tempInfos[i]; // 显式转换double到float
            }

            // 清除缓冲区并初始化前瞻
            if (!CrdBufClear(Coordinate) || !InitLookAhead(Coordinate))
                return false;

            //优先级较高的轴先移动到安全位置
            var HighPriorityAxis = Config.AllAxis.Where(p => (p.Priority == En_Priority.Top || p.Priority == En_Priority.High)).ToList();
            foreach (var axis in HighPriorityAxis)
            {
                // 执行Z轴慢速上抬
                ExecuteZAxisLift(axis.AxisNum, curPos, (float)axis.SafetyDis, param.upZSpeed, is3Axis: false, currentR: curPos[3]);

                // 确保当前位置在安全高度
                EnsureSafeHeightCurrentPosition(axis.AxisNum, curPos, param.DefaultSpeed, is3Axis: false, currentR: curPos[3]);
            }

            // 移动到目标位置（带减速区）
            MoveToTargetWithDecelerationZone(En_AxisNum.Z, (float)param.TargetPosDic[En_AxisNum.X],
                   (float)param.TargetPosDic[En_AxisNum.Y],
                   (float)param.TargetPosDic[En_AxisNum.Z], (float)Config.AllAxis.Where(p => p.AxisNum == En_AxisNum.Z).FirstOrDefault()?.DecelerateDis,
                                           param.DefaultSpeed, curPos[2], is3Axis: false,
                                           targetR: (float)param.TargetPosDic[En_AxisNum.Z1]/* ?? curPos[3]*/);

            if (Config.AllAxis.Where(p => p.AxisNum == En_AxisNum.Z).FirstOrDefault()?.DecelerateDis > 0)
            {
                CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { param.TargetPosDic[En_AxisNum.X],
                   param.TargetPosDic[En_AxisNum.Y],
                   param.TargetPosDic[En_AxisNum.Z] }, param.decZSpeed, En_AxisNum.X);
            }

            // 执行Z轴慢速下降
            if (Config.AllAxis.Where(p => p.AxisNum == En_AxisNum.Z).FirstOrDefault()?.DecelerateDis > 0)
            {
                CrdMoveAbsoluteToPointXYZR(Coordinate, new double[] { param.TargetPosDic[En_AxisNum.X],
                   param.TargetPosDic[En_AxisNum.Y],
                   param.TargetPosDic[En_AxisNum.Z], param.TargetPosDic[En_AxisNum.Z1]/* ?? curPos[3]*/ }, param.decZSpeed, En_AxisNum.Z);
            }

            return true;
        }

        /// <summary>
        /// 执行Z轴慢速上抬
        /// </summary>
        private void ExecuteZAxisLift(En_AxisNum zAxis, float[] curPos,
                                     float upZContent, double[] upZSpeed, bool is3Axis,
                                     float currentR = 0)
        {
            if (ConvertAxis(zAxis).SafetyDis > 0 && upZContent > 0)
            {
                float newZ = curPos[ConvertAxis(zAxis).AxisNo - 1] - upZContent; // 抬升Z高度（Z值减小）

                if (is3Axis)
                {
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { curPos[ConvertAxis(En_AxisNum.X).AxisNo - 1], curPos[ConvertAxis(En_AxisNum.Y).AxisNo - 1], newZ }, upZSpeed, zAxis);
                }
                else
                {
                    CrdMoveAbsoluteToPointXYZR(Coordinate, new double[] { curPos[ConvertAxis(En_AxisNum.X).AxisNo - 1], curPos[ConvertAxis(En_AxisNum.Y).AxisNo - 1], newZ, currentR }, upZSpeed, zAxis);
                }
            }
        }

        /// <summary>
        /// 确保当前位置在安全高度
        /// </summary>
        private void EnsureSafeHeightCurrentPosition(En_AxisNum zAxis,
                                                    float[] curPos, EN_SpeedType speedType,
                                                    bool is3Axis, float currentR = 0)
        {
            float safeZ = (float)ConvertAxis(zAxis).SafetyDis;
            // 判断当前Z轴位置是否低于安全高度
            if ((safeZ < 0 && curPos[2] < safeZ) || (safeZ > 0 && curPos[2] > safeZ))
            {
                if (is3Axis)
                {
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { curPos[ConvertAxis(En_AxisNum.X).AxisNo - 1], curPos[ConvertAxis(En_AxisNum.Y).AxisNo - 1], safeZ });
                }
                else
                {
                    //CrdMoveAbsoluteToPointXYZR(Coordinate, new double[] { curPos[ConvertAxis(En_AxisNum.X).AxisNo - 1], curPos[ConvertAxis(En_AxisNum.Y).AxisNo - 1], safeZ, currentR });
                }
            }
        }

        /// <summary>
        /// 移动到目标位置（带减速区）
        /// </summary>
        private void MoveToTargetWithDecelerationZone(En_AxisNum zAxis,
                                                     float x, float y, float z, float decZContent,
                                                     EN_SpeedType speedType, float currentZ,
                                                     bool is3Axis, float targetR = 0)
        {
            float targetZWithDec = z - decZContent;
            float safeZ = (float)ConvertAxis(zAxis).SafetyDis;

            // 判断目标Z轴位置是否低于安全高度
            if ((safeZ < 0 && targetZWithDec < safeZ) || (safeZ > 0 && targetZWithDec > safeZ))
            {
                // 先移动到安全高度
                if (is3Axis)
                {
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { x, y, safeZ });
                    // 再下降到减速区
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { x, y, targetZWithDec });
                }
                else
                {
                    CrdMoveAbsoluteToPointXYZR(Coordinate, new double[] { x, y, safeZ, targetR }, speedType);
                    // 再下降到减速区
                    CrdMoveAbsoluteToPointXYZR(Coordinate, new double[] { x, y, targetZWithDec, targetR }, speedType, zAxis);
                }
            }
            else
            {
                // 直接移动到减速区
                if (is3Axis)
                {
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { x, y, targetZWithDec });
                }
                else
                {
                    CrdMoveAbsoluteToPointXYZR(Coordinate, new double[] { x, y, targetZWithDec, targetR }, speedType);
                }
            }
        }

        /// <summary>
        /// 移动到目标位置（带减速区）
        /// </summary>
        private void MoveToTargetWithDecelerationZone(LineInterPoParam param)
        {
            var targetZWithDecs = new float[Enum.GetValues(typeof(AxisStatusFlags)).Length];
            var SafetyDiss = new float[Enum.GetValues(typeof(AxisStatusFlags)).Length];
            var HighPriorityAxis = Config.AllAxis.Where(p => (p.Priority == En_Priority.Top || p.Priority == En_Priority.High)).ToList();
            foreach (var axis in HighPriorityAxis)
            {
                targetZWithDecs[(int)axis.AxisNum] = (float)(param.TargetPosDic[axis.AxisNum] - axis.DecelerateDis);
                SafetyDiss[(int)axis.AxisNum] = (float)axis.SafetyDis;
            }

            // 判断目标Z轴位置是否低于安全高度
            if ((SafetyDiss[(int)En_AxisNum.Z] < 0 && targetZWithDecs[(int)En_AxisNum.Z] < SafetyDiss[(int)En_AxisNum.Z]) ||
                (SafetyDiss[(int)En_AxisNum.Z] > 0 && targetZWithDecs[(int)En_AxisNum.Z] > SafetyDiss[(int)En_AxisNum.Z]))
            {
                // 先移动到安全高度3轴

                if (param.InterPoAxiss.Count == 3)
                {
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { param.TargetPosDic[En_AxisNum.X],
                        param.TargetPosDic[En_AxisNum.Y],
                        SafetyDiss[(int)En_AxisNum.Z] });
                    // 再下降到减速区
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { param.TargetPosDic[En_AxisNum.X],
                        param.TargetPosDic[En_AxisNum.Y],
                        targetZWithDecs[(int)En_AxisNum.Z] });
                }
                else
                {
                    CrdMoveAbsoluteToPointXYZR(Coordinate, new double[] { param.TargetPosDic[En_AxisNum.X],
                        param.TargetPosDic[En_AxisNum.Y],
                        SafetyDiss[(int)En_AxisNum.Z],
                        param.TargetPosDic[En_AxisNum.Z1] }, param.DefaultSpeed);
                    // 再下降到减速区
                    CrdMoveAbsoluteToPointXYZR(Coordinate, new double[] { param.TargetPosDic[En_AxisNum.X],
                        param.TargetPosDic[En_AxisNum.Y],
                        targetZWithDecs[(int)En_AxisNum.Z],
                        param.TargetPosDic[En_AxisNum.Z1] }, param.DefaultSpeed, En_AxisNum.Z);
                }
            }
            else
            {
                // 直接移动到减速区
                if (param.InterPoAxiss.Count == 3)
                {
                    CrdMoveAbsoluteToPointXYZ(Coordinate, new double[] { param.TargetPosDic[En_AxisNum.X],
                        param.TargetPosDic[En_AxisNum.Y],
                        targetZWithDecs[(int)En_AxisNum.Z] });
                }
                else
                {
                    CrdMoveAbsoluteToPointXYZR(Coordinate, new double[] { param.TargetPosDic[En_AxisNum.X],
                        param.TargetPosDic[En_AxisNum.Y],
                        targetZWithDecs[(int)En_AxisNum.Z],
                        param.TargetPosDic[En_AxisNum.Z1] }, param.DefaultSpeed);
                }
            }
        }
        #endregion
    }

}
