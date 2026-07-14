using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KBTBox.Models
{
    /// <summary>
    /// 限值项（包含启用开关、最小值、最大值）
    /// </summary>
    [Serializable]
    public class LimitItem : BindableBase
    {
        [JsonIgnore]
        private bool _enabled;
        /// <summary>
        /// 是否启用该项判定
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _min;
        /// <summary>
        /// 最小值
        /// </summary>
        public double Min
        {
            get { return _min; }
            set { _min = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _max = 99999;
        /// <summary>
        /// 最大值
        /// </summary>
        public double Max
        {
            get { return _max; }
            set { _max = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 判定值是否在限值范围内（未启用时始终返回true）
        /// </summary>
        public bool Check(double value)
        {
            if (!Enabled) return true;
            if (double.IsNegativeInfinity(value) || double.IsNaN(value)) return false;
            return value >= Min && value <= Max;
        }
    }

    /// <summary>
    /// 判定参数配置（存储各参数的限值范围，应用于每条边的SideResult）
    /// </summary>
    [Serializable]
    public class JudgmentConfigModel : BindableBase
    {
        #region 胶面参数

        [JsonIgnore]
        private LimitItem _glueFlatness = new();
        /// <summary>
        /// 胶平面度限值
        /// </summary>
        public LimitItem GlueFlatness
        {
            get { return _glueFlatness; }
            set { _glueFlatness = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private LimitItem _glueWidth = new();
        /// <summary>
        /// 胶宽限值
        /// </summary>
        public LimitItem GlueWidth
        {
            get { return _glueWidth; }
            set { _glueWidth = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private LimitItem _glueThickness = new();
        /// <summary>
        /// 胶厚限值
        /// </summary>
        public LimitItem GlueThickness
        {
            get { return _glueThickness; }
            set { _glueThickness = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private LimitItem _gluePathTiltAngle = new();
        /// <summary>
        /// 胶路偏转角度限值（判定时取绝对值）
        /// </summary>
        public LimitItem GluePathTiltAngle
        {
            get { return _gluePathTiltAngle; }
            set { _gluePathTiltAngle = value; RaisePropertyChanged(); }
        }

        #endregion

        #region 缺陷参数

        [JsonIgnore]
        private LimitItem _defectArea = new();
        /// <summary>
        /// 缺陷面积限值
        /// </summary>
        public LimitItem DefectArea
        {
            get { return _defectArea; }
            set { _defectArea = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private LimitItem _defectDiameter = new();
        /// <summary>
        /// 缺陷直径限值
        /// </summary>
        public LimitItem DefectDiameter
        {
            get { return _defectDiameter; }
            set { _defectDiameter = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private LimitItem _defectDepth = new();
        /// <summary>
        /// 缺陷深度限值
        /// </summary>
        public LimitItem DefectDepth
        {
            get { return _defectDepth; }
            set { _defectDepth = value; RaisePropertyChanged(); }
        }

        #endregion
    }
}
