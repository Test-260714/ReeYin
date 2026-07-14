using Newtonsoft.Json;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    /// <summary>
    /// 运轴动控制参数
    /// </summary>
    [Serializable]
    public class AxisMoveParameter : BindableBase
    {
        #region Fields
        public string MouseOrder { get; set; }
        #endregion

        #region Properties
        public En_AxisNum AxisType { get; set; }
        public MoveDirection Direction { get; set; }

        private double _curPosInfos;
        /// <summary>
        /// 当前轴位置
        /// </summary>
        public double CurPosInfos
        {
            get { return _curPosInfos; }
            set { _curPosInfos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _axisInfo = "NULL";
        /// <summary>
        /// 轴卡信息
        /// </summary>
        [JsonIgnore]
        public string AxisInfo
        {
            get { return _axisInfo; }
            set { _axisInfo = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public AxisMoveParameter()
        {
            
        }
        #endregion

        #region Methods

        #endregion

    }
}
