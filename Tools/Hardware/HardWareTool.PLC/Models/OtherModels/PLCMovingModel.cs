using Newtonsoft.Json;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.PLC.Interface;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardWareTool.PLC.Models
{
    [Serializable]
    public class PLCMovingModel : BindableBase
    {
        #region Fields
        [JsonIgnore]
        public PLCBase CurPLC { get; set; }
        #endregion

        #region Properties
        private AxisMotionPLCParasModel _axis;
        /// <summary>
        /// 轴操作
        /// </summary>
        public AxisMotionPLCParasModel Axis
        {
            get { return _axis; }
            set { _axis = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public PLCMovingModel()
        {
            this.CurPLC = (PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] as PLCSetModel).Models[0];
        }
        #endregion

        #region Methods
        /// <summary>
        /// 初始化运动参数
        /// </summary>
        /// <param name="enumMotionAxis"></param>
        /// <returns></returns>
        private bool PrepareForMotion(EnumAxisType enumMotionAxis)
        {
            //var axis = _pointMappingDataService.GetSelectedAxis(enumMotionAxis);

            //if (axis == null) return false;

            //_model = axis.GetPara<AxisMotionPLCParasModel>(Axis.AxisPLCAddress, new());

            return true;
        }

        /// <summary>
        /// 直线运动
        /// </summary>
        /// <param name="enumMotionAxis"></param>
        /// <param name="position"></param>
        /// <param name="isRelative"></param>
        /// <param name="speed"></param>
        /// <param name="adSpeed"></param>
        public void LinearMotion(EnumAxisType enumMotionAxis, double position, bool? isRelative = false, double? speed = null, double? adSpeed = null)
        {
            if (!PrepareForMotion(enumMotionAxis))
                return;

            //var axis = _pointMappingDataService.GetSelectedAxis(enumMotionAxis);

            //step1:速度写入
            var speedPim = Axis.MotionSpeed;
            speedPim.Value = speed;
            if (!CurPLC.WritePLCPara(speedPim))
                return;

            //step2:位置写入
            var targetPositionPim = Axis.Position;
            targetPositionPim.Value = position;
            if (!CurPLC.WritePLCPara(targetPositionPim))
                return;

            //step3:触发运动
            var motionStartPim = Axis.MotionStart;
            motionStartPim.Value = 1;
            if (!CurPLC.WritePLCPara(motionStartPim))
                return;
        }

        /// <summary>
        /// 复位运动
        /// </summary>
        /// <param name="enumMotionAxis"></param>
        /// <param name="speed"></param>
        /// <param name="adSpeed"></param>
        public void ResetMotion(EnumAxisType enumMotionAxis, double? speed = null, double? adSpeed = null)
        {
            if (!PrepareForMotion(enumMotionAxis))
                return;

            //step1: 速度写入
            var speedPim = Axis.ResetSpeed;
            speedPim.Value = speed;
            if (!CurPLC.WritePLCPara(speedPim))
                return;

            //step2:触发复位
            var resetStartPim = Axis.ResetStart;
            resetStartPim.Value = 1;
            if (!CurPLC.WritePLCPara(resetStartPim))
                return;

        }

        /// <summary>
        /// 等待到位
        /// </summary>
        /// <param name="enumMotionAxis"></param>
        /// <param name="enumMotionType"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool WaitForReach(EnumAxisType enumMotionAxis, EnumMotionType enumMotionType, double position)
        {
            bool result = false;

            if (!PrepareForMotion(enumMotionAxis))
                return result;

            // 到位读取
            // 用于取消任务
            var cts = new CancellationTokenSource();
            int failedCount = 0;
            AddressMappingItem addressItem = null;

            switch (enumMotionType)
            {
                case EnumMotionType.LinearMotion:
                    addressItem = Axis.MotionRealPosition;
                    break;
                case EnumMotionType.ResetMotion:
                    addressItem = Axis.ResetRealPosition;
                    break;
                case EnumMotionType.None:
                    break;
                default:
                    break;
            }

            Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (CurPLC.ReadPLCPara(addressItem) && Math.Abs(Convert.ToDouble(addressItem.Value) - position) <= 0.02)
                    {
                        result = true;
                        cts.Cancel();
                        break;
                    }

                    //60s 还未到，认为超时失败
                    if (failedCount == 600)
                    {
                        cts.Cancel();
                        break;
                    }

                    failedCount++;

                    await Task.Delay(100); // 降低 CPU 使用率
                }

            }).Wait();

            return result;
        }

        /// <summary>
        /// 获取当前轴位置
        /// </summary>
        /// <param name="enumAxisType"></param>
        /// <returns></returns>
        public double GetCurrentPosition(EnumAxisType enumAxisType)
        {
            lock (this)
            {
                if (!PrepareForMotion(enumAxisType))
                    return 0.0;

                var positionPim = Axis.MotionRealPosition;

                return CurPLC.ReadPLCPara(positionPim) ? Math.Round(Convert.ToDouble(positionPim.Value), 6) : 0.0;

            }
        }

        /// <summary>
        /// 写入目标位置
        /// </summary>
        /// <param name="enumAxisType"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool WritePosition(EnumAxisType enumAxisType, double position)
        {
            if (!PrepareForMotion(enumAxisType))
                return false;

            var positionPim = Axis.Position;
            positionPim.Value = position;
            return CurPLC.WritePLCPara(positionPim);
        }
        #endregion
    }


    /// <summary>
    /// PLC控制轴运动参数
    /// </summary>
    public class AxisMotionPLCParasModel
    {
        /// <summary>
        /// 轴类型
        /// </summary>
        public EnumAxisType AxisType { get; set; }

        #region 直线运动
        /// <summary>
        /// 运动开始
        /// </summary>
        public AddressMappingItem MotionStart { get; set; }

        /// <summary>
        /// 实时位置
        /// </summary>
        public AddressMappingItem MotionRealPosition { get; set; }

        /// <summary>
        /// 运动速度
        /// </summary>
        public AddressMappingItem MotionSpeed { get; set; }

        /// <summary>
        /// 运动加减速
        /// </summary>
        public AddressMappingItem MotionADSpeed { get; set; }

        /// <summary>
        /// 目标位置
        /// </summary>
        public AddressMappingItem Position { get; set; }

        /// <summary>
        /// 相对绝对
        /// </summary>
        public AddressMappingItem RelativeAbsolute { get; set; }
        #endregion

        #region 复位
        /// <summary>
        /// 复位开始
        /// </summary>
        public AddressMappingItem ResetStart { get; set; }

        /// <summary>
        /// 实时位置
        /// </summary>
        public AddressMappingItem ResetRealPosition { get; set; }

        /// <summary>
        /// 复位速度
        /// </summary>
        public AddressMappingItem ResetSpeed { get; set; }

        /// <summary>
        /// 复位加减速
        /// </summary>
        public AddressMappingItem ResetADSpeed { get; set; }
        #endregion

    }
}
