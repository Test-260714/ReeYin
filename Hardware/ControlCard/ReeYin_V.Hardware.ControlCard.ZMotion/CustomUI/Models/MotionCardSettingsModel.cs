using Prism.Mvvm;
using System;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.Models
{
    /// <summary>
    /// 运动控制卡设置参数模型 - 对应Modbus地址表
    /// </summary>
    [Serializable]
    public class MotionCardSettingsModel : BindableBase
    {
        #region 定位运动参数
        private float _positionSpeed = 100f;
        /// <summary>
        /// 定位定长运动速度 - 地址0
        /// </summary>
        public float PositionSpeed
        {
            get { return _positionSpeed; }
            set { _positionSpeed = value; RaisePropertyChanged(); }
        }

        private float _positionAccel = 10000f;
        /// <summary>
        /// 定位加速度 - 地址2（一般设置为定位速度的100倍）
        /// </summary>
        public float PositionAccel
        {
            get { return _positionAccel; }
            set { _positionAccel = value; RaisePropertyChanged(); }
        }

        private float _positionDecel = 10000f;
        /// <summary>
        /// 定位减速度 - 地址4（一般设置为定位速度的100倍）
        /// </summary>
        public float PositionDecel
        {
            get { return _positionDecel; }
            set { _positionDecel = value; RaisePropertyChanged(); }
        }

        private float _startSpeed = 10f;
        /// <summary>
        /// 起始速度 - 地址6（电机加速一般并非从0开始）
        /// </summary>
        public float StartSpeed
        {
            get { return _startSpeed; }
            set { _startSpeed = value; RaisePropertyChanged(); }
        }

        private float _positionTarget = 0f;
        /// <summary>
        /// 定位位置 - 地址8
        /// </summary>
        public float PositionTarget
        {
            get { return _positionTarget; }
            set { _positionTarget = value; RaisePropertyChanged(); }
        }

        private float _positionTrigger = 0f;
        /// <summary>
        /// 定位运动发脉冲位置 - 地址38
        /// </summary>
        public float PositionTrigger
        {
            get { return _positionTrigger; }
            set { _positionTrigger = value; RaisePropertyChanged(); }
        }

        private float _positionLength = 100f;
        /// <summary>
        /// 定长运动长度 - 地址42
        /// </summary>
        public float PositionLength
        {
            get { return _positionLength; }
            set { _positionLength = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 回原参数
        private float _homeFastSpeed = 50f;
        /// <summary>
        /// 回原快速速度 - 地址10
        /// </summary>
        public float HomeFastSpeed
        {
            get { return _homeFastSpeed; }
            set { _homeFastSpeed = value; RaisePropertyChanged(); }
        }

        private float _homeSlowSpeed = 10f;
        /// <summary>
        /// 回原反找慢速 - 地址12
        /// </summary>
        public float HomeSlowSpeed
        {
            get { return _homeSlowSpeed; }
            set { _homeSlowSpeed = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 往复运动参数
        private float _reciprocateSpeed = 250f;
        /// <summary>
        /// 往复运动速度 - 地址26
        /// </summary>
        public float ReciprocateSpeed
        {
            get { return _reciprocateSpeed; }
            set { _reciprocateSpeed = value; RaisePropertyChanged(); }
        }

        private float _reciprocateAccel = 500f;
        /// <summary>
        /// 往复运动加速度 - 地址22
        /// </summary>
        public float ReciprocateAccel
        {
            get { return _reciprocateAccel; }
            set { _reciprocateAccel = value; RaisePropertyChanged(); }
        }

        private float _reciprocateDecel = 500f;
        /// <summary>
        /// 往复运动减速度 - 地址24
        /// </summary>
        public float ReciprocateDecel
        {
            get { return _reciprocateDecel; }
            set { _reciprocateDecel = value; RaisePropertyChanged(); }
        }

        private float _reciprocateNegPos = 0f;
        /// <summary>
        /// 往复运动负位置（靠近原点）- 地址28
        /// </summary>
        public float ReciprocateNegPos
        {
            get { return _reciprocateNegPos; }
            set { _reciprocateNegPos = value; RaisePropertyChanged(); }
        }

        private float _reciprocatePosPos = 550f;
        /// <summary>
        /// 往复运动正位置（远离原点）- 地址30
        /// </summary>
        public float ReciprocatePosPos
        {
            get { return _reciprocatePosPos; }
            set { _reciprocatePosPos = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 触发位置参数
        private float _forwardTrigger1 = 30f;
        /// <summary>
        /// 往复运动前进时发脉冲位置1 (Tri3) - 地址18
        /// </summary>
        public float ForwardTrigger1
        {
            get { return _forwardTrigger1; }
            set { _forwardTrigger1 = value; RaisePropertyChanged(); }
        }

        private float _forwardTrigger2 = 40f;
        /// <summary>
        /// 往复运动前进时发脉冲位置2 (Ti2) - 地址14
        /// </summary>
        public float ForwardTrigger2
        {
            get { return _forwardTrigger2; }
            set { _forwardTrigger2 = value; RaisePropertyChanged(); }
        }

        private float _returnTrigger = 35f;
        /// <summary>
        /// 往复运动返回时发脉冲位置 (Tri3) - 地址40
        /// </summary>
        public float ReturnTrigger
        {
            get { return _returnTrigger; }
            set { _returnTrigger = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 循环与延时参数
        private int _reciprocateCycles = 0;
        /// <summary>
        /// 预设的往复循环次数 - 地址34
        /// </summary>
        public int ReciprocateCycles
        {
            get { return _reciprocateCycles; }
            set { _reciprocateCycles = value; RaisePropertyChanged(); }
        }

        private int _currentCycles = 0;
        /// <summary>
        /// 实时循环次数（只读）- 地址35
        /// </summary>
        public int CurrentCycles
        {
            get { return _currentCycles; }
            set { _currentCycles = value; RaisePropertyChanged(); }
        }

        private int _reciprocateDelay = 0;
        /// <summary>
        /// 往复运动到位后延时（ms）- 地址44
        /// </summary>
        public int ReciprocateDelay
        {
            get { return _reciprocateDelay; }
            set { _reciprocateDelay = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 状态参数（只读）
        private float _currentPosition = 0f;
        /// <summary>
        /// 当前位置（只读）- 地址32
        /// </summary>
        public float CurrentPosition
        {
            get { return _currentPosition; }
            set { _currentPosition = value; RaisePropertyChanged(); }
        }

        private int _workState = 0;
        /// <summary>
        /// 轴工作状态（只读）- 地址36
        /// </summary>
        public int WorkState
        {
            get { return _workState; }
            set { _workState = value; RaisePropertyChanged(); }
        }

        public string WorkStateText
        {
            get
            {
                switch (_workState)
                {
                    case 0: return "停机中";
                    case 1: return "往复运动";
                    case 2: return "定位";
                    case 3: return "回原";
                    case 4: return "暂停";
                    default: return "未知";
                }
            }
        }

        /// <summary>
        /// 是否正在运动
        /// </summary>
        public bool IsRunning => _workState > 0 && _workState < 4;
        #endregion
    }
}
