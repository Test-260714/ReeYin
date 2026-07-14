using ReeYin_V.Core.Config;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.PLC.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.PLC.ViewModels
{
    public class PLCAxisGroupMoveViewModel : BindableBase
    {
        #region Properties
        private readonly SemaphoreSlim _jogCommandSemaphore = new SemaphoreSlim(1, 1);
        private readonly HashSet<PLCAxisItem> _activeJogAxisItems = new HashSet<PLCAxisItem>();
        private DelegateCommand<PLCAxisItem> _startJogPositiveCommand;
        private DelegateCommand<PLCAxisItem> _startJogNegativeCommand;
        private DelegateCommand<PLCAxisItem> _stopAxisCommand;
        private DelegateCommand<PLCAxisItem> _forceStopAxisCommand;
        private DelegateCommand<PLCAxisItem> _runAxisCommand;
        private DelegateCommand<PLCAxisItem> _homeAxisCommand;
        private DelegateCommand<PLCAxisItem> _enableShieldCommand;
        private DelegateCommand<PLCAxisItem> _disableShieldCommand;
        private DelegateCommand<PLCAxisItem> _setAxisPositionCommand;
        private DelegateCommand<PLCAxisItem> _syncAxisTargetCommand;

        private readonly PLCAxisGroupMoveModel _model = new PLCAxisGroupMoveModel();
        /// <summary>
        /// 点动控制模型
        /// </summary>
        public PLCAxisGroupMoveModel Model
        {
            get { return _model; }
        }

        #endregion

        #region Constructor

        public PLCAxisGroupMoveViewModel()
        {
            var plcSetModel = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel;
            if (plcSetModel != null)
            {
                plcSetModel.PropertyChanged += OnPlcSetModelPropertyChanged;
                SyncReferences(plcSetModel);
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 全部停止
        /// </summary>
        public DelegateCommand StopAllCommand => new DelegateCommand(() =>
        {
            _model.StopAll();
            _activeJogAxisItems.Clear();
        });

        public DelegateCommand<PLCAxisItem> StartJogPositiveCommand => _startJogPositiveCommand ??= new DelegateCommand<PLCAxisItem>(async axis =>
        {
            if (axis == null)
            {
                return;
            }

            await _jogCommandSemaphore.WaitAsync();
            try
            {
                bool result = await Task.Run(() => _model.StartJog(axis, true));
                if (result)
                {
                    _activeJogAxisItems.Add(axis);
                }
            }
            finally
            {
                _jogCommandSemaphore.Release();
            }
        });

        public DelegateCommand<PLCAxisItem> StartJogNegativeCommand => _startJogNegativeCommand ??= new DelegateCommand<PLCAxisItem>(async axis =>
        {
            if (axis == null)
            {
                return;
            }

            await _jogCommandSemaphore.WaitAsync();
            try
            {
                bool result = await Task.Run(() => _model.StartJog(axis, false));
                if (result)
                {
                    _activeJogAxisItems.Add(axis);
                }
            }
            finally
            {
                _jogCommandSemaphore.Release();
            }
        });

        public DelegateCommand<PLCAxisItem> StopAxisCommand => _stopAxisCommand ??= new DelegateCommand<PLCAxisItem>(async axis =>
        {
            if (axis == null || !_activeJogAxisItems.Contains(axis))
            {
                return;
            }

            await _jogCommandSemaphore.WaitAsync();
            try
            {
                await Task.Run(() => _model.StopJog(axis));
                _activeJogAxisItems.Remove(axis);
            }
            finally
            {
                _jogCommandSemaphore.Release();
            }
        });

        /// <summary>
        /// 主动停止当前轴，不依赖点动按下状态
        /// </summary>
        public DelegateCommand<PLCAxisItem> ForceStopAxisCommand => _forceStopAxisCommand ??= new DelegateCommand<PLCAxisItem>(async axis =>
        {
            if (axis == null)
            {
                return;
            }

            await _jogCommandSemaphore.WaitAsync();
            try
            {
                await Task.Run(() => _model.StopAxis(axis));
                _activeJogAxisItems.Remove(axis);
            }
            finally
            {
                _jogCommandSemaphore.Release();
            }
        });

        public DelegateCommand<PLCAxisItem> RunAxisCommand => _runAxisCommand ??= new DelegateCommand<PLCAxisItem>(axis =>
        {
            _model.RunAxis(axis);
        });

        public DelegateCommand<PLCAxisItem> HomeAxisCommand => _homeAxisCommand ??= new DelegateCommand<PLCAxisItem>(axis =>
        {
            _model.HomeAxis(axis);
        });

        public DelegateCommand<PLCAxisItem> EnableShieldCommand => _enableShieldCommand ??= new DelegateCommand<PLCAxisItem>(axis =>
        {
            _model.EnableShield(axis);
        });

        public DelegateCommand<PLCAxisItem> DisableShieldCommand => _disableShieldCommand ??= new DelegateCommand<PLCAxisItem>(axis =>
        {
            _model.DisableShield(axis);
        });

        public DelegateCommand<PLCAxisItem> SetAxisPositionCommand => _setAxisPositionCommand ??= new DelegateCommand<PLCAxisItem>(axis =>
        {
            _model.SetAxisPosition(axis);
        });

        public DelegateCommand<PLCAxisItem> SyncAxisTargetCommand => _syncAxisTargetCommand ??= new DelegateCommand<PLCAxisItem>(axis =>
        {
            _model.SyncTargetFromRuntime(axis);
        });

        /// <summary>
        /// 全部启用轴跑位
        /// </summary>
        public DelegateCommand MoveAllCommand => new DelegateCommand(() =>
        {
            _model.MoveAll();
        });

        /// <summary>
        /// 复位
        /// </summary>
        public DelegateCommand ResetCommand => new DelegateCommand(() =>
        {
            _model.Reset();
        });

        /// <summary>
        /// 清报警
        /// </summary>
        public DelegateCommand ClearAlarmCommand => new DelegateCommand(() =>
        {
            _model.ClearAlarm();
        });

        #endregion

        #region Methods

        private void OnPlcSetModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PLCSetModel.CurSlt) ||
                e.PropertyName == nameof(PLCSetModel.SltAxisGroup))
            {
                if (sender is PLCSetModel model)
                {
                    SyncReferences(model);
                }
            }
        }

        private void SyncReferences(PLCSetModel model)
        {
            model.NormalizeSelections();
            _model.PlcDevice = model.CurSlt;
            _model.AxisGroup = model.SltAxisGroup;
        }

        #endregion
    }
}
