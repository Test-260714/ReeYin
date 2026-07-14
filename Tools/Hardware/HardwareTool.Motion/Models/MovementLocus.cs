using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardwareTool.Motion.Models
{
    public enum CardOperaion
    {
        点,
        IO,
        直线线段,
        圆弧线段,
        位置比较,
        延时,
        触发事件,
        自定义
    }


    /// <summary>
    /// 运动轨迹
    /// </summary>
    [Serializable]
    public class MovementLocus : BindableBase
    {
        [JsonIgnore]
        private bool isUsing;
        [Browsable(false)]
        public bool IsUsing
        {
            get { return isUsing; }
            set { isUsing = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private CardOperaion _movingMode = CardOperaion.点;
        /// <summary>
        /// 移动方式
        /// 点：目标点，不论当前所在位置，直接插补过去
        /// 直线线段：有起点和终点（从起点运动至终点）
        /// 圆弧线段：
        ///     方式1：有终点，圆心，旋转方向；
        ///     方式2：有终点，半径，旋转方向；
        /// </summary>
        [Browsable(false)]
        public CardOperaion MovingMode
        {
            get { return _movingMode; }
            set { _movingMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _sltTab = 0;

        [Browsable(false)]
        public int SltTab
        {
            get { return _sltTab; }
            set { _sltTab = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _OutputIO;
        /// <summary>
        /// 输出的IO
        /// </summary>
        public int OutputIO
        {
            get { return _OutputIO; }
            set { _OutputIO = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _outputIOStatus;
        /// <summary>
        /// 输出IO状态
        /// </summary>
        public bool OutputIOStatus
        {
            get { return _outputIOStatus; }
            set { _outputIOStatus = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _outputIODelay;
        /// <summary>
        /// 输出IO延时时间
        /// </summary>
        public int OutputIODelay
        {
            get { return _outputIODelay; }
            set { _outputIODelay = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private CoordinatePos _assignPosInfo = new CoordinatePos();
        /// <summary>
        /// 指定的目标位置
        /// </summary>
        public CoordinatePos AssignPosInfo
        {
            get { return _assignPosInfo; }
            set { _assignPosInfo = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _originX;
        /// <summary>
        /// 起点X
        /// </summary>
        [Category("位置"), DisplayName("起点X(mm)"), Description("仅直线运动时用到")]
        public double OriginX
        {
            get { return _originX; }
            set { _originX = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _originY;
        /// <summary>
        /// 起点Y
        /// </summary>
        [Category("位置"), DisplayName("起点Y(mm)"), Description("仅直线运动时用到")]
        public double OriginY
        {
            get { return _originY; }
            set { _originY = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _destinationX;
        /// <summary>
        /// 终点X
        /// </summary>
        [Category("位置"), DisplayName("终点X(mm)")]
        public double DestinationX
        {
            get { return _destinationX; }
            set { _destinationX = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _destinationY;
        /// <summary>
        /// 终点Y
        /// </summary>
        [Category("位置"), DisplayName("终点Y(mm)")]
        public double DestinationY
        {
            get { return _destinationY; }
            set { _destinationY = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _centerX;
        /// <summary>
        /// 圆心X
        /// </summary>
        [Category("位置"), DisplayName("圆心X(mm)"), Description("仅圆弧运动时用到")]
        public double CenterX
        {
            get { return _centerX; }
            set { _centerX = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _centerY;
        /// <summary>
        /// 圆心Y
        /// </summary>
        [Category("位置"), DisplayName("圆心Y(mm)"), Description("仅圆弧运动时用到")]
        public double CenterY
        {
            get { return _centerY; }
            set { _centerY = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _rotation;
        /// <summary>
        /// 旋转方向
        /// </summary>
        [Category("位置"), DisplayName("旋转方向(true:顺时针)"), Description("仅圆弧运动时用到")]
        public bool Rotation
        {
            get { return _rotation; }
            set { _rotation = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isSimulateFullCircle;
        /// <summary>
        /// 是否按整圆执行
        /// </summary>
        [Browsable(false)]
        public bool IsSimulateFullCircle
        {
            get { return _isSimulateFullCircle; }
            set { _isSimulateFullCircle = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _radius;
        /// <summary>
        /// 半径
        /// </summary>
        [Category("位置"), DisplayName("半径(mm)"), Description("仅圆弧运动时用到")]
        public double Radius
        {
            get { return _radius; }
            set { _radius = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _sltDrawingMethod = 1;
        /// <summary>
        /// 0:表示以圆心绘制
        /// 1:表示以半径绘制
        /// </summary>
        public int SltDrawingMethod
        {
            get { return _sltDrawingMethod; }
            set { _sltDrawingMethod = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _switch;
        /// <summary>
        /// 开关
        /// </summary>
        public bool Switch
        {
            get { return _switch; }
            set { _switch = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isUsingValid;
        /// <summary>
        /// 启用指定线段开启位置比较
        /// </summary>
        public bool IsUsingValid
        {
            get { return _isUsingValid; }
            set { _isUsingValid = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PosComparisonOutputParam _posComparisonParam = new PosComparisonOutputParam();
        /// <summary>
        /// 位置比较参数
        /// </summary>
        public PosComparisonOutputParam PosComparisonParam
        {
            get { return _posComparisonParam; }
            set { _posComparisonParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _eventName;
        /// <summary>
        /// 触发外部事件名称
        /// </summary>
        public string EventName
        {
            get { return _eventName; }
            set { _eventName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _describe;
        /// <summary>
        /// 描述
        /// </summary>
        [Browsable(false)]
        public string Describe
        {
            get { return _describe; }
            set { _describe = value; RaisePropertyChanged(); }
        }
    }
}
