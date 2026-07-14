using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.LightController.Models
{
    /// <summary>
    /// 光源控制器接口
    /// </summary>
    public interface ILightController
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 控制器名称
        /// </summary>
        string NickName { get; set; }

        /// <summary>
        /// 连接状态
        /// </summary>
        bool IsConnected { get; set; }

        /// <summary>
        /// 通道数量
        /// </summary>
        int ChannelCount { get; set; }

        /// <summary>
        /// 初始化/连接
        /// </summary>
        /// <returns></returns>
        bool Init();

        /// <summary>
        /// 关闭控制器
        /// </summary>
        void Close();

        /// <summary>
        /// 设置单通道亮度
        /// </summary>
        /// <param name="channelIndex">通道索引(从1开始)</param>
        /// <param name="value">亮度值(0-255)</param>
        /// <returns></returns>
        bool SetBrightness(int channelIndex, int value);

        /// <summary>
        /// 获取单通道亮度
        /// </summary>
        /// <param name="channelIndex">通道索引(从1开始)</param>
        /// <returns>亮度值</returns>
        int GetBrightness(int channelIndex);

        /// <summary>
        /// 批量设置亮度
        /// </summary>
        /// <param name="channelValues">通道-亮度值字典</param>
        /// <returns></returns>
        bool SetMultiBrightness(Dictionary<int, int> channelValues);

        /// <summary>
        /// 设置通道开关
        /// </summary>
        /// <param name="channelIndex">通道索引</param>
        /// <param name="isOn">是否打开</param>
        /// <returns></returns>
        bool SetChannelOnOff(int channelIndex, bool isOn);

        /// <summary>
        /// 获取通道开关状态
        /// </summary>
        /// <param name="channelIndex">通道索引</param>
        /// <returns></returns>
        bool GetChannelOnOff(int channelIndex);

        /// <summary>
        /// 设置频闪时间
        /// </summary>
        /// <param name="channelIndex">通道索引</param>
        /// <param name="strobeTime">频闪时间(us)</param>
        /// <returns></returns>
        bool SetStrobeTime(int channelIndex, int strobeTime);

        /// <summary>
        /// 获取频闪时间
        /// </summary>
        /// <param name="channelIndex">通道索引</param>
        /// <returns>频闪时间(us)</returns>
        int GetStrobeTime(int channelIndex);

        /// <summary>
        /// 设置触发模式
        /// </summary>
        /// <param name="mode">触发模式(0:常亮 1:外触发)</param>
        /// <returns></returns>
        bool SetTriggerMode(int mode);

        /// <summary>
        /// 获取触发模式
        /// </summary>
        /// <returns>触发模式</returns>
        int GetTriggerMode();
    }
}
