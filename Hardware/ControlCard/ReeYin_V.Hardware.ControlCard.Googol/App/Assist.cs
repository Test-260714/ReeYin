using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static ReeYin_V.Hardware.ControlCard.ControlCardConfig;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    /// <summary>
    /// 辅助方法
    /// </summary>
    public partial class GoogolControlCard : ControlCardBase
    {
        #region ALGO

        /// <summary>
        /// 传入一个数组,求出一个数组的最大值的位置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static int MaxIndex<T>(T[] arr) where T : IComparable<T>
        {
            var i_Pos = 0;
            var value = arr[0];
            for (var i = 1; i < arr.Length; ++i)
            {
                var _value = arr[i];
                if (_value.CompareTo(value) > 0)
                {
                    value = _value;
                    i_Pos = i;
                }
            }
            return i_Pos;
        }
        #endregion
        /// <summary>
        /// 轴速度转换成脉冲数
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private double[] ConvertToPluse(En_AxisNum axisNum, double[] values)
        {
            var model = ConvertAxis(axisNum);
            if (model == null)
            {
                Console.WriteLine($"当前轴{axisNum}未配置");
                return null;
            }

            double[] pluses = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                pluses[i] = values[i] * model.PulseEquivalent;
            }
            return pluses;
        }

        private double ConvertToPluse(En_AxisNum axisNum, double value)
        {
            var model = ConvertAxis(axisNum);
            if (model == null)
            {
                Console.WriteLine($"当前轴{axisNum}未配置");
                return 0;
            }
            return value * model.PulseEquivalent;
        }

        /// <summary>
        /// 轴Enum转换AxisModel
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private SingleAxisParam ConvertAxis(En_AxisNum axisNum)
        {
            var model = Config.AllAxis.FirstOrDefault(m => m.AxisNum == axisNum);
            return model;
        }

        /// <summary>
        /// 获取脉冲当量
        /// </summary>
        /// <param name="axisNum"></param>
        /// <returns></returns>
        public double GetPulseequ(En_AxisNum axisNum)
        {
            return ConvertAxis(axisNum).PulseEquivalent;
        }
    }
}
