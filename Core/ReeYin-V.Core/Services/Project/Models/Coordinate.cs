using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Project
{
    /// <summary>
    /// 轴编号
    /// </summary>
    public enum En_AxisNum
    {
        X = 0,
        Y = 1,
        Z = 2,
        R = 3,

        X1 = 4,
        Y1 = 5,
        Z1 = 6,
        R1 = 7,

        X2 = 8,
        Y2 = 9,
        Z2 = 10,
        R2 = 11,
    }

    /// <summary>
    /// 坐标位置
    /// </summary>
    [Serializable]
    public class CoordinatePos : BindableBase
    {
        [JsonIgnore]
        private string _name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _describe;
        /// <summary>
        /// 描述
        /// </summary>
        public string Describe
        {
            get { return _describe; }
            set { _describe = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private List<double> _targetPos = new List<double>();
        /// <summary>
        /// 位置信息
        /// </summary>
        public List<double> TargetPos
        {
            get { return _targetPos; }
            set { _targetPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _movingMode;
        /// <summary>
        /// 移动方式
        /// </summary>
        public int MovingMode
        {
            get { return _movingMode; }
            set { _movingMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private List<double> _pLimitPos = new List<double>();
        /// <summary>
        /// 正限位
        /// </summary>
        public List<double> PLimitPos
        {
            get { return _pLimitPos; }
            set { _pLimitPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private List<double> _nLimitPos = new List<double>();
        /// <summary>
        /// 负限位
        /// </summary>
        public List<double> NLimitPos
        {
            get { return _nLimitPos; }
            set { _nLimitPos = value; RaisePropertyChanged(); }
        }
    }
}
