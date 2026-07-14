using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KBTBox.Models
{
    /// <summary>
    /// 算法输出的参数
    /// </summary>
    public class OutputResultParam : BindableBase
    {
        #region 单边每隔倍数（100um）的胶宽/高数据
        public double[] GlueThicknessList { get; set; }
        #endregion

        #region 整个框的胶宽/高：最大值/最小值/平均值/平面度/缺陷数量
        private double _glueWidthMin;
        /// <summary>
        /// 胶宽最小值
        /// </summary>
        public double GlueWidthMin
        {
            get { return _glueWidthMin; }
            set { _glueWidthMin = value; RaisePropertyChanged(); }
        }

        private double _glueWidthMax;
        /// <summary>
        /// 胶宽最大值
        /// </summary>
        public double GlueWidthMax
        {
            get { return _glueWidthMax; }
            set { _glueWidthMax = value; RaisePropertyChanged(); }
        }

        private double _glueWidthAvg;
        /// <summary>
        /// 胶宽平均值
        /// </summary>
        public double GlueWidthAvg
        {
            get { return _glueWidthAvg; }
            set { _glueWidthAvg = value; RaisePropertyChanged(); }
        }

        private double _glueThicknessMin;
        /// <summary>
        /// 胶高最小值
        /// </summary>
        public double GlueThicknessMin
        {
            get { return _glueThicknessMin; }
            set { _glueThicknessMin = value; RaisePropertyChanged(); }
        }

        private double _glueThicknessMax;
        /// <summary>
        /// 胶高最大值
        /// </summary>
        public double GlueThicknessMax
        {
            get { return _glueThicknessMax; }
            set { _glueThicknessMax = value; RaisePropertyChanged(); }
        }

        private double _glueThicknessAvg;
        /// <summary>
        /// 胶高平均值
        /// </summary>
        public double GlueThicknessAvg
        {
            get { return _glueThicknessAvg; }
            set { _glueThicknessAvg = value; RaisePropertyChanged(); }
        }

        private double _glueFlatness;
        /// <summary>
        /// 胶平面度
        /// </summary>
        public double GlueFlatness
        {
            get { return _glueFlatness; }
            set { _glueFlatness = value; RaisePropertyChanged(); }
        }

        private double _frameFlatness;
        /// <summary>
        /// 胶框平面度
        /// </summary>
        public double FrameFlatness
        {
            get { return _frameFlatness; }
            set { _frameFlatness = value; RaisePropertyChanged(); }
        }

        private int _defectNum;
        /// <summary>
        /// 缺陷数量
        /// </summary>
        public int DefectNum
        {
            get { return _defectNum; }
            set { _defectNum = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 半径/深度/面积

        #endregion
    }
}
