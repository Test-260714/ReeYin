using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Custom.LineScan.Models
{
    /// <summary>
    /// 运动控制节点模型
    /// </summary>
    [Serializable]
    public class MotionControlModel : BindableBase, IModuleParam
    {
        #region IModuleParam
        public Guid Guid { get; set; } = Guid.NewGuid();
        
        private int _serial = -999;
        public int Serial
        {
            get { return _serial; }
            set { _serial = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public ModuleParam moduleInputParam { get; set; } = new ModuleParam();

        public ModuleParam moduleOutputParam { get; set; } = new ModuleParam();

        [JsonIgnore]
        public Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        public List<(int, NodeStatus)> InputNodeStatus { get; set; } = new List<(int, NodeStatus)>();

        [JsonIgnore]
        private ExecuteModuleOutput _output;
        [JsonIgnore]
        public ExecuteModuleOutput Output
        {
            get { return _output; }
            set { _output = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Properties
        private int _axisNumber = 0;
        /// <summary>
        /// 轴号
        /// </summary>
        public int AxisNumber
        {
            get { return _axisNumber; }
            set { _axisNumber = value; RaisePropertyChanged(); }
        }

        private float _targetPosition = 0f;
        /// <summary>
        /// 目标位置
        /// </summary>
        public float TargetPosition
        {
            get { return _targetPosition; }
            set { _targetPosition = value; RaisePropertyChanged(); }
        }

        private float _moveSpeed = 100f;
        /// <summary>
        /// 运动速度
        /// </summary>
        public float MoveSpeed
        {
            get { return _moveSpeed; }
            set { _moveSpeed = value; RaisePropertyChanged(); }
        }

        private bool _isContinuousMove = false;
        /// <summary>
        /// 是否连续运动
        /// </summary>
        public bool IsContinuousMove
        {
            get { return _isContinuousMove; }
            set { _isContinuousMove = value; RaisePropertyChanged(); }
        }

        private bool _waitForComplete = true;
        /// <summary>
        /// 是否等待运动完成
        /// </summary>
        public bool WaitForComplete
        {
            get { return _waitForComplete; }
            set { _waitForComplete = value; RaisePropertyChanged(); }
        }

        private float _currentPosition = 0f;
        /// <summary>
        /// 当前位置（输出参数）
        /// </summary>
        [OutputParam(description: "当前位置")]
        public float CurrentPosition
        {
            get { return _currentPosition; }
            set { _currentPosition = value; RaisePropertyChanged(); }
        }

        private bool _isMoving = false;
        /// <summary>
        /// 是否正在运动（输出参数）
        /// </summary>
        [OutputParam(description: "运动状态")]
        public bool IsMoving
        {
            get { return _isMoving; }
            set { _isMoving = value; RaisePropertyChanged(); }
        }

        private bool _moveCompleted = false;
        /// <summary>
        /// 运动完成标志（输出参数）
        /// </summary>
        [OutputParam(description: "运动完成")]
        public bool MoveCompleted
        {
            get { return _moveCompleted; }
            set { _moveCompleted = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _inputParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 输入参数
        /// </summary>
        public ObservableCollection<TransmitParam> InputParams
        {
            get { return _inputParams; }
            set { _inputParams = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _outputParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 输出参数
        /// </summary>
        public ObservableCollection<TransmitParam> OutputParams
        {
            get { return _outputParams; }
            set { _outputParams = value; RaisePropertyChanged(); }
        }

        private Dictionary<string, TransmitParam> _outputParamResource = new Dictionary<string, TransmitParam>();
        /// <summary>
        /// 输出参数资源
        /// </summary>
        public Dictionary<string, TransmitParam> OutputParamResource
        {
            get { return _outputParamResource; }
            set { _outputParamResource = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Methods
        /// <summary>
        /// 执行运动控制
        /// </summary>
        public Task<ExecuteModuleOutput> ExecuteModule()
        {
            return Task.Run(() =>
            {
                try
                {
                    // 这里会在实际执行时调用控制卡API
                    Console.WriteLine($"执行运动控制 - 轴号:{AxisNumber}, 目标位置:{TargetPosition}, 速度:{MoveSpeed}");
                    
                    // 更新输出参数
                    MoveCompleted = false;
                    IsMoving = true;

                    return new ExecuteModuleOutput
                    {
                        RunStatus = NodeStatus.Success
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"运动控制执行失败：{ex.Message}");
                    return new ExecuteModuleOutput
                    {
                        RunStatus = NodeStatus.Error
                    };
                }
            });
        }

        public void TransferParam()
        {
            // 参数传递逻辑
        }

        public void Dispose()
        {
        }
        #endregion
    }
}
