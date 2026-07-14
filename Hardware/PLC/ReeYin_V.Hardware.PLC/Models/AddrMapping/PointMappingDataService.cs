using Newtonsoft.Json;
using OpenCvSharp;
using Prism.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.PLC.Models
{
    [Serializable]
    public class PointMappingDataService : BindableBase
    {
        #region Fields
        //public static Dictionary<EnumMotionType, string> DictMotionTypesToDisplay { get; set; } = new()
        //{
        //    {EnumMotionType.LinearMotion, Resources.AddressMappingEditItem_LinearMotion },
        //    {EnumMotionType.ResetMotion,Resources.AddressMappingEditItem_ResetMotion },
        //    {EnumMotionType.None,Resources.AddressMappingEditItem_None},
        //};
        [JsonIgnore]
        private PointMappingModel _pointMappingModel;

        public PointMappingModel PointMappingModel
        {
            get { return _pointMappingModel; }
            set { _pointMappingModel = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties

        #endregion

        #region Constructor
        public PointMappingDataService()
        {
            //_pointMappingModel = ConfigCenter.CommonConfig.GetPara(CommonKeyName.PointMappingDataService_PointMappingModel, new PointMappingModel());

        }
        #endregion

        #region Constructor
        public List<AddressMappingItem> GetAddressMappingItems()
        {
            //DictMotionTypesToDisplay = new()
            //{
            //    {EnumMotionType.LinearMotion, _localizeService.Localize("AddressMappingEditItem_LinearMotion") },
            //    {EnumMotionType.ResetMotion,_localizeService.Localize("AddressMappingEditItem_ResetMotion") },
            //    {EnumMotionType.None,_localizeService.Localize("AddressMappingEditItem_None")},
            //};

            //_pointMappingModel.AddressMappingItems.ForEach(item =>
            //{
            //    item.DisplayMotionType = DictMotionTypesToDisplay[item.MotionType];
            //});
            return _pointMappingModel.AddressMappingItems;
        }


        public List<Axis> GetAllAxis()
        {
            return _pointMappingModel.Axises;
        }


        public Axis GetSelectedAxis(EnumAxisType key)
        {
            if (_pointMappingModel.Axises.Any(x => x.AxisType == key))
                return _pointMappingModel.Axises.First(x => x.AxisType == key);
            return null;
        }

        //public void Save(List<Axis> axises, List<AddressMappingItem> addressMappingItems, string clientID)
        //{
        //    _pointMappingModel.Axises = axises;
        //    _pointMappingModel.AddressMappingItems = addressMappingItems;
        //    _clientID = clientID;

        //    ConfigCenter.CommonConfig.SetPara(CommonKeyName.PointMappingDataService_PointMappingModel, _pointMappingModel);
        //    ConfigCenter.FrameConfig.SetPara(CommonKeyName.PointMappingDataService_ClientID, _clientID);
        //}

        //public List<AddressMappingItem> GetSystemAddressMappingItems()
        //{
        //    return new List<AddressMappingItem>()
        //    {
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_DoorSwitch,EnumAxisType.Undefined,EnumMotionType.None,"M1000",1,EnumParaInfoModelParaType.Bool,"门开关"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_RaySwitch,EnumAxisType.Undefined,EnumMotionType.None,"M1002",1,EnumParaInfoModelParaType.Bool,"射线开关"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_XJogA,EnumAxisType.Undefined,EnumMotionType.None,"M5071",1,EnumParaInfoModelParaType.Bool,"X-Jog+"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_XJogD,EnumAxisType.Undefined,EnumMotionType.None,"M5072",1,EnumParaInfoModelParaType.Bool,"X-Jog-"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_YJogA,EnumAxisType.Undefined,EnumMotionType.None,"M5171",1,EnumParaInfoModelParaType.Bool,"Y-Jog+"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_YJogD,EnumAxisType.Undefined,EnumMotionType.None,"M5172",1,EnumParaInfoModelParaType.Bool,"Y-Jog-"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_ZTJogA,EnumAxisType.Undefined,EnumMotionType.None,"M5371",1,EnumParaInfoModelParaType.Bool,"ZT-Jog+"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_ZTJogD,EnumAxisType.Undefined,EnumMotionType.None,"M5372",1,EnumParaInfoModelParaType.Bool,"ZT-Jog-"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_ZBJogA,EnumAxisType.Undefined,EnumMotionType.None,"M5271",1,EnumParaInfoModelParaType.Bool,"ZB-Jog+"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_ZBJogD,EnumAxisType.Undefined,EnumMotionType.None,"M5272",1,EnumParaInfoModelParaType.Bool,"ZB-Jog-"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_Reset,EnumAxisType.Undefined,EnumMotionType.None,"M9004",1,EnumParaInfoModelParaType.Bool,"轴复位"),
        //        new AddressMappingItem(CommonKeyName.PointMappingDataService_Go,EnumAxisType.Undefined,EnumMotionType.None,"M1004",1,EnumParaInfoModelParaType.Bool,"轴Go"),
        //    };
        //}
        #endregion


    }


    public class PointMappingModel
    {
        /// <summary>
        /// 二级tabpage+功能映射点
        /// </summary>
        public List<Axis> Axises { get; set; } = new();

        /// <summary>
        /// 地址映射表
        /// </summary>
        public List<AddressMappingItem> AddressMappingItems { get; set; } = new();


        public PointMappingModel()
        {

        }
    }

    //public partial class AddressMappingEditItem : BindableBase
    //{
    //    #region Properties
    //    [JsonIgnore]
    //    private string _key;
    //    /// <summary>
    //    /// 定制化指Key
    //    /// </summary>
    //    public string Key
    //    {
    //        get { return _key; }
    //        set { _key = value; RaisePropertyChanged(); }
    //    }

    //    [JsonIgnore]
    //    private EnumAxisType _motionAxis;
    //    /// <summary>
    //    /// 轴
    //    /// </summary>
    //    public EnumAxisType MotionAxis
    //    {
    //        get { return _motionAxis; }
    //        set { _motionAxis = value; RaisePropertyChanged(); }
    //    }

    //    [JsonIgnore]
    //    private EnumMotionType _motionType;
    //    /// <summary>
    //    /// 运动类型
    //    /// </summary>
    //    public EnumMotionType MotionType
    //    {
    //        get { return _motionType; }
    //        set { _motionType = value; RaisePropertyChanged(); }
    //    }

    //    [JsonIgnore]
    //    private string _address;
    //    /// <summary>
    //    /// 地址
    //    /// </summary>
    //    public string Address
    //    {
    //        get { return _address; }
    //        set { _address = value; RaisePropertyChanged(); }
    //    }

    //    [JsonIgnore]
    //    private object _value;
    //    /// <summary>
    //    /// 值
    //    /// </summary>
    //    public object Value
    //    {
    //        get { return _value; }
    //        set { _value = value; RaisePropertyChanged(); }
    //    }

    //    [JsonIgnore]
    //    private string _description;
    //    /// <summary>
    //    /// 描述
    //    /// </summary>
    //    public string Description
    //    {
    //        get { return _description; }
    //        set { _description = value; RaisePropertyChanged(); }
    //    }

    //    [JsonIgnore]
    //    private EnumParaInfoModelParaType _dataType;
    //    /// <summary>
    //    /// 数据类型
    //    /// </summary>
    //    public EnumParaInfoModelParaType DataType
    //    {
    //        get { return _dataType; }
    //        set { _dataType = value; RaisePropertyChanged(); }
    //    }

    //    [JsonIgnore]
    //    private string _displaySelectedKey;

    //    public string DisplaySelectedKey
    //    {
    //        get { return _displaySelectedKey; }
    //        set { _displaySelectedKey = value; RaisePropertyChanged(); }
    //    }

    //    [JsonIgnore]
    //    private string _displayMotionType;

    //    public string DisplayMotionType
    //    {
    //        get { return _displayMotionType; }
    //        set
    //        {
    //            SetProperty(ref _displayMotionType, value);
    //            GetMotionType(_displayMotionType);
    //            RaisePropertyChanged();
    //        }
    //    }

    //    [JsonIgnore]
    //    private bool _notCustomization;

    //    public bool NotCustomization
    //    {
    //        get { return _notCustomization; }
    //        set { _notCustomization = value; RaisePropertyChanged(); }
    //    }
    //    #endregion

    //    public Dictionary<EnumMotionType, string> DictMotionTypes { get; set; } = PointMappingDataService.DictMotionTypesToDisplay;

    //    public List<EnumAxisType> DictPageTypes { get; set; } = AddressMappingItem.ListEnumMotionAxis;

    //    public List<EnumParaInfoModelParaType> DictDataTypes { get; set; } = BasePlcConfigPara.ComboxParaTypes;

        

    //    private void GetMotionType(string displayMotionType)
    //    {
    //        foreach (var item in PointMappingDataService.DictMotionTypesToDisplay)
    //        {
    //            if (displayMotionType == item.Value)
    //                MotionType = item.Key;
    //        }
    //    }

    //    public void GetDisplayMotionType(EnumMotionType enumMotionType)
    //    {
    //        foreach (var item in PointMappingDataService.DictMotionTypesToDisplay)
    //        {
    //            if (enumMotionType == item.Key)
    //                DisplayMotionType = item.Value;
    //        }
    //    }



    //}
}
