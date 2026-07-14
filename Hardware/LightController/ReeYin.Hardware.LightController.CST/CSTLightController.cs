using Newtonsoft.Json;
using ReeYin.Hardware.LightController.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ControllerDllCSharp.ClassLibControllerDll;

namespace ReeYin.Hardware.LightController.CST
{
    /// <summary>
    /// CST光源控制器
    /// </summary>
    public class CSTLightController : LightControllerBase
    {
        #region Fields
        /// <summary>
        /// 控制器句柄
        /// </summary>
        private long _controllerHandle = 0;

        /// <summary>
        /// 连接超时时间(秒)
        /// </summary>
        private int _connectTimeout = 1;
        #endregion

        #region Constructor
        public CSTLightController()
        {
            VenderName = "CST";
            VenderType = "LightController";
            IP = "192.168.1.208";  // CST控制器默认IP
            Port = 5000;
            ChannelCount = 4;
        }
        #endregion

        #region Override Methods
        public override bool Init()
        {
            try
            {
                if (IsConnected)
                {
                    return true;
                }

                int result;
                if (ConnectionType == 0) // 网口连接
                {
                    result = ConnectIP(IP, _connectTimeout, ref _controllerHandle);
                }
                else // 串口连接
                {
                    result = CreateSerialPort(ComPort, ref _controllerHandle);
                }

                if (result == SUCCESS)
                {
                    IsConnected = true;
                    // 获取通道数量（串口模式）
                    if (ConnectionType == 1)
                    {
                        int channelCount = 0;
                        if (GetChannelNumberSummary_s(ref channelCount, _controllerHandle) == SUCCESS)
                        {
                            ChannelCount = channelCount;
                        }
                    }
                    Console.WriteLine($"CST光源控制器连接成功，句柄: {_controllerHandle}");
                    return true;
                }
                else
                {
                    IsConnected = false;
                    Console.WriteLine($"CST光源控制器连接失败，错误码: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CST光源控制器连接异常: {ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        public override void Close()
        {
            try
            {
                if (!IsConnected || _controllerHandle == 0)
                {
                    IsConnected = false;
                    return;
                }

                int result;
                if (ConnectionType == 0) // 网口连接
                {
                    result = DestroyIpConnection(_controllerHandle);
                }
                else // 串口连接
                {
                    result = ReleaseSerialPort(_controllerHandle);
                }

                if (result == SUCCESS)
                {
                    Console.WriteLine("CST光源控制器断开成功");
                }
                else
                {
                    Console.WriteLine($"CST光源控制器断开失败，错误码: {result}");
                }

                _controllerHandle = 0;
                IsConnected = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CST光源控制器断开异常: {ex.Message}");
                IsConnected = false;
            }
        }

        public override bool SetBrightness(int channelIndex, int value)
        {
            if (!CheckConnection()) return false;

            try
            {
                int result;
                if (ConnectionType == 0)
                {
                    result = SetDigitalValue(channelIndex, value, _controllerHandle);
                }
                else
                {
                    result = SetDigitalValue_s(channelIndex, value, _controllerHandle);
                }

                if (result == SUCCESS)
                {
                    Console.WriteLine($"设置通道{channelIndex}亮度为{value}成功");
                    return true;
                }
                else
                {
                    Console.WriteLine($"设置通道{channelIndex}亮度失败，错误码: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置亮度异常: {ex.Message}");
                return false;
            }
        }

        public override int GetBrightness(int channelIndex)
        {
            if (!CheckConnection()) return -1;

            try
            {
                int value = 0;
                int result;
                if (ConnectionType == 0)
                {
                    result = GetDigitalValue(ref value, channelIndex, _controllerHandle);
                }
                else
                {
                    result = GetDigitalValue_s(ref value, channelIndex, _controllerHandle);
                }

                if (result == SUCCESS)
                {
                    Console.WriteLine($"获取通道{channelIndex}亮度为{value}");
                    return value;
                }
                else
                {
                    Console.WriteLine($"获取通道{channelIndex}亮度失败，错误码: {result}");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取亮度异常: {ex.Message}");
                return -1;
            }
        }

        public override bool SetMultiBrightness(Dictionary<int, int> channelValues)
        {
            if (!CheckConnection()) return false;

            try
            {
                if (ConnectionType == 0)
                {
                    var mulDigValArray = channelValues.Select(kv => new MulDigitalValue
                    {
                        channelIndex = kv.Key,
                        DigitalValue = kv.Value
                    }).ToArray();

                    int result = SetMulDigitalValue(mulDigValArray, mulDigValArray.Length, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        Console.WriteLine("批量设置亮度成功");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"批量设置亮度失败，错误码: {result}");
                        return false;
                    }
                }
                else
                {
                    var mulDigValArray = channelValues.Select(kv => new MulDigitalValue_s
                    {
                        channelIndex = kv.Key,
                        DigitalValue = kv.Value
                    }).ToArray();

                    int result = SetMulDigitalValue_s(mulDigValArray, mulDigValArray.Length, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        Console.WriteLine("批量设置亮度成功");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"批量设置亮度失败，错误码: {result}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"批量设置亮度异常: {ex.Message}");
                return false;
            }
        }

        public override bool SetChannelOnOff(int channelIndex, bool isOn)
        {
            if (!CheckConnection()) return false;

            try
            {
                // 串口模式才支持开关控制
                if (ConnectionType == 1)
                {
                    int result = SetON_OFF_s(channelIndex, isOn ? 1 : 0, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        Console.WriteLine($"设置通道{channelIndex}开关为{(isOn ? "开" : "关")}成功");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"设置通道{channelIndex}开关失败，错误码: {result}");
                        return false;
                    }
                }
                else
                {
                    // 网口模式通过设置亮度为0来关闭
                    return SetBrightness(channelIndex, isOn ? 255 : 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置开关异常: {ex.Message}");
                return false;
            }
        }

        public override bool GetChannelOnOff(int channelIndex)
        {
            if (!CheckConnection()) return false;

            try
            {
                if (ConnectionType == 1)
                {
                    int onOff = 0;
                    int result = GetON_OFF_s(channelIndex, ref onOff, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        return onOff == 1;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取开关状态异常: {ex.Message}");
                return false;
            }
        }

        public override bool SetStrobeTime(int channelIndex, int strobeTime)
        {
            if (!CheckConnection()) return false;

            try
            {
                // 仅网口模式支持
                if (ConnectionType == 0)
                {
                    int result = SetStrobeValue(channelIndex, strobeTime, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        Console.WriteLine($"设置通道{channelIndex}频闪时间为{strobeTime}成功");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"设置通道{channelIndex}频闪时间失败，错误码: {result}");
                        return false;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置频闪时间异常: {ex.Message}");
                return false;
            }
        }

        public override int GetStrobeTime(int channelIndex)
        {
            if (!CheckConnection()) return -1;

            try
            {
                if (ConnectionType == 0)
                {
                    int value = 0;
                    int result = GetStrobeValue(ref value, channelIndex, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        return value;
                    }
                }
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取频闪时间异常: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 最后一次操作的错误码
        /// </summary>
        public int LastErrorCode { get; private set; } = 0;

        public override bool SetTriggerMode(int mode)
        {
            if (!CheckConnection()) return false;

            try
            {
                if (ConnectionType == 0)
                {
                    int result = SetLightTriMode(mode, _controllerHandle);
                    LastErrorCode = result;
                    if (result == SUCCESS)
                    {
                        Console.WriteLine($"设置触发模式为{mode}成功");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"设置触发模式失败，错误码: {result}");
                        return false;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置触发模式异常: {ex.Message}");
                return false;
            }
        }

        public override int GetTriggerMode()
        {
            if (!CheckConnection()) return -1;

            try
            {
                if (ConnectionType == 0)
                {
                    int mode = 0;
                    int result = GetLightTriMode(ref mode, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        return mode;
                    }
                }
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取触发模式异常: {ex.Message}");
                return -1;
            }
        }
        #endregion

        #region Private Methods
        private bool CheckConnection()
        {
            if (!IsConnected || _controllerHandle == 0)
            {
                Console.WriteLine("光源控制器未连接！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 发送心跳保持连接
        /// </summary>
        public bool KeepAlive()
        {
            if (!CheckConnection()) return false;

            try
            {
                if (ConnectionType == 0)
                {
                    int result = ControllerDllCSharp.ClassLibControllerDll.KeepAlive(_controllerHandle);
                    return result == SUCCESS;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 设置光源延时
        /// </summary>
        public bool SetLightDelay(int channelIndex, int delayValue)
        {
            if (!CheckConnection()) return false;

            try
            {
                if (ConnectionType == 0)
                {
                    int result = SetLightDelayValue(channelIndex, delayValue, _controllerHandle);
                    return result == SUCCESS;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取光源延时
        /// </summary>
        public int GetLightDelay(int channelIndex)
        {
            if (!CheckConnection()) return -1;

            try
            {
                if (ConnectionType == 0)
                {
                    int value = 0;
                    int result = GetLightDelayValue(ref value, channelIndex, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        return value;
                    }
                }
                return -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 设置相机延时
        /// </summary>
        public bool SetCameraDelay(int channelIndex, int delayValue)
        {
            if (!CheckConnection()) return false;

            try
            {
                if (ConnectionType == 0)
                {
                    int result = SetCameraDelayValue(channelIndex, delayValue, _controllerHandle);
                    return result == SUCCESS;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取相机延时
        /// </summary>
        public int GetCameraDelay(int channelIndex)
        {
            if (!CheckConnection()) return -1;

            try
            {
                if (ConnectionType == 0)
                {
                    int value = 0;
                    int result = GetCameraDelayValue(ref value, channelIndex, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        return value;
                    }
                }
                return -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 设置内触发周期(ms)
        /// </summary>
        public bool SetInternalTriggerCycle(int cycleValue)
        {
            if (!CheckConnection()) return false;

            try
            {
                if (ConnectionType == 0)
                {
                    int result = SetIntCycleValue(cycleValue, _controllerHandle);
                    LastErrorCode = result;
                    if (result == SUCCESS)
                    {
                        Console.WriteLine($"设置内触发周期为{cycleValue}ms成功");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"设置内触发周期失败，错误码: {result}");
                        return false;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置内触发周期异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取内触发周期(ms)
        /// </summary>
        public int GetInternalTriggerCycle()
        {
            if (!CheckConnection()) return -1;

            try
            {
                if (ConnectionType == 0)
                {
                    int value = 0;
                    int result = GetIntCycleValue(ref value, _controllerHandle);
                    if (result == SUCCESS)
                    {
                        Console.WriteLine($"获取内触发周期为{value}ms");
                        return value;
                    }
                    else
                    {
                        Console.WriteLine($"获取内触发周期失败，错误码: {result}");
                        return -1;
                    }
                }
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取内触发周期异常: {ex.Message}");
                return -1;
            }
        }
        #endregion
    }
}
