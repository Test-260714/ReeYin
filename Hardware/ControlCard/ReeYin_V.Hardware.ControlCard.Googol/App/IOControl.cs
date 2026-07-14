using HandyControl.Expression.Media;
using ImageTool.Halcon.Config;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    public partial class GoogolControlCard : ControlCardBase
    {
        public const int OutputNum = 10;
        public const int InputNum = 22;


        #region 映射IO管理
        public class IOModel
        {
            public int GLinkNum { get; set; } = -1;
            public int Port { get; set; } = -1;
            public string IOType { get; set; } = "";
            public bool Switch { get; set; } = false;
            public bool Enable { get; set; } = false;

            public IOModel()
            {
            }

            public IOModel(int gLinkNum, int port, string iOType)
            {
                GLinkNum = gLinkNum;
                Port = port;
                IOType = iOType;
            }
        }

        public bool GetIOModel(En_IOName name, out IOModel model)
        {
            if (!IOModels.ContainsKey(name.ToString()))
            {
                model = new IOModel(-1, -1, "");
                return false;
            }
            model = IOModels[name.ToString()];
            return true;
        }

        public bool SetIO(En_IOName name, bool open)
        {
            if (!IsConnected)
            {
                return false;
            }
            if (!IOModels.ContainsKey(name.ToString()))
            {
                Console.WriteLine($"映射表不含对应IO{name} ，请检查");
                return false;
            }
            var model = IOModels[name.ToString()];
            if (model.IOType != "DO")
            {
                Console.WriteLine($"无法对输入{name}进行设置 ，请检查");
                return false;
            }
            GetAllOutput(out bool[] Status);
            if (Status[model.Port] == open)
            {
                return true;
            }
            Motion.SetSingleDOut((short)model.Port, open);
            return true;
        }

        public bool SetIO(IOModel model, bool reverse = false)
        {
            if (!IsConnected)
            {
                return false;
            }
            // 如果 reverse 为 true，则取反 Switch 的值
            bool switchValue = reverse ? !model.Switch : model.Switch;

            // 设置输出值时也应用 reverse
            Motion.SetSingleDOut((short)model.Port, switchValue);
            return true;
        }

        public bool GetIO(En_IOName name, ref bool state)
        {
            state = false;
            if (!IsConnected)
            {
                return false;
            }
            if (!IOModels.ContainsKey(name.ToString()))
            {
                Console.WriteLine($"映射表不含对应IO{name} ，请检查");
                return false;
            }
            var model = IOModels[name.ToString()];

            if (model.IOType == "DO")
            {
                GetAllOutput(out bool[] Status);
                state = Status[model.Port];
                return true;
            }
            if (model.IOType == "DI")
            {
                short value = 0;
                if (!Motion.GetSingleDIn((short)model.Port, ref value))
                {
                    return false;
                }
                ;
                state = value == 1;
                return true;
            }
            return false;
        }

        public bool GetIO(En_IOName name, out short val)
        {
            var vals = new short[1];
            val = 0;
            if (!IsConnected)
            {
                return false;
            }
            if (!IOModels.ContainsKey(name.ToString()))
            {
                warningTimes++;
                if (warningTimes < 5)
                {
                    Console.WriteLine($"映射表不含对应IO{name} ，请检查");
                }
                return false;
            }
            var model = IOModels[name.ToString()];

            //if (model.IOType == "AI")
            //{
            //    bool result = Motion.GetAIn((short)model.GLinkNum, (ushort)model.Port, ref vals, 1);
            //    val = vals[0];
            //    return result;
            //}

            return false;
        }

        public bool GetIO(IOModel model, ref bool state)
        {
            state = false;
            if (!IsConnected)
            {
                return false;
            }

            if (model.IOType == "DO")
            {
                GetAllOutput(out bool[] Status);
                state = Status[model.Port];
                return true;
            }
            if (model.IOType == "DI")
            {
                short value = 0;
                if (!Motion.GetSingleDIn((short)model.Port, ref value))
                {
                    return false;
                };
                state = value == 1;
                return true;
            }
            return false;
        }

        #endregion


        #region 映射IO
        public bool Glink_MappingSingleDout(int num)
        {
            if (!Motion.Glink_MappingDout((short)(num + 1), 0, (short)num))
            {
                return false;
            }
            return true;
        }


        #endregion

        #region IO设置
        /// <summary>
        /// 获取单个输入信号
        /// </summary>
        /// <param name="iSlave">拓展模块编号从0开始</param>
        /// <param name="input">输入信号索引从0开始</param>
        /// <returns></returns>
        public bool GetSingleInput(short iSlave, short input)
        {
            if (IsConnected == true)
            {
                short value = 0;
                if (warningTimes > 10)
                {
                    Console.WriteLine($"连续10次IO请求错误，断开固高控制器连接！！");
                    IsConnected = false;
                }
                var result = Motion.GetSingleDIn(input, ref value);
                warningTimes = result ? 0 : warningTimes + 1;
                return value == 1;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 等待单个输入信号
        /// </summary>
        /// <param name="iSlave">拓展模块编号从0开始</param>
        /// <param name="input">输入信号索引从0开始</param>
        /// <param name="isOn">等待True还是False</param>
        /// <param name="waitime">等待时间</param>
        /// <returns></returns>
        public bool WaitInput(short iSlave, short input, bool isOn, double waitime = 8)
        {
            if (IsConnected == true)
            {
                bool ret = false;
                short value = 0;
                DateTime starttime = DateTime.Now;
                while (true)
                {
                    if ((DateTime.Now - starttime).TotalSeconds > waitime)
                    {
                        break;
                    }
                    if (!Motion.GetSingleDIn(input, ref value))
                    {
                        return false;
                    }
                    ret = value == 0 ? false : true;
                    if (ret == isOn)
                    {
                        return true;
                    }
                    Thread.Sleep(50);
                }
                return ret == isOn;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 设置指定IO开关
        /// </summary>
        /// <param name="Part"></param>
        /// <param name="OnOrOff"></param>
        /// <returns></returns>
        public override bool SetSpecifiedIO(int Part, bool OnOrOff)
        {
            if (!IsConnected)
            {
                return false;
            }
            return Motion.SetSingleDOut((short)Part, OnOrOff);
        }

        /// <summary>
        /// 获取指定IO状态
        /// </summary>
        /// <param name="InOrOut"></param>
        /// <param name="Part"></param>
        /// <param name="OnOrOff"></param>
        /// <returns></returns>
        public override bool GetSpecifiedIO(bool InOrOut,int Part,out bool OnOrOff)
        {
            if (IsConnected == true)
            {
                //对获取到的所有IO信号进行解析
                if (InOrOut)
                {
                    OnOrOff = false;
                    short value = 0;
                    if (warningTimes > 10)
                    {
                        Console.WriteLine($"连续10次IO请求错误，断开固高控制器连接！！");
                        IsConnected = false;
                    }
                    var result = Motion.GetSingleDIn((short)Part, ref value);
                    warningTimes = result ? 0 : warningTimes + 1;
                    return value == 1;
                }
                else
                {
                    OnOrOff = false;
                    short value = 0;
                    if (warningTimes > 10)
                    {
                        Console.WriteLine($"连续10次IO请求错误，断开固高控制器连接！！");
                        IsConnected = false;
                    }
                    var result = Motion.GetSingleDIn((short)Part, ref value);
                    warningTimes = result ? 0 : warningTimes + 1;


                    return false;
                }

            }
            else
            {
                OnOrOff = false;
                return false;
            }
        }

        #endregion

        #region 获取IO模块全部信号
        /// <summary>
        /// 获取所有输入信号
        /// </summary>
        /// <param name="iSlave"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        public override bool GetAllInput(out bool[] Status)
        {
            Status = new bool[InputNum];
            int input = 0;
            if (!Motion.GetDIn(ref input))
            {
                return false;
            }
            for (int i = 0; i < InputNum; i++)
            {
                Status[i] = (input & (1 << i)) != 0 ? false : true;
            }
            return true;
        }

        /// <summary>
        /// 获取所有输出信息
        /// </summary>
        /// <param name="Status"></param>
        /// <returns></returns>
        public override bool GetAllOutput(out bool[] Status)
        {
            Status = new bool[OutputNum];
            int output = 0;
            if (!Motion.GetDOut(ref output))
            {
                return false;
            }
            for (int i = 0; i < OutputNum; i++)
            {
                Status[i] = (output & (1 << i)) != 0 ? false : true;
            }
            return true;
        }
        #endregion
    }
}
