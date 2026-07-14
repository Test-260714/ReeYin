using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Core.Device;
using HslCommunication.ModBus;
using HslCommunication.Profinet.AllenBradley;
using HslCommunication.Profinet.Beckhoff;
using HslCommunication.Profinet.Inovance;
using HslCommunication.Profinet.Keyence;
using HslCommunication.Profinet.Melsec;
using HslCommunication.Profinet.Omron;
using HslCommunication.Profinet.Siemens;
using HslCommunication.Profinet.XINJE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup.Localizer;
using static OpenCvSharp.FileStorage;

namespace ReeYin_V.Hardware.PLC.Models
{
    /// <summary>
    /// modbus暂时没弄
    /// </summary>
    public class HslClient
    {
        #region Fields
        private IReadWriteNet _rawRw;
        private DeviceTcpNet _deviceTcpNet;

        private OperateResult _operateResult = new OperateResult();
        private StatusItemModel _globalStatus = new StatusItemModel();

        private int failedConnectCount = 0;

        private readonly Dictionary<EnumParaInfoModelParaType, Func<PLCParaInfoModel, OperateResult>> _writeHandlers;
        // 存储枚举与对应处理逻辑的映射（构造时初始化）
        private readonly Dictionary<EnumParaInfoModelParaType, ReadHandler> _readHandlers;

        // 定义泛型委托：接收PLCParaInfoModel，返回void（封装读取和填充逻辑）
        private delegate void ReadHandler(PLCParaInfoModel pim);

        #endregion

        #region Properties
        public OperateResult OperateResult
        {
            get { return _operateResult; }
            set
            {
                _operateResult = value;
                _globalStatus.StatusValue = value.IsSuccess;
            }
        }

        public string ID { get; set; }
        public string DisplayName { get; set; }

        /// <summary>
        /// 配置
        /// </summary>
        public PlcConfigModel Config { get; set; }


        #endregion

        #region Constructor
        public HslClient()
        {

            _writeHandlers = new Dictionary<EnumParaInfoModelParaType, Func<PLCParaInfoModel, OperateResult>>
            {
                { EnumParaInfoModelParaType.Short, p => _rawRw.Write(p.PLCAddress, Convert.ToInt16(p.ParaValue)) },
                { EnumParaInfoModelParaType.Int, p => _rawRw.Write(p.PLCAddress, Convert.ToInt32(p.ParaValue)) },
                { EnumParaInfoModelParaType.Float, p => _rawRw.Write(p.PLCAddress, Convert.ToSingle(p.ParaValue)) },
                { EnumParaInfoModelParaType.Double, p => _rawRw.Write(p.PLCAddress, Convert.ToDouble(p.ParaValue)) },
                { EnumParaInfoModelParaType.Long, p => _rawRw.Write(p.PLCAddress, Convert.ToInt64(p.ParaValue)) },
                { EnumParaInfoModelParaType.String, p => p.ParaLength == 0
                    ? _rawRw.Write(p.PLCAddress, p.ParaValue.ToString())
                    : _rawRw.Write(p.PLCAddress, p.ParaValue.ToString(), p.ParaLength)
                },
                { EnumParaInfoModelParaType.StringUTF8, p => _rawRw.Write(p.PLCAddress, p.ParaValue.ToString(), p.ParaLength, Encoding.UTF8) },
                { EnumParaInfoModelParaType.Ushort, p => _rawRw.Write(p.PLCAddress, Convert.ToUInt16(p.ParaValue)) },
                { EnumParaInfoModelParaType.Uint, p => _rawRw.Write(p.PLCAddress, Convert.ToUInt32(p.ParaValue)) },
                { EnumParaInfoModelParaType.Ulong, p => _rawRw.Write(p.PLCAddress, Convert.ToUInt64(p.ParaValue)) },
                { EnumParaInfoModelParaType.Bool, p => _rawRw.Write(p.PLCAddress, Convert.ToBoolean(p.ParaValue)) },
                { EnumParaInfoModelParaType.FloatArray, p => _rawRw.Write(p.PLCAddress, (float[])p.ParaValue) }
            };

            _readHandlers = new Dictionary<EnumParaInfoModelParaType, ReadHandler>
            {
                { EnumParaInfoModelParaType.Short, p => HandleRead(p, _rawRw.ReadInt16) },
                { EnumParaInfoModelParaType.Int, p => HandleRead(p, _rawRw.ReadInt32) },
                { EnumParaInfoModelParaType.Float, p => HandleRead(p, _rawRw.ReadFloat) },
                { EnumParaInfoModelParaType.Double, p => HandleRead(p, _rawRw.ReadDouble) },
                { EnumParaInfoModelParaType.Long, p => HandleRead(p, _rawRw.ReadInt64) },
                { EnumParaInfoModelParaType.Ushort, p => HandleRead(p, _rawRw.ReadUInt16) },
                { EnumParaInfoModelParaType.Uint, p => HandleRead(p, _rawRw.ReadUInt32) },
                { EnumParaInfoModelParaType.Ulong, p => HandleRead(p, _rawRw.ReadUInt64) },
                { EnumParaInfoModelParaType.Bool, p => HandleRead(p, _rawRw.ReadBool) },
                { EnumParaInfoModelParaType.String, p => HandleRead(p, addr => _rawRw.ReadString(addr, p.ParaLength)) },
                { EnumParaInfoModelParaType.StringUTF8, p => HandleRead(p, addr => _rawRw.ReadString(addr, p.ParaLength, Encoding.UTF8)) },
                { EnumParaInfoModelParaType.FloatArray, p => HandleRead(p, addr => _rawRw.ReadFloat(addr, p.ParaLength)) }
            };
        }
        #endregion

