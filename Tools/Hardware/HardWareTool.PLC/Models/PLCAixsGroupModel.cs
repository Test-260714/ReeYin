using Newtonsoft.Json;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardWareTool.PLC.Models
{
    [Serializable]
    public class PLCAixsGroupModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private string _addrX;
        /// <summary>
        /// X地址
        /// </summary>
        public string AddrX
        {
            get { return _addrX; }
            set { _addrX = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _addrY;
        /// <summary>
        /// Y地址
        /// </summary>
        public string AddrY
        {
            get { return _addrY; }
            set { _addrY = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _addrZ;
        /// <summary>
        /// Z地址
        /// </summary>
        public string AddrZ
        {
            get { return _addrZ; }
            set { _addrZ = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _addrReach;
        /// <summary>
        /// 到达地址
        /// </summary>
        public string AddrReach
        {
            get { return _addrReach; }
            set { _addrReach = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private EnumParaInfoModelParaType _paramType;
        /// <summary>
        /// 发给PLC的参数类型
        /// </summary>
        public EnumParaInfoModelParaType ParamType
        {
            get { return _paramType; }
            set { _paramType = value; RaisePropertyChanged(); }
        }
        #endregion

    }
}
