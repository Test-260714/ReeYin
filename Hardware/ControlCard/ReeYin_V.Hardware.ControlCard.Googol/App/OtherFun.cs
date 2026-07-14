using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    public partial class GoogolControlCard : ControlCardBase
    {
        /// <summary>
        /// 开启位置比较
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override bool ControlPosComparison(bool On_Off, PosComparisonOutputParam param)
        {
            try
            {
                if (On_Off)
                {
                    if (!Motion.StartPosComparisonOutput(param))
                    {
                        Logs.LogWarning($"ControlPosComparison()_开始位置比较失败！");
                    }
                }
                else
                {
                    Motion.StopPosComparisonOutput(param);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError($"{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 获取实际的比较位置
        /// </summary>
        /// <param name="posCompareIndex"></param>
        /// <param name="ActualX"></param>
        /// <param name="ActualY"></param>
        /// <returns></returns>
        public override bool GetActualComparePos(short posCompareIndex, ref int[] ActualX, ref int[] ActualY)
        {
            try
            {
                return Motion.GetActualComparePos(posCompareIndex, ref ActualX, ref ActualY);
            }
            catch (Exception ex)
            {
                Logs.LogError($"{ex.StackTrace}");
                return false;
            }
        }

        public override void InsertPosCompareData(double[] pos, PosCompareData posCompareData)
        {
            posCompareData.PosX = (int)(pos[0] * 10000);
            posCompareData.PosY = (int)(pos[1] * 10000);
            Motion.InsertPosCompareDatas.Enqueue(posCompareData);
        }

    }
}