        #region Methods


        public void Init(PlcConfigModel plcConfigModel)
        {
            Config = new(plcConfigModel);
            (_deviceTcpNet, _rawRw) = CreateRwNet(plcConfigModel);

            ID = plcConfigModel.GetID();
            DisplayName = Config.DisplayName;

            if (!Authorization.SetAuthorizationCode("aa45f91b-2668-4822-8a8b-8d34e03a393b"))
            {
                Console.WriteLine("HslCommunication 组件认证失败，组件只能使用8小时!");
                //Log.Error("HslCommunication 组件认证失败，组件只能使用8小时!");
            }
        }

        /// <summary>
        /// 通用处理方法：执行读取并填充数据（泛型抽象）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pim"></param>
        /// <param name="readFunc"></param>
        private void HandleRead<T>(PLCParaInfoModel pim, Func<string, OperateResult<T>> readFunc)
        {
            var result = readFunc.Invoke(pim.PLCAddress);
            FillData<T>(pim, result);
        }
        #endregion

        public StatusItemModel GetStatus()
        {
            _globalStatus.Key = ID;
            _globalStatus.DisplayName = DisplayName;
            _globalStatus.StatusValue = IsConnected();
            return _globalStatus;
        }

        /// <summary>
        /// 连接
        /// </summary>
        public void Connect()
        {
            OperateResult = Config.PlcType == PLCType.ModbusRtu ? new OperateResult() { IsSuccess = true, Message = "" } : _deviceTcpNet.ConnectServer();
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            _globalStatus.StatusValue = OperateResult.IsSuccess;
            return OperateResult.IsSuccess;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public bool Dispose()
        {
            try
            {
                if (_rawRw is IDisposable d)
                {
                    d.Dispose();
                }

                // 重置连接状态
                OperateResult = new OperateResult() { IsSuccess = false, Message = "已断开连接" };
                _globalStatus.StatusValue = false;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"断开PLC连接时发生异常: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// 读逻辑（不需要判断对象是否为空）
        /// </summary>
        /// <param name="pim"></param>
        /// <returns></returns>
        public bool ReadPLCPara(PLCParaInfoModel pim)
        {
            if (!IsConnected())
            {
                Console.WriteLine("plc未连接");
                return false;
            }
            OperateResult<ushort> read = _rawRw.ReadUInt16(pim.PLCAddress);

            if (pim == null || pim?.PLCAddress == null || pim?.PLCAddress == "") return false;

            ReadPimData(pim!);

            //原代码
            // OperateResult.IsSuccess = pim!.IsSuccess;
            // _globalStatus.StatusValue = pim!.IsSuccess;

            // 读取失败时记录错误信息，但不影响连接状态
            if (!pim!.IsSuccess)
            {
                Console.WriteLine($"PLC读取失败 - 地址: {pim.PLCAddress}, 类型: {pim.ParaType}");
            }
            
            return pim!.IsSuccess;
        }

        /// <summary>
        /// 读逻辑（不需要判断对象是否为空）
        /// </summary>
        /// <param name="pim"></param>
        /// <returns></returns>
        public bool ReadPLCPara(AddressMappingItem amim)
        {
            if (!IsConnected())
            {
                Console.WriteLine("plc未连接");
                return false;
            }

            if (amim == null || amim?.Address == null || amim?.Address == "") return false;

            var pim = new PLCParaInfoModel();

            pim.PLCAddress = amim!.Address;
            pim.ParaDescription = amim.Description;
            pim.ParaType = amim.DataType;
            pim.Key = amim.DisplaySelectedKey;

            ReadPimData(pim!);

            amim.Value = pim.ParaValue;

            // OperateResult.IsSuccess = pim!.IsSuccess;
            // _globalStatus.StatusValue = pim!.IsSuccess;

            // 读取失败时记录错误信息，但不影响连接状态
            if (!pim!.IsSuccess)
            {
                Console.WriteLine($"PLC读取失败 - 地址: {pim.PLCAddress}, 类型: {pim.ParaType}, 描述: {pim.ParaDescription}");
            }
            
            return pim!.IsSuccess;
        }

        /// <summary>
        /// 写逻辑（不需要判断对象是否为空）
        /// </summary>
        /// <param name="pim"></param>
        /// <returns></returns>
        public bool WritePLCPara(PLCParaInfoModel pim)
        {
            if (!IsConnected())
            {
                Console.WriteLine("plc未连接");
                return false;
            }

            if (pim == null || pim?.PLCAddress == null || pim?.PLCAddress == "") return false;

            if (pim!.ParaType != EnumParaInfoModelParaType.String && (pim.ParaValue == null || pim.ParaValue.ToString() == ""))
            {
                Console.WriteLine($"{pim.ParaName},值为空");
                return false;
            }
            
            var writeResult = WritePimData(pim);

            // 写入失败时记录详细错误信息，但不影响连接状态
            pim.IsSuccess = writeResult?.IsSuccess ?? false;

            // OperateResult.IsSuccess = pim!.IsSuccess;
            // _globalStatus.StatusValue = pim!.IsSuccess;

            if (!pim.IsSuccess)
            {
                Console.WriteLine($"PLC写入失败 - 地址: {pim.PLCAddress}, 类型: {pim.ParaType}, 值: {pim.ParaValue}, 错误: {writeResult?.Message ?? "未知错误"}");
            }
            
            return pim.IsSuccess;
        }

        /// <summary>
        /// 写逻辑（不需要判断对象是否为空）
        /// </summary>
        /// <param name="amim"></param>
        public bool WritePLCPara(AddressMappingItem amim)
        {
            if (!IsConnected())
            {
                Console.WriteLine("plc未连接");
                return false;
            }

            if (amim == null || amim?.Address == null || amim?.Address == "") return false;

            var pim = new PLCParaInfoModel();

            pim.PLCAddress = amim!.Address;
            pim.ParaDescription = amim.Description;
            pim.ParaValue = amim.Value;
            pim.Key = amim.DisplaySelectedKey;
            pim.ParaType = amim.DataType;

            if (pim!.ParaType != EnumParaInfoModelParaType.String && (pim.ParaValue == null || pim.ParaValue.ToString() == ""))
            {
                //Log.Error($"{pim.ParaName},值为空");
                Console.WriteLine($"{pim.ParaDescription},值为空");
                return false;
            }
            
            var writeResult = WritePimData(pim);

            // 写入失败时记录详细错误信息，但不影响连接状态
            pim.IsSuccess = writeResult?.IsSuccess ?? false;

            // OperateResult.IsSuccess = pim!.IsSuccess;
            // _globalStatus.StatusValue = pim!.IsSuccess;

            if (!pim.IsSuccess)
            {
                Console.WriteLine($"PLC写入失败 - 地址: {pim.PLCAddress}, 类型: {pim.ParaType}, 值: {pim.ParaValue}, 描述: {pim.ParaDescription}, 错误: {writeResult?.Message ?? "未知错误"}");
            }
            
            return pim.IsSuccess;
        }

        /// <summary>
        /// 创建连接
        /// </summary>
        /// <param name="plcConfigModel"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        private (DeviceTcpNet, IReadWriteNet) CreateRwNet(PlcConfigModel plcConfigModel)
        {
            string ipAddress = plcConfigModel.Ip;
            int port = plcConfigModel.Port;

            switch (plcConfigModel.PlcType)
            {
                case PLCType.OmronFinsNet:
                    {
                        OmronFinsNet omronClient = new OmronFinsNet();
                        bool OmronIsStringReverseByteWord = true;   //欧姆龙字符串模式
                        omronClient.ByteTransform.IsStringReverseByteWord = OmronIsStringReverseByteWord;
                        omronClient.IpAddress = ipAddress;
                        omronClient.Port = port;
                        return (omronClient, omronClient);
                    }
                case PLCType.MelsecMcNet:
                    {
                        var c = new MelsecMcNet();
                        c.IpAddress = ipAddress;
                        c.Port = port;
                        return (c, c);
                    }
                case PLCType.MelsecMcUdp:
                    {
                        var c = new MelsecMcUdp();
                        c.IpAddress = ipAddress;
                        c.Port = port;
                        return (c, c);
                    }
                case PLCType.MelsecMcAsciiUdp:
                    {
                        var c = new MelsecMcAsciiUdp();
                        c.IpAddress = ipAddress;
                        c.Port = port;
                        return (c, c);
                    }
                case PLCType.SiemensS7Net:
                    {
                        SiemensS7Net siemens = new SiemensS7Net(plcConfigModel.SiemensType, ipAddress);
                        return (siemens, siemens);
                    }
                case PLCType.InovanceTcpNet:
                    {
                        var inovanceTcpNet = new InovanceTcpNet();
                        inovanceTcpNet.IpAddress = ipAddress;
                        inovanceTcpNet.Port = port;
                        inovanceTcpNet.DataFormat = plcConfigModel.DataFormat;
                        inovanceTcpNet.Series = plcConfigModel.InovanceSeries;
                        return (inovanceTcpNet, inovanceTcpNet);
                    }
                case PLCType.KeyenceNanoSerialOverTcp:
                    {
                        var keyenceNanoSerialOverTcp = new KeyenceNanoSerialOverTcp();
                        keyenceNanoSerialOverTcp.IpAddress = ipAddress;
                        keyenceNanoSerialOverTcp.Port = port;
                        return (keyenceNanoSerialOverTcp, keyenceNanoSerialOverTcp);
                    }
                case PLCType.KeyenceMcNet:
                    {
                        KeyenceMcNet keyenceMcNet = new KeyenceMcNet();
                        keyenceMcNet.IpAddress = ipAddress;
                        keyenceMcNet.Port = port;
                        return (keyenceMcNet, keyenceMcNet);
                    }
                case PLCType.BeckhoffAdsNet:
                    {
                        BeckhoffAdsNet beckhoffAdsNet = new BeckhoffAdsNet()
                        {
                            IpAddress = ipAddress,
                            Port = port,
                        };

                        beckhoffAdsNet.SetTargetAMSNetId(plcConfigModel.TargetNetId);
                        beckhoffAdsNet.SetSenderAMSNetId(plcConfigModel.SenderNetId);
                        return (beckhoffAdsNet, beckhoffAdsNet);
                    }
                case PLCType.AllenBradleyNet:
                    {
                        AllenBradleyNet allenBradleyNet = new AllenBradleyNet();
                        allenBradleyNet.IpAddress = ipAddress;
                        allenBradleyNet.Port = port;
                        allenBradleyNet.Slot = Convert.ToByte(plcConfigModel.Slot);
                        return (allenBradleyNet, allenBradleyNet);
                    }
                case PLCType.OmronCipNet:
                    {
                        OmronCipNet omronCipNet = new OmronCipNet();
                        omronCipNet.IpAddress = ipAddress;
                        omronCipNet.Port = port;
                        omronCipNet.Slot = Convert.ToByte(plcConfigModel.Slot);
                        return (omronCipNet, omronCipNet);
                    }
                case PLCType.XinJETcpNetModbus:
                    {
                        XinJETcpNet xinJeTcpNet = new XinJETcpNet(ipAddress, port);
                        xinJeTcpNet.Series = plcConfigModel.XinJESeries;
                        xinJeTcpNet.Station = Convert.ToByte(plcConfigModel.Station);
                        xinJeTcpNet.DataFormat = plcConfigModel.DataFormat;
                        return (xinJeTcpNet, xinJeTcpNet);
                    }
                case PLCType.XinJEInternalNet:
                    {
                        XinJEInternalNet xinJeTcpNet = new XinJEInternalNet();
                        xinJeTcpNet.Station = Convert.ToByte(plcConfigModel.Station);
                        xinJeTcpNet.CommunicationPipe = new HslCommunication.Core.Pipe.PipeTcpNet(ipAddress, port)
                        {
                            ConnectTimeOut = 5000,    // 连接超时时间，单位毫秒
                            ReceiveTimeOut = 10000,    // 接收设备数据反馈的超时时间
                            SleepTime = 0,
                            SocketKeepAliveTime = -1,
                            IsPersistentConnection = true,
                        };
                        return (xinJeTcpNet, xinJeTcpNet);
                    }
                default:
                    break;

            }

            if (plcConfigModel.PlcType == PLCType.ModbusRtu ||
                plcConfigModel.PlcType == PLCType.ModbusTcpNet ||
                plcConfigModel.PlcType == PLCType.XinJEInternalNet)
            {
                return CreateModbusDevice(plcConfigModel);
            }

            throw new InvalidDataException();
        }

        /// <summary>
        /// Modus连接
        /// </summary>
        /// <param name="plcConfigModel"></param>
        /// <returns></returns>
        private (DeviceTcpNet, IReadWriteNet) CreateModbusDevice(PlcConfigModel plcConfigModel)
        {
            string ipAddress = plcConfigModel.Ip;
            int port = plcConfigModel.Port;
            byte station = Convert.ToByte(plcConfigModel.Station);

            if(plcConfigModel.PlcType == PLCType.ModbusTcpNet)
            {
                ModbusTcpNet modbustcp = new();
                modbustcp.Station = station;
                modbustcp.IpAddress = ipAddress;
                modbustcp.Port = port;
                modbustcp.ConnectServer();
                return (modbustcp, modbustcp);
            }

            if(plcConfigModel.PlcType == PLCType.ModbusRtuOverTcp)
            {
                ModbusRtuOverTcp modbustcp = new();
                modbustcp.Station = station;
                modbustcp.IpAddress = ipAddress;
                modbustcp.Port = port;
                modbustcp.ConnectServer();
                return (modbustcp, modbustcp);
            }

            ModbusRtu modbusRtu = new();
            string portName = ipAddress;
            int baudRate = port;
            modbusRtu.Station = station;

            modbusRtu.SerialPortInni(portName, baudRate);
            return (new DeviceTcpNet() { }, modbusRtu);
        }


        public void ReadPimData(PLCParaInfoModel pim)
        {
            if (pim == null)
            {
                Console.WriteLine("PLCParaInfoModel 为空");
                return;
            }

            try
            {
                pim.IsSuccess = false;

                // 从字典获取处理逻辑并执行
                if (_readHandlers.TryGetValue(pim.ParaType, out var handler))
                {
                    handler.Invoke(pim);
                }
                else
                {
                    Console.WriteLine($"不支持的参数类型: {pim.ParaType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取数据异常: {ex.StackTrace}");
            }
        }

        public OperateResult WritePimData(PLCParaInfoModel pim)
        {
            if (pim == null || pim.ParaValue == null)
                return new OperateResult { IsSuccess = false, Message = "参数模型或参数值为空" };

            try
            {
                pim.IsSuccess = false;

                // 检查是否有对应的处理逻辑
                if (!_writeHandlers.TryGetValue(pim.ParaType, out var writeHandler))
                {
                    throw new NotSupportedException($"不支持的参数类型: {pim.ParaType}");
                }

                // 执行写入逻辑
                var writeResult = writeHandler.Invoke(pim);

                // 更新状态并记录日志（失败时）
                pim.IsSuccess = writeResult?.IsSuccess ?? false;
                if (!pim.IsSuccess)
                {
                    Console.WriteLine($"写入参数失败 [类型: {pim.ParaType}, 地址: {pim.PLCAddress}]，原因: {writeResult?.Message ?? "未知错误"}");
                }

                return writeResult ?? new OperateResult { IsSuccess = false, Message = "写入操作返回空结果" };
            }
            catch (Exception ex)
            {
                // 区分转换异常和其他异常，增强可读性
                var errorMsg = ex is FormatException || ex is InvalidCastException
                    ? $"参数值转换失败 [类型: {pim.ParaType}, 值: {pim.ParaValue}]：{ex.Message}"
                    : $"写入参数时发生异常 [类型: {pim.ParaType}]：{ex.Message}";

                Console.WriteLine($"{ex.StackTrace}" + errorMsg);
                return new OperateResult { IsSuccess = false, Message = errorMsg };
            }
        }

        private OperateResult WritePimDataByString(PLCParaInfoModel pim)
        {
            if (pim?.ParaValue == null)
                return new OperateResult();


            pim.IsSuccess = false;
            var writeResult = new OperateResult();
            if (EnumParaInfoModelParaType.Short.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, (short)double.Parse(pim.ParaValueString));
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.Int.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, (int)double.Parse(pim.ParaValueString));
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.Float.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, (float)double.Parse(pim.ParaValueString));
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.Double.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, double.Parse(pim.ParaValueString));
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.Long.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, (long)double.Parse(pim.ParaValueString));
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.String.Equals(pim.ParaType))
            {
                writeResult = pim.ParaLength == 0 ? _rawRw.Write(pim.PLCAddress, (string)pim.ParaValueString) : _rawRw.Write(pim.PLCAddress, (string)pim.ParaValueString, pim.ParaLength);
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.StringUTF8.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, (string)pim.ParaValueString, pim.ParaLength, System.Text.Encoding.UTF8);
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.Ushort.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, (ushort)double.Parse(pim.ParaValueString));
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.Uint.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, (uint)double.Parse(pim.ParaValueString));
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.Ulong.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, (ulong)double.Parse(pim.ParaValueString));
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.Bool.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, bool.Parse(pim.ParaValueString));
                pim.IsSuccess = writeResult.IsSuccess;
            }
            else if (EnumParaInfoModelParaType.FloatArray.Equals(pim.ParaType))
            {
                writeResult = _rawRw.Write(pim.PLCAddress, pim.ParaValueString.Trim('[', ']').Split(',').Select(float.Parse).ToArray());
                pim.IsSuccess = writeResult.IsSuccess;
            }

            return writeResult;
        }

        private void FillData<T>(PLCParaInfoModel pim, OperateResult<T> result)
        {
            if (result.IsSuccess)
            {
                pim.ParaValue = result.Content;
                pim.OrinParaValue = result.Content;
                pim.ParaValueString = pim.ParaValue.ToString();
                pim.IsSuccess = true;
            }
            else
            {
                failedConnectCount++;
                if (failedConnectCount > 100)
                {
                    failedConnectCount = 0;
                    Console.WriteLine("PLC read failed，addr" + pim.PLCAddress + ",type:" + pim.ParaType + ",result:" + " false,info:" + result.Message);
                }
            }
        }


    }
}
