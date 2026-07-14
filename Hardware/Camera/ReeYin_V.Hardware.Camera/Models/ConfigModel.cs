using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.Camera
{
    /// <summary>
    /// 需要修改/展示的参数
    /// </summary>
    public class ConfigModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        private float _exposeTime;
        /// <summary>
        /// 曝光
        /// </summary>
        public float ExposeTime
        {
            get { return _exposeTime; }
            set { _exposeTime = value; RaisePropertyChanged(); }
        }

        private float _gain;
        /// <summary>
        /// 增益
        /// </summary>
        public float Gain
        {
            get { return _gain; }
            set { _gain = value; RaisePropertyChanged(); }
        }

        private int _framerate;
        /// <summary>
        /// 帧率
        /// </summary>
        public int Framerate
        {
            get { return _framerate; }
            set { _framerate = value; RaisePropertyChanged(); }
        }

        private float _lineRate;
        /// <summary>
        /// 行频（Hz）- 线扫相机
        /// </summary>
        public float LineRate
        {
            get { return _lineRate; }
            set { _lineRate = value; RaisePropertyChanged(); }
        }

        private float _encoderResolution = 0;
        /// <summary>
        /// 编码器分辨率（脉冲/mm）
        /// </summary>
        public float EncoderResolution
        {
            get { return _encoderResolution; }
            set { _encoderResolution = value; RaisePropertyChanged(); }
        }

        private float _pixelSize = 0;
        /// <summary>
        /// 像素尺寸（mm/像素）
        /// </summary>
        public float PixelSize
        {
            get { return _pixelSize; }
            set { _pixelSize = value; RaisePropertyChanged(); }
        }

        private int _lineScanFrameHeight = 6000;
        /// <summary>
        /// 线扫拼帧行数：累积多少行后拼合为一帧完整图像。
        /// 默认 6000，可在运行时实时修改。
        /// </summary>
        public int LineScanFrameHeight
        {
            get { return _lineScanFrameHeight; }
            set
            {
                if (value < 1) value = 1;
                _lineScanFrameHeight = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Constructor
        public ConfigModel()
        {

        }
        #endregion

        #region Methods

        #endregion
    }
}
