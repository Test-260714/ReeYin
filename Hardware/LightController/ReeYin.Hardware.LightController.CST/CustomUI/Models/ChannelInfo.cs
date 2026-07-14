using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.LightController.CST.CustomUI.Models
{
    /// <summary>
    /// 通道信息
    /// </summary>
    public class ChannelInfo : BindableBase
    {
        private int _channelIndex;
        /// <summary>
        /// 通道索引
        /// </summary>
        public int ChannelIndex
        {
            get { return _channelIndex; }
            set { _channelIndex = value; RaisePropertyChanged(); }
        }

        private int _brightness;
        /// <summary>
        /// 亮度值(0-255)
        /// </summary>
        public int Brightness
        {
            get { return _brightness; }
            set { _brightness = value; RaisePropertyChanged(); }
        }

        private bool _isOn;
        /// <summary>
        /// 开关状态
        /// </summary>
        public bool IsOn
        {
            get { return _isOn; }
            set { _isOn = value; RaisePropertyChanged(); }
        }

        private int _strobeTime;
        /// <summary>
        /// 频闪时间(us)
        /// </summary>
        public int StrobeTime
        {
            get { return _strobeTime; }
            set { _strobeTime = value; RaisePropertyChanged(); }
        }

        private int _lightDelay;
        /// <summary>
        /// 光源延时(us)
        /// </summary>
        public int LightDelay
        {
            get { return _lightDelay; }
            set { _lightDelay = value; RaisePropertyChanged(); }
        }

        private int _cameraDelay;
        /// <summary>
        /// 相机延时(us)
        /// </summary>
        public int CameraDelay
        {
            get { return _cameraDelay; }
            set { _cameraDelay = value; RaisePropertyChanged(); }
        }
    }
}
