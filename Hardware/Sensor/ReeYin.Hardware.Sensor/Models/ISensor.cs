using ReeYin_V.Core;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.Models
{
    public interface ISensor
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 设备状态
        /// </summary>
        HardwareState State { get; set; }

        /// <summary>
        /// 传感器名称
        /// </summary>
        string NickName { get; set; }

        /// <summary>
        /// 传感器连接状态
        /// </summary>
        bool IsConnected { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        bool Init();

        /// <summary>
        /// 关闭传感器
        /// </summary>
        void Close();

        /// <summary>
        /// 开始收集
        /// </summary>
        void StartCollect();

        /// <summary>
        /// 停止收集
        /// </summary>
        void StopCollect();

        /// <summary>
        /// 收集数据
        /// </summary>
        /// <returns></returns>
        List<MeasureData> ReceiveSensorData();



        ///定义一个接受传感器数据的事件
    }
}
