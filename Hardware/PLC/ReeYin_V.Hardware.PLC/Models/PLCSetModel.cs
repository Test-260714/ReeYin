using HslCommunication.Core;
using HslCommunication.Profinet.Inovance;
using HslCommunication.Profinet.Siemens;
using HslCommunication.Profinet.XINJE;
using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.PLC.Models
{
    [Serializable]
    public class PLCSetModel : BindableBase, IHardwareModule
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private PLCType _sltType = PLCType.None;
        /// <summary>
        /// 选中的PLC类型
        /// </summary>
        public PLCType SltType
        {
            get { return _sltType; }
            set { _sltType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _ip = "127.0.0.1";
        /// <summary>
        /// 网口地址
        /// </summary>
        public string IP
        {
            get { return _ip; }
            set { _ip = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _port = 8080;
        /// <summary>
        /// 端口地址
        /// </summary>
        public int Port
        {
            get { return _port; }
            set { _port = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<PLCBase> _Models = new ObservableCollection<PLCBase>();

        public ObservableCollection<PLCBase> Models
        {
            get { return _Models; }
            set { _Models = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCBase _curSlt;
        public PLCBase CurSlt
        {
            get
            {
                return _curSlt;
            }
            set
            {
                _curSlt = value;
                RaisePropertyChanged();
                IsCurSltCardIsNotNull = _curSlt != null;
                SltAxisItem = null;
                // 自动选择默认轴组，若不存在则创建
                if (_curSlt != null)
                {
                    if (_curSlt.AxisGroups == null)
                        _curSlt.AxisGroups = new ObservableCollection<PLCAxisGroup>();
                    if (_curSlt.AxisGroups.Count == 0)
                        _curSlt.AxisGroups.Add(new PLCAxisGroup { GroupName = "默认轴组" });
                    SltAxisGroup = _curSlt.AxisGroups[0];
                }
                else
                {
                    SltAxisGroup = null;
                }
            }
        }

        [JsonIgnore]
        private PLCAxisGroup _sltAxisGroup;
        /// <summary>
        /// 当前选中的轴组
        /// </summary>
        public PLCAxisGroup SltAxisGroup
        {
            get { return _sltAxisGroup; }
            set
            {
                _sltAxisGroup = value;
                RaisePropertyChanged();
                if (_sltAxisGroup == null)
                {
                    SltAxisItem = null;
                }
                else if (_sltAxisGroup.AxisItems?.Count > 0)
                {
                    SltAxisItem = _sltAxisGroup.AxisItems[0];
                }
            }
        }

        [JsonIgnore]
        private PLCAxisItem _sltAxisItem;
        /// <summary>
        /// 当前选中的轴项
        /// </summary>
        public PLCAxisItem SltAxisItem
        {
            get { return _sltAxisItem; }
            set { _sltAxisItem = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCSpeedProfile _sltSpeedProfile;
        /// <summary>
        /// 当前选中的速度档（用于写入PLC操作）
        /// </summary>
        [JsonIgnore]
        public PLCSpeedProfile SltSpeedProfile
        {
            get { return _sltSpeedProfile; }
            set { _sltSpeedProfile = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 兼容旧页面绑定
        /// </summary>
        [JsonIgnore]
        public PLCAxisItem SltAxis
        {
            get { return SltAxisItem; }
            set { SltAxisItem = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<AddressMappingItem> _allAddr = new ObservableCollection<AddressMappingItem>();
        /// <summary>
        /// 所有地址
        /// </summary>
        public ObservableCollection<AddressMappingItem> AllAddr
        {
            get { return _allAddr; }
            set { _allAddr = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private AddressMappingItem _sltAddrItem;
        /// <summary>
        /// 当前选中的地址项
        /// </summary>
        public AddressMappingItem SltAddrItem
        {
            get { return _sltAddrItem; }
            set { _sltAddrItem = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isCurSltCardIsNotNull;
        /// <summary>
        /// 兼容页面启用状态绑定
        /// </summary>
        [JsonIgnore]
        public bool IsCurSltCardIsNotNull
        {
            get { return _isCurSltCardIsNotNull; }
            set { _isCurSltCardIsNotNull = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private RecipeManager _recipeManager = new RecipeManager();

        public RecipeManager RecipeManager
        {
            get { return _recipeManager; }
            set { _recipeManager = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCOrder _powerOff = new PLCOrder();
        /// <summary>
        /// 整机断电配置
        /// </summary>
        public PLCOrder PowerOff
        {
            get { return _powerOff; }
            set { _powerOff = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCOrder _delayPowerOff = new PLCOrder();
        /// <summary>
        /// 延时断电配置
        /// </summary>
        public PLCOrder DelayPowerOff
        {
            get { return _delayPowerOff; }
            set { _delayPowerOff = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public PLCSetModel()
        {
            //订阅
            PrismProvider.EventAggregator.GetEvent<ModuleRalatedEvent>().Subscribe((obj) =>
            {
                //触发关机
                if (obj.Item1 == "PLC" &&  obj.Item2 == "关机")
                {
                    var Param = new PLCParaInfoModel
                    {
                        PLCAddress = PowerOff.Addr,
                        ParaType = PowerOff.ParamType,
                        ParaValue = PowerOff.Value
                    };
                    Logs.LogInfo($"PLCAddress:{Param.PLCAddress},ParaType:{Param.ParaType},ParaValue:{Param.ParaValue}");

                    //先临时这样写一下
                    CurSlt = Models[0];
                    if (CurSlt.WritePLCPara(Param))
                    {
                        // 立即关机
                        Process.Start("shutdown", "/s /t 0");
                    }
                }
            }, ThreadOption.UIThread);

            NormalizeSelections();
        }
        #endregion

        #region Commands

        #endregion

        #region Methods
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            NormalizeSelections();
        }

        public void NormalizeSelections()
        {
            Models ??= new ObservableCollection<PLCBase>();
            AllAddr ??= new ObservableCollection<AddressMappingItem>();

            if (Models.Count == 0)
            {
                IsCurSltCardIsNotNull = false;
                CurSlt = null;
                return;
            }

            if (CurSlt == null || !Models.Contains(CurSlt))
            {
                CurSlt = MatchCurrentPlc() ?? Models[0];
            }

            IsCurSltCardIsNotNull = true;

            CurSlt.AxisGroups ??= new ObservableCollection<PLCAxisGroup>();
            if (CurSlt.AxisGroups.Count == 0)
            {
                CurSlt.AxisGroups.Add(new PLCAxisGroup { GroupName = "默认轴组" });
            }

            if (SltAxisGroup == null || !CurSlt.AxisGroups.Contains(SltAxisGroup))
            {
                SltAxisGroup = MatchAxisGroup(CurSlt) ?? CurSlt.AxisGroups[0];
            }

            SltAxisGroup.AxisItems ??= new ObservableCollection<PLCAxisItem>();
            if (SltAxisItem != null && !SltAxisGroup.AxisItems.Contains(SltAxisItem))
            {
                SltAxisItem = MatchAxisItem(SltAxisGroup);
            }

            if (SltAxisItem == null && SltAxisGroup.AxisItems.Count > 0)
            {
                SltAxisItem = SltAxisGroup.AxisItems[0];
            }
        }

        private PLCBase MatchCurrentPlc()
        {
            if (CurSlt == null)
            {
                return null;
            }

            return Models.FirstOrDefault(model =>
                model != null &&
                model.Config != null &&
                CurSlt.Config != null &&
                string.Equals(model.Config.DisplayName, CurSlt.Config.DisplayName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(model.Config.Ip, CurSlt.Config.Ip, StringComparison.OrdinalIgnoreCase) &&
                model.Config.Port == CurSlt.Config.Port &&
                model.Config.PlcType == CurSlt.Config.PlcType);
        }

        private PLCAxisGroup MatchAxisGroup(PLCBase device)
        {
            if (device?.AxisGroups == null || SltAxisGroup == null)
            {
                return null;
            }

            return device.AxisGroups.FirstOrDefault(group =>
                group != null &&
                string.Equals(group.GroupName, SltAxisGroup.GroupName, StringComparison.OrdinalIgnoreCase));
        }

        private PLCAxisItem MatchAxisItem(PLCAxisGroup axisGroup)
        {
            if (axisGroup?.AxisItems == null || SltAxisItem == null)
            {
                return null;
            }

            return axisGroup.AxisItems.FirstOrDefault(axis =>
                axis != null &&
                axis.AxisType == SltAxisItem.AxisType &&
                axis.AxisNo == SltAxisItem.AxisNo &&
                string.Equals(axis.AxisName, SltAxisItem.AxisName, StringComparison.OrdinalIgnoreCase));
        }

        public InitResult Init()
        {
            NormalizeSelections();
            Dictionary<string, bool> Status = new Dictionary<string, bool>();

            foreach (var model in Models)
            {
                model?.Init();
            }

            InitResult result = new InitResult();

            if (Status.Values.Any(value => value == false))
            {
                result = new InitResult
                {
                    Message = "连接失败！",
                    Success = false,
                };
            }
            else
            {
                result = new InitResult
                {
                    Message = "连接成功！",
                    Success = true,
                };
            }
            return result;
        }

        public void Shutdown()
        {
            foreach (var model in Models)
            {
                model.Close();
            }
        }

        /// <summary>
        /// 软件启动时按PLC运动配置触发复位，并等待PLC复位完成信号。
        /// </summary>
        public async Task<InitResult> ExecuteStartupResetAsync(Action<string> updateMessage)
        {
            foreach (var model in Models.Where(item => item?.DeviceMotionConfig?.StartupResetEnabled == true))
            {
                string plcName = model.Config?.DisplayName ?? "PLC";
                updateMessage?.Invoke($"{plcName}启动复位中...");

                if (model.Config?.IsConnected != true)
                {
                    return new InitResult { Success = false, Message = $"{plcName}未连接，启动复位失败。" };
                }

                if (string.IsNullOrWhiteSpace(model.DeviceMotionConfig.ResetWrite?.Address))
                {
                    return new InitResult { Success = false, Message = $"{plcName}未配置复位命令地址。" };
                }

                if (string.IsNullOrWhiteSpace(model.DeviceMotionConfig.ResetDoneRead?.Address))
                {
                    return new InitResult { Success = false, Message = $"{plcName}未配置复位完成读取地址。" };
                }

                bool resetWritten = await Task.Run(() => model.MotionService.Reset());
                if (!resetWritten)
                {
                    return new InitResult { Success = false, Message = $"{plcName}启动复位命令写入失败。" };
                }

                int timeoutMs = model.DeviceMotionConfig.StartupResetTimeoutMs <= 0 ? 30000 : model.DeviceMotionConfig.StartupResetTimeoutMs;
                DateTime timeoutTime = DateTime.Now.AddMilliseconds(timeoutMs);
                bool resetCompleted = false;
                while (DateTime.Now <= timeoutTime)
                {
                    var readResult = await Task.Run(() =>
                    {
                        bool success = model.TryReadPointValue(model.DeviceMotionConfig.ResetDoneRead, out var value);
                        return (success, value);
                    });

                    if (readResult.success && IsTrueValue(readResult.value))
                    {
                        updateMessage?.Invoke($"{plcName}启动复位完成。");
                        resetCompleted = true;
                        break;
                    }

                    await Task.Delay(200);
                }

                if (!resetCompleted)
                {
                    return new InitResult { Success = false, Message = $"{plcName}启动复位等待超时。" };
                }
            }

            return new InitResult { Success = true, Message = "PLC启动复位完成。" };
        }

        private static bool IsTrueValue(object value)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }

            string text = Convert.ToString(value)?.Trim() ?? string.Empty;
            return string.Equals(text, "True", StringComparison.OrdinalIgnoreCase) || text == "1";
        }
        public void RefreshStatus()
        {
            foreach (var model in Models)
            {
                model.State = model.State;
            }
        }
        #endregion

    }

    #region Param

    public enum PLCType
    {
        None,
        InovanceAMTcp,
        MelsecMcNet,
        MelsecMcUdp,
        MelsecMcAsciiUdp,
        OmronFinsNet,
        OmronCipNet,
        SiemensS7Net,
        KeyenceNanoSerialOverTcp,
        KeyenceMcNet,
        BeckhoffAdsNet,
        AllenBradleyNet,
        XinJETcpNetModbus,
        ModbusTcpNet,
        InovanceTcpNet,
        ModbusRtu,
        XinJEInternalNet,
        ModbusRtuOverTcp,
    }

    [Serializable]
    public class BasePlcConfigPara : BindableBase
    {
        public string DisplayName { get; set; } = "PLC1";
        public string Ip { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9600;
        
        private bool _isConnected = false;
        [JsonIgnore]
        public bool IsConnected 
        { 
            get { return _isConnected; }
            set { _isConnected = value; RaisePropertyChanged(); }
        }
        
        private PLCType _plcType = PLCType.OmronFinsNet;
        public PLCType PlcType
        {
            get { return _plcType; }
            set { _plcType = value; RaisePropertyChanged(); }
        }
        public SiemensPLCS SiemensType { get; set; } = SiemensPLCS.S1500;
        public string SenderNetId { get; set; } = "127.0.0.1.1.1:801";
        public string TargetNetId { get; set; } = "127.0.0.1.1.1:801";
        public string Slot { get; set; } = "1";
        public string Station { get; set; } = "1";
        public DataFormat DataFormat { get; set; } = DataFormat.CDAB;
        public XinJESeries XinJESeries { get; set; }
        public InovanceSeries InovanceSeries { get; set; }

        public string GetID()
        {
            return $"{DisplayName}:{Ip}:{Port}:{PlcType}";
        }

    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PlcConfigModel : BasePlcConfigPara
    {
        public string Brand { get; set; } = "Hsl通用库";
        public PlcConfigModel()
        {

        }

        public PlcConfigModel(PlcConfigModel plcConfigPara)
        {
            DisplayName = plcConfigPara.DisplayName;
            Ip = plcConfigPara.Ip;
            Port = plcConfigPara.Port;
            PlcType = plcConfigPara.PlcType;
            SiemensType = plcConfigPara.SiemensType;
            SenderNetId = plcConfigPara.SenderNetId;
            TargetNetId = plcConfigPara.TargetNetId;
            Slot = plcConfigPara.Slot;
            Station = plcConfigPara.Station;
            DataFormat = plcConfigPara.DataFormat;
            InovanceSeries = plcConfigPara.InovanceSeries;
            XinJESeries = plcConfigPara.XinJESeries;
        }

    }
    #endregion


    public enum EnumParaInfoModelParaType
    {
        Float,
        Bool,
        Short,
        Ushort,
        Int,
        Uint,
        Long,
        Ulong,
        Double,
        String,
        FloatArray,
        BoolArray,
        StringUTF8,
    }



    public enum EnumParaInfoModelActionType
    {
        Write_InitFromConfig,
        Read_InitFromConfig,
        Write_AfterScan,
        Timer_Write,
        Timer_Read,
        Read_ClearAfterRead,
        Read_ReadOnly,
        Others,
        NoAction,
        Custom,          //厂商自定义
    }

    public class PLCParaInfoModel : BindableBase
    {
        public string ParaValueString { get; set; }
        public object ParaValue { get; set; }
        public object OrinParaValue { get; set; }
        public EnumParaInfoModelParaType ParaType { get; set; }
        public ushort ParaLength { get; set; }
        public string PLCAddress { get; set; }
        public bool IsSuccess { get; set; }
        public string Key { get; set; }
        public string ParaName { get; set; }
        public string ParaDescription { get; set; }
        public string DefaultParaValue { get; set; }
        public string RangeType { get; set; }
        public string AccessLevel { get; set; }
        public string MapName { get; set; }
        public EnumParaInfoModelActionType ActionType { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }

        public bool NumberValueInRange()
        {
            double pv = double.Parse(ParaValueString);
            return pv <= Max && pv >= Min;
        }

        public double getRangeValue()
        {
            double pv = double.Parse(ParaValueString);

            if (pv > Max)
            {
                return Max;
            }
            else if (pv < Min)
            {
                return Min;
            }

            return pv;
        }

        public double getRangeValue(double pv)
        {
            if (pv > Max)
            {
                return Max;
            }
            else if (pv < Min)
            {
                return Min;
            }

            return pv;
        }

        public PLCParaInfoModel()
        {
        }

        public PLCParaInfoModel(string paraName, EnumParaInfoModelParaType paraType, string paraDescription)
        {
            this.ParaName = paraName;
            this.ParaType = paraType;
            this.ParaDescription = paraDescription;
        }

        public PLCParaInfoModel(string paraName, string paraValueString, EnumParaInfoModelParaType paraType,
            string paraDescription, string accessLevel)
        {
            this.ParaName = paraName;
            this.ParaType = paraType;
            this.ParaDescription = paraDescription;
            this.ParaValueString = paraValueString;
            this.AccessLevel = accessLevel;
        }

    }
}
