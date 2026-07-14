using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.PLC.Models
{
    /// <summary>
    /// 轴对象(包含轴的所有定义属性)
    /// </summary>
    public class Axis
    {
        /// <summary>
        /// 轴类型
        /// </summary>
        public EnumAxisType AxisType { get; set; }

        /// <summary>
        /// 含有的运动类型
        /// </summary>
        public List<EnumMotionType> MotionTypes { get; set; } = new List<EnumMotionType>();

        public Dictionary<string, object> ObjectParas { get; set; } = new();


        public const string AxisPLCAddress = "AxisPLCAddress";


        public T GetPara<T>(string paraName, T defaultValue)
        {
            lock (this)
            {
                try
                {
                    if (ObjectParas!.ContainsKey(paraName))
                    {
                        object ob = ObjectParas[paraName];
                        try
                        {
                            if (ob is T)//过程中保存时，类型是T
                            {
                                return (T)ob;
                            }
                            else
                            {
                                JsonElement jsonElement = (JsonElement)ob;
                                var je = jsonElement.Deserialize<T>()!;
                                ObjectParas[paraName] = je;
                                return je;
                            }
                        }
                        catch
                        {
                            Exception ex = new Exception($"AxisGetPara key error:{paraName} is not {typeof(T)}, value is {ob.GetType()}");
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        return defaultValue;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            return defaultValue;
        }


        public void SetPara<T>(string paraName, T paraValue)
        {
            lock (this)
            {
                ObjectParas[paraName] = paraValue;
            }
        }

    }

    /// <summary>
    /// 轴
    /// </summary>
    public enum EnumAxisType
    {
        X,                         //X轴
        Y,                         //Y轴
        ZTop,                      //Z轴上
        ZBottom,                   //Z轴下
        R,                         //旋转轴
        Undefined,                 //未定义
    }

    /// <summary>
    /// 运动类型
    /// </summary>
    public enum EnumMotionType
    {
        LinearMotion,              //直线运动
        ResetMotion,               //复位运动
        None,                      //无
    }


    /// <summary>
    /// 运动参数，后续可扩展
    /// </summary>
    public class MotionPara
    {
        /// <summary>
        /// 轴类型
        /// </summary>
        public EnumAxisType AxisType { get; set; }

        /// <summary>
        /// 运动类型
        /// </summary>
        public EnumMotionType MotionType { get; set; }

        /// <summary>
        /// 位置参数
        /// </summary>
        public double Position { get; set; }

        public MotionPara()
        {

        }

        public MotionPara(EnumAxisType axisType, EnumMotionType motionType, double position)
        {
            AxisType = axisType;
            MotionType = motionType;
            Position = position;
        }
    }
}
